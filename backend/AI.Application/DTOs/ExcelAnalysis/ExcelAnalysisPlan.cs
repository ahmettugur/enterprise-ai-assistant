namespace AI.Application.DTOs.ExcelAnalysis;

/// <summary>
/// LLM'den gelen çoklu SQL analiz planı
/// </summary>
public class ExcelAnalysisPlan
{
    /// <summary>
    /// "single" (spesifik soru → tek SQL) veya "comprehensive" (genel analiz → çoklu SQL)
    /// </summary>
    public string AnalysisType { get; set; } = "single";

    /// <summary>
    /// Çalıştırılacak SQL sorguları listesi
    /// </summary>
    public List<AnalysisQuery> Queries { get; set; } = new();
}

/// <summary>
/// Analiz planındaki tek bir sorgu
/// </summary>
public class AnalysisQuery
{
    /// <summary>
    /// Sorgu başlığı (ör: "Genel Bakış", "Şehir Dağılımı")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Sorgunun kısa açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// DuckDB'de çalıştırılacak SQL sorgusu
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Önerilen grafik tipi: "bar", "pie", "line", "area", "donut", null (grafik yok)
    /// </summary>
    public string? ChartType { get; set; }
}
