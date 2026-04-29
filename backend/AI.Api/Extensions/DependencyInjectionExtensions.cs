using AI.Api.Configuration;
using AI.Application.Configuration;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.UseCases;
using AI.Infrastructure.Adapters.External.Caching;
using AI.Infrastructure.Extensions;
using AI.Infrastructure.Adapters.Persistence.Repositories;
using Microsoft.SemanticKernel;
using OpenAI;
using Serilog;
using System.ClientModel;
using AI.Infrastructure.Logging;
using AI.Application.Extensions;
using AI.Api.Hubs;
using AI.Api.Common;
using AI.Api.Endpoints.Auth;
using AI.Api.Endpoints.Common;
using AI.Api.Endpoints.Dashboard;
using AI.Api.Endpoints.Documents;
using AI.Api.Endpoints.Feedback;
using AI.Api.Endpoints.History;
using AI.Api.Endpoints.Reports;
using AI.Api.Endpoints.Search;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.Ports.Secondary.Services.Database;
using AI.Application.Ports.Secondary.Services.Document;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.Ports.Secondary.Services.Vector;
using AI.Infrastructure.Adapters.AI.Agents.SqlAgents;
using AI.Infrastructure.Adapters.AI.Common;
using AI.Infrastructure.Adapters.AI.Reports.SqlServer;
using AI.Infrastructure.Adapters.External.DatabaseServices.SqlServer;
using AI.Infrastructure.Adapters.AI.DocumentServices;
using AI.Infrastructure.Adapters.AI.ExcelServices;
using AI.Infrastructure.Adapters.AI.ReadyReports.AdventureWorks;
using AI.Infrastructure.Adapters.AI.VectorServices;

