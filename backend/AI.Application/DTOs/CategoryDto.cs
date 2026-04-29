namespace AI.Application.DTOs;

/// <summary>
/// Kategori DTO'su
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// Kategori ID'si
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Kategori adı
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Kategori açıklaması
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Bu kategorideki doküman sayısı
    /// </summary>
    public int DocumentCount { get; set; }
    
    /// <summary>
    /// Kategori rengi (UI için)
    /// </summary>
    public string? Color { get; set; }
    
    /// <summary>
    /// Kategori ikonu (UI için)
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Kategori oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Kategori aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}