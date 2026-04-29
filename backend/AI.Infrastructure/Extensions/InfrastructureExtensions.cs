using AI.Application.Ports.Secondary.Notifications;
using AI.Domain.Common;
using AI.Domain.Documents;
using AI.Domain.Feedback;
using AI.Domain.Memory;
using AI.Domain.Scheduling;
using AI.Application.Ports.Secondary.Scheduling;
using AI.Application.Ports.Secondary.Services.Document;
using AI.Infrastructure.Adapters.External.Notifications;
using AI.Infrastructure.Adapters.Persistence;
using AI.Infrastructure.Adapters.Persistence.Repositories;
using AI.Infrastructure.Adapters.External.Scheduling;
using AI.Infrastructure.Adapters.AI.DocumentServices;
using AI.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Infrastructure.Extensions;

/// <summary>
/// Infrastructure servisleri için dependency injection extension'ları
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Infrastructure servislerini ekler
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Note: ChatDbContext is registered by ConversationServiceExtensions.AddPersistentChatHistory()
        // using AddPooledDbContextFactory for better performance and background task support.
        // Do NOT register DbContext here to avoid conflicts.

        // Configuration
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<TeamsSettings>(configuration.GetSection(TeamsSettings.SectionName));

        // Domain Event Infrastructure
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Repositories
        services.AddScoped<IScheduledReportRepository, ScheduledReportRepository>();
        // Note: IConversationRepository is registered in ConversationServiceExtensions
        // services.AddScoped<IConversationRepository, PostgreSqlConversationRepository>();
        // Note: IMessageFeedbackRepository is registered in ConversationServiceExtensions
        // services.AddScoped<IMessageFeedbackRepository, MessageFeedbackRepository>();
        services.AddScoped<IFeedbackAnalysisReportRepository, FeedbackAnalysisReportRepository>();
        services.AddScoped<IDocumentCategoryRepository, DocumentCategoryRepository>();
        services.AddScoped<IUserMemoryRepository, UserMemoryRepository>();

        // Scheduling Data Services (for reading schedule data only)
        services.AddScoped<ISchedulerDataService, SchedulerDataService>();

        // AI Services - Secondary Adapters (implementations for Application layer interfaces)
        services.AddScoped<Application.Ports.Secondary.Services.AIChat.IReranker, Adapters.AI.Reranking.LLMReranker>();
        services.AddScoped<Application.Ports.Secondary.Services.AIChat.ISelfQueryExtractor, Adapters.AI.SelfQuery.SelfQueryExtractor>();

        // Notification Services
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddHttpClient<ITeamsNotificationService, TeamsNotificationService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Document Processing
        services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

        return services;
    }
}
