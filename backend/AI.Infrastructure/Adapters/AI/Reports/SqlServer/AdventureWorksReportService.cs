
using AI.Application.Configuration;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AIChat;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Infrastructure.Adapters.AI.Reports.SqlServer;

/// <summary>
/// AdventureWorks veritabanı için rapor üretim servisi
/// SqlServerReportServiceBase'den türetilmiştir ve OracleReportServiceBase ile aynı özelliklere sahiptir:
/// - Retry mekanizması (exponential backoff)
/// - SQL Agent Pipeline entegrasyonu
/// - Fast dashboard desteği
/// - Gelişmiş hata yönetimi
/// </summary>
public class AdventureWorksReportService : SqlServerReportServiceBase
{
    protected override string SystemPromptFileName => "adventurerworks_server_assistant_prompt.md";

    protected override string ReportServiceType => "adventureworks";

    public AdventureWorksReportService(
        [FromKeyedServices("adventureworks")] IDatabaseService databaseService,
        ISignalRHubContext hubContext,
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ILogger<AdventureWorksReportService> logger,
        IDashboardUseCase dashboardService,
        IConversationUseCase historyService,
        ISqlAgentPipeline? sqlAgentPipeline = null,
        MultiAgentSettings? multiAgentSettings = null,
        DashboardSettings? dashboardSettings = null,
        InsightAnalysisSettings? insightAnalysisSettings = null,
        IUserMemoryUseCase? userMemoryService = null,
        ICurrentUserService? currentUserService = null,
        IDynamicPromptBuilder? dynamicPromptBuilder = null,
        IReActUseCase? reactUseCase = null)
        : base(databaseService, hubContext, chatCompletionService, kernel, logger, dashboardService, historyService, sqlAgentPipeline, multiAgentSettings, dashboardSettings, insightAnalysisSettings, userMemoryService, currentUserService, dynamicPromptBuilder, reactUseCase)
    {
    }
}
