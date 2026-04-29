using System.ComponentModel.DataAnnotations;

namespace AI.Application.DTOs;

/// <summary>
/// DTO for updating conversation title
/// </summary>
public class UpdateConversationTitleDto
{
    /// <summary>
    /// Conversation ID to update
    /// </summary>
    [Required(ErrorMessage = "Conversation ID is required.")]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// New title for the conversation
    /// </summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;
}