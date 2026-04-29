using AI.Domain.Common;
using AI.Domain.Enums;

namespace AI.Domain.Documents;

/// <summary>
/// Yüklenen dokümanların metadata bilgilerini tutan entity
/// Not: Data Annotations korunuyor çünkü bu entity EF Core Fluent API ile değil,
/// farklı bir persistence mekanizması ile kullanılabilir
/// </summary>
public sealed class DocumentMetadata : Entity<Guid>
{

    /// <summary>
    /// Dosyanın orijinal adı
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Dosya türü (PDF, TXT, DOCX vb.)
    /// </summary>
    public string FileType { get; private set; } = string.Empty;

    /// <summary>
    /// Döküman tipi (Document veya QuestionAnswer)
    /// </summary>
    public DocumentType DocumentType { get; private set; } = DocumentType.Document;

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// Dosyanın hash değeri (duplicate kontrolü için)
    /// </summary>
    public string FileHash { get; private set; } = string.Empty;

    /// <summary>
    /// Dosyanın fiziksel yolu
    /// </summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>
    /// Doküman başlığı
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Doküman açıklaması
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Doküman kategorisi/etiketi
    /// </summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Dokümanın yüklenme tarihi
    /// </summary>
    public DateTime UploadedAt { get; private set; }

    /// <summary>
    /// Dokümanın işlenme tarihi
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// İşlenme durumu
    /// </summary>
    public DocumentProcessingStatus Status { get; private set; } = DocumentProcessingStatus.Pending;

    /// <summary>
    /// Toplam chunk sayısı
    /// </summary>
    public int TotalChunks { get; private set; }

    /// <summary>
    /// İşlenme sırasında oluşan hata mesajı
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Dokümanın dili
    /// </summary>
    public string Language { get; private set; } = "tr";

    /// <summary>
    /// Dokümanı yükleyen kullanıcının ID'si
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Dokümanın yükleyicisi
    /// </summary>
    public string? UploadedBy { get; private set; }

    /// <summary>
    /// Qdrant collection adı
    /// </summary>
    public string? QdrantCollection { get; private set; }

    // EF Core / deserialization constructor
    private DocumentMetadata() { }

    /// <summary>
    /// Factory method - yeni doküman metadata bilgisi oluşturur
    /// </summary>
    public static DocumentMetadata Create(
        string fileName,
        string fileType,
        long fileSize,
        string fileHash,
        string filePath,
        DocumentType documentType = DocumentType.Document,
        string? title = null,
        string? description = null,
        string? category = null,
        string language = "tr",
        string? userId = null,
        string? uploadedBy = null,
        string? qdrantCollection = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        // filePath can be empty initially and set later after file save

        return new DocumentMetadata
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            FileType = fileType,
            FileSize = fileSize,
            FileHash = fileHash,
            FilePath = filePath,
            DocumentType = documentType,
            Title = title,
            Description = description,
            Category = category,
            UploadedAt = DateTime.UtcNow,
            Status = DocumentProcessingStatus.Pending,
            Language = language,
            UserId = userId,
            UploadedBy = uploadedBy,
            QdrantCollection = qdrantCollection
        };
    }

    // Navigation property - encapsulated collection
    private readonly List<DocumentChunk> _chunks = [];
    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks.AsReadOnly();

    /// <summary>
    /// Aggregate Root üzerinden yeni chunk oluşturur ve koleksiyona ekler.
    /// DDD invariant koruması: DocumentChunk sadece bu metod ile oluşturulabilir.
    /// </summary>
    public DocumentChunk AddChunk(
        int chunkIndex,
        string content,
        int startPosition,
        int endPosition,
        string? metadata = null)
    {
        var chunk = DocumentChunk.Create(Id, chunkIndex, content, startPosition, endPosition, metadata);
        _chunks.Add(chunk);
        TotalChunks = _chunks.Count;
        return chunk;
    }

    /// <summary>
    /// İşlenme başarılı sonuçlandığında günceller
    /// </summary>
    public void MarkAsCompleted(int totalChunks)
    {
        Status = DocumentProcessingStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        TotalChunks = totalChunks;
        ErrorMessage = null;
    }

    /// <summary>
    /// İşlenme başarısız olduğunda günceller
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = DocumentProcessingStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Dosya kaydedildikten sonra yolu ayarlar
    /// </summary>
    public void SetFilePath(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Qdrant collection adını ayarlar
    /// </summary>
    public void SetQdrantCollection(string collectionName)
    {
        QdrantCollection = collectionName;
    }

    /// <summary>
    /// Dosya adını günceller (encoding düzeltmesi için)
    /// </summary>
    public void FixFileName(string fixedFileName)
    {
        FileName = fixedFileName;
    }

    /// <summary>
    /// Encoding düzeltmesi için string alanları günceller
    /// </summary>
    public void FixEncodings(
        string? fixedFileName = null,
        string? fixedTitle = null,
        string? fixedDescription = null,
        string? fixedCategory = null,
        string? fixedUploadedBy = null,
        string? fixedErrorMessage = null)
    {
        if (fixedFileName != null) FileName = fixedFileName;
        if (fixedTitle != null) Title = fixedTitle;
        if (fixedDescription != null) Description = fixedDescription;
        if (fixedCategory != null) Category = fixedCategory;
        if (fixedUploadedBy != null) UploadedBy = fixedUploadedBy;
        if (fixedErrorMessage != null) ErrorMessage = fixedErrorMessage;
    }

    /// <summary>
    /// İşlenme başladığında günceller
    /// </summary>
    public void MarkAsProcessing()
    {
        Status = DocumentProcessingStatus.Processing;
    }
}