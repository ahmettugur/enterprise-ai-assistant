using AI.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AI.Application.DTOs;

/// <summary>
/// Döküman görüntüleme bilgisi DTO
/// </summary>
public record DocumentDisplayInfoDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Keywords { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? UserId { get; init; }
    public bool IsActive { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    
    /// <summary>
    /// Qdrant'ta collection var mı?
    /// </summary>
    public bool HasEmbeddings { get; init; }
    
    /// <summary>
    /// Qdrant'taki chunk sayısı
    /// </summary>
    public int ChunkCount { get; init; }
}

/// <summary>
/// Döküman oluşturma isteği (dosya yükleme + metadata)
/// </summary>
public record CreateDocumentDisplayInfoRequest
{
    public required string DisplayName { get; init; }
    public DocumentType DocumentType { get; init; } = DocumentType.Document;
    public string? Description { get; init; }
    public string? Keywords { get; init; }
    public string? CategoryId { get; init; }
}

/// <summary>
/// Döküman güncelleme isteği (sadece metadata)
/// </summary>
public record UpdateDocumentDisplayInfoRequest
{
    public required string DisplayName { get; init; }
    public DocumentType DocumentType { get; init; } = DocumentType.Document;
    public string? Description { get; init; }
    public string? Keywords { get; init; }
    public string? CategoryId { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Dosya yükleme isteği (form-data)
/// </summary>
public record DocumentDisplayInfoUploadRequest
{
    public required IFormFile File { get; init; }
    public required string DisplayName { get; init; }
    public DocumentType DocumentType { get; init; } = DocumentType.Document;
    public string? Description { get; init; }
    public string? Keywords { get; init; }
    public string? CategoryId { get; init; }
}

/// <summary>
/// Select2 dropdown için basit döküman DTO
/// </summary>
public record DocumentDisplayInfoSelectDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
}

/// <summary>
/// Döküman listesi için özet DTO
/// </summary>
public record DocumentDisplayInfoListDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? UserId { get; init; }
    public bool IsActive { get; init; }
    public bool HasEmbeddings { get; init; }
    public int ChunkCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