namespace AI.Api.Extensions;
public static class DependencyInjectionExtensions
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = LoggingHelper.CustomLoggerConfigurationConsole();
        services.AddOpenApi();

        // CORS Configuration
        services.AddCors(options =>
            options.AddDefaultPolicy(policy => policy.AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowed(s => true)));

        // SignalR Configuration
        services.AddSignalR();

        // Register SignalR Hub Context wrapper for Application layer
        services.AddSingleton<ISignalRHubContext, SignalRHubContextWrapper>();

        // Authentication & Authorization
        services.AddAuthenticationConfiguration(configuration);
        services.AddAuthenticationServices(configuration);

        // Application Layer Services (Primary Ports - Use Cases)
        services.AddApplicationServices();

        // Infrastructure Layer Services (Secondary Ports - Adapters)
        services.AddInfrastructureServices(configuration);

        // Feature-based registration
        AddChatFeatures(services, configuration);
        AddDashboardFeatures(services, configuration);
        AddReportFeatures(services, configuration);
        AddSchedulingFeatures(services, configuration);
        AddRagFeatures(services, configuration);
        AddLLMProvider(services, configuration);

        // Neo4j Schema Catalog
        services.AddNeo4jSchemaCatalog(configuration);
    }

    public static void UseAiApiEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(opt => opt.SwaggerEndpoint("/openapi/v1.json", "AI Api v1"));
        }

        // API Version endpoint
        app.MapApiVersionEndpoint();

        // Health check endpoints
        app.MapHealthCheckEndpoints();

        // Versioned endpoints (v1)
        app.MapCommonEndpoints();
        app.MapConversationEndpoints();
        app.MapDocumentEndpoints();
        app.MapDocumentCategoryEndpoints();
        app.MapDocumentDisplayInfoEndpoints();
        app.MapSearchEndpoints();
        app.MapHistoryEndpoints();
        app.MapAuthEndpoints();
        app.MapScheduledReportEndpoints();
        app.MapFeedbackEndpoints();
        app.MapDashboardEndpoints();

        // AdventureWorks Ready Reports endpoints
        app.MapAdventureWorksReportEndpoints();

        // Neo4j Schema Catalog endpoints
        app.MapNeo4JEndpoints();

        app.MapHub<AIHub>("ai-hub");
    }

    /// <summary>
    /// Registers chat-related services
    /// </summary>
    private static void AddChatFeatures(IServiceCollection services, IConfiguration configuration)
    {
        // Business logic services are now registered in Application layer
        // Only Infrastructure adapters are registered here

        // Document Cache Service (Redis + Memory)
        services.AddSingleton<IDocumentCacheService, DocumentCacheService>();

        // Note: IDocumentCategoryRepository, IUserMemoryRepository
        // are registered in InfrastructureExtensions.AddInfrastructureServices()
        // Note: IDocumentCategoryUseCase, IDocumentDisplayInfoUseCase
        // are registered in ApplicationExtensions.AddApplicationServices()

        // Excel Analysis Service (DuckDB)
        services.AddScoped<IExcelAnalysisService, DuckDbExcelService>();
    }

    /// <summary>
    /// Registers dashboard-related services
    /// </summary>
    private static void AddDashboardFeatures(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDashboardParser, DashboardResponseParser>();
        services.AddScoped<IFileSaver, DashboardFileSaver>();
        // Dashboard Services - MOVED TO APPLICATION LAYER
        // services.AddScoped<IDashboardUseCase, DashboardUseCase>();

        // Dashboard Settings
        var dashboardSettings = configuration.GetSection("Dashboard").Get<DashboardSettings>() ?? new DashboardSettings();
        services.AddSingleton(dashboardSettings);
    }

    /// <summary>
    /// Registers report and database services
    /// </summary>
    private static void AddReportFeatures(IServiceCollection services, IConfiguration configuration)
    {
        // RouteConversationUseCase - MOVED TO APPLICATION LAYER
        // services.AddScoped<IRouteConversationUseCase, RouteConversationUseCase>();

        // Multi-Agent Settings
        var multiAgentSettings = configuration.GetSection("MultiAgent").Get<MultiAgentSettings>() ?? new MultiAgentSettings();
        services.AddSingleton(multiAgentSettings);

        // ReAct Settings
        var reactSettings = configuration.GetSection("ReAct").Get<ReActSettings>() ?? new ReActSettings();
        services.AddSingleton(reactSettings);

        // ReAct UseCase (merkezi ReAct servisi)
        services.AddScoped<IReActUseCase, ReActUseCase>();

        // Insight Analysis Settings
        var insightAnalysisSettings = configuration.GetSection("InsightAnalysis").Get<InsightAnalysisSettings>() ?? new InsightAnalysisSettings();
        services.AddSingleton(insightAnalysisSettings);

        // SQL Agent Services (koşullu kayıt - ayarlara göre)
        if (multiAgentSettings.Enabled && multiAgentSettings.SqlAgents.Enabled)
        {
            services.AddScoped<ISqlValidationAgent, SqlValidationAgent>();
            services.AddScoped<ISqlOptimizationAgent, SqlOptimizationAgent>();
            services.AddScoped<ISqlAgentPipeline, SqlAgentPipeline>();
        }

        // HTTP Client for Ollama (if needed)
        services.AddHttpClient("OllamaClient", client =>
        {
            client.BaseAddress = new Uri("http://localhost:11434");
            client.Timeout = TimeSpan.FromMinutes(45);
        });

        //Sql Server AdventureWorks Report Service
        services.AddKeyedScoped<IReportService, AdventureWorksReportService>("adventureworks");

        // Sql Server DI Registrations
        services.AddKeyedSingleton<ISqlServerConnectionFactory>("adventureworks", (serviceProvider, key) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            return new SqlServerConnectionFactory(config, "AdventureWorks2022");
        });

        services.AddKeyedScoped<IDatabaseService>("adventureworks", (serviceProvider, key) =>
        {
            var connectionFactory = serviceProvider.GetRequiredKeyedService<ISqlServerConnectionFactory>("adventureworks");
            var logger = serviceProvider.GetRequiredService<ILogger<SqlServerDatabaseService>>();
            return new SqlServerDatabaseService(connectionFactory, logger);
        });

        services.AddScoped<IAdventureWorksReadyReportService, AdventureWorksReadyReportService>();


    }

    /// <summary>
    /// Registers RAG (Retrieval-Augmented Generation) services
    /// </summary>
    private static void AddRagFeatures(IServiceCollection services, IConfiguration configuration)
    {
        // Qdrant Settings
        var qdrantSettings = configuration.GetSection("Qdrant").Get<QdrantSettings>() ?? new QdrantSettings();
        services.AddSingleton(qdrantSettings);

        // Vector Services - Qdrant (Singleton - QdrantClient is thread-safe)
        services.AddSingleton<IQdrantService, QdrantService>();

        // Document Processing Services
        services.AddScoped<IDocumentParser, PdfDocumentParser>();
        services.AddScoped<IDocumentParser, TextDocumentParser>();
        services.AddScoped<ITextChunker, TextChunker>();
        services.AddScoped<IJsonQuestionAnswerParser, JsonQuestionAnswerParser>();
        services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
        // Sparse Vector Service (for BM25-style keyword matching)
        services.AddSingleton<ISparseVectorService, SparseVectorService>();

        // Document Processing Service - MOVED TO APPLICATION LAYER
        // services.AddScoped<IDocumentProcessingUseCase, DocumentProcessingUseCase>();

        // RAG Search Service - MOVED TO APPLICATION LAYER
        // services.AddScoped<IRagSearchUseCase, RagSearchUseCase>();

        // Advanced RAG Services - Business logic services moved to Application layer
        var advancedRagSettings = configuration.GetSection(AdvancedRagSettings.SectionName).Get<AdvancedRagSettings>() ?? new AdvancedRagSettings();
        services.AddSingleton(advancedRagSettings);
    }

    /// <summary>
    /// Adds LLM Provider based on configuration (OpenAI or Azure)
    /// </summary>
    private static void AddLLMProvider(IServiceCollection services, IConfiguration configuration)
    {
        // Get LLM Provider settings
        var llmSettings = configuration.GetSection("LLMProvider").Get<LLMSettings>() ?? new LLMSettings();

        // Get Qdrant settings for embedding model
        var qdrantSettings = configuration.GetSection("Qdrant").Get<QdrantSettings>() ?? new QdrantSettings();

        // Provider type'a göre karar ver
        switch (llmSettings.Type?.ToLower())
        {
            case "azure":
                AddAzureOpenAILlmModel(services, llmSettings, qdrantSettings);
                break;
            case "openai":
            default:
                AddOpenAILlmModel(services, llmSettings, qdrantSettings);
                break;
        }
    }

    /// <summary>
    /// Adds OpenAI LLM model
    /// </summary>
    private static void AddOpenAILlmModel(IServiceCollection services, LLMSettings llmSettings, QdrantSettings qdrantSettings)
    {
        var openAiSettings = llmSettings.OpenAI;

        // OpenAI SDK uses NetworkTimeout instead of HttpClient.Timeout
        // Dead HttpClient removed - SDK handles connections internally
        services
            .AddKernel()
            .AddOpenAIChatCompletion(
                modelId: openAiSettings.ChatModel,
                openAIClient: new OpenAIClient(
                    credential: new ApiKeyCredential(openAiSettings.ApiKey),
                    options: new OpenAIClientOptions
                    {
                        Endpoint = new Uri(openAiSettings.Endpoint),
                        NetworkTimeout = TimeSpan.FromMinutes(openAiSettings.TimeoutMinutes)
                    }))
            .AddOpenAIEmbeddingGenerator(
                modelId: qdrantSettings.EmbeddingModel,
                openAIClient: new OpenAIClient(
                    credential: new ApiKeyCredential(openAiSettings.ApiKey),
                    options: new OpenAIClientOptions
                    {
                        Endpoint = new Uri(openAiSettings.Endpoint),
                        NetworkTimeout = TimeSpan.FromMinutes(openAiSettings.TimeoutMinutes)
                    }));
    }

    /// <summary>
    /// Adds Azure OpenAI LLM model using IHttpClientFactory
    /// </summary>
    private static void AddAzureOpenAILlmModel(IServiceCollection services, LLMSettings llmSettings, QdrantSettings qdrantSettings)
    {
        var azureSettings = llmSettings.Azure;

        // Register named HttpClient with IHttpClientFactory for proper socket management
        services.AddHttpClient("AzureOpenAI", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(azureSettings.TimeoutMinutes);
        });

        // Build a temporary service provider to get IHttpClientFactory
        // This is needed because Semantic Kernel needs HttpClient at registration time
        services.AddKernel();

        services.AddSingleton(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("AzureOpenAI");
            return httpClient;
        });

        services.AddAzureOpenAIChatCompletion(
            deploymentName: azureSettings.ChatDeployment,
            endpoint: azureSettings.Endpoint,
            apiKey: azureSettings.ApiKey);

        services.AddAzureOpenAIEmbeddingGenerator(
            deploymentName: azureSettings.EmbeddingDeployment,
            endpoint: azureSettings.Endpoint,
            apiKey: azureSettings.ApiKey);
    }

    /// <summary>
    /// Registers scheduling-related services (Scheduled Reports)
    /// </summary>
    private static void AddSchedulingFeatures(IServiceCollection services, IConfiguration configuration)
    {
        // Note: IScheduledReportRepository is registered in InfrastructureExtensions.AddInfrastructureServices()
        // Scheduled Report Service - MOVED TO APPLICATION LAYER
        // services.AddScoped<IScheduledReportUseCase, ScheduledReportUseCase>();
    }
}