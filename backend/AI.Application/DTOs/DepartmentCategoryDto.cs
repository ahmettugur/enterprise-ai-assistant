namespace AI.Application.DTOs;

/// <summary>
/// Departman/Kategori DTO'su (CRM_MIY_GROUP tablosundan)
/// </summary>
public class DepartmentCategoryDto
{
    /// <summary>
    /// Kategori ID'si (GROUP_NO)
    /// </summary>
    public string? CategoryId { get; set; }
    
    /// <summary>
    /// Kategori adı (GROUP_NAME)
    /// </summary>
    public string? CategoryName { get; set; }
}
