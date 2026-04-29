using System.Security.Cryptography;
using AI.Application.Common.Constants;
using AI.Application.Common.Helpers;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Documents;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Document;
using AI.Application.Ports.Secondary.Services.Vector;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases;

/// <summary>
/// Document Display Info Use Case implementation
/// Veritabanı + Qdrant + Redis cache işlemlerini yönetir
/// </summary>
public sealed class DocumentDisplayInfoUseCase : IDocumentDisplayInfoUseCase
{
    private readonly IDocumentCategoryRepository _repository;
    private readonly IDocumentProcessingUseCase _documentProcessingUseCase;
    private readonly IQdrantService _qdrantService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentCacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentDisplayInfoUseCase> _logger;

    public DocumentDisplayInfoUseCase(
        IDocumentCategoryRepository repository,
        IDocumentProcessingUseCase documentProcessingUseCase,
        IQdrantService qdrantService,
        IEmbeddingService embeddingService,
        IDocumentCacheService cacheService,
        ICurrentUserService currentUserService,
        ILogger<DocumentDisplayInfoUseCase> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _documentProcessingUseCase = documentProcessingUseCase ?? throw new ArgumentNullException(nameof(documentProcessingUseCase));
        _qdrantService = qdrantService ?? throw new ArgumentNullException(nameof(qdrantService));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentDisplayInfoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Tek kayıt için cache kullanmıyoruz, direkt DB'den
        var entity = await _repository.GetDocumentByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
        var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

        return MapToDto(entity, hasEmbeddings, chunkCount);
    }

    public async Task<DocumentDisplayInfoDto?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetDocumentByFileNameAsync(fileName, cancellationToken);
        if (entity == null) return null;

        var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
        var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

