using AI.Domain.Common;
using AI.Domain.Enums;

namespace AI.Domain.Documents;

/// <summary>
/// Döküman UI görüntüleme bilgileri için entity
/// Router prompt'a inject edilecek ve UI'da gösterilecek metadata
/// Qdrant'taki dökümanla FileName üzerinden eşleşir
/// </summary>
public sealed class DocumentDisplayInfo : Entity<Guid>
{

    /// <summary>
    /// Dosya adı - Qdrant collection ile eşleşme için kullanılır
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Döküman tipi - Normal döküman veya Soru-Cevap formatı
    /// </summary>
    public DocumentType DocumentType { get; private set; } = DocumentType.Document;

    /// <summary>
    /// Ekranda görünen döküman adı
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Döküman açıklaması
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Anahtar kelimeler (virgülle ayrılmış)
    /// Router prompt'unda döküman eşleştirme için kullanılır
    /// </summary>
    public string? Keywords { get; private set; }

    /// <summary>
    /// Kategori ID (Foreign Key) — zorunlu, her döküman bir kategoriye bağlı
    /// </summary>
    public string CategoryId { get; private set; } = null!;

    /// <summary>
    /// Kategori navigation property
    /// </summary>
    public DocumentCategory? Category { get; private set; }

    /// <summary>
    /// Kullanıcı ID - null ise tüm kullanıcılar görebilir (sistem dökümanı)
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Oluşturan kullanıcı
    /// </summary>
    public string? CreatedBy { get; private set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private DocumentDisplayInfo() { }

    /// <summary>
    /// Factory method — yeni döküman oluşturur.
    /// internal: Sadece DocumentCategory.AddDocument() üzerinden çağrılmalı.
    /// </summary>
    internal static DocumentDisplayInfo Create(
        string fileName,
        string displayName,
        DocumentType documentType = DocumentType.Document,
        string? description = null,
        string? keywords = null,
        string? categoryId = null,
        string? userId = null,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new DocumentDisplayInfo
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            DisplayName = displayName,
            DocumentType = documentType,
            Description = description,
            Keywords = keywords,
            CategoryId = categoryId ?? string.Empty,
            UserId = userId,
            CreatedBy = createdBy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Görüntüleme bilgilerini günceller
    /// </summary>
    public void Update(string displayName, string? description, string? keywords, string categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
        DisplayName = displayName;
        Description = description;
        Keywords = keywords;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Dökümanı deaktif eder
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Dökümanı aktif eder
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
