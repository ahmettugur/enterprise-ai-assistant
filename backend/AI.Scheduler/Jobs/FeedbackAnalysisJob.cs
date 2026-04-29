using AI.Application.Ports.Secondary.Scheduling;
using AI.Application.DTOs.FeedbackAnalysis;
using AI.Domain.Feedback;
using AI.Scheduler.Configuration;
using Hangfire;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using System.Text.Json;

namespace AI.Scheduler.Jobs;

/// <summary>
/// Daily job to analyze negative feedbacks and generate improvement suggestions
/// Runs every day at 2:00 AM
/// </summary>
public sealed class FeedbackAnalysisJob
{
    private readonly ISchedulerDataService _dataService;
    private readonly Kernel _kernel;
    private readonly ILogger<FeedbackAnalysisJob> _logger;
    private readonly ScheduledReportSettings _settings;

    public FeedbackAnalysisJob(
        ISchedulerDataService dataService,
        Kernel kernel,
        ILogger<FeedbackAnalysisJob> logger,
        IOptions<ScheduledReportSettings> settings)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Analyze pending negative feedbacks and generate improvement suggestions
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    [Queue("default")]
    public async Task AnalyzeFeedbacksAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting daily feedback analysis job...");

        try
        {
            // Get pending feedbacks via Application Port
            var feedbacks = await _dataService.GetFeedbacksPendingAnalysisAsync(50, cancellationToken);

            if (feedbacks.Count == 0)
            {
                _logger.LogInformation("No pending feedbacks to analyze");
                return;
            }

            _logger.LogInformation("Found {Count} feedbacks to analyze", feedbacks.Count);

            // Prepare feedback data
            var feedbackData = PrepareFeedbackData(feedbacks);

            // Call AI directly using Kernel
            _logger.LogInformation("Calling AI for analysis...");
            var analysisResult = await AnalyzeWithKernelAsync(feedbackData, cancellationToken);

            // Mark feedbacks as analyzed via Application Port
            var feedbackIds = feedbacks.Select(f => f.Id);
            await _dataService.MarkFeedbacksAsAnalyzedAsync(feedbackIds, cancellationToken);

            analysisResult.TotalFeedbacksAnalyzed = feedbacks.Count;

            // Save to database via Application Port
            var report = CreateReport(analysisResult, feedbacks);
            await _dataService.SaveAnalysisReportAsync(report, cancellationToken);

            _logger.LogInformation(
                "Feedback analysis completed successfully. Analyzed: {Count}, Categories: {CategoryCount}, Suggestions: {SuggestionCount}",
                feedbacks.Count,
                analysisResult.Categories.Count,
                analysisResult.Suggestions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during feedback analysis job");
            throw;
        }
    }

    private async Task<FeedbackAnalysisResult> AnalyzeWithKernelAsync(
        List<FeedbackDataItem> feedbackData,
        CancellationToken cancellationToken)
    {
        var prompt = BuildAnalysisPrompt(feedbackData);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.3f
        };

        var response = await _kernel.InvokePromptAsync(
            prompt,
            new KernelArguments(settings),
            cancellationToken: cancellationToken);

