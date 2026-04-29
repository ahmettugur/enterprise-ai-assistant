
using AI.Application.Configuration;
using AI.Application.DTOs;
using AI.Application.Ports.Secondary.Services.Vector;
using AI.Domain.Documents;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AI.Infrastructure.Adapters.AI.VectorServices;

/// <summary>
/// Qdrant vector database servisi implementasyonu
/// </summary>
public class QdrantService : IQdrantService, IDisposable
{
    private readonly QdrantClient _client;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantService> _logger;
    private bool _disposed = false;

    public QdrantService(QdrantSettings settings, ILogger<QdrantService> logger)
    {
        _settings = settings;
        _logger = logger;

        try
        {
            // Qdrant client'ı port ile başlat (gRPC portu genellikle 6334)
            // REST API portu 6333, gRPC portu 6334
            var grpcPort = _settings.Port == 6333 ? 6334 : _settings.Port;
            _client = new QdrantClient(
                host: _settings.Host,
                port: grpcPort,
                https: _settings.UseHttps,
                apiKey: string.IsNullOrEmpty(_settings.ApiKey) ? null : _settings.ApiKey
            );

            _logger.LogInformation("Qdrant client initialized successfully. Host: {Host}, gRPC Port: {Port}", _settings.Host, grpcPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Qdrant client");
            throw;
        }
    }

    public async Task<bool> CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var distance = _settings.CosineSimilarity.ToLowerInvariant() switch
            {
                "cosine" => Distance.Cosine,
                "euclidean" => Distance.Euclid,
                "dot" => Distance.Dot,
                _ => Distance.Cosine
            };

            // Named vectors configuration: "dense" (semantic) + "sparse" (keywords)
            // HNSW optimizasyonu eklenmiş: M=16, EfConstruct=200
            var vectorsConfig = new VectorParamsMap
            {
                Map =
                {
                    ["dense"] = new VectorParams
                    {
                        Size = (ulong)vectorSize,
                        Distance = distance,
                        HnswConfig = new HnswConfigDiff
                        {
                            M = 16,           // Bağlantı sayısı (16-64 arası önerilir)
                            EfConstruct = 200 // Index oluşturma kalitesi
                        }
                    }
                }
            };

            // Sparse vectors configuration for keyword matching
            var sparseVectorsConfig = new SparseVectorConfig
            {
                Map =
                {
                    ["sparse"] = new SparseVectorParams()
                }
            };

            await _client.CreateCollectionAsync(
                collectionName: collectionName,
                vectorsConfig: vectorsConfig,
                sparseVectorsConfig: sparseVectorsConfig,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Collection '{CollectionName}' created successfully with named vectors: dense (size={VectorSize}) + sparse",
                collectionName, vectorSize);
            return true;
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
        {
            // Collection zaten var - bu bir hata değil
            _logger.LogDebug("Collection '{CollectionName}' already exists, skipping creation", collectionName);
            return true;
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // Collection zaten var - bu bir hata değil (alternatif exception handling)
            _logger.LogDebug("Collection '{CollectionName}' already exists, skipping creation", collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection '{CollectionName}'", collectionName);
            return false;
        }
    }

    public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            return collections.Any(c => c == collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if collection '{CollectionName}' exists", collectionName);
            return false;
        }
    }