        return MapToDto(entity, hasEmbeddings, chunkCount);
    }

    public async Task<List<DocumentDisplayInfoListDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // Admin kontrolü yap
        var isAdmin = _currentUserService.IsAdmin;
        var userId = _currentUserService.UserId;

        if (!isAdmin && string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GetAllAsync called without authenticated user");
            return new List<DocumentDisplayInfoListDto>();
        }

        // Admin ise userId null olanları getir (sistem dökümanları)
        // Normal kullanıcı ise kendi dökümanlarını getir
        var queryUserId = isAdmin ? null : userId;
        var entities = await _repository.GetDocumentsByOwnerUserIdAsync(queryUserId!, includeInactive, cancellationToken);
        var result = new List<DocumentDisplayInfoListDto>();

        foreach (var entity in entities)
        {
            var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
            var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

            result.Add(new DocumentDisplayInfoListDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                DocumentType = entity.DocumentType,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.DisplayName,
                UserId = entity.UserId,
                IsActive = entity.IsActive,
                HasEmbeddings = hasEmbeddings,
                ChunkCount = chunkCount,
                CreatedAt = entity.CreatedAt
            });
        }

        _logger.LogDebug("Documents loaded - IsAdmin: {IsAdmin}, QueryUserId: {QueryUserId} ({Count} items)",
            isAdmin, queryUserId, result.Count);

        return result;
    }

    public async Task<List<DocumentDisplayInfoListDto>> GetAllByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // User bazlı sorgu için cache kullanmıyoruz - her kullanıcının farklı sonucu var
        var entities = await _repository.GetAllDocumentsByUserIdAsync(userId, includeInactive, cancellationToken);
        var result = new List<DocumentDisplayInfoListDto>();

        foreach (var entity in entities)
        {
            var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
            var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

            result.Add(new DocumentDisplayInfoListDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                DocumentType = entity.DocumentType,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.DisplayName,
                UserId = entity.UserId,
                IsActive = entity.IsActive,
                HasEmbeddings = hasEmbeddings,
                ChunkCount = chunkCount,
                CreatedAt = entity.CreatedAt
            });
        }

        return result;
    }

    public async Task<List<DocumentDisplayInfoListDto>> GetByCategoryAsync(string categoryId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // Sadece aktif dökümanlar için cache kullan
        if (!includeInactive)
        {
            var cached = await _cacheService.GetDocumentsByCategoryAsync(categoryId, cancellationToken);
            if (cached != null && cached.Count > 0)
            {
                _logger.LogDebug("Documents by category loaded from cache: {CategoryId} ({Count} items)", categoryId, cached.Count);
                return cached;
            }
        }

        var entities = await _repository.GetDocumentsByCategoryAsync(categoryId, includeInactive, cancellationToken);
        var result = new List<DocumentDisplayInfoListDto>();

        foreach (var entity in entities)
        {
            var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
            var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

            result.Add(new DocumentDisplayInfoListDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                DocumentType = entity.DocumentType,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.DisplayName,
                UserId = entity.UserId,
                IsActive = entity.IsActive,
                HasEmbeddings = hasEmbeddings,
                ChunkCount = chunkCount,
                CreatedAt = entity.CreatedAt
            });
        }

        // Sadece aktif dökümanları ve veri varsa cache'le
        if (!includeInactive && result.Count > 0)
        {
            await _cacheService.SetDocumentsByCategoryAsync(categoryId, result, cancellationToken);
            _logger.LogDebug("Documents by category cached from database: {CategoryId} ({Count} items)", categoryId, result.Count);
        }

        return result;
    }

    public async Task<List<DocumentDisplayInfoListDto>> GetByCategoryByUserIdAsync(string categoryId, string? userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // User bazlı sorgu için cache kullanmıyoruz
        var entities = await _repository.GetDocumentsByCategoryAndUserIdAsync(categoryId, userId, includeInactive, cancellationToken);
        var result = new List<DocumentDisplayInfoListDto>();

        foreach (var entity in entities)
        {
            var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
            var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

            result.Add(new DocumentDisplayInfoListDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                DocumentType = entity.DocumentType,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.DisplayName,
                UserId = entity.UserId,
                IsActive = entity.IsActive,
                HasEmbeddings = hasEmbeddings,
                ChunkCount = chunkCount,
                CreatedAt = entity.CreatedAt
            });
        }

        return result;
    }

    public async Task<List<DocumentDisplayInfoSelectDto>> GetAllForSelectAsync(CancellationToken cancellationToken = default)
    {
        // Kullanıcı bazlı filtreleme: Admin dökümanları (UserId = null) + kullanıcının kendi dökümanları
        var userId = _currentUserService.UserId;

        // User bazlı sorgu - her kullanıcının farklı sonucu olduğu için cache kullanmıyoruz
        var entities = await _repository.GetAllDocumentsByUserIdAsync(userId, includeInactive: false, cancellationToken);

        _logger.LogDebug("Documents for select loaded for user {UserId} ({Count} items)", userId ?? "anonymous", entities.Count);

        return entities.Select(e => new DocumentDisplayInfoSelectDto
        {
            Id = e.Id,
            Text = e.DisplayName,
            FileName = e.FileName,
            DocumentType = e.DocumentType
        }).ToList();
    }

    public async Task<List<DocumentDisplayInfoSelectDto>> GetAllForSelectByUserIdAsync(string? userId, CancellationToken cancellationToken = default)
    {
        // User bazlı sorgu için cache kullanmıyoruz
        var entities = await _repository.GetAllDocumentsByUserIdAsync(userId, includeInactive: false, cancellationToken);
        return entities.Select(e => new DocumentDisplayInfoSelectDto
        {
            Id = e.Id,
            Text = e.DisplayName,
            FileName = e.FileName,
            DocumentType = e.DocumentType
        }).ToList();
    }

    public async Task<DocumentDisplayInfoDto> UploadAndProcessAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CreateDocumentDisplayInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        // Dosya adını sanitize et (Türkçe karakterler, özel karakterler ve boşluklar temizlenir)
        var sanitizedFileName = TurkishEncodingHelper.SanitizeFileName(fileName);
        _logger.LogInformation("Sanitized filename: {OriginalFileName} -> {SanitizedFileName}", fileName, sanitizedFileName);

        // Dosya adı zaten var mı kontrol et (sanitize edilmiş isimle)
        if (await _repository.DocumentExistsByFileNameAsync(sanitizedFileName, cancellationToken))
        {
            throw new InvalidOperationException($"'{sanitizedFileName}' adında bir döküman zaten mevcut.");
        }

        // Dosya hash'i oluştur
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
        var fileHash = Convert.ToBase64String(hashBytes);
        fileStream.Position = 0;

        // Admin tarafından yüklenen dökümanlar herkes tarafından görülebilir (UserId = null)
        // Normal kullanıcılar tarafından yüklenen dökümanlar sadece o kullanıcı tarafından görülebilir
        var effectiveUserId = _currentUserService.IsAdmin ? null : _currentUserService.UserId;

        // DocumentUploadDto oluştur — entity oluşturma UseCase içinde yapılır
        var uploadDto = new AI.Application.DTOs.DocumentProcessing.DocumentUploadDto
        {
            FileName = sanitizedFileName,
            FileType = contentType,
            FileSize = fileStream.Length,
            FileHash = fileHash,
            DocumentType = request.DocumentType,
            Title = request.DisplayName,
            Description = request.Description,
            Category = request.CategoryId ?? "Genel",
            UserId = effectiveUserId,
            UploadedBy = _currentUserService.DisplayName ?? _currentUserService.Email ?? "System"
        };

        _logger.LogInformation("Starting document upload and processing: {FileName} (original: {OriginalFileName})",
            sanitizedFileName, fileName);

        // Qdrant'a embedding işlemi yap
        var uploadResult = await _documentProcessingUseCase.ProcessDocumentFromUploadAsync(
            uploadDto, fileStream, cancellationToken);

        if (!uploadResult.Success)
        {
            throw new InvalidOperationException($"Döküman işlenemedi: {uploadResult.ErrorMessage}");
        }

        // Veritabanına kaydet — Aggregate Root üzerinden child entity oluştur
        var categoryId = request.CategoryId ?? "genel";
        var category = await _repository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Kategori bulunamadı: {categoryId}");

        var entity = category.AddDocument(
            fileName: uploadDto.FileName,
            displayName: request.DisplayName,
            documentType: request.DocumentType,
            description: request.Description,
            keywords: request.Keywords,
            userId: effectiveUserId,
            createdBy: _currentUserService.DisplayName ?? _currentUserService.Email
        );

        var created = await _repository.SaveDocumentAsync(entity, cancellationToken);

        // Cache'i invalidate et ve yeniden yükle
        await InvalidateAndReloadCacheAsync(cancellationToken);

        _logger.LogInformation("Document uploaded and processed successfully: {DocumentId} - {FileName}, Chunks: {ChunkCount}",
            created.Id, created.FileName, uploadResult.ProcessedChunks);

        return MapToDto(created, true, uploadResult.ProcessedChunks);
    }

    public async Task<DocumentDisplayInfoDto> UpdateAsync(Guid id, UpdateDocumentDisplayInfoRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetDocumentByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new InvalidOperationException($"Döküman bulunamadı: {id}");
        }

        existing.Update(request.DisplayName, request.Description, request.Keywords, request.CategoryId ?? existing.CategoryId);
        if (request.IsActive && !existing.IsActive)
            existing.Activate();
        else if (!request.IsActive && existing.IsActive)
            existing.Deactivate();

        var updated = await _repository.SaveDocumentAsync(existing, cancellationToken);

        // Cache'i invalidate et ve yeniden yükle
        await InvalidateAndReloadCacheAsync(cancellationToken);

        var hasEmbeddings = await HasEmbeddingsAsync(updated.FileName, cancellationToken);
        var chunkCount = hasEmbeddings ? await GetChunkCountAsync(updated.FileName, cancellationToken) : 0;

        _logger.LogInformation("Document display info updated: {DocumentId} - {FileName}", updated.Id, updated.FileName);

        return MapToDto(updated, hasEmbeddings, chunkCount);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetDocumentByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        // Qdrant'tan embedding'leri sil
        var collectionName = QdrantCollections.GetCollectionName(entity.FileName);
        try
        {
            var deleted = await _qdrantService.DeleteVectorsByDocumentIdAsync(collectionName, id, cancellationToken);
            _logger.LogInformation("Deleted {Count} vectors from Qdrant for document: {DocumentId}", deleted, id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete vectors from Qdrant for document: {DocumentId}", id);
            // Qdrant silme başarısız olsa bile veritabanından silmeye devam et
        }

        // Veritabanından sil
        var result = await _repository.DeleteDocumentAsync(id, cancellationToken);

        if (result)
        {
            // Cache'i invalidate et ve yeniden yükle
            await InvalidateAndReloadCacheAsync(cancellationToken);
            _logger.LogInformation("Document deleted: {DocumentId} - {FileName}", id, entity.FileName);
        }

        return result;
    }

    public async Task<DocumentDisplayInfoDto> ReprocessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetDocumentByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"Döküman bulunamadı: {id}");
        }

        // Mevcut embedding'leri sil
        var collectionName = QdrantCollections.GetCollectionName(entity.FileName);
        try
        {
            await _qdrantService.DeleteVectorsByDocumentIdAsync(collectionName, id, cancellationToken);
            _logger.LogInformation("Deleted existing vectors for reprocessing: {DocumentId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete existing vectors for reprocessing: {DocumentId}", id);
        }

        // TODO: Dosyayı yeniden işlemek için dosya içeriğine ihtiyaç var
        // Bu metot şu an sadece metadata döndürür, gerçek reprocess için dosya gerekli
        _logger.LogWarning("Reprocess requires file content which is not stored. Document: {DocumentId}", id);

        var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
        var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

        return MapToDto(entity, hasEmbeddings, chunkCount);
    }

    public async Task<bool> HasEmbeddingsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var collectionName = QdrantCollections.GetCollectionName(fileName);
            return await _qdrantService.CollectionExistsAsync(collectionName, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GetChunkCountAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var collectionName = QdrantCollections.GetCollectionName(fileName);
            var pointsCount = await _qdrantService.GetPointsCountAsync(collectionName, cancellationToken);
            return (int)pointsCount;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Cache'i invalidate edip veritabanından yeniden yükler
    /// </summary>
    private async Task InvalidateAndReloadCacheAsync(CancellationToken cancellationToken)
    {
        // Önce tüm cache'leri temizle
        await _cacheService.InvalidateDocumentCacheAsync(cancellationToken);

        // Kullanıcı bazlı cache'i de temizle
        var userId = _currentUserService.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            await _cacheService.InvalidateUserCacheAsync(userId, cancellationToken);
            _logger.LogDebug("Invalidated user cache for: {UserId}", userId);
        }

        // Veritabanından tüm verileri çek
        var entities = await _repository.GetAllDocumentsAsync(includeInactive: false, cancellationToken);

        // Document list DTO'ları oluştur
        var allDocuments = new List<DocumentDisplayInfoListDto>();
        foreach (var entity in entities)
        {
            var hasEmbeddings = await HasEmbeddingsAsync(entity.FileName, cancellationToken);
            var chunkCount = hasEmbeddings ? await GetChunkCountAsync(entity.FileName, cancellationToken) : 0;

            allDocuments.Add(new DocumentDisplayInfoListDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                DocumentType = entity.DocumentType,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category?.DisplayName,
                UserId = entity.UserId,
                IsActive = entity.IsActive,
                HasEmbeddings = hasEmbeddings,
                ChunkCount = chunkCount,
                CreatedAt = entity.CreatedAt
            });
        }

        // Cache'e kaydet
        await _cacheService.SetAllDocumentsAsync(allDocuments, cancellationToken);

        // Select için de cache'le
        var selectDocuments = entities.Select(e => new DocumentDisplayInfoSelectDto
        {
            Id = e.Id,
            Text = e.DisplayName,
            FileName = e.FileName,
            DocumentType = e.DocumentType
        }).ToList();
        await _cacheService.SetDocumentsForSelectAsync(selectDocuments, cancellationToken);

        _logger.LogInformation("Document cache invalidated and reloaded with {Count} documents", allDocuments.Count);
    }

    private static DocumentDisplayInfoDto MapToDto(DocumentDisplayInfo entity, bool hasEmbeddings, int chunkCount)
    {
        return new DocumentDisplayInfoDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            DocumentType = entity.DocumentType,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Keywords = entity.Keywords,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category?.DisplayName,
            UserId = entity.UserId,
            IsActive = entity.IsActive,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            HasEmbeddings = hasEmbeddings,
            ChunkCount = chunkCount
        };
    }
}
