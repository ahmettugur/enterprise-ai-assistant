using AI.Infrastructure.Adapters.External.Caching;
using AI.Infrastructure.Adapters.Persistence;
using AI.Infrastructure.Adapters.Persistence.Repositories;
using AI.Application.Configuration;
using AI.Application.DTOs.History;
using AI.Application.Ports.Secondary.Services.Cache;
using AI.Application.Ports.Secondary.Services.Query;
using Microsoft.EntityFrameworkCore;

namespace AI.Api.Extensions;

/// <summary>
/// Extension methods for configuring chat history services
/// </summary>
public static class ConversationServiceExtensions
{
    /// <summary>
    /// Adds chat history services with InMemory storage (no persistence)
    /// Best for: Development, testing, single-instance scenarios
    /// </summary>
    public static IServiceCollection AddInMemoryChatHistory(this IServiceCollection services, IConfiguration configuration)
    {
        // Register CacheSettings from configuration
        services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));

        // Register InMemory repository
        services.AddSingleton<InMemoryConversationRepository>();
        services.AddSingleton<AI.Domain.Conversations.IConversationRepository>(sp => sp.GetRequiredService<InMemoryConversationRepository>());
        services.AddSingleton<IConversationQueryService, InMemoryConversationQueryService>();

        // Register null cache service (no caching needed for in-memory)
        services.AddSingleton<IChatCacheService, NullCacheService>();

        // Register orchestration service - MOVED TO APPLICATION LAYER
        // services.AddScoped<IConversationUseCase, ConversationUseCase>();

        // Add memory cache for L1
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Adds chat history services with PostgreSQL persistence and optional Redis caching
    /// Best for: Production, multi-instance scenarios
    /// </summary>
    public static IServiceCollection AddPersistentChatHistory(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useRedisCache = true)
    {
        // Register CacheSettings from configuration
        services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));

        // Validate configuration
        var connectionString = configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL connection string not found. Please add 'ConnectionStrings:PostgreSQL' to appsettings.json");
        }

        // Add PostgreSQL DbContext with Pooled Factory support for background tasks
        // AddPooledDbContextFactory registers both IDbContextFactory<T> and DbContext
        services.AddPooledDbContextFactory<ChatDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Connection resilience
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Command timeout
                npgsqlOptions.CommandTimeout(30);

                // Migrations assembly
                npgsqlOptions.MigrationsAssembly(typeof(ChatDbContext).Assembly.FullName);
            });

            // Enable sensitive data logging in development only
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging", false))
            {
                options.EnableSensitiveDataLogging();
            }

            // Use no-tracking by default for better performance
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Register scoped DbContext from the factory for normal request-scoped usage
        // IServiceProvider injected for domain event dispatch support
        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ChatDbContext>>();
            var dbContext = factory.CreateDbContext();
            // Inject service provider for domain event dispatch
            dbContext.SetServiceProvider(sp);
            return dbContext;
        });

        // Register PostgreSQL repository (Command) + query service (Query) — CQRS
        services.AddScoped<PostgreSqlConversationRepository>();
        services.AddScoped<AI.Domain.Conversations.IConversationRepository>(sp => sp.GetRequiredService<PostgreSqlConversationRepository>());
        services.AddScoped<IConversationQueryService, ConversationQueryService>();

        // Register MessageFeedback repository (Command) + query service (Query) — CQRS
        services.AddScoped<AI.Domain.Feedback.IMessageFeedbackRepository, MessageFeedbackRepository>();
        services.AddScoped<IFeedbackQueryService, FeedbackQueryService>();

        // Register FeedbackAnalysisReport repository
        services.AddScoped<AI.Domain.Feedback.IFeedbackAnalysisReportRepository, FeedbackAnalysisReportRepository>();

        // Register PromptImprovement query service (Query) — CQRS
        // Command ops are handled via IFeedbackAnalysisReportRepository (aggregate root)
        services.AddScoped<IPromptImprovementQueryService, PromptImprovementQueryService>();

        // Configure caching
        if (useRedisCache)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis connection string not found. Please add 'ConnectionStrings:Redis' to appsettings.json or set useRedisCache to false");
            }

            // Add Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = configuration["Redis:InstanceName"] ?? "ChatHistory_";
            });

            // Add Redis cache service
            services.AddSingleton<IChatCacheService, RedisCacheService>();
        }
        else
        {
            // Use in-memory distributed cache (not recommended for production multi-instance)
            services.AddDistributedMemoryCache();
            services.AddSingleton<IChatCacheService, RedisCacheService>();
        }

        // Add memory cache for L1
        services.AddMemoryCache();

        // Register orchestration service - MOVED TO APPLICATION LAYER
        // services.AddScoped<IConversationUseCase, ConversationUseCase>();

        // Not: Health check'ler HealthChecksExtensions.cs dosyasında merkezi olarak yönetilmektedir.
        // Çakışmayı önlemek için burada health check eklenmemiştir.

        return services;
    }

    /// <summary>
    /// Ensures database is created and migrations are applied
    /// Should only be called for persistent storage mode
    /// </summary>
    public static async Task<IApplicationBuilder> UseChatHistoryDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ChatDbContext>();

        if (dbContext == null)
        {
            throw new InvalidOperationException(
                "ChatDbContext not found. Make sure you called AddPersistentChatHistory() before calling this method.");
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ChatDbContext>>();

        try
        {
            // Check if database exists
            var canConnect = await dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                logger.LogError("Cannot connect to database. Please check your connection string and ensure PostgreSQL is running.");
                throw new InvalidOperationException("Database connection failed");
            }

            // Get pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations...", pendingList.Count);

                foreach (var migration in pendingList)
                {
                    logger.LogInformation("  - {Migration}", migration);
                }

                await dbContext.Database.MigrateAsync();
                logger.LogInformation("✅ Database migrations applied successfully");
            }
            else
            {
                logger.LogInformation("✅ Database is up to date (no pending migrations)");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error applying database migrations");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Configures chat history services based on ChatHistorySettings
    /// </summary>
    public static IServiceCollection AddChatConversationUseCases(this IServiceCollection services, IConfiguration configuration)
    {
        var chatHistorySettings = new ChatHistorySettings();
        configuration.GetSection("ChatHistory").Bind(chatHistorySettings);
        services.Configure<ChatHistorySettings>(configuration.GetSection("ChatHistory"));

        // Context Summarization ayarlarını ekle
        services.Configure<ContextSummarizationSettings>(configuration.GetSection("ContextSummarization"));
        // ContextSummarizationUseCase is now registered in Application layer

        switch (chatHistorySettings.StorageMode?.ToLowerInvariant())
        {
            case "inmemory":
                services.AddInMemoryChatHistory(configuration);
                break;

            case "postgresql":
                services.AddPersistentChatHistory(configuration, useRedisCache: false);
                break;

            case "postgresqlwithredis":
            case "persistent":
                services.AddPersistentChatHistory(configuration, useRedisCache: true);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported ChatHistory StorageMode: '{chatHistorySettings.StorageMode}'. " +
                    "Supported values: 'InMemory', 'PostgreSQL', 'PostgreSQLWithRedis'");
        }

        return services;
    }
}