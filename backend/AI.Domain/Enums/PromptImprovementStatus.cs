using System.Text.Json.Serialization;

namespace AI.Domain.Enums;

/// <summary>
/// Status of a prompt improvement suggestion
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PromptImprovementStatus
{
    /// <summary>
    /// Waiting for review
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Currently being reviewed
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// Applied to the system
    /// </summary>
    Applied = 2,

    /// <summary>
    /// Rejected - not applicable
    /// </summary>
    Rejected = 3
}
