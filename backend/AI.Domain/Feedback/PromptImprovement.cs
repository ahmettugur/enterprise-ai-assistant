using AI.Domain.Common;
using AI.Domain.Enums;

namespace AI.Domain.Feedback;

/// <summary>
/// Tracks AI-suggested prompt improvements and their implementation status
/// </summary>
public sealed class PromptImprovement : Entity<Guid>
{

    /// <summary>
    /// Reference to the analysis report that generated this suggestion
    /// </summary>
    public Guid AnalysisReportId { get; private set; }

    /// <summary>
    /// Category of the issue (e.g., "Yanlış Bilgi", "Format Sorunu")
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the identified issue
    /// </summary>
    public string Issue { get; private set; } = string.Empty;

    /// <summary>
    /// AI's suggested improvement
    /// </summary>
    public string Suggestion { get; private set; } = string.Empty;

    /// <summary>
    /// Priority level: High, Medium, Low
    /// </summary>
    public string Priority { get; private set; } = "Medium";

    /// <summary>
    /// Suggested modification to the system prompt
    /// </summary>
    public string PromptModification { get; private set; } = string.Empty;

    /// <summary>
    /// Current status of the improvement
    /// </summary>
    public PromptImprovementStatus Status { get; private set; } = PromptImprovementStatus.Pending;

    /// <summary>
    /// Notes from the person who reviewed/applied the improvement
    /// </summary>
    public string? ReviewNotes { get; private set; }

    /// <summary>
    /// User who applied/rejected the improvement
    /// </summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>
    /// When the improvement was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>
    /// When the suggestion was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Related analysis report
    /// </summary>
    public FeedbackAnalysisReport? AnalysisReport { get; private set; }

    private PromptImprovement() { }

    internal static PromptImprovement Create(
        Guid analysisReportId,
        string category,
        string issue,
        string suggestion,
        string priority,
        string promptModification)
    {
        if (analysisReportId == Guid.Empty)
            throw new ArgumentException("AnalysisReportId cannot be empty", nameof(analysisReportId));
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(issue);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);
        ArgumentException.ThrowIfNullOrWhiteSpace(priority);
        ArgumentException.ThrowIfNullOrWhiteSpace(promptModification);

        return new PromptImprovement
        {
            Id = Guid.NewGuid(),
            AnalysisReportId = analysisReportId,
            Category = category,
            Issue = issue,
            Suggestion = suggestion,
            Priority = priority,
            PromptModification = promptModification,
            Status = PromptImprovementStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Mark the improvement as applied
    /// </summary>
    public void Apply(string reviewedBy, string? notes = null)
    {
        Status = PromptImprovementStatus.Applied;
        ReviewedBy = reviewedBy;
        ReviewNotes = notes;
        ReviewedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark the improvement as rejected
    /// </summary>
    public void Reject(string reviewedBy, string? notes = null)
    {
        Status = PromptImprovementStatus.Rejected;
        ReviewedBy = reviewedBy;
        ReviewNotes = notes;
        ReviewedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark the improvement as under review
    /// </summary>
    public void StartReview(string reviewedBy)
    {
        Status = PromptImprovementStatus.UnderReview;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
    }
}

