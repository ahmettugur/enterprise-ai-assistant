namespace AI.Application.DTOs.ReAct;

/// <summary>
/// ReAct (Reasoning + Acting) pattern için tek bir adımı temsil eder
/// Frontend'e SignalR ile gönderilir
/// </summary>
public record ReActStep
{
    /// <summary>
    /// Adım numarası (1, 2, 3...)
    /// </summary>
    public int StepNumber { get; init; }

    /// <summary>
    /// Adım tipi: "thought", "action", "observation"
    /// </summary>
    public string StepType { get; init; } = string.Empty;

    /// <summary>
    /// Adım içeriği - kullanıcıya gösterilecek metin
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Yapılan action (opsiyonel)
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Adımın oluşturulma zamanı
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
