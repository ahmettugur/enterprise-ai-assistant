namespace AI.Application.Configuration;

/// <summary>
/// LLM Provider konfigürasyon ayarları
/// </summary>
public class LLMSettings
{
    /// <summary>
    /// Provider tipi: OpenAI, Azure, vb
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// OpenAI ayarları
    /// </summary>
    public OpenAISettings OpenAI { get; set; } = null!;

    /// <summary>
    /// Azure OpenAI ayarları
    /// </summary>
    public AzureOpenAISettings Azure { get; set; } = null!;
}

/// <summary>
/// OpenAI spesifik ayarları
/// </summary>
public class OpenAISettings
{
    /// <summary>
    /// OpenAI API anahtarı
    /// </summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// Chat completion model ID (örn: gpt-4.1, gpt-3.5-turbo)
    /// </summary>
    public string ChatModel { get; set; } = null!;

    /// <summary>
    /// OpenAI API endpoint
    /// </summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>
    /// API request timeout (dakika)
    /// </summary>
    public int TimeoutMinutes { get; set; }
}

/// <summary>
/// Azure OpenAI spesifik ayarları
/// </summary>
public class AzureOpenAISettings
{
    /// <summary>
    /// Azure API anahtarı
    /// </summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// Azure OpenAI resource endpoint
    /// </summary>
    public string Endpoint { get; set; } = null!;

    /// <summary>
    /// Chat completion deployment name
    /// </summary>
    public string ChatDeployment { get; set; } = null!;

    /// <summary>
    /// Embedding deployment name
    /// </summary>
    public string EmbeddingDeployment { get; set; } = null!;

    /// <summary>
    /// API request timeout (dakika)
    /// </summary>
    public int TimeoutMinutes { get; set; }
}
