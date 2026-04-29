using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace AI.Application.DTOs.Dashboard;

/// <summary>
/// LLM tarafından üretilen dashboard JSON konfigürasyonu
/// Prompt'taki JSON yapısına birebir uyumlu
/// </summary>
public class DashboardConfig
{
    /// <summary>
    /// Dashboard benzersiz ID'si
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// KPI kartları konfigürasyonu
    /// </summary>
    [JsonPropertyName("kpis")]
    [JsonProperty("kpis")]
    public List<KpiConfig> Kpis { get; set; } = [];
    
    /// <summary>
    /// Grafik konfigürasyonları
    /// </summary>
    [JsonPropertyName("charts")]
    [JsonProperty("charts")]
    public List<ChartConfig> Charts { get; set; } = [];
    
    /// <summary>
    /// Tablo konfigürasyonu
    /// </summary>
    [JsonPropertyName("table")]
    [JsonProperty("table")]
    public TableConfig? Table { get; set; }
    
    /// <summary>
    /// AI analiz bölümü konfigürasyonu
    /// </summary>
    [JsonPropertyName("analysis")]
    [JsonProperty("analysis")]
    public AnalysisConfig? Analysis { get; set; }
    
    /// <summary>
    /// LLM tarafından üretilen özel CSS stilleri
    /// </summary>
    [JsonPropertyName("customCss")]
    [JsonProperty("customCss")]
    public string? CustomCss { get; set; }
}

/// <summary>
/// KPI (Key Performance Indicator) kart konfigürasyonu
/// </summary>
public class KpiConfig
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Hesaplama türü: sum, avg, count, min, max, countDistinct
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = "count";
    
    /// <summary>
    /// Hesaplamada kullanılacak kolon adı
    /// </summary>
    [JsonPropertyName("column")]
    [JsonProperty("column")]
    public string Column { get; set; } = string.Empty;
    
    /// <summary>
    /// KPI emoji ikonu
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonProperty("icon")]
    public string Icon { get; set; } = "📊";
    
    /// <summary>
    /// KPI rengi: blue, green, red, purple, orange, teal, indigo, pink
    /// </summary>
    [JsonPropertyName("color")]
    [JsonProperty("color")]
    public string Color { get; set; } = "blue";
    
    /// <summary>
    /// Değer formatı: number, currency, percent, duration
    /// </summary>
    [JsonPropertyName("format")]
    [JsonProperty("format")]
    public string Format { get; set; } = "number";
}

/// <summary>
/// Grafik konfigürasyonu
/// </summary>
public class ChartConfig
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Grafik türü: bar, line, pie, donut, area, radar, heatmap, treemap
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = "bar";
    
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// X ekseni kolon adı (bar, line, area için)
    /// </summary>
    [JsonPropertyName("xAxis")]
    [JsonProperty("xAxis")]
    public string? XAxis { get; set; }
    
    /// <summary>
    /// Y ekseni kolon adı (bar, line, area için)
    /// </summary>
    [JsonPropertyName("yAxis")]
    [JsonProperty("yAxis")]
    public string? YAxis { get; set; }
    
    /// <summary>
    /// Etiket kolonu (pie, donut için)
    /// </summary>
    [JsonPropertyName("labelColumn")]
    [JsonProperty("labelColumn")]
    public string? LabelColumn { get; set; }
    
    /// <summary>
    /// Değer kolonu (pie, donut için)
    /// </summary>
    [JsonPropertyName("valueColumn")]
    [JsonProperty("valueColumn")]
    public string? ValueColumn { get; set; }
    
    /// <summary>
    /// Yatay bar chart mi?
    /// </summary>
    [JsonPropertyName("horizontal")]
    [JsonProperty("horizontal")]
    public bool Horizontal { get; set; } = false;
    
    /// <summary>
    /// Yumuşak çizgi mi? (line, area için)
    /// </summary>
    [JsonPropertyName("smooth")]
    [JsonProperty("smooth")]
    public bool Smooth { get; set; } = true;
    
    /// <summary>
    /// Grafik yüksekliği (px)
    /// </summary>
    [JsonPropertyName("height")]
    [JsonProperty("height")]
    public int Height { get; set; } = 350;
    
    /// <summary>
    /// Renk paleti
    /// </summary>
    [JsonPropertyName("colors")]
    [JsonProperty("colors")]
    public List<string>? Colors { get; set; }
}

/// <summary>
/// Tablo konfigürasyonu
/// </summary>
public class TableConfig
{
    /// <summary>
    /// Gösterilecek kolon adları
    /// </summary>
    [JsonPropertyName("columns")]
    [JsonProperty("columns")]
    public List<string> Columns { get; set; } = [];
    
