using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AgentCore;
using AI.Application.Ports.Secondary.Services.Database;
using AI.Application.UseCases;
using AI.Application.UseCases.ActionAgents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Application.Extensions;

/// <summary>
/// Application layer servisleri için dependency injection extension'ları
/// Primary port implementations (Use Cases) burada kayıt edilir
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Application layer servislerini ekler
    /// Primary port implementations (Use Cases) - Hexagonal Architecture
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Primary Port Implementations (Use Cases)
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IDocumentProcessingUseCase, DocumentProcessingUseCase>();
        services.AddScoped<IFeedbackAnalysisUseCase, FeedbackAnalysisUseCase>();
        services.AddScoped<IConversationUseCase, ConversationUseCase>();
        services.AddScoped<IRagSearchUseCase, RagSearchUseCase>();
        services.AddScoped<IScheduledReportUseCase, ScheduledReportUseCase>();
        services.AddScoped<IRouteConversationUseCase, RouteConversationUseCase>();
        services.AddScoped<IDashboardQueryUseCase, DashboardQueryUseCase>();
        services.AddScoped<IFeedbackUseCase, FeedbackUseCase>();
        services.AddScoped<IDocumentCategoryUseCase, DocumentCategoryUseCase>();
        services.AddScoped<IDocumentDisplayInfoUseCase, DocumentDisplayInfoUseCase>();

        // Primary Port Implementations (Use Cases — internally consumed by other use cases)
        services.AddScoped<IDashboardUseCase, DashboardUseCase>();
        services.AddScoped<IAIChatUseCase, AIChatUseCase>();
        services.AddScoped<IExcelAnalysisUseCase, ExcelAnalysisUseCase>();
        services.AddScoped<IContextSummarizationUseCase, ContextSummarizationUseCase>();
        services.AddScoped<IUserMemoryUseCase, UserMemoryUseCase>();
        services.AddScoped<IDocumentMetadataUseCase, DocumentMetadataUseCase>();
        services.AddScoped<IReportMetadataUseCase>(sp =>
            new ReportMetadataUseCase(
                sp.GetRequiredService<ILogger<ReportMetadataUseCase>>(),
                sp.GetKeyedService<IDatabaseService>("adventureworks")));
        // Note: IReranker and ISelfQueryExtractor are registered in InfrastructureExtensions
        // since their implementations (LLMReranker, SelfQueryExtractor) reside in Infrastructure layer

        // Action Agents — Strategy pattern ile RouteConversationUseCase'deki if/else zincirini ortadan kaldırır
        services.AddScoped<IActionAgent, ChatActionAgent>();
        services.AddScoped<IActionAgent, DocumentActionAgent>();
        services.AddScoped<IActionAgent, ReportActionAgent>();
        services.AddScoped<IActionAgent, AskActionAgent>();

        // Action Agent Registry — Merkezi agent keşif ve yönetim servisi
        services.AddScoped<IActionAgentRegistry, ActionAgentRegistry>();

        return services;
    }
}