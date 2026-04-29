using System.Security.Cryptography;
using AI.Application.Common.Constants;
using AI.Application.Common.Helpers;
using AI.Application.Common.Telemetry;
using AI.Application.DTOs;
using AI.Application.DTOs.DocumentProcessing;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Document;
using AI.Application.Ports.Secondary.Services.Vector;
using AI.Domain.Documents;
using AI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases;

/// <summary>
/// Doküman işleme servisi implementasyonu - Sadece Qdrant tabanlı
/// </summary>
public class DocumentProcessingUseCase : IDocumentProcessingUseCase
{
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly ITextChunker _textChunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly ISparseVectorService _sparseVectorService;
    private readonly IJsonQuestionAnswerParser _jsonQuestionAnswerParser;
    private readonly ILogger<DocumentProcessingUseCase> _logger;

    private const string UploadsFolder = "uploads";

    public DocumentProcessingUseCase(
        IEnumerable<IDocumentParser> parsers,
        ITextChunker textChunker,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        ISparseVectorService sparseVectorService,
        IJsonQuestionAnswerParser jsonQuestionAnswerParser,
        ILogger<DocumentProcessingUseCase> logger)
    {
        _parsers = parsers;
        _textChunker = textChunker;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _sparseVectorService = sparseVectorService;
        _jsonQuestionAnswerParser = jsonQuestionAnswerParser;
        _logger = logger;
    }

