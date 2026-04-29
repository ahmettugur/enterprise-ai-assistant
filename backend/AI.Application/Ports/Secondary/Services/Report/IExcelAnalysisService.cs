using AI.Application.DTOs.ExcelAnalysis;

namespace AI.Application.Ports.Secondary.Services.Report;


/// <summary>
/// Excel/CSV dosya analiz servisi interface'i
/// DuckDB kullanarak büyük dosyalarda SQL sorguları çalıştırır
/// </summary>
public interface IExcelAnalysisService
{
    /// <summary>
    /// Excel/CSV dosyasından şema bilgisi çıkarır
    /// </summary>
    /// <param name="fileStream">Dosya stream'i</param>
    /// <param name="fileName">Dosya adı (uzantı tespiti için)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Şema bilgisi</returns>
    Task<ExcelSchemaResult> GetSchemaAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Excel/CSV dosyasında SQL sorgusu çalıştırır
    /// </summary>
    /// <param name="fileStream">Dosya stream'i</param>
    /// <param name="fileName">Dosya adı</param>
    /// <param name="sqlQuery">Çalıştırılacak SQL sorgusu</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Sorgu sonuçları</returns>
    Task<ExcelQueryResult> ExecuteQueryAsync(Stream fileStream, string fileName, string sqlQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı sorusundan SQL üretir ve çalıştırır
    /// </summary>
    /// <param name="fileStream">Dosya stream'i</param>
    /// <param name="fileName">Dosya adı</param>
    /// <param name="userQuery">Kullanıcının doğal dil sorusu</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Analiz sonucu</returns>
    Task<ExcelAnalysisResult> AnalyzeAsync(Stream fileStream, string fileName, string userQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desteklenen dosya uzantılarını döndürür
    /// </summary>
    IEnumerable<string> GetSupportedExtensions();

    /// <summary>
    /// Dosya uzantısının desteklenip desteklenmediğini kontrol eder
    /// </summary>
    bool IsSupported(string fileName);
}
