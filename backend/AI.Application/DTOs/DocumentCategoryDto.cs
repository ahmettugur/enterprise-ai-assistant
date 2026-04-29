namespace AI.Application.DTOs;

/// <summary>
/// Döküman kategorisi DTO
/// </summary>
public record DocumentCategoryDto
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? UserId { get; init; }
    public bool IsActive { get; init; }
    public int DocumentCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Kategori oluşturma isteği
/// </summary>
public record CreateDocumentCategoryRequest
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Kategori güncelleme isteği
/// </summary>
public record UpdateDocumentCategoryRequest
{
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? UserId { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Select2 dropdown için basit kategori DTO
/// </summary>
public record DocumentCategorySelectDto
{
    public string Id { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string? Description { get; init; }
}
