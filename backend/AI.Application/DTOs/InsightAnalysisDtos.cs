using System.Text.Json.Serialization;

namespace AI.Application.DTOs;

#region Chunking DTOs

/// <summary>
/// Veri chunk'ı - LLM'e gönderilecek veri parçası
/// </summary>
public class DataChunk
{
    /// <summary>
    /// Chunk sırası (1-based)
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Toplam chunk sayısı
    /// </summary>
    public int TotalChunks { get; set; }
    
    /// <summary>
    /// Bu chunk'taki kayıtlar
    /// </summary>
    public List<dynamic> Data { get; set; } = new();
    
    /// <summary>
    /// Bu chunk'taki kayıt sayısı
    /// </summary>
    public int RecordCount { get; set; }
    
    /// <summary>
    /// Tahmini token sayısı
    /// </summary>
    public int EstimatedTokens { get; set; }
}

#endregion

#region Chunk Analysis DTOs

/// <summary>
/// Chunk analiz özeti - LLM'in ürettiği çıktı
/// </summary>
public class ChunkSummary
{
    /// <summary>
    /// Chunk ID (1-based)
    /// </summary>
    [JsonPropertyName("chunk_id")]
    public int ChunkId { get; set; }
    
    /// <summary>
    /// Chunk sırası (alias for ChunkId)
    /// </summary>
    [JsonPropertyName("chunkIndex")]
    public int ChunkIndex { get; set; }
    
    /// <summary>
    /// Bu chunk'taki kayıt sayısı
    /// </summary>
    [JsonPropertyName("recordCount")]
    public int RecordCount { get; set; }
    
    /// <summary>
    /// Chunk özeti
    /// </summary>
    [JsonPropertyName("chunk_summary")]
    public string? ChunkSummaryText { get; set; }
    
    /// <summary>
    /// Temalar
    /// </summary>
    [JsonPropertyName("themes")]
    public List<ThemeInfo>? Themes { get; set; }
    
    /// <summary>
    /// Metrikler
    /// </summary>
    [JsonPropertyName("metrics")]
    public ChunkMetrics? Metrics { get; set; }
    
    /// <summary>
    /// Varlıklar (entities)
    /// </summary>
    [JsonPropertyName("entities")]
    public ChunkEntities? Entities { get; set; }
    
    /// <summary>
    /// Kritik vakalar
    /// </summary>
    [JsonPropertyName("critical_cases")]
    public List<CriticalCase>? CriticalCases { get; set; }
    
    /// <summary>
    /// Alan bazlı istatistikler
    /// </summary>
    [JsonPropertyName("statistics")]
    public ChunkStatistics Statistics { get; set; } = new();
    
    /// <summary>
    /// Sıralamalar (top/bottom)
    /// </summary>
    [JsonPropertyName("rankings")]
    public ChunkRankings Rankings { get; set; } = new();
    
    /// <summary>
    /// Tespit edilen pattern'lar
    /// </summary>
    [JsonPropertyName("patterns")]
    public List<string> Patterns { get; set; } = new();
    
    /// <summary>
    /// Anomaliler
    /// </summary>
    [JsonPropertyName("anomalies")]
    public List<ChunkAnomaly> Anomalies { get; set; } = new();
    
    /// <summary>
    /// Chunk'a özel insight'lar
    /// </summary>
    [JsonPropertyName("insights")]
    public List<string> Insights { get; set; } = new();
    
    /// <summary>
    /// Anahtar içgörüler
    /// </summary>
    [JsonPropertyName("key_insights")]
    public List<string>? KeyInsights { get; set; }
    
    /// <summary>
    /// İşlem süresi (ms)
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Chunk istatistikleri
/// </summary>
public class ChunkStatistics
{
    /// <summary>
    /// Sayısal alan istatistikleri
    /// </summary>
    [JsonPropertyName("numericFields")]
    public Dictionary<string, NumericFieldStats> NumericFields { get; set; } = new();
    
    /// <summary>
    /// Kategorik alan dağılımları
    /// </summary>
    [JsonPropertyName("categoricalFields")]
    public Dictionary<string, Dictionary<string, int>> CategoricalFields { get; set; } = new();
    
    /// <summary>
    /// Tarih alan istatistikleri
    /// </summary>
    [JsonPropertyName("dateFields")]
    public Dictionary<string, DateFieldStats> DateFields { get; set; } = new();
}

/// <summary>
/// Sayısal alan istatistikleri
/// </summary>
public class NumericFieldStats
{
    [JsonPropertyName("sum")]
    public double Sum { get; set; }
    
