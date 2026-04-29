using AI.Application.DTOs;
using AI.Domain.Documents;

namespace AI.Application.Ports.Secondary.Services.Vector;

/// <summary>
/// Qdrant vector database servisi interface'i
/// </summary>
public interface IQdrantService
{
    /// <summary>
    /// Koleksiyon oluşturur
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="vectorSize">Vector boyutu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Başarı durumu</returns>
    Task<bool> CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Koleksiyonun var olup olmadığını kontrol eder
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Koleksiyon var mı?</returns>
    Task<bool> CollectionExistsAsync(string collectionName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Koleksiyonu siler
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Başarı durumu</returns>
    Task<bool> DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tek bir vector ekler
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="chunk">Document chunk</param>
    /// <param name="vector">Embedding vector</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vector ID</returns>
    Task<Guid?> UpsertVectorAsync(string collectionName, DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Birden fazla vector ekler
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="chunks">Document chunk'ları</param>
    /// <param name="vectors">Embedding vector'ları</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Başarı durumu</returns>
    Task<bool> UpsertVectorsAsync(string collectionName, List<DocumentChunk> chunks, List<float[]> vectors, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Semantic search yapar
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="queryVector">Arama vector'ü</param>
    /// <param name="limit">Maksimum sonuç sayısı</param>
    /// <param name="minScore">Minimum benzerlik skoru</param>
    /// <param name="filter">Filtreleme koşulları</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Arama sonuçları</returns>
    Task<List<SearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, float minScore = 0.7f, Dictionary<string, object>? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hybrid search yapar (Dense vector + Sparse vector)
    /// Qdrant native fusion kullanır
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="denseVector">Dense vector (semantic embeddings)</param>
    /// <param name="sparseIndices">Sparse vector indices</param>
    /// <param name="sparseValues">Sparse vector values</param>
    /// <param name="limit">Maksimum sonuç sayısı</param>
    /// <param name="minScore">Minimum benzerlik skoru</param>
    /// <param name="denseWeight">Dense vector ağırlığı (0.0-1.0)</param>
    /// <param name="sparseWeight">Sparse vector ağırlığı (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Arama sonuçları</returns>
    Task<List<SearchResult>> HybridSearchAsync(
        string collectionName,
        float[] denseVector,
        uint[] sparseIndices,
        float[] sparseValues,
        int limit = 10,
        float minScore = 0.0f,
        float denseWeight = 0.7f,
        float sparseWeight = 0.3f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vector'ü siler
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="vectorId">Vector ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Başarı durumu</returns>
    Task<bool> DeleteVectorAsync(string collectionName, Guid vectorId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Doküman ID'sine göre tüm vector'leri siler
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="documentId">Doküman ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Silinen vector sayısı</returns>
    Task<int> DeleteVectorsByDocumentIdAsync(string collectionName, Guid documentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Koleksiyon bilgilerini getirir
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Koleksiyon bilgileri</returns>
    Task<object?> GetCollectionInfoAsync(string collectionName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Koleksiyondaki toplam nokta (vector) sayısını getirir
    /// </summary>
    /// <param name="collectionName">Koleksiyon adı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Toplam nokta sayısı</returns>
    Task<long> GetPointsCountAsync(string collectionName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tüm koleksiyonları listeler
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Koleksiyon adları listesi</returns>
    Task<List<string>> GetCollectionsAsync(CancellationToken cancellationToken = default);
}
