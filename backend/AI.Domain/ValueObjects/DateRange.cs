using AI.Domain.Common;

namespace AI.Domain.ValueObjects;

/// <summary>
/// DateRange Value Object — CreatedAt / UpdatedAt çiftini kapsüller
/// Birçok entity'de tekrarlanan tarih yönetimi pattern'ini birleştirir
/// </summary>
public sealed class DateRange : ValueObject
{
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private DateRange(DateTime createdAt, DateTime updatedAt)
    {
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Yeni DateRange oluşturur (now/now)
    /// </summary>
    public static DateRange Create()
        => new(DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>
    /// Mevcut tarihlerden DateRange oluşturur (DB'den yükleme)
    /// </summary>
    public static DateRange FromExisting(DateTime createdAt, DateTime updatedAt)
        => new(createdAt, updatedAt);

    /// <summary>
    /// UpdatedAt günceller ve yeni DateRange döndürür (immutability)
    /// </summary>
    public DateRange MarkUpdated()
        => new(CreatedAt, DateTime.UtcNow);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CreatedAt;
        yield return UpdatedAt;
    }
}