    /// <summary>
    /// Sıralama kolonu
    /// </summary>
    [JsonPropertyName("sortBy")]
    [JsonProperty("sortBy")]
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Sıralama yönü: asc, desc
    /// </summary>
    [JsonPropertyName("sortOrder")]
    [JsonProperty("sortOrder")]
    public string SortOrder { get; set; } = "asc";
    
    /// <summary>
    /// Sayfa başına kayıt sayısı
    /// </summary>
    [JsonPropertyName("pageSize")]
    [JsonProperty("pageSize")]
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// AI Analiz bölümü konfigürasyonu
/// </summary>
public class AnalysisConfig
{
    /// <summary>
    /// Yönetici özeti - üst yönetim için kapsamlı değerlendirme
    /// </summary>
    [JsonPropertyName("executiveSummary")]
    [JsonProperty("executiveSummary")]
    public ExecutiveSummaryConfig? ExecutiveSummary { get; set; }
    
    /// <summary>
    /// Veri özeti (2-3 cümle)
    /// </summary>
    [JsonPropertyName("summary")]
    [JsonProperty("summary")]
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Öne çıkan noktalar
    /// </summary>
    [JsonPropertyName("highlights")]
    [JsonProperty("highlights")]
    public List<HighlightConfig> Highlights { get; set; } = [];
    
    /// <summary>
    /// Dağılım verileri
    /// </summary>
    [JsonPropertyName("distribution")]
    [JsonProperty("distribution")]
    public List<DistributionConfig> Distribution { get; set; } = [];
    
    /// <summary>
    /// İçgörüler
    /// </summary>
    [JsonPropertyName("insights")]
    [JsonProperty("insights")]
    public List<InsightConfig> Insights { get; set; } = [];
    
    /// <summary>
    /// Öneriler
    /// </summary>
    [JsonPropertyName("recommendations")]
    [JsonProperty("recommendations")]
    public List<RecommendationConfig> Recommendations { get; set; } = [];
    
    /// <summary>
    /// İstatistikler
    /// </summary>
    [JsonPropertyName("statistics")]
    [JsonProperty("statistics")]
    public StatisticsConfig? Statistics { get; set; }
}

/// <summary>
/// Yönetici özeti konfigürasyonu
/// </summary>
public class ExecutiveSummaryConfig
{
    /// <summary>
    /// Rapor başlığı
    /// </summary>
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Genel değerlendirme (3-5 cümle)
    /// </summary>
    [JsonPropertyName("overview")]
    [JsonProperty("overview")]
    public string Overview { get; set; } = string.Empty;
    
    /// <summary>
    /// Ana bulgular listesi
    /// </summary>
    [JsonPropertyName("keyFindings")]
    [JsonProperty("keyFindings")]
    public List<string> KeyFindings { get; set; } = [];
    
    /// <summary>
    /// Aksiyon maddeleri
    /// </summary>
    [JsonPropertyName("actionItems")]
    [JsonProperty("actionItems")]
    public List<string> ActionItems { get; set; } = [];
    
    /// <summary>
    /// Sonuç ve genel değerlendirme
    /// </summary>
    [JsonPropertyName("conclusion")]
    [JsonProperty("conclusion")]
    public string Conclusion { get; set; } = string.Empty;
}

/// <summary>
/// Highlight (öne çıkan nokta) konfigürasyonu
/// </summary>
public class HighlightConfig
{
    /// <summary>
    /// Tip: top, low, trend, info
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = "info";
    
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }
}

/// <summary>
/// Dağılım konfigürasyonu
/// </summary>
public class DistributionConfig
{
    [JsonPropertyName("category")]
    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public double Value { get; set; }
    
    [JsonPropertyName("percent")]
    [JsonProperty("percent")]
    public double Percent { get; set; }
}

/// <summary>
/// İçgörü konfigürasyonu
/// </summary>
public class InsightConfig
{
    /// <summary>
    /// Tip: trend, warning, info, success
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string Type { get; set; } = "info";
    
    [JsonPropertyName("text")]
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Öneri konfigürasyonu
/// </summary>
public class RecommendationConfig
{
    /// <summary>
    /// Öncelik: high, medium, low
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonProperty("priority")]
    public string Priority { get; set; } = "medium";
    
    [JsonPropertyName("title")]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("text")]
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// İstatistik konfigürasyonu
/// </summary>
public class StatisticsConfig
{
    [JsonPropertyName("total")]
    [JsonProperty("total")]
    public double Total { get; set; }
    
    [JsonPropertyName("average")]
    [JsonProperty("average")]
    public double Average { get; set; }
    
    [JsonPropertyName("min")]
    [JsonProperty("min")]
    public double Min { get; set; }
    
    [JsonPropertyName("max")]
    [JsonProperty("max")]
    public double Max { get; set; }
    
    [JsonPropertyName("median")]
    [JsonProperty("median")]
    public double Median { get; set; }
}
