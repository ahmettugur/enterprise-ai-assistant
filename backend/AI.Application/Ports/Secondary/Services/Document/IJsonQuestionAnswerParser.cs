using AI.Domain.Documents;

namespace AI.Application.Ports.Secondary.Services.Document;

/// <summary>
/// Soru-Cevap formatındaki JSON dosyalarını parse eden servis interface'i
/// </summary>
public interface IJsonQuestionAnswerParser
{
    /// <summary>
    /// JSON dosyasındaki soru-cevap verilerini DocumentChunk'lara dönüştürür
    /// </summary>
    /// <param name="fileStream">JSON dosya stream'i</param>
    /// <param name="documentId">Doküman ID'si</param>
    /// <param name="fileName">Dosya adı</param>
    /// <returns>Oluşturulan DocumentChunk listesi</returns>
    List<DocumentChunk> ParseQuestionsAnswers(Stream fileStream, Guid documentId, string fileName);
}