    [JsonPropertyName("avg")]
    public double Avg { get; set; }
    
    [JsonPropertyName("min")]
    public double Min { get; set; }
    
    [JsonPropertyName("max")]
    public double Max { get; set; }
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// Tarih alan istatistikleri
/// </summary>
public class DateFieldStats
{
    [JsonPropertyName("minDate")]
    public string? MinDate { get; set; }
    
    [JsonPropertyName("maxDate")]
    public string? MaxDate { get; set; }
    
    [JsonPropertyName("periodCount")]
    public int PeriodCount { get; set; }
}

/// <summary>
/// Chunk sıralamaları
/// </summary>
public class ChunkRankings
{
    /// <summary>
    /// En yüksek değerler
    /// </summary>
    [JsonPropertyName("top5ByMetric")]
    public List<RankingItem> Top5ByMetric { get; set; } = new();
    
    /// <summary>
    /// En düşük değerler
    /// </summary>
    [JsonPropertyName("bottom5ByMetric")]
    public List<RankingItem> Bottom5ByMetric { get; set; } = new();
}

/// <summary>
/// Sıralama öğesi
/// </summary>
public class RankingItem
{
    [JsonPropertyName("groupField")]
    public string? GroupField { get; set; }
    
    [JsonPropertyName("groupValue")]
    public string? GroupValue { get; set; }
    
    [JsonPropertyName("metricField")]
    public string? MetricField { get; set; }
    
    [JsonPropertyName("metricValue")]
    public double MetricValue { get; set; }
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// Anomali
/// </summary>
public class ChunkAnomaly
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "info"; // critical, warning, info
    
    [JsonPropertyName("field")]
    public string? Field { get; set; }
    
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// Tema bilgisi - Chunk analizinden
/// </summary>
public class ThemeInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }
    
    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }
    
    [JsonPropertyName("representative_examples")]
    public List<string>? RepresentativeExamples { get; set; }
}

/// <summary>
/// Chunk metrikleri
/// </summary>
public class ChunkMetrics
{
    [JsonPropertyName("total_in_chunk")]
    public int TotalInChunk { get; set; }
    
    [JsonPropertyName("by_category")]
    public Dictionary<string, int>? ByCategory { get; set; }
    
    [JsonPropertyName("by_severity")]
    public Dictionary<string, int>? BySeverity { get; set; }
}

/// <summary>
/// Chunk varlıkları (entities)
/// </summary>
public class ChunkEntities
{
    [JsonPropertyName("stores_mentioned")]
    public List<StoreMention>? StoresMentioned { get; set; }
    
    [JsonPropertyName("time_patterns")]
    public TimePatterns? TimePatterns { get; set; }
    
    [JsonPropertyName("products_mentioned")]
    public List<string>? ProductsMentioned { get; set; }
    
    [JsonPropertyName("channels_mentioned")]
    public List<string>? ChannelsMentioned { get; set; }
}

/// <summary>
/// Mağaza referansı
/// </summary>
public class StoreMention
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("count")]
    public int Count { get; set; }
    
    [JsonPropertyName("main_issues")]
    public List<string>? MainIssues { get; set; }
}

/// <summary>
/// Zaman pattern'ları
/// </summary>
public class TimePatterns
{
    [JsonPropertyName("peak_hours")]
    public List<string>? PeakHours { get; set; }
    
    [JsonPropertyName("peak_days")]
    public List<string>? PeakDays { get; set; }
}

/// <summary>
/// Kritik vaka
/// </summary>
public class CriticalCase
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Birleştirilmiş tema
/// </summary>
public class MergedTheme
{
    public string Name { get; set; } = "";
    public int TotalCount { get; set; }
    public string Severity { get; set; } = "medium";
    public HashSet<string> Keywords { get; set; } = new();
    public List<string> Examples { get; set; } = new();
    public List<int> ChunksFound { get; set; } = new();
}

/// <summary>
/// Mağaza bilgisi (birleştirilmiş)
/// </summary>
public class StoreInfo
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public HashSet<string> MainIssues { get; set; } = new();
}

