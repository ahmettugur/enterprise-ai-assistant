using AI.Domain.Documents;

namespace AI.Application.Ports.Secondary.Services.Document;


/// <summary>
/// Metin parçalama servisi interface'i
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Metni chunk'lara böler
    /// </summary>
    /// <param name="text">Bölünecek metin</param>
    /// <param name="documentId">Doküman ID'si</param>
    /// <param name="chunkSize">Chunk boyutu (karakter)</param>
    /// <param name="overlapSize">Overlap boyutu (karakter)</param>
    /// <returns>Oluşturulan chunk'lar</returns>
    List<DocumentChunk> ChunkText(string text, Guid documentId, int chunkSize = 1000, int overlapSize = 200);
    
    /// <summary>
    /// Metni semantik olarak chunk'lara böler (paragraf, cümle bazlı)
    /// </summary>
    /// <param name="text">Bölünecek metin</param>
    /// <param name="documentId">Doküman ID'si</param>
    /// <param name="maxChunkSize">Maksimum chunk boyutu</param>
    /// <param name="minChunkSize">Minimum chunk boyutu</param>
    /// <returns>Oluşturulan chunk'lar</returns>
    List<DocumentChunk> ChunkTextSemantic(string text, Guid documentId, int maxChunkSize = 1000, int minChunkSize = 100);
}
