using AI.Application.DTOs;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Document Display Info Use Case interface
/// Primary Port - API'den doğrudan çağrılır (DocumentDisplayInfoEndpoints.cs)
/// Veritabanı + Qdrant işlemlerini yönetir
/// </summary>
public interface IDocumentDisplayInfoUseCase
{
    /// <summary>
    /// Id'ye göre döküman bilgisi getirir
    /// </summary>
    Task<DocumentDisplayInfoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dosya adına göre döküman bilgisi getirir
    /// </summary>
    Task<DocumentDisplayInfoDto?> GetByFileNameAsync(string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tüm dökümanları getirir
    /// </summary>
    Task<List<DocumentDisplayInfoListDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kullanıcıya göre tüm dökümanları getirir (UserId null olanlar + userId eşleşenler)
    /// </summary>
    Task<List<DocumentDisplayInfoListDto>> GetAllByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kategoriye göre dökümanları getirir
    /// </summary>
    Task<List<DocumentDisplayInfoListDto>> GetByCategoryAsync(string categoryId, bool includeInactive = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kullanıcıya göre kategorideki dökümanları getirir
    /// </summary>
    Task<List<DocumentDisplayInfoListDto>> GetByCategoryByUserIdAsync(string categoryId, string? userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Select2 dropdown için dökümanları getirir
    /// </summary>
    Task<List<DocumentDisplayInfoSelectDto>> GetAllForSelectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Kullanıcıya göre Select2 dropdown için dökümanları getirir
    /// </summary>
    Task<List<DocumentDisplayInfoSelectDto>> GetAllForSelectByUserIdAsync(string? userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dosya yükler, embedding oluşturur ve veritabanına kaydeder
    /// </summary>
    Task<DocumentDisplayInfoDto> UploadAndProcessAsync(
        Stream fileStream, 
        string fileName, 
        string contentType,
        CreateDocumentDisplayInfoRequest request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Döküman metadata'sını günceller (embedding'lere dokunmaz)
    /// </summary>
    Task<DocumentDisplayInfoDto> UpdateAsync(Guid id, UpdateDocumentDisplayInfoRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dökümanı ve Qdrant'taki embedding'lerini siler
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dökümanı yeniden işler (mevcut embedding'leri siler, yeniden oluşturur)
    /// </summary>
    Task<DocumentDisplayInfoDto> ReprocessAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Qdrant'ta koleksiyon var mı kontrol eder
    /// </summary>
    Task<bool> HasEmbeddingsAsync(string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Qdrant'taki chunk sayısını getirir
    /// </summary>
    Task<int> GetChunkCountAsync(string fileName, CancellationToken cancellationToken = default);
}
