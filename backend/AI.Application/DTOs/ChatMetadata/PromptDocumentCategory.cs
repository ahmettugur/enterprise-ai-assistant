namespace AI.Application.DTOs.ChatMetadata;

/// <summary>
/// Doküman kategorisi modeli (Prompt için)
/// </summary>
public class PromptDocumentCategory
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fas fa-folder";
    public bool IsActive { get; set; } = true;
}