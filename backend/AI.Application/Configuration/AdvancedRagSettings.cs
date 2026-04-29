using System.ComponentModel.DataAnnotations;

namespace AI.Application.Configuration;

/// <summary>
/// Advanced RAG özellikleri için konfigürasyon ayarları
/// </summary>
public class AdvancedRagSettings
{
    /// <summary>
    /// Konfigürasyon section adı
    /// </summary>
    public const string SectionName = "AdvancedRag";

    #region Reranking Settings

    /// <summary>
    /// Reranking özelliğini etkinleştir
    /// </summary>
    public bool EnableReranking { get; set; } = true;

    /// <summary>
    /// Reranking için aday sayısı (ilk aramada çekilecek sonuç sayısı)
    /// </summary>
    [Range(5, 100, ErrorMessage = "RerankCandidateCount must be between 5 and 100")]
    public int RerankCandidateCount { get; set; } = 20;

    /// <summary>
    /// Reranking sonrası döndürülecek sonuç sayısı
    /// </summary>
    [Range(1, 20, ErrorMessage = "RerankTopK must be between 1 and 20")]
    public int RerankTopK { get; set; } = 5;

    /// <summary>
    /// Reranking için batch boyutu (LLM token limiti için)
    /// </summary>
    [Range(1, 20, ErrorMessage = "RerankBatchSize must be between 1 and 20")]
    public int RerankBatchSize { get; set; } = 5;

    /// <summary>
    /// Reranking için maksimum içerik uzunluğu (karakter)
    /// </summary>
    [Range(100, 2000, ErrorMessage = "MaxContentLengthForRerank must be between 100 and 2000")]
    public int MaxContentLengthForRerank { get; set; } = 500;

    #endregion

    #region Self-Query Settings

    /// <summary>
    /// Self-Query (metadata filtering) özelliğini etkinleştir
    /// </summary>
    public bool EnableSelfQuery { get; set; } = true;

    /// <summary>
    /// Self-Query için minimum sorgu uzunluğu
    /// Çok kısa sorgularda filter extraction yapılmaz
    /// </summary>
    [Range(3, 50, ErrorMessage = "SelfQueryMinLength must be between 3 and 50")]
    public int SelfQueryMinLength { get; set; } = 10;

    #endregion

    #region Conditional Features

    /// <summary>
    /// Multi-Query özelliğini etkinleştir (karmaşık sorgularda otomatik aktif)
    /// </summary>
    public bool EnableMultiQuery { get; set; } = false;

    /// <summary>
    /// Contextual Compression özelliğini etkinleştir (uzun context'te otomatik aktif)
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// Compression için token threshold
    /// Toplam context bu değeri aşarsa compression aktif olur
    /// </summary>
    [Range(1000, 10000, ErrorMessage = "CompressionTokenThreshold must be between 1000 and 10000")]
    public int CompressionTokenThreshold { get; set; } = 4000;

    #endregion

    #region Performance Settings

    /// <summary>
    /// Paralel LLM çağrılarını etkinleştir
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// LLM çağrısı timeout (saniye)
    /// </summary>
    [Range(5, 60, ErrorMessage = "LLMTimeoutSeconds must be between 5 and 60")]
    public int LLMTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Hata durumunda orijinal sonuçları döndür (fail-safe)
    /// </summary>
    public bool FailSafeEnabled { get; set; } = true;

    #endregion

    /// <summary>
    /// Ayarları validate eder
    /// </summary>
    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, context, results, true))
        {
            errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
        }

        // RerankTopK, RerankCandidateCount'tan küçük olmalı
        if (RerankTopK >= RerankCandidateCount)
        {
            errors.Add($"RerankTopK ({RerankTopK}) must be less than RerankCandidateCount ({RerankCandidateCount})");
        }

        return errors.Count == 0;
    }
}
