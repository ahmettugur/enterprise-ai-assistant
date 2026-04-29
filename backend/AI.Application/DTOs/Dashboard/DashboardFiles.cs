namespace AI.Application.DTOs.Dashboard;

/// <summary>
/// Dashboard dosyaları için DTO
/// </summary>
public class DashboardFiles
{
    public string HtmlContent { get; set; } = string.Empty;
    public string CssContent { get; set; } = string.Empty;
    public Dictionary<string, string> JsFiles { get; set; } = new Dictionary<string, string>();
    public string UniqId { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    
    /// <summary>
    /// AI Veri Analizi HTML içeriği - Placeholder yerine konulacak
    /// </summary>
    public string InsightHtml { get; set; } = string.Empty;
}
