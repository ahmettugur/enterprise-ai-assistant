namespace AI.Application.Configuration;

/// <summary>
/// Dashboard üretimi ayarları
/// </summary>
public class DashboardSettings
{
    /// <summary>
    /// Template-based hızlı dashboard kullanılsın mı?
    /// true: LLM sadece config üretir (~0.5-2 sn), template dosyaları kullanılır
    /// false: LLM tam HTML+CSS+JS üretir (~5-15 sn)
    /// </summary>
    public bool UseFastDashboard { get; set; } = false;
    
    /// <summary>
    /// Dashboard config üretim prompt dosyası adı
    /// </summary>
    public string ConfigPromptFileName { get; set; } = "dashboard_config_generator_prompt.md";
    
    /// <summary>
    /// Tam dashboard üretim prompt dosyası adı (legacy)
    /// </summary>
    public string FullPromptFileName { get; set; } = "dashboard_generator_prompt_2.md";
    
    /// <summary>
    /// Dashboard çıktı klasörü
    /// </summary>
    public string OutputFolder { get; set; } = "output-folder";
}
