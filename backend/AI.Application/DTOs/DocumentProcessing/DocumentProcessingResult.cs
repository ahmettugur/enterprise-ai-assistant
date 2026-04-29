using AI.Domain.Enums;

namespace AI.Application.DTOs.DocumentProcessing;

/// <summary>
/// Doküman işleme sonucu
/// </summary>
public class DocumentProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProcessedChunks { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public long ExtractedTextLength { get; set; }
    public DocumentProcessingStatus Status { get; set; }
}