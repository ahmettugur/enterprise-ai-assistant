using AI.Application.DTOs;
using AI.Application.DTOs.DocumentProcessing;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Doküman işleme Use Case interface'i
/// </summary>
public interface IDocumentProcessingUseCase
{
    /// <summary>
    /// Dokümanı DTO üzerinden işler — entity oluşturma use case içinde yapılır.
    /// API katmanı bu metodu kullanmalıdır.
    /// </summary>
    Task<DocumentUploadResultDto> ProcessDocumentFromUploadAsync(
        DocumentUploadDto uploadDto,
        Stream fileStream,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// Dokümanı ve ilgili chunk'ları siler
    /// </summary>
    /// <param name="documentId">Doküman ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Silme başarılı mı</returns>
    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desteklenen dosya türlerini döndürür
    /// </summary>
    /// <returns>Desteklenen dosya uzantıları</returns>
    IEnumerable<string> GetSupportedFileTypes();

    /// <summary>
    /// Dosya türünün desteklenip desteklenmediğini kontrol eder
    /// </summary>
    /// <param name="fileExtension">Dosya uzantısı</param>
    /// <returns>Destekleniyor mu</returns>
    bool IsFileTypeSupported(string fileExtension);

    /// <summary>
    /// Dokümanın index'te var olup olmadığını kontrol eder
    /// </summary>
    /// <param name="documentId">Doküman ID'si</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Doküman var mı</returns>
    Task<bool> IsExistAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosya hash'ine göre dokümanın var olup olmadığını kontrol eder
    /// </summary>
    /// <param name="fileHash">Dosya hash'i</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Doküman var mı</returns>
    Task<bool> IsExistByHashAsync(string fileHash, CancellationToken cancellationToken = default);


    /// <summary>
    /// Mevcut tüm index'leri (collection'ları) listeler
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Index listesi</returns>
    Task<List<string>> GetIndexesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir index'in var olup olmadığını kontrol eder
    /// </summary>
    /// <param name="indexName">Index adı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Index var mı</returns>
    Task<bool> IsIndexExistAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir index'teki dokümanları arar
    /// </summary>
    /// <param name="indexName">Index adı</param>
    /// <param name="query">Arama sorgusu</param>
    /// <param name="limit">Sonuç limiti</param>
    /// <param name="minScore">Minimum skor</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Arama sonuçları</returns>
    Task<List<SearchResult>> SearchInIndexAsync(string indexName, string query, int limit = 10, float minScore = 0.7f, CancellationToken cancellationToken = default);
}
