namespace AI.Application.DTOs.Chat;

/// <summary>
/// Chat isteği için DTO
/// </summary>
public record ChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string FileBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;  // Dosya adı (system prompt'ta kullanılacak)
}