    public async Task<bool> DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteCollectionAsync(collectionName, timeout: null, cancellationToken);
            _logger.LogInformation("Collection '{CollectionName}' deleted successfully", collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection '{CollectionName}'", collectionName);
            return false;
        }
    }

    public async Task<Guid?> UpsertVectorAsync(string collectionName, DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default)
    {
        try
        {
            var vectorId = chunk.VectorId ?? Guid.NewGuid();
            
            var payload = new Dictionary<string, Value>
            {
                ["document_id"] = chunk.DocumentId.ToString(),
                ["chunk_id"] = chunk.Id.ToString(),
                ["chunk_index"] = chunk.ChunkIndex,
                ["content"] = chunk.Content,
                ["content_length"] = chunk.ContentLength,
                ["start_position"] = chunk.StartPosition,
                ["end_position"] = chunk.EndPosition,
                ["created_at"] = chunk.CreatedAt.ToString("O")
            };

            // Chunk metadata'sını parse edip payload'a ekle (Türkçe karakter encoding'i koru)
            if (!string.IsNullOrEmpty(chunk.Metadata))
            {
                try
                {
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(chunk.Metadata);
                    if (metadata != null)
                    {
                        foreach (var kvp in metadata)
                        {
                            // Encoding'i korumak için değeri doğrudan metadata'dan al
                            if (kvp.Value is System.Text.Json.JsonElement jsonElement)
                            {
                                // JsonElement'i string olarak al ve JSON tırnaklarını kaldır
                                var rawText = jsonElement.GetRawText();

                                // JSON string değerler tırnak içinde gelir, onları kaldır
                                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    payload[kvp.Key] = jsonElement.GetString() ?? rawText;
                                }
                                else
                                {
                                    payload[kvp.Key] = rawText;
                                }
                            }
                            else if (kvp.Value is string str)
                            {
                                // String ise doğrudan ata
                                payload[kvp.Key] = str;
                            }
                            else
                            {
                                // Diğer tipler için (int, long, etc.) string'e dönüştür
                                payload[kvp.Key] = kvp.Value?.ToString() ?? "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse chunk metadata for chunk {ChunkId}", chunk.Id);
                    payload["metadata"] = chunk.Metadata;
                }
            }
            else
            {
                payload["metadata"] = "{}";
            }

            // Check if chunk has sparse vector - Named vectors kullan (UpsertVectorsAsync ile tutarlı)
            var hasSparseVector = chunk.SparseIndices != null && chunk.SparseValues != null &&
                                  chunk.SparseIndices.Length > 0 && chunk.SparseValues.Length > 0;

            // Named vectors oluştur (her zaman named vector kullan - tutarlılık için)
            var namedVectors = new NamedVectors();
            namedVectors.Vectors.Add("dense", vector);

            if (hasSparseVector)
            {
                // Sparse vector - Indeks-değer çiftlerini sırala ve tekilleştir
                var (sparseIndicesArr, sparseValuesArr) = ProcessSparseVector(
                    chunk.SparseIndices ?? Array.Empty<uint>(),
                    chunk.SparseValues ?? Array.Empty<float>());

                namedVectors.Vectors.Add("sparse", (sparseValuesArr, sparseIndicesArr));
            }

            var point = new PointStruct
            {
                Id = new PointId { Uuid = vectorId.ToString() },
                Vectors = new Vectors { Vectors_ = namedVectors },
                Payload = { payload }
            };

            await _client.UpsertAsync(
                collectionName: collectionName,
                points: new[] { point },
                cancellationToken: cancellationToken
            );

            _logger.LogDebug("Vector upserted successfully. VectorId: {VectorId}, ChunkId: {ChunkId}", vectorId, chunk.Id);
            return vectorId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert vector for chunk {ChunkId}", chunk.Id);
            return null;
        }
    }

    public async Task<bool> UpsertVectorsAsync(string collectionName, List<DocumentChunk> chunks, List<float[]> vectors, CancellationToken cancellationToken = default)
    {
        try
        {
            if (chunks.Count != vectors.Count)
            {
                throw new ArgumentException("Chunks and vectors count must be equal");
            }

            var points = new List<PointStruct>();
            
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                var vector = vectors[i];
                var vectorId = chunk.VectorId ?? Guid.NewGuid();
                
                var payload = new Dictionary<string, Value>
                {
                    ["document_id"] = chunk.DocumentId.ToString(),
                    ["chunk_id"] = chunk.Id.ToString(),
                    ["chunk_index"] = chunk.ChunkIndex,
                    ["content"] = chunk.Content,
                    ["content_length"] = chunk.ContentLength,
                    ["start_position"] = chunk.StartPosition,
                    ["end_position"] = chunk.EndPosition,
                    ["created_at"] = chunk.CreatedAt.ToString("O")
                };

                // Chunk metadata'sını parse edip payload'a ekle (Türkçe karakter encoding'i koru)
                if (!string.IsNullOrEmpty(chunk.Metadata))
                {
                    try
                    {
                        var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(chunk.Metadata);
                        if (metadata != null)
                        {
                            foreach (var kvp in metadata)
                            {
                                // Encoding'i korumak için değeri doğrudan metadata'dan al
                                if (kvp.Value is System.Text.Json.JsonElement jsonElement)
                                {
                                    // JsonElement'i string olarak al ve JSON tırnaklarını kaldır
                                    var rawText = jsonElement.GetRawText();

                                    // JSON string değerler tırnak içinde gelir, onları kaldır
                                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        payload[kvp.Key] = jsonElement.GetString() ?? rawText;
                                    }
                                    else
                                    {
                                        payload[kvp.Key] = rawText;
                                    }
                                }
                                else if (kvp.Value is string str)
                                {
                                    // String ise doğrudan ata
                                    payload[kvp.Key] = str;
                                }
                                else
                                {
                                    // Diğer tipler için (int, long, etc.) string'e dönüştür
                                    payload[kvp.Key] = kvp.Value?.ToString() ?? "";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse chunk metadata for chunk {ChunkId}", chunk.Id);
                        payload["metadata"] = chunk.Metadata;
                    }
                }
                else
                {
                    payload["metadata"] = "{}";
                }

                // Check if chunk has sparse vector
                var hasSparseVector = chunk.SparseIndices != null && chunk.SparseValues != null &&
                                      chunk.SparseIndices.Length > 0 && chunk.SparseValues.Length > 0;

                // Named vectors oluştur (her zaman named vector kullan - tutarlılık için)
                var namedVectors = new NamedVectors();
                namedVectors.Vectors.Add("dense", vector);

                if (hasSparseVector)
                {
                    // Sparse vector - Indeks-değer çiftlerini sırala ve tekilleştir
                    var (sparseIndicesArr, sparseValuesArr) = ProcessSparseVector(
                        chunk.SparseIndices ?? Array.Empty<uint>(),
                        chunk.SparseValues ?? Array.Empty<float>());

                    namedVectors.Vectors.Add("sparse", (sparseValuesArr, sparseIndicesArr));
                }

                points.Add(new PointStruct
                {
                    Id = new PointId { Uuid = vectorId.ToString() },
                    Vectors = new Vectors { Vectors_ = namedVectors },
                    Payload = { payload }
                });
                
                // Update chunk with vector ID
                chunk.SetEmbedding(vectorId);
            }

            await _client.UpsertAsync(
                collectionName: collectionName,
                points: points,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Successfully upserted {Count} vectors to collection '{CollectionName}'", points.Count, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert {Count} vectors to collection '{CollectionName}'", chunks.Count, collectionName);
            return false;
        }
    }

    public async Task<List<SearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, float minScore = 0.7f, Dictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("SearchAsync çağrısı: Collection={Collection}, Limit={Limit}, MinScore={MinScore}, VectorDimension={Dimension}",
                collectionName, limit, minScore, queryVector?.Length ?? 0);

            var searchParams = new SearchParams
            {
                HnswEf = (ulong)_settings.HnswEf, // Settings'ten alınıyor
                Exact = false // HNSW algoritması kullan, exact search yerine
            };

            Filter? qdrantFilter = null;
            if (filter != null && filter.Any())
            {
                qdrantFilter = BuildFilter(filter);
            }

            var searchResults = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryVector,
                limit: (ulong)limit,
                scoreThreshold: minScore,
                filter: qdrantFilter,
                searchParams: searchParams,
                vectorName: "dense",
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Qdrant'tan {Count} sonuç alındı (MinScore={MinScore})", searchResults.Count, minScore);

            if (searchResults.Count == 0)
            {
                _logger.LogWarning("⚠️ Qdrant'ta hiç sonuç bulunamadı! Collection={Collection}, MinScore={MinScore}, VectorDim={Dim}",
                    collectionName, minScore, queryVector?.Length ?? 0);
            }

            var results = new List<SearchResult>();

            foreach (var result in searchResults)
            {
                try
                {
                    var payload = result.Payload;

                    // Log similarity score details for debugging
                    _logger.LogDebug("Qdrant SearchResult - ID: {Id}, Score: {Score}, ScoreValue: {ScoreValue}",
                        result.Id, result.Score, result.Score);

                    var searchResult = new SearchResult
                    {
                        ChunkId = Guid.Parse(payload["chunk_id"].StringValue),
                        DocumentId = Guid.Parse(payload["document_id"].StringValue),
                        Content = payload["content"].StringValue,
                        Score = result.Score,
                        ChunkIndex = (int)payload["chunk_index"].IntegerValue,
                        StartPosition = payload.ContainsKey("start_position") ? (int)payload["start_position"].IntegerValue : 0,
                        EndPosition = payload.ContainsKey("end_position") ? (int)payload["end_position"].IntegerValue : 0
                    };

                    // Document metadata fields
                    if (payload.ContainsKey("fileName"))
                    {
                        searchResult.DocumentName = payload["fileName"].StringValue;
                    }
                    if (payload.ContainsKey("title"))
                    {
                        searchResult.DocumentTitle = payload["title"].StringValue;
                    }
                    if (payload.ContainsKey("category"))
                    {
                        searchResult.Category = payload["category"].StringValue;
                        searchResult.DocumentCategory = payload["category"].StringValue;
                    }
                    if (payload.ContainsKey("uploadedAt") && DateTime.TryParse(payload["uploadedAt"].StringValue, out var uploadedAt))
                    {
                        searchResult.UploadedAt = uploadedAt;
                    }
                    if (payload.ContainsKey("created_at") && DateTime.TryParse(payload["created_at"].StringValue, out var createdAt))
                    {
                        searchResult.UploadedAt = createdAt;
                    }

                    // Metadata dictionary oluştur (Türkçe karakter encoding'i koru)
                    var metadata = new Dictionary<string, object>();
                    foreach (var kvp in payload)
                    {
                        // Temel alanları ve istenmeyen metadata alanlarını metadata'ya ekleme
                        var excludedFields = new[] {
                            "chunk_id", "document_id", "content", "chunk_index", "start_position", "end_position",
                            "fileSize", "uploadedBy", "content_length", "created_at", "category", "status", "fileHash", "uploadedAt"
                        };

                        if (!excludedFields.Contains(kvp.Key))
                        {
                            // StringValue doğrudan kullan (UTF-8 encoding korunur)
                            metadata[kvp.Key] = kvp.Value.StringValue ?? "";
                        }
                    }
                    searchResult.Metadata = metadata;

                    results.Add(searchResult);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse search result payload");
                }
            }

            _logger.LogDebug("Search completed. Found {Count} results for collection '{CollectionName}'", results.Count, collectionName);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search in collection '{CollectionName}'", collectionName);
            return new List<SearchResult>();
        }
    }

    public async Task<bool> DeleteVectorAsync(string collectionName, Guid vectorId, CancellationToken cancellationToken = default)
    {
        try
        {
            // BUG FIX: Guid.GetHashCode() kullanmak yanlış - data loss oluyor
            // Doğru yöntem: UUID string olarak kullan
            await _client.DeleteAsync(
                collectionName: collectionName,
                ids: new[] { new PointId { Uuid = vectorId.ToString() } },
                cancellationToken: cancellationToken
            );

            _logger.LogDebug("Vector {VectorId} deleted successfully from collection '{CollectionName}'", vectorId, collectionName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vector {VectorId} from collection '{CollectionName}'", vectorId, collectionName);
            return false;
        }
    }

    public async Task<int> DeleteVectorsByDocumentIdAsync(string collectionName, Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "document_id",
                            Match = new Match { Text = documentId.ToString() }
                        }
                    }
                }
            };

            await _client.DeleteAsync(
                collectionName: collectionName,
                filter: filter,
                cancellationToken: cancellationToken
            );

            // Not: Qdrant delete işlemi silinen kayıt sayısını döndürmez
            // Başarılı silme için 1, hata için 0 döndürüyoruz
            _logger.LogInformation("Delete operation completed for document {DocumentId} from collection '{CollectionName}'", documentId, collectionName);
            return 1; // İşlem başarılı
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vectors for document {DocumentId} from collection '{CollectionName}'", documentId, collectionName);
            return 0;
        }
    }

    public async Task<object?> GetCollectionInfoAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(collectionName, cancellationToken);
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection info for '{CollectionName}'", collectionName);
            return null;
        }
    }

    public async Task<List<string>> GetCollectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching collections list from Qdrant...");
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            var result = collections.ToList();
            _logger.LogDebug("Successfully fetched {Count} collections from Qdrant", result.Count);
            return result;
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while fetching collections list. Status: {Status}, Detail: {Detail}", 
                ex.StatusCode, ex.Status.Detail);
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections list. Exception type: {ExceptionType}", ex.GetType().Name);
            return new List<string>();
        }
    }

    public async Task<long> GetPointsCountAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(collectionName, cancellationToken);
            return (long)info.PointsCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get points count for collection '{CollectionName}'", collectionName);
            return 0;
        }
    }

    private static Filter BuildFilter(Dictionary<string, object> filterDict)
    {
        var filter = new Filter();
        
        foreach (var kvp in filterDict)
        {
            var condition = new Condition
            {
                Field = new FieldCondition
                {
                    Key = kvp.Key,
                    Match = new Match { Text = kvp.Value.ToString() }
                }
            };
            
            filter.Must.Add(condition);
        }
        
        return filter;
    }

    /// <summary>
    /// Sparse vector indeks-değer çiftlerini sıralar ve tekilleştirir
    /// Qdrant, sparse vector'lerin sıralı ve unique index'lere sahip olmasını gerektirir
    /// </summary>
    private static (uint[] Indices, float[] Values) ProcessSparseVector(uint[] indices, float[] values)
    {
        if (indices.Length == 0 || values.Length == 0)
            return (Array.Empty<uint>(), Array.Empty<float>());

        var pairs = indices.Zip(values, (i, v) => (i, v))
            .GroupBy(p => p.i)                              // Duplicate indices'leri birleştir
            .Select(g => (i: g.Key, v: g.Sum(p => p.v)))    // Values'ları topla
            .OrderBy(p => p.i)                              // Sırala (Qdrant requirement)
            .ToList();

        return (
            pairs.Select(p => p.i).ToArray(),
            pairs.Select(p => p.v).ToArray()
        );
    }

    public async Task<List<SearchResult>> HybridSearchAsync(
        string collectionName,
        float[] denseVector,
        uint[] sparseIndices,
        float[] sparseValues,
        int limit = 10,
        float minScore = 0.0f,
        float denseWeight = 0.7f,
        float sparseWeight = 0.3f,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be empty", nameof(collectionName));

            if (denseVector == null || denseVector.Length == 0)
                throw new ArgumentException("Dense vector cannot be null or empty", nameof(denseVector));

            if (sparseIndices == null || sparseIndices.Length == 0)
            {
                _logger.LogWarning("Sparse vector is empty, falling back to dense-only search");
                return await SearchAsync(collectionName, denseVector, limit, minScore, null, cancellationToken);
            }

            if (sparseIndices.Length != sparseValues.Length)
                throw new ArgumentException("Sparse indices and values must have the same length");

            // Weight validation
            var totalWeight = denseWeight + sparseWeight;
            if (Math.Abs(totalWeight - 1.0f) > 0.01f)
            {
                _logger.LogWarning("Weight sum ({TotalWeight}) != 1.0, normalizing weights", totalWeight);
                denseWeight /= totalWeight;
                sparseWeight /= totalWeight;
            }

            _logger.LogInformation(
                "HybridSearchAsync - Collection: {Collection}, DenseVector: {DenseDim}, SparseTerms: {SparseTerms}, " +
                "Limit: {Limit}, Weights: Dense={DenseWeight:F2}, Sparse={SparseWeight:F2}",
                collectionName, denseVector.Length, sparseIndices.Length, limit, denseWeight, sparseWeight);

            // Create sparse vector tuples (value, index)
            var sparseVectorTuples = sparseValues
                .Zip(sparseIndices, (value, index) => (value, index))
                .ToArray();

            // Qdrant native hybrid search with RRF fusion
            // Prefetch: 1) Sparse vector search, 2) Dense vector search
            // Query: RRF fusion (Reciprocal Rank Fusion)
            var rrfK = _settings.RRF_K; // Use settings value
            var prefetchLimit = (ulong)Math.Max(limit * 2, 20); // Minimum 20 for better fusion

            _logger.LogDebug("RRF parameters: K={K}, PrefetchLimit={PrefetchLimit}", rrfK, prefetchLimit);

            // RRF score'ları tipik olarak 0.01-0.05 arasında olur (1/(k+rank))
            // Bu nedenle yüksek minScore değerleri (0.7 gibi) hiç sonuç dönmemesine neden olur
            // RRF için score threshold'u sıfırla veya çok düşük tut
            var effectiveScoreThreshold = minScore > 0.1f ? 0.0f : minScore;

            var queryResults = await _client.QueryAsync(
                collectionName: collectionName,
                prefetch: new[]
                {
                    // Sparse vector prefetch (keyword matching)
                    new PrefetchQuery
                    {
                        Query = sparseVectorTuples,
                        Using = "sparse",
                        Limit = prefetchLimit
                    },
                    // Dense vector prefetch (semantic similarity)
                    new PrefetchQuery
                    {
                        Query = denseVector,
                        Using = "dense",
                        Limit = prefetchLimit
                    }
                },
                query: new Rrf { K = (uint)rrfK }, // RRF fusion with configurable k
                limit: (ulong)limit,
                scoreThreshold: effectiveScoreThreshold, // RRF için ayarlanmış threshold
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("HybridSearch (RRF fusion) returned {Count} results", queryResults.Count);

            // Convert to SearchResult
            var results = new List<SearchResult>();
            foreach (var result in queryResults)
            {
                try
                {
                    var payload = result.Payload;

                    var searchResult = new SearchResult
                    {
                        ChunkId = Guid.TryParse(payload["chunk_id"].StringValue, out var chunkId) ? chunkId : Guid.Empty,
                        DocumentId = Guid.TryParse(payload["document_id"].StringValue, out var docId) ? docId : Guid.Empty,
                        Content = payload.ContainsKey("content") ? payload["content"].StringValue : "",
                        Score = result.Score,
                        // Field name tutarlılığı: SearchAsync ile aynı field'lar (camelCase)
                        DocumentName = payload.ContainsKey("fileName") ? payload["fileName"].StringValue : "",
                        DocumentTitle = payload.ContainsKey("title") ? payload["title"].StringValue : "",
                        DocumentCategory = payload.ContainsKey("category") ? payload["category"].StringValue : "",
                        Category = payload.ContainsKey("category") ? payload["category"].StringValue : "",
                        ChunkIndex = payload.ContainsKey("chunk_index") ? (int)payload["chunk_index"].IntegerValue : 0,
                        StartPosition = payload.ContainsKey("start_position") ? (int)payload["start_position"].IntegerValue : 0,
                        EndPosition = payload.ContainsKey("end_position") ? (int)payload["end_position"].IntegerValue : 0,
                        UploadedAt = payload.ContainsKey("uploadedAt") && DateTime.TryParse(payload["uploadedAt"].StringValue, out var uploadedAt)
                            ? uploadedAt
                            : (payload.ContainsKey("created_at") && DateTime.TryParse(payload["created_at"].StringValue, out var createdAt) ? createdAt : null)
                    };

                    // Metadata dictionary oluştur (SearchAsync ile tutarlı - excludedFields + StringValue)
                    var metadata = new Dictionary<string, object>();
                    foreach (var kvp in payload)
                    {
                        // Temel alanları ve istenmeyen metadata alanlarını metadata'ya ekleme
                        var excludedFields = new[] {
                            "chunk_id", "document_id", "content", "chunk_index", "start_position", "end_position",
                            "fileSize", "uploadedBy", "content_length", "created_at", "category", "status", "fileHash", "uploadedAt"
                        };

                        if (!excludedFields.Contains(kvp.Key))
                        {
                            // StringValue kullan (UTF-8 encoding korunur, JSON format sorunu çözülür)
                            metadata[kvp.Key] = kvp.Value.StringValue ?? "";
                        }
                    }
                    searchResult.Metadata = metadata;

                    results.Add(searchResult);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse hybrid search result");
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HybridSearchAsync failed for collection '{CollectionName}'", collectionName);
            return new List<SearchResult>();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}
