using AI.Application.Configuration;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Feedback;
using AI.Application.DTOs.FeedbackAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using System.Text.Json;

namespace AI.Application.UseCases;

/// <summary>
/// Service that analyzes negative feedbacks using AI to generate improvement suggestions
/// </summary>
public class FeedbackAnalysisUseCase : IFeedbackAnalysisUseCase
{
    private readonly IMessageFeedbackRepository _feedbackRepository;
    private readonly IFeedbackAnalysisReportRepository _reportRepository;
    private readonly Kernel _kernel;
    private readonly ILogger<FeedbackAnalysisUseCase> _logger;
    private readonly LLMSettings _llmSettings;

    public FeedbackAnalysisUseCase(
        IMessageFeedbackRepository feedbackRepository,
        IFeedbackAnalysisReportRepository reportRepository,
        Kernel kernel,
        IOptions<LLMSettings> llmSettings,
        ILogger<FeedbackAnalysisUseCase> logger)
    {
        _feedbackRepository = feedbackRepository;
        _reportRepository = reportRepository;
        _kernel = kernel;
        _llmSettings = llmSettings.Value;
        _logger = logger;
    }

    public async Task<FeedbackAnalysisResult> AnalyzePendingFeedbacksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting feedback analysis...");

        try
        {
            // Get pending feedbacks (not yet analyzed)
            var feedbacks = await _feedbackRepository.GetFeedbacksPendingAnalysisAsync(50, cancellationToken);

            if (feedbacks.Count == 0)
            {
                _logger.LogInformation("No pending feedbacks to analyze");
                return new FeedbackAnalysisResult
                {
                    TotalFeedbacksAnalyzed = 0,
                    OverallSummary = "Analiz edilecek yeni olumsuz geri bildirim bulunmamaktadır."
                };
            }

            // Prepare feedback data for analysis
            var feedbackData = PrepareFeedbackData(feedbacks);

            // Call AI to analyze feedbacks
            var analysisResult = await AnalyzeWithAIAsync(feedbackData, cancellationToken);

            // Mark feedbacks as analyzed
            var feedbackIds = feedbacks.Select(f => f.Id);
            await _feedbackRepository.MarkAsAnalyzedAsync(feedbackIds, cancellationToken);

            analysisResult.TotalFeedbacksAnalyzed = feedbacks.Count;

            // Save analysis result to database
            var report = SaveAnalysisToDatabase(analysisResult, feedbacks);
            await _reportRepository.AddAsync(report, cancellationToken);

            _logger.LogInformation("Feedback analysis completed and saved. Analyzed {Count} feedbacks, generated {SuggestionCount} suggestions",
                feedbacks.Count, analysisResult.Suggestions.Count);

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during feedback analysis");
            throw;
        }
    }

    public async Task<FeedbackAnalysisResult?> GetLatestAnalysisAsync(CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetLatestAsync(cancellationToken);

        if (report == null)
        {
            return null;
        }

        return ConvertToAnalysisResult(report);
    }

    public async Task<List<FeedbackAnalysisResult>> GetAnalysisHistoryAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.GetAllAsync(limit, cancellationToken);
        return reports.Select(ConvertToAnalysisResult).ToList();
    }

    private FeedbackAnalysisReport SaveAnalysisToDatabase(FeedbackAnalysisResult result, List<MessageFeedback> feedbacks)
    {
        // Calculate priority counts
        var highPriorityCount = result.Suggestions.Count(s => s.Priority.Equals("High", StringComparison.OrdinalIgnoreCase));
        var mediumPriorityCount = result.Suggestions.Count(s => s.Priority.Equals("Medium", StringComparison.OrdinalIgnoreCase));
        var lowPriorityCount = result.Suggestions.Count(s => s.Priority.Equals("Low", StringComparison.OrdinalIgnoreCase));

        // Determine period
        DateTime? periodStart = feedbacks.Count > 0 ? feedbacks.Min(f => f.CreatedAt) : null;
        DateTime? periodEnd = feedbacks.Count > 0 ? feedbacks.Max(f => f.CreatedAt) : null;

        // Serialize categories and suggestions to JSON
        var categoriesJson = JsonSerializer.Serialize(result.Categories);
        var suggestionsJson = JsonSerializer.Serialize(result.Suggestions);

        return FeedbackAnalysisReport.Create(
            result.TotalFeedbacksAnalyzed,
            result.OverallSummary,
            categoriesJson,
            suggestionsJson,
            highPriorityCount,
            mediumPriorityCount,
            lowPriorityCount,
            periodStart,
            periodEnd
        );
    }

    private FeedbackAnalysisResult ConvertToAnalysisResult(FeedbackAnalysisReport report)
    {
        var categories = new List<FeedbackCategory>();
        var suggestions = new List<ImprovementSuggestion>();

        try
        {
            categories = JsonSerializer.Deserialize<List<FeedbackCategory>>(
                report.CategoriesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FeedbackCategory>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize categories from report {ReportId}", report.Id);
        }

        try
        {
            suggestions = JsonSerializer.Deserialize<List<ImprovementSuggestion>>(
                report.SuggestionsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ImprovementSuggestion>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize suggestions from report {ReportId}", report.Id);
        }

        return new FeedbackAnalysisResult
        {
            Id = report.Id,
            AnalyzedAt = report.AnalyzedAt,
            TotalFeedbacksAnalyzed = report.TotalFeedbacksAnalyzed,
            OverallSummary = report.OverallSummary,
            Categories = categories,
            Suggestions = suggestions
        };
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

    private async Task<FeedbackAnalysisResult> AnalyzeWithAIAsync(List<FeedbackDataItem> feedbackData, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting AI analysis with Kernel...");

            // Basit test prompt
            var testPrompt = "Merhaba, nasılsın?";

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3f
            };

            _logger.LogInformation("Calling Kernel.InvokePromptAsync...");

            var response = await _kernel.InvokePromptAsync(
                testPrompt,
                new KernelArguments(settings),
                cancellationToken: cancellationToken);

            _logger.LogInformation("AI Response received: {Response}", response.ToString());

            // Gerçek analiz için bu satırları aktif edin
            // var prompt = BuildAnalysisPrompt(feedbackData);
            // var systemPrompt = GetSystemPrompt();
            // var fullPrompt = $"{systemPrompt}\n\n{prompt}";

            return new FeedbackAnalysisResult
            {
                OverallSummary = response.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalyzeWithAIAsync. Exception Type: {Type}, Message: {Message}",
                ex.GetType().Name, ex.Message);

            if (ex.InnerException != null)
            {
                _logger.LogError("Inner Exception: {Type} - {Message}",
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }

            throw;
        }
    }

    private string GetSystemPrompt()
    {
        return """
            Sen bir yapay zeka kalite analisti ve geliştirme uzmanısın. 
            Kullanıcılardan gelen olumsuz geri bildirimleri analiz edip, 
            yapay zeka yanıtlarını iyileştirmek için somut öneriler sunuyorsun.
            
            Görevlerin:
            1. Geri bildirimleri kategorilere ayır (örn: yanlış bilgi, eksik yanıt, anlama hatası, format sorunu)
            2. Her kategori için ortak sorunları tespit et
            3. Her sorun için somut iyileştirme önerileri sun
            4. Prompt'larda yapılabilecek değişiklikleri öner
            
            Yanıtını JSON formatında ver.
            """;
    }

    private string BuildAnalysisPrompt(List<FeedbackDataItem> feedbackData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Aşağıdaki olumsuz geri bildirimleri analiz et ve iyileştirme önerileri sun:");
        sb.AppendLine();
        sb.AppendLine("## Olumsuz Geri Bildirimler:");
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
                // Truncate long responses
                var truncatedResponse = item.AiResponse.Length > 500
                    ? item.AiResponse[..500] + "..."
                    : item.AiResponse;
                sb.AppendLine($"**AI Yanıtı:** {truncatedResponse}");
            }
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("""
            ## İstenen Analiz Formatı (JSON):
            ```json
            {
              "overallSummary": "Genel değerlendirme özeti",
              "categories": [
                {
                  "name": "Kategori Adı",
                  "description": "Kategori açıklaması",
                  "count": 5,
                  "percentage": 25.0,
                  "exampleComments": ["örnek yorum 1", "örnek yorum 2"]
                }
              ],
              "suggestions": [
                {
                  "category": "Kategori Adı",
                  "issue": "Tespit edilen sorun",
                  "suggestion": "İyileştirme önerisi",
                  "priority": "High|Medium|Low",
                  "promptModification": "System prompt'ta yapılması gereken değişiklik önerisi"
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
            // Extract JSON from response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response[jsonStart..(jsonEnd + 1)];
                var analysisData = JsonSerializer.Deserialize<AnalysisResponseDto>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (analysisData != null)
                {
                    return new FeedbackAnalysisResult
                    {
                        OverallSummary = analysisData.OverallSummary ?? "",
                        Categories = analysisData.Categories?.Select(c => new FeedbackCategory
                        {
                            Name = c.Name ?? "",
                            Description = c.Description ?? "",
                            Count = c.Count,
                            Percentage = c.Percentage,
                            ExampleComments = c.ExampleComments ?? new List<string>()
                        }).ToList() ?? new List<FeedbackCategory>(),
                        Suggestions = analysisData.Suggestions?.Select(s => new ImprovementSuggestion
                        {
                            Category = s.Category ?? "",
                            Issue = s.Issue ?? "",
                            Suggestion = s.Suggestion ?? "",
                            Priority = s.Priority ?? "Medium",
                            PromptModification = s.PromptModification ?? ""
                        }).ToList() ?? new List<ImprovementSuggestion>()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI analysis response as JSON");
        }

        // Fallback: return raw response as summary
        return new FeedbackAnalysisResult
        {
            OverallSummary = response
        };
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