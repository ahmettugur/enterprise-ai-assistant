namespace AI.Application.DTOs.ChatMetadata;

/// <summary>
/// Prompt için doküman bilgisi modeli
/// </summary>
public class PromptDocumentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public Domain.Enums.DocumentType DocumentType { get; set; } = Domain.Enums.DocumentType.Document;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}