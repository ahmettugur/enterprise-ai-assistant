using AI.Domain.Enums;

namespace AI.Application.DTOs.DocumentProcessing;

/// <summary>
/// Upload request DTO — API katmanı bu DTO'yu gönderir,
/// DocumentMetadata entity oluşturma işlemi UseCase içinde yapılır.
/// </summary>
public class DocumentUploadDto
{
    public string FileName { get; set; } = null!;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; } = DocumentType.Document;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = "Genel";
    public string? UserId { get; set; }
    public string UploadedBy { get; set; } = "Anonim";
}

/// <summary>
/// Upload result DTO — entity yerine bu DTO döndürülür
/// </summary>
public class DocumentUploadResultDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public DateTime UploadedAt { get; set; }
    public int ProcessedChunks { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
