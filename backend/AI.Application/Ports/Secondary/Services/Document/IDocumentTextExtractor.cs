namespace AI.Application.Ports.Secondary.Services.Document;

/// <summary>
/// Döküman dosyalarından metin çıkarmak için port interface
/// PDF, Excel, Word, CSV, TXT, PowerPoint formatlarını destekler
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>
    /// Dosya türüne göre metni çıkarır
    /// </summary>
    /// <param name="fileName">Dosya adı (uzantı için)</param>
    /// <param name="fileStream">Dosya içeriği</param>
    /// <returns>Çıkarılan metin</returns>
    string ExtractText(string fileName, Stream fileStream);
}