        return ParseAnalysisResponse(response.ToString());
    }

    private List<FeedbackDataItem> PrepareFeedbackData(List<MessageFeedback> feedbacks)
    {
        return feedbacks
            .Where(f => !string.IsNullOrEmpty(f.Comment) || !string.IsNullOrEmpty(f.MessageContent))
            .Select(f => new FeedbackDataItem
            {
                FeedbackId = f.Id.ToString(),
                UserComment = f.Comment ?? "",
                AiResponse = f.MessageContent ?? "",
                CreatedAt = f.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();
    }

    private string BuildAnalysisPrompt(List<FeedbackDataItem> feedbackData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            Sen bir yapay zeka kalite analisti ve geliştirme uzmanısın. 
            Kullanıcılardan gelen olumsuz geri bildirimleri analiz edip, 
            yapay zeka yanıtlarını iyileştirmek için somut öneriler sunuyorsun.
            
            Aşağıdaki olumsuz geri bildirimleri analiz et ve iyileştirme önerileri sun:
            """);
        sb.AppendLine();

        foreach (var item in feedbackData)
        {
            sb.AppendLine($"### Geri Bildirim ({item.CreatedAt})");
            if (!string.IsNullOrEmpty(item.UserComment))
            {
                sb.AppendLine($"**Kullanıcı Yorumu:** {item.UserComment}");
            }
            if (!string.IsNullOrEmpty(item.AiResponse))
            {
                var truncated = item.AiResponse.Length > 500 ? item.AiResponse[..500] + "..." : item.AiResponse;
                sb.AppendLine($"**AI Yanıtı:** {truncated}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("""
            Yanıtını aşağıdaki JSON formatında ver:
            ```json
            {
              "overallSummary": "Genel değerlendirme özeti",
              "categories": [
                {
                  "name": "Kategori Adı",
                  "description": "Kategori açıklaması",
                  "count": 5,
                  "percentage": 25.0,
                  "exampleComments": ["örnek yorum 1"]
                }
              ],
              "suggestions": [
                {
                  "category": "Kategori Adı",
                  "issue": "Tespit edilen sorun",
                  "suggestion": "İyileştirme önerisi",
                  "priority": "High|Medium|Low",
                  "promptModification": "Prompt değişiklik önerisi"
                }
              ]
            }
            ```
            """);

        return sb.ToString();
    }

    private FeedbackAnalysisResult ParseAnalysisResponse(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response[jsonStart..(jsonEnd + 1)];
                var data = JsonSerializer.Deserialize<AnalysisResponseDto>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data != null)
                {
                    return new FeedbackAnalysisResult
                    {
                        OverallSummary = data.OverallSummary ?? "",
                        Categories = data.Categories?.Select(c => new FeedbackCategory
                        {
                            Name = c.Name ?? "",
                            Description = c.Description ?? "",
                            Count = c.Count,
                            Percentage = c.Percentage,
                            ExampleComments = c.ExampleComments ?? []
                        }).ToList() ?? [],
                        Suggestions = data.Suggestions?.Select(s => new ImprovementSuggestion
                        {
                            Category = s.Category ?? "",
                            Issue = s.Issue ?? "",
                            Suggestion = s.Suggestion ?? "",
                            Priority = s.Priority ?? "Medium",
                            PromptModification = s.PromptModification ?? ""
                        }).ToList() ?? []
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI analysis response");
        }

        return new FeedbackAnalysisResult { OverallSummary = response };
    }

    private FeedbackAnalysisReport CreateReport(FeedbackAnalysisResult result, List<MessageFeedback> feedbacks)
    {
        var highCount = result.Suggestions.Count(s => s.Priority.Equals("High", StringComparison.OrdinalIgnoreCase));
        var mediumCount = result.Suggestions.Count(s => s.Priority.Equals("Medium", StringComparison.OrdinalIgnoreCase));
        var lowCount = result.Suggestions.Count(s => s.Priority.Equals("Low", StringComparison.OrdinalIgnoreCase));

        DateTime? periodStart = feedbacks.Count > 0 ? feedbacks.Min(f => f.CreatedAt) : null;
        DateTime? periodEnd = feedbacks.Count > 0 ? feedbacks.Max(f => f.CreatedAt) : null;

        return FeedbackAnalysisReport.Create(
            result.TotalFeedbacksAnalyzed,
            result.OverallSummary,
            JsonSerializer.Serialize(result.Categories),
            JsonSerializer.Serialize(result.Suggestions),
            highCount, mediumCount, lowCount,
            periodStart, periodEnd);
    }

    /// <summary>
    /// Register the recurring job with Hangfire
    /// </summary>
    public static void RegisterRecurringJob(IRecurringJobManager recurringJobManager)
    {
        recurringJobManager.AddOrUpdate<FeedbackAnalysisJob>(
            "feedback-analysis",
            job => job.AnalyzeFeedbacksAsync(CancellationToken.None),
            "0 2 * * *",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")
            });
    }

    #region DTOs
    private class FeedbackDataItem
    {
        public string FeedbackId { get; set; } = "";
        public string UserComment { get; set; } = "";
        public string AiResponse { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    private class AnalysisResponseDto
    {
        public string? OverallSummary { get; set; }
        public List<CategoryDto>? Categories { get; set; }
        public List<SuggestionDto>? Suggestions { get; set; }
    }

    private class CategoryDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public List<string>? ExampleComments { get; set; }
    }

    private class SuggestionDto
    {
        public string? Category { get; set; }
        public string? Issue { get; set; }
        public string? Suggestion { get; set; }
        public string? Priority { get; set; }
        public string? PromptModification { get; set; }
    }
    #endregion
}
