namespace AI.Application.Ports.Secondary.Services.Document;

/// <summary>
/// Doküman parser interface'i
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Desteklenen dosya türleri
    /// </summary>
    IEnumerable<string> SupportedFileTypes { get; }
    
    /// <summary>
    /// Dosya türünün desteklenip desteklenmediğini kontrol eder
    /// </summary>
    /// <param name="fileExtension">Dosya uzantısı</param>
    /// <returns>Destekleniyor mu?</returns>
    bool CanParse(string fileExtension);
    
    /// <summary>
    /// Dosyadan metin çıkarır
    /// </summary>
    /// <param name="filePath">Dosya yolu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Çıkarılan metin</returns>
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stream'den metin çıkarır
    /// </summary>
    /// <param name="stream">Dosya stream'i</param>
    /// <param name="fileName">Dosya adı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Çıkarılan metin</returns>
    Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