/// <summary>
/// Birleştirilmiş analiz verisi
/// </summary>
public class MergedAnalysisData
{
    public int TotalRecords { get; set; }
    public int ChunkCount { get; set; }
    public List<string> ChunkSummaries { get; set; } = new();
    public Dictionary<string, MergedTheme> AllThemes { get; set; } = new();
    public List<CriticalCase> AllCriticalCases { get; set; } = new();
    public List<string> AllPatterns { get; set; } = new();
    public Dictionary<string, int> CategoryMetrics { get; set; } = new();
    public Dictionary<string, int> SeverityMetrics { get; set; } = new();
    public Dictionary<string, StoreInfo> StoresMentioned { get; set; } = new();
}

#endregion

#region Aggregation DTOs

/// <summary>
/// Tüm chunk'ların birleştirilmiş sonucu
/// </summary>
public class AggregatedInsightData
{
    /// <summary>
    /// Toplam kayıt sayısı
    /// </summary>
    public int TotalRecords { get; set; }
    
    /// <summary>
    /// Chunk sayısı
    /// </summary>
    public int ChunkCount { get; set; }
    
    /// <summary>
    /// Chunk özetleri
    /// </summary>
    public List<ChunkSummary> ChunkSummaries { get; set; } = new();
    
    /// <summary>
    /// Birleştirilmiş temalar
    /// </summary>
    public Dictionary<string, MergedTheme> AllThemes { get; set; } = new();
    
    /// <summary>
    /// Kritik vakalar
    /// </summary>
    public List<CriticalCase> AllCriticalCases { get; set; } = new();
    
    /// <summary>
    /// Tüm chunk'lardan toplanan pattern'lar
    /// </summary>
    public List<string> AllPatterns { get; set; } = new();
    
    /// <summary>
    /// Kategori metrikleri
    /// </summary>
    public Dictionary<string, int> CategoryMetrics { get; set; } = new();
    
    /// <summary>
    /// Severity metrikleri
    /// </summary>
    public Dictionary<string, int> SeverityMetrics { get; set; } = new();
    
    /// <summary>
    /// Mağaza bilgileri
    /// </summary>
    public Dictionary<string, StoreInfo> StoresMentioned { get; set; } = new();
}

/// <summary>
/// Alan şema bilgisi
/// </summary>
public class FieldSchemaInfo
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = "String";
}

/// <summary>
/// Global istatistikler
/// </summary>
public class GlobalStatistics
{
    /// <summary>
    /// Sayısal alan istatistikleri (weighted)
    /// </summary>
    public Dictionary<string, NumericFieldStats> NumericFields { get; set; } = new();
    
    /// <summary>
    /// Kategorik alan dağılımları (birleştirilmiş)
    /// </summary>
    public Dictionary<string, List<CategoryDistribution>> CategoricalFields { get; set; } = new();
    
    /// <summary>
    /// Tarih alanları (global min/max)
    /// </summary>
    public Dictionary<string, DateFieldStats> DateFields { get; set; } = new();
}

/// <summary>
/// Kategori dağılımı
/// </summary>
public class CategoryDistribution
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Global sıralamalar
/// </summary>
public class GlobalRankings
{
    /// <summary>
    /// Her metrik için top N
    /// </summary>
    public Dictionary<string, List<RankingItem>> TopByMetric { get; set; } = new();
    
    /// <summary>
    /// Her metrik için bottom N
    /// </summary>
    public Dictionary<string, List<RankingItem>> BottomByMetric { get; set; } = new();
}

#endregion

#region Progress DTOs

/// <summary>
/// Analiz ilerleme durumu
/// </summary>
public class AnalysisProgress
{
    /// <summary>
    /// Aşama: "Chunking", "ChunkAnalysis", "Aggregation", "FinalAnalysis"
    /// </summary>
    public string Stage { get; set; } = "Chunking";
    
    /// <summary>
    /// Mevcut chunk (ChunkAnalysis aşamasında)
    /// </summary>
    public int CurrentChunk { get; set; }
    
    /// <summary>
    /// Toplam chunk
    /// </summary>
    public int TotalChunks { get; set; }
    
    /// <summary>
    /// Tamamlanan chunk sayısı
    /// </summary>
    public int CompletedChunks { get; set; }
    
    /// <summary>
    /// İşlenmekte olan chunk sayısı
    /// </summary>
    public int InProgressChunks { get; set; }
    
    /// <summary>
    /// Yüzde ilerleme (0-100)
    /// </summary>
    public int PercentComplete { get; set; }
    
    /// <summary>
    /// Tahmini kalan süre (saniye)
    /// </summary>
    public int EstimatedSecondsRemaining { get; set; }
    
    /// <summary>
    /// Durum mesajı
    /// </summary>
    public string? Message { get; set; }
}

#endregion
