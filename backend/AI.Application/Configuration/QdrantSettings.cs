using System.ComponentModel.DataAnnotations;

namespace AI.Application.Configuration;

/// <summary>
/// Qdrant vector database konfigürasyon ayarları
/// </summary>
public class QdrantSettings
{
    /// <summary>
    /// Qdrant sunucu URL'i
    /// </summary>
    [Required(ErrorMessage = "Qdrant Host is required")]
    public string Host { get; set; } = "localhost";


    /// <summary>
    /// Qdrant sunucu portu
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 6334;

    /// <summary>
    /// HTTPS kullanılacak mı?
    /// </summary>
    public bool UseHttps { get; set; } = false;

    /// <summary>
    /// API anahtarı (varsa)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Varsayılan koleksiyon adı
    /// </summary>
    [Required(ErrorMessage = "Default Collection is required")]
    public string DefaultCollection { get; set; } = "documents";

    /// <summary>
    /// Embedding model adı (OpenAI modeli)
    /// </summary>
    [Required(ErrorMessage = "Embedding Model is required")]
    public string EmbeddingModel { get; set; } = "text-embedding-3-large";

    /// <summary>
    /// Vector boyutu (embedding model'e göre)
    /// </summary>
    [Range(1, 10000, ErrorMessage = "Vector size must be between 1 and 10000")]
    public int VectorSize { get; set; } = 1536;

    /// <summary>
    /// Cosine benzerlik metriği
    /// </summary>
    [RegularExpression("^(cosine|euclidean|dot)$", ErrorMessage = "CosineSimilarity must be 'cosine', 'euclidean', or 'dot'")]
    public string CosineSimilarity { get; set; } = "cosine";

    /// <summary>
    /// Bağlantı timeout süresi (saniye)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maksimum retry sayısı
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Vector search minimum similarity score threshold
    /// </summary>
    [Range(0.0f, 1.0f, ErrorMessage = "MinSimilarityScore must be between 0.0 and 1.0")]
    public float MinSimilarityScore { get; set; } = 0.7f;

    /// <summary>
    /// HyDE için maksimum token sayısı
    /// Orijinal HyDE makalesi 50-200 kelime önerir (yaklaşık 100-400 token)
    /// </summary>
    [Range(100, 1000, ErrorMessage = "HyDEMaxTokens must be between 100 and 1000")]
    public int HyDEMaxTokens { get; set; } = 350;

    /// <summary>
    /// HyDE embedding ağırlığı (0.0-1.0)
    /// Corrected query: (1 - HyDEWeight), HyDE: HyDEWeight
    /// 0.6 = HyDE %60, Corrected %40 ağırlık alır
    /// </summary>
    [Range(0.0f, 1.0f, ErrorMessage = "HyDEWeight must be between 0.0 and 1.0")]
    public float HyDEWeight { get; set; } = 0.6f;

    /// <summary>
    /// Native hybrid search için dense vector ağırlığı (0.0-1.0)
    /// </summary>
    [Range(0.0f, 1.0f, ErrorMessage = "DenseWeight must be between 0.0 and 1.0")]
    public float DenseWeight { get; set; } = 0.7f;

    /// <summary>
    /// Native hybrid search için sparse vector ağırlığı (0.0-1.0)
    /// </summary>
    [Range(0.0f, 1.0f, ErrorMessage = "SparseWeight must be between 0.0 and 1.0")]
    public float SparseWeight { get; set; } = 0.3f;

    /// <summary>
    /// RRF (Reciprocal Rank Fusion) k parametresi
    /// </summary>
    [Range(1, 200, ErrorMessage = "RRF_K must be between 1 and 200")]
    public int RRF_K { get; set; } = 60;

    /// <summary>
    /// HNSW search için ef parametresi (arama kalitesi)
    /// Yüksek değer = daha iyi kalite, daha yavaş arama
    /// </summary>
    [Range(16, 512, ErrorMessage = "HnswEf must be between 16 and 512")]
    public int HnswEf { get; set; } = 128;

    /// <summary>
    /// Tam Qdrant URL'ini döndürür
    /// </summary>
    public string GetConnectionString()
    {
        var protocol = UseHttps ? "https" : "http";
        return $"{protocol}://{Host}:{Port}";
    }

    /// <summary>
    /// Settings'leri validate eder
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        // Manuel validation için DataAnnotations context kullan
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, context, results, true))
        {
            errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
        }

        // Hybrid search weight validation
        var totalWeight = DenseWeight + SparseWeight;
        if (Math.Abs(totalWeight - 1.0f) > 0.01f)
        {
            errors.Add($"DenseWeight ({DenseWeight}) + SparseWeight ({SparseWeight}) must equal 1.0 (current: {totalWeight})");
        }

        return errors.Count == 0;
    }
}