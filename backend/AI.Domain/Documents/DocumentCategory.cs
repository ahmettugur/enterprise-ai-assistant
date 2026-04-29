using AI.Domain.Common;
using AI.Domain.Enums;

namespace AI.Domain.Documents;

/// <summary>
/// Döküman kategorileri için entity
/// UI'da kategori seçimi ve gruplama için kullanılır
/// Aggregate Root — DocumentDisplayInfo bu aggregate'in child entity'sidir.
/// </summary>
public sealed class DocumentCategory : AggregateRoot<string>
{

    /// <summary>
    /// Ekranda görünen kategori adı
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Kategori açıklaması
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Kullanıcı ID - null ise tüm kullanıcılar görebilir (sistem kategorisi)
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Bu kategorideki dökümanlar (child entities)
    /// </summary>
    private readonly List<DocumentDisplayInfo> _documents = [];
    public IReadOnlyCollection<DocumentDisplayInfo> Documents => _documents.AsReadOnly();

    // EF Core constructor
    private DocumentCategory() { }

    /// <summary>
    /// Factory method - yeni kategori oluşturur
    /// </summary>
    public static DocumentCategory Create(string id, string displayName, string? description = null, string? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new DocumentCategory
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            UserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Kategori bilgilerini günceller
    /// </summary>
    public void Update(string displayName, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Kategoriyi deaktif eder
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Kategoriyi aktif eder
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Child entity management ──

    /// <summary>
    /// Bu kategoriye yeni bir döküman ekler
    /// </summary>
    public DocumentDisplayInfo AddDocument(
        string fileName,
        string displayName,
        DocumentType documentType = DocumentType.Document,
        string? description = null,
        string? keywords = null,
        string? userId = null,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var document = DocumentDisplayInfo.Create(
            fileName, displayName, documentType, description, keywords, Id, userId, createdBy);

        _documents.Add(document);
        UpdatedAt = DateTime.UtcNow;
        return document;
    }

    /// <summary>
    /// Kategoriden bir dökümanı kaldırır
    /// </summary>
    public bool RemoveDocument(Guid documentId)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId);
        if (document == null) return false;

        _documents.Remove(document);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Kategorideki bir dökümanı bulur
    /// </summary>
    public DocumentDisplayInfo? GetDocumentById(Guid documentId)
        => _documents.FirstOrDefault(d => d.Id == documentId);
}
