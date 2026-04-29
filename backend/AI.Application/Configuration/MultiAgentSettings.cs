namespace AI.Application.Configuration;

/// <summary>
/// Multi-Agent yapılandırma ayarları
/// </summary>
public class MultiAgentSettings
{
    /// <summary>
    /// Multi-Agent sistemi aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SQL Agent ayarları
    /// </summary>
    public SqlAgentSettings SqlAgents { get; set; } = new();
}

/// <summary>
/// SQL Agent'lar için yapılandırma ayarları
/// </summary>
public class SqlAgentSettings
{
    /// <summary>
    /// SQL Agent'lar aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// SQL Validation aktif mi?
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// SQL Optimization aktif mi?
    /// </summary>
    public bool EnableOptimization { get; set; } = true;

    /// <summary>
    /// Güvenlik kontrolü aktif mi?
    /// </summary>
    public bool EnableSecurityCheck { get; set; } = true;

    /// <summary>
    /// Otomatik düzeltme aktif mi?
    /// </summary>
    public bool EnableAutoCorrection { get; set; } = true;

    /// <summary>
    /// Maksimum retry sayısı
    /// </summary>
    public int MaxRetries { get; set; } = 2;
}