    public async Task<DocumentUploadResultDto> ProcessDocumentFromUploadAsync(
        DocumentUploadDto uploadDto,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var documentMetadata = DocumentMetadata.Create(
            fileName: uploadDto.FileName,
            fileType: uploadDto.FileType,
            fileSize: uploadDto.FileSize,
            fileHash: uploadDto.FileHash,
            filePath: string.Empty,
            documentType: uploadDto.DocumentType,
            title: uploadDto.Title ?? Path.GetFileNameWithoutExtension(uploadDto.FileName),
            description: uploadDto.Description ?? string.Empty,
            category: uploadDto.Category,
            userId: uploadDto.UserId,
            uploadedBy: uploadDto.UploadedBy
        );

        var result = await ProcessDocumentAsync(documentMetadata, fileStream, cancellationToken);

        return new DocumentUploadResultDto
        {
            Id = documentMetadata.Id,
            FileName = documentMetadata.FileName,
            FileType = documentMetadata.FileType,
            FileSize = documentMetadata.FileSize,
            Category = documentMetadata.Category ?? string.Empty,
            Status = result.Success ? "Processing" : "Failed",
            UploadedAt = documentMetadata.UploadedAt,
            ProcessedChunks = result.ProcessedChunks,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }

    private async Task<DocumentProcessingResult> ProcessDocumentAsync(
        DocumentMetadata documentMetadata,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        using var activity = ActivitySources.DocumentProcessing.StartActivity("ProcessDocument");
        if (activity != null)
        {
            activity.SetTag("document.id", documentMetadata.Id);
            activity.SetTag("document.filename", documentMetadata.FileName);
            activity.SetTag("document.filesize", documentMetadata.FileSize);
            activity.SetTag("document.filetype", documentMetadata.FileType);
            activity.SetTag("document.language", documentMetadata.Language);

            BaggageHelper.SetDocumentBaggage(documentMetadata.Id.ToString());
            BaggageHelper.AddBaggageToActivity(activity);
        }

        try
        {
            // DocumentMetadata'daki tüm string alanlarında Türkçe karakter encoding sorunlarını düzelt
            FixDocumentMetadataEncoding(documentMetadata);

            // Dosyayı uploads klasörüne kaydet
            var savedFilePath = await SaveFileToUploadsAsync(fileStream, documentMetadata, cancellationToken);
            documentMetadata.SetFilePath(savedFilePath);
            _logger.LogInformation("File saved to: {FilePath}", savedFilePath);

            // Collection adını belirle (dosya adına göre)
            var collectionName = QdrantCollections.GetCollectionName(documentMetadata.FileName);
            documentMetadata.SetQdrantCollection(collectionName);

            _logger.LogInformation("Starting document processing for: {FileName}, Collection: {Collection}, DocumentType: {DocumentType}",
                documentMetadata.FileName, collectionName, documentMetadata.DocumentType);

            // Dosya hash'ini kontrol et (Qdrant'ta duplicate check)
            var existingPoints = await _qdrantService.SearchAsync(
                collectionName,
                new float[_embeddingService.EmbeddingDimension], // Dummy vector for filter-only search
                1,
                0.0f,
                new Dictionary<string, object> { ["fileHash"] = documentMetadata.FileHash },
                cancellationToken);

            if (existingPoints.Any())
            {
                _logger.LogWarning("Document with hash {FileHash} already exists", documentMetadata.FileHash);
                return new DocumentProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Bu dosya daha önce yüklenmiş",
                    Status = DocumentProcessingStatus.Failed
                };
            }

            // Doküman metadata'sını Qdrant'ta sakla (processing durumunda)
            documentMetadata.MarkAsProcessing();

            // Dosya türüne göre chunks al (JSON veya metin-tabanlı)
            List<DocumentChunk> chunks = [];

            // DocumentType'a göre parser seç
            if (documentMetadata.DocumentType == Domain.Enums.DocumentType.QuestionAnswer)
            {
                chunks = _jsonQuestionAnswerParser.ParseQuestionsAnswers(fileStream, documentMetadata.Id, documentMetadata.FileName);

                if (!chunks.Any())
                {
                    await UpdateDocumentStatus(documentMetadata.Id, DocumentProcessingStatus.Failed,
                        "JSON dosyasından soru-cevaplar çıkarılamadı", cancellationToken);

                    return new DocumentProcessingResult
                    {
                        Success = false,
                        ErrorMessage = "JSON dosyasından soru-cevaplar çıkarılamadı",
                        Status = DocumentProcessingStatus.Failed
                    };
                }

                _logger.LogInformation("Extracted {ChunkCount} Q&A pairs from JSON file: {FileName}",
                    chunks.Count, documentMetadata.FileName);
            }
            else
            {
                // PDF, TXT vb. dosyalar için genel parser kullan
                var parser = GetParserForFile(documentMetadata.FileType);
                if (parser == null)
                {
                    await UpdateDocumentStatus(documentMetadata.Id, DocumentProcessingStatus.Failed,
                        "Desteklenmeyen dosya türü", cancellationToken);

                    return new DocumentProcessingResult
                    {
                        Success = false,
                        ErrorMessage = "Desteklenmeyen dosya türü",
                        Status = DocumentProcessingStatus.Failed
                    };
                }

                // Metni çıkar
                var extractedText = await parser.ExtractTextAsync(fileStream, documentMetadata.FileName, cancellationToken);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    await UpdateDocumentStatus(documentMetadata.Id, DocumentProcessingStatus.Failed,
                        "Dosyadan metin çıkarılamadı", cancellationToken);

                    return new DocumentProcessingResult
                    {
                        Success = false,
                        ErrorMessage = "Dosyadan metin çıkarılamadı",
                        Status = DocumentProcessingStatus.Failed
                    };
                }

                // Metni chunk'lara böl
                chunks = _textChunker.ChunkTextSemantic(extractedText, documentMetadata.Id);
                if (!chunks.Any())
                {
                    await UpdateDocumentStatus(documentMetadata.Id, DocumentProcessingStatus.Failed,
                        "Metin parçalanamadı", cancellationToken);

                    return new DocumentProcessingResult
                    {
                        Success = false,
                        ErrorMessage = "Metin parçalanamadı",
                        Status = DocumentProcessingStatus.Failed
                    };
                }
            }

            // Chunk'lardaki Türkçe karakter encoding sorunlarını düzelt ve sparse vector extract et
            foreach (var chunk in chunks)
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    chunk.UpdateContent(TurkishEncodingHelper.FixEncoding(chunk.Content));

                    // Native sparse vector generation (Qdrant hybrid search için)
                    var sparseVector = _sparseVectorService.GenerateSparseVectorResult(chunk.Content);
                    chunk.SetSparseVector(sparseVector.Indices, sparseVector.Values);
                    _logger.LogDebug("Generated sparse vector for chunk {ChunkId}: {NonZeroCount} non-zero terms",
                        chunk.Id, sparseVector.NonZeroCount);
                }
            }

            // Embedding'leri oluştur ve Qdrant'a kaydet (chunk'lar da burada kaydedilecek)
            var processedChunks = await ProcessChunksAsync(chunks, documentMetadata, collectionName, cancellationToken);

            // Doküman metadata'sını güncelle
            documentMetadata.MarkAsCompleted(processedChunks);

            var processingTime = DateTime.UtcNow - startTime;
            var totalTextLength = chunks.Sum(c => c.ContentLength);

            _logger.LogInformation("Document processing completed for: {FileName}. Processed {ChunkCount} chunks in {ProcessingTime}ms",
                documentMetadata.FileName, processedChunks, processingTime.TotalMilliseconds);

            return new DocumentProcessingResult
            {
                Success = true,
                ProcessedChunks = processedChunks,
                ProcessingTime = processingTime,
                ExtractedTextLength = totalTextLength,
                Status = DocumentProcessingStatus.Completed
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Document processing was cancelled for: {FileName}", documentMetadata.FileName);
            await UpdateDocumentStatus(documentMetadata.Id, DocumentProcessingStatus.Failed,
                "İşlem iptal edildi", cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document: {FileName}", documentMetadata.FileName);
            await UpdateDocumentStatus(documentMetadata.Id, DocumentProcessingStatus.Failed,
                ex.Message, cancellationToken);

            var processingTime = DateTime.UtcNow - startTime;
            return new DocumentProcessingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTime = processingTime,
                Status = DocumentProcessingStatus.Failed
            };
        }
    }


    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Qdrant'tan dokümanın tüm chunk'larını al
            var filter = new Dictionary<string, object>
            {
                ["documentId"] = documentId
            };
            var searchResult = await _qdrantService.SearchAsync("documents", new float[_embeddingService.EmbeddingDimension], 1000, 0.0f, filter, cancellationToken);
            var documentChunks = searchResult
                .Where(r => r.DocumentId == documentId)
                .ToList();

            // Doküman bulunamazsa false döndür
            if (documentChunks.Count == 0)
            {
                _logger.LogWarning("Document {DocumentId} not found in Qdrant", documentId);
                return false;
            }

            // Qdrant'tan tüm chunk'ları sil
            foreach (var chunk in documentChunks)
            {
                await _qdrantService.DeleteVectorAsync("documents", chunk.ChunkId, cancellationToken);
            }

            _logger.LogInformation("Document {DocumentId} and its {ChunkCount} chunks deleted successfully from Qdrant",
                documentId, documentChunks.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
            return false;
        }
    }

    public IEnumerable<string> GetSupportedFileTypes()
    {
        return _parsers.SelectMany(p => p.SupportedFileTypes).Distinct();
    }

    public bool IsFileTypeSupported(string fileExtension)
    {
        return _parsers.Any(p => p.CanParse(fileExtension));
    }

    public async Task<bool> IsExistAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Qdrant'ta doküman ID'sine göre arama yap
            var filter = new Dictionary<string, object> { ["document_id"] = documentId.ToString() };
            var points = await _qdrantService.SearchAsync(
                "documents",
                new float[_embeddingService.EmbeddingDimension], // Dummy vector for filter-only search
                1,
                0.0f,
                filter,
                cancellationToken);

            var exists = points.Any();
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document exists: {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> IsExistByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        try
        {
            // Qdrant'ta dosya hash'ine göre arama yap
            var filter = new Dictionary<string, object> { ["fileHash"] = fileHash };
            var points = await _qdrantService.SearchAsync(
                "documents",
                new float[_embeddingService.EmbeddingDimension], // Dummy vector for filter-only search
                1,
                0.0f,
                filter,
                cancellationToken);

            var exists = points.Any();
            _logger.LogDebug("Document with hash {FileHash} exists check: {Exists}", fileHash, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document exists by hash: {FileHash}", fileHash);
            return false;
        }
    }

    private IDocumentParser? GetParserForFile(string fileExtension)
    {
        return _parsers.FirstOrDefault(p => p.CanParse(fileExtension));
    }

    /// <summary>
    /// Belirtilen collection'ın var olduğundan emin olur
    /// </summary>
    private async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _qdrantService.GetCollectionsAsync(cancellationToken);
            if (!collections.Contains(collectionName))
            {
                await _qdrantService.CreateCollectionAsync(collectionName, _embeddingService.EmbeddingDimension, cancellationToken);
                _logger.LogInformation("Created collection '{CollectionName}' in Qdrant", collectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring collection '{CollectionName}' exists", collectionName);
            throw;
        }
    }

    private async Task<int> ProcessChunksAsync(
        List<DocumentChunk> chunks,
        DocumentMetadata documentMetadata,
        string collectionName,
        CancellationToken cancellationToken)
    {
        // Koleksiyonun var olduğundan emin ol
        await EnsureCollectionExistsAsync(collectionName, cancellationToken);

        var processedCount = 0;
        var batchSize = 10; // Batch'ler halinde işle

        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var batch = chunks.Skip(i).Take(batchSize).ToList();

            try
            {
                // Embedding'leri oluştur (ORİJİNAL content kullanarak - stemming YOK!)
                // NOT: Semantic embedding modeli doğal dil için optimize edilmiştir
                // Stemming sadece sparse vector (BM25) için kullanılır, dense embedding için DEĞİL
                var textsForEmbedding = batch.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(textsForEmbedding, cancellationToken);

                // Chunk'lara metadata bilgilerini ekle (orijinal content ile)
                for (int j = 0; j < batch.Count; j++)
                {
                    var chunk = batch[j];

                    // Chunk'a metadata bilgilerini ekle
                    var chunkMetadata = new Dictionary<string, object>
                    {
                        ["fileName"] = documentMetadata.FileName,
                        ["documentName"] = documentMetadata.FileName, // Arama için doküman adı
                        ["fileType"] = documentMetadata.FileType,
                        ["fileSize"] = documentMetadata.FileSize,
                        ["title"] = documentMetadata.Title ?? "",
                        ["category"] = documentMetadata.Category ?? "",
                        ["language"] = documentMetadata.Language ?? "tr",
                        ["uploadedAt"] = documentMetadata.UploadedAt.ToString("O"),
                        ["uploadedBy"] = documentMetadata.UploadedBy ?? "",
                        ["filePath"] = documentMetadata.FilePath ?? "",
                        ["fileHash"] = documentMetadata.FileHash ?? "",
                        ["status"] = documentMetadata.Status.ToString()
                    };

                    // Chunk'ın Metadata property'sine JSON string olarak ekle
                    // UTF-8 karakterlerin escape edilmemesi için özel JsonSerializerOptions kullan
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    chunk.SetMetadata(System.Text.Json.JsonSerializer.Serialize(chunkMetadata, jsonOptions));
                }

                // Qdrant'a batch upsert (orijinal content ile, embedding normalize edilmiş)
                await _qdrantService.UpsertVectorsAsync(collectionName, batch, embeddings, cancellationToken);

                // Chunk'lar artık sadece Qdrant'ta saklanıyor
                _logger.LogDebug("Batch {BatchSize} chunks uploaded to Qdrant for document {DocumentId} with normalized embeddings",
                    batch.Count, documentMetadata.Id);

                processedCount += batch.Count;

                _logger.LogDebug("Processed batch {BatchNumber}/{TotalBatches} for document {DocumentId}",
                    (i / batchSize) + 1, (chunks.Count + batchSize - 1) / batchSize, documentMetadata.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchNumber} for document {DocumentId}",
                    (i / batchSize) + 1, documentMetadata.Id);
                throw;
            }
        }

        return processedCount;
    }

    private async Task UpdateDocumentStatus(
        Guid documentId,
        DocumentProcessingStatus status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            // Qdrant'ta doküman status'unu güncelle
            // Bu işlem için dokümanın chunk'larını bulup payload'larını güncelle
            var filter = new Dictionary<string, object> { ["documentId"] = documentId.ToString() };
            var points = await _qdrantService.SearchAsync(
                "documents",
                new float[_embeddingService.EmbeddingDimension], // Dummy vector for filter-only search
                1000, // Dokümanın tüm chunk'larını al
                0.0f,
                filter,
                cancellationToken);

            if (points.Any())
            {
                // Tüm chunk'ların metadata'sını güncelle
                foreach (var point in points)
                {
                    if (point.Metadata != null)
                    {
                        point.Metadata["status"] = status.ToString();
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            point.Metadata["errorMessage"] = errorMessage;
                        }
                        if (status == DocumentProcessingStatus.Completed)
                        {
                            point.Metadata["processedAt"] = DateTime.UtcNow.ToString("O");
                        }
                    }
                }

                _logger.LogInformation("Updated status for document {DocumentId} to {Status}", documentId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document status for: {DocumentId}", documentId);
        }
    }

    /// <summary>
    /// Mevcut tüm index'leri (collection'ları) listeler
    /// </summary>
    public async Task<List<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting all indexes (collections) from Qdrant");
            var collections = await _qdrantService.GetCollectionsAsync(cancellationToken);
            _logger.LogDebug("Found {Count} indexes", collections.Count);
            return collections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indexes");
            return new List<string>();
        }
    }

    /// <summary>
    /// Belirli bir index'in var olup olmadığını kontrol eder
    /// </summary>
    public async Task<bool> IsIndexExistAsync(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(indexName))
            {
                _logger.LogWarning("Index name is null or empty");
                return false;
            }

            _logger.LogDebug("Checking if index '{IndexName}' exists", indexName);
            var exists = await _qdrantService.CollectionExistsAsync(indexName, cancellationToken);
            _logger.LogDebug("Index '{IndexName}' exists: {Exists}", indexName, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if index '{IndexName}' exists", indexName);
            return false;
        }
    }

    /// <summary>
    /// Belirli bir index'teki dokümanları arar
    /// </summary>
    public async Task<List<SearchResult>> SearchInIndexAsync(string indexName, string query, int limit = 10, float minScore = 0.7f, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(indexName))
            {
                _logger.LogWarning("Index name is null or empty");
                return new List<SearchResult>();
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Search query is null or empty");
                return new List<SearchResult>();
            }

            // Index'in var olup olmadığını kontrol et
            var indexExists = await _qdrantService.CollectionExistsAsync(indexName, cancellationToken);
            if (!indexExists)
            {
                _logger.LogWarning("Index '{IndexName}' does not exist", indexName);
                return new List<SearchResult>();
            }

            _logger.LogDebug("Searching in index '{IndexName}' with query: '{Query}', limit: {Limit}, minScore: {MinScore}",
                indexName, query, limit, minScore);

            // Query'yi embedding'e çevir
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            if (queryEmbedding == null || queryEmbedding.Length == 0)
            {
                _logger.LogWarning("Failed to generate embedding for query: '{Query}'", query);
                return new List<SearchResult>();
            }

            // Qdrant'ta arama yap
            var searchResults = await _qdrantService.SearchAsync(
                indexName,
                queryEmbedding,
                limit,
                minScore,
                null,
                cancellationToken);

            _logger.LogDebug("Found {Count} results in index '{IndexName}' for query: '{Query}'",
                searchResults.Count, indexName, query);

            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching in index '{IndexName}' with query: '{Query}'", indexName, query);
            return new List<SearchResult>();
        }
    }

    public static string CalculateFileHash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var originalPosition = stream.Position;
        stream.Position = 0;

        var hashBytes = sha256.ComputeHash(stream);
        stream.Position = originalPosition;

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// DocumentMetadata'daki tüm string alanlarında Türkçe karakter encoding sorunlarını düzeltir
    /// </summary>
    private static void FixDocumentMetadataEncoding(DocumentMetadata metadata)
    {
        if (metadata == null)
            return;

        // Tüm string alanlarında encoding fix uygula (merkezi helper kullanarak)
        metadata.FixEncodings(
            fixedFileName: !string.IsNullOrEmpty(metadata.FileName) ? TurkishEncodingHelper.FixEncoding(metadata.FileName) : null,
            fixedTitle: !string.IsNullOrEmpty(metadata.Title) ? TurkishEncodingHelper.FixEncoding(metadata.Title) : null,
            fixedDescription: !string.IsNullOrEmpty(metadata.Description) ? TurkishEncodingHelper.FixEncoding(metadata.Description) : null,
            fixedCategory: !string.IsNullOrEmpty(metadata.Category) ? TurkishEncodingHelper.FixEncoding(metadata.Category) : null,
            fixedUploadedBy: !string.IsNullOrEmpty(metadata.UploadedBy) ? TurkishEncodingHelper.FixEncoding(metadata.UploadedBy) : null,
            fixedErrorMessage: !string.IsNullOrEmpty(metadata.ErrorMessage) ? TurkishEncodingHelper.FixEncoding(metadata.ErrorMessage) : null
        );
    }

    /// <summary>
    /// File extension/type'a göre document type string'i döndürür
    /// </summary>
    private static string GetDocumentType(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            "application/pdf" => "PDF",
            "text/plain" => "Text",
            "text/html" => "HTML",
            "application/json" => "JSON",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "DOCX",
            "application/msword" => "DOC",
            _ => fileType
        };
    }

    /// <summary>
    /// Dosyayı uploads klasörüne kaydeder
    /// UserId varsa: uploads/{UserId}/{fileName}
    /// UserId yoksa: uploads/System/{fileName}
    /// </summary>
    private async Task<string> SaveFileToUploadsAsync(Stream fileStream, DocumentMetadata metadata, CancellationToken cancellationToken)
    {
        // Kullanıcı klasörünü belirle: UserId varsa UserId, yoksa "System"
        var userFolder = string.IsNullOrWhiteSpace(metadata.UserId) ? "System" : metadata.UserId;

        // AppDomain.CurrentDomain.BaseDirectory kullanarak uploads altına kaydet
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var uploadsPath = Path.Combine(basePath, UploadsFolder, userFolder);

        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
            _logger.LogInformation("Created uploads directory: {UploadsPath}", uploadsPath);
        }

        // Dosya adı zaten sanitize edilmiş olmalı (DocumentDisplayInfoService'den geliyor)
        // Yine de güvenlik için tekrar sanitize et
        var safeFileName = TurkishEncodingHelper.SanitizeFileName(metadata.FileName);

        // Eğer sanitize sonucu farklıysa güncelle
        if (safeFileName != metadata.FileName)
        {
            _logger.LogInformation("Sanitized filename in SaveFileToUploadsAsync: {OriginalFileName} -> {SafeFileName}",
                metadata.FileName, safeFileName);
            metadata.FixFileName(safeFileName);
        }

        var fullFilePath = Path.Combine(uploadsPath, safeFileName);

        // Stream pozisyonunu başa al
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        await using var outputStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await fileStream.CopyToAsync(outputStream, cancellationToken);

        // Stream pozisyonunu tekrar başa al (sonraki işlemler için)
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        // Qdrant'a kaydedilecek relative path: uploads/{userFolder}/{fileName}
        var relativePath = Path.Combine(UploadsFolder, userFolder, safeFileName).Replace("\\", "/");

        return relativePath;
    }
}