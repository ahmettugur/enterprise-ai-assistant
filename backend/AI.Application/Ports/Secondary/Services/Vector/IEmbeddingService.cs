namespace AI.Application.Ports.Secondary.Services.Vector;

/// <summary>
/// Embedding üretme servisi interface'i
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Tekil metin için embedding üretir
    /// </summary>
    /// <param name="text">Embedding üretilecek metin</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Embedding vektörü</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Çoklu metin için embedding üretir
    /// </summary>
    /// <param name="texts">Embedding üretilecek metinler</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Embedding vektörleri</returns>
    Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Embedding boyutunu döndürür
    /// </summary>
    int EmbeddingDimension { get; }
    
    /// <summary>
    /// Kullanılan model adını döndürür
    /// </summary>
    string ModelName { get; }
}
