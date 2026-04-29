using AI.Application.Configuration;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ClientModel;

namespace AI.Scheduler.Extensions;

/// <summary>
/// LLM Provider extension methods for dependency injection
/// </summary>
public static class LLMExtensions
{
    /// <summary>
    /// Adds LLM Provider based on configuration (OpenAI or Azure)
    /// </summary>
    public static IServiceCollection AddLLMProvider(this IServiceCollection services, IConfiguration configuration)
    {
        // Get LLM Provider settings
        var llmSettings = configuration.GetSection("LLMProvider").Get<LLMSettings>() ?? new LLMSettings();

        // Configure LLMSettings for DI
        services.Configure<LLMSettings>(configuration.GetSection("LLMProvider"));

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

        return services;
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
}
