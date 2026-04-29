using AI.Domain.Common;
using AI.Domain.Enums;
using AI.Domain.ValueObjects;

namespace AI.Domain.Memory;

/// <summary>
/// Kullanıcının uzun vadeli hafızasını temsil eden entity.
/// Kullanıcı tercihleri, davranış kalıpları ve öğrenilen bilgileri saklar.
/// </summary>
public sealed class UserMemory : AggregateRoot<Guid>
{

    /// <summary>
    /// Hafızanın ait olduğu kullanıcı ID'si (CurrentUserService.UserId)
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// Hafıza anahtarı (örn: "preferred_format", "favorite_report", "preferred_region")
    /// </summary>
    public string Key { get; private set; } = null!;

    /// <summary>
    /// Hafıza değeri (örn: "Excel", "Satış raporu", "İstanbul")
    /// </summary>
    public string Value { get; private set; } = null!;

    /// <summary>
    /// Hafızanın kategorisi
    /// </summary>
    public MemoryCategory Category { get; private set; }

    /// <summary>
    /// Bilginin öğrenildiği orijinal bağlam
    /// </summary>
    public string? Context { get; private set; }

    /// <summary>
    /// Bilginin güvenilirlik skoru (0.0 - 1.0)
    /// </summary>
    public Confidence Confidence { get; private set; } = null!;

    /// <summary>
    /// Bu hafızanın kaç kez kullanıldığı
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Son erişim tarihi
    /// </summary>
    public DateTime LastAccessedAt { get; private set; }

    /// <summary>
    /// Soft delete için
    /// </summary>
    public bool IsDeleted { get; private set; }

    // EF Core constructor
    private UserMemory() { }

    /// <summary>
    /// Factory method - yeni hafıza oluşturur
    /// </summary>
    public static UserMemory Create(
        string userId,
        string key,
        string value,
        MemoryCategory category,
        float confidence = 1.0f,
        string? context = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new UserMemory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Key = key.ToLowerInvariant().Trim(),
            Value = value.Trim(),
            Category = category,
            Context = context,
            Confidence = ValueObjects.Confidence.Create(confidence),
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        // Not: Domain event, return new pattern nedeniyle
        // factory method dışında eklenmelidir
    }

    /// <summary>
    /// Hafıza değerini günceller
    /// </summary>
    public void UpdateValue(string newValue, float? newConfidence = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newValue);

        Value = newValue.Trim();
        if (newConfidence.HasValue)
            Confidence = ValueObjects.Confidence.Create(newConfidence.Value);
        LastAccessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Erişim sayısını artırır
    /// </summary>
    public void IncrementUsage()
    {
        UsageCount++;
        LastAccessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Güven skorunu artırır (tekrarlanan bilgilerde)
    /// </summary>
    public void BoostConfidence(float boost = 0.1f)
    {
        Confidence = Confidence.Boost(boost);
        LastAccessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete
    /// </summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }

    /// <summary>
    /// Embedding için metin üretir
    /// </summary>
    public string ToEmbeddingText()
    {
        return $"{Key}: {Value}";
    }
}

