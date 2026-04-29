using AI.Application.DTOs;

namespace AI.Application.Ports.Secondary.Services.Document;


/// <summary>
/// Document ve Category cache işlemleri için interface
/// </summary>
public interface IDocumentCacheService
{
    #region Category Cache

    /// <summary>
    /// Tüm kategorileri cache'den getirir
    /// </summary>
    Task<List<DocumentCategoryDto>?> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm kategorileri cache'e kaydeder
    /// </summary>
    Task SetAllCategoriesAsync(List<DocumentCategoryDto> categories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Select için kategorileri cache'den getirir
    /// </summary>
    Task<List<DocumentCategorySelectDto>?> GetCategoriesForSelectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Select için kategorileri cache'e kaydeder
    /// </summary>
    Task SetCategoriesForSelectAsync(List<DocumentCategorySelectDto> categories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kategori cache'ini invalidate eder
    /// </summary>
    Task InvalidateCategoryCacheAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Document Display Info Cache

    /// <summary>
    /// Tüm dökümanları cache'den getirir
    /// </summary>
    Task<List<DocumentDisplayInfoListDto>?> GetAllDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm dökümanları cache'e kaydeder
    /// </summary>
    Task SetAllDocumentsAsync(List<DocumentDisplayInfoListDto> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Select için dökümanları cache'den getirir
    /// </summary>
    Task<List<DocumentDisplayInfoSelectDto>?> GetDocumentsForSelectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Select için dökümanları cache'e kaydeder
    /// </summary>
    Task SetDocumentsForSelectAsync(List<DocumentDisplayInfoSelectDto> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kategoriye göre dökümanları cache'den getirir
    /// </summary>
    Task<List<DocumentDisplayInfoListDto>?> GetDocumentsByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kategoriye göre dökümanları cache'e kaydeder
    /// </summary>
    Task SetDocumentsByCategoryAsync(string categoryId, List<DocumentDisplayInfoListDto> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Döküman cache'ini invalidate eder
    /// </summary>
    Task InvalidateDocumentCacheAsync(CancellationToken cancellationToken = default);

    #endregion

    #region User-Specific Cache

    /// <summary>
    /// Kullanıcıya ait dökümanları cache'den getirir (Admin + kullanıcının kendi dökümanları)
    /// </summary>
    Task<List<DocumentDisplayInfoListDto>?> GetDocumentsForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcıya ait dökümanları cache'e kaydeder
    /// </summary>
    Task SetDocumentsForUserAsync(string userId, List<DocumentDisplayInfoListDto> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcıya ait kategorileri cache'den getirir (Admin + kullanıcının kendi kategorileri)
    /// </summary>
    Task<List<DocumentCategoryDto>?> GetCategoriesForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcıya ait kategorileri cache'e kaydeder
    /// </summary>
    Task SetCategoriesForUserAsync(string userId, List<DocumentCategoryDto> categories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir kullanıcının cache'ini invalidate eder
    /// </summary>
    Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default);

    #endregion

    /// <summary>
    /// Tüm döküman ve kategori cache'ini invalidate eder
    /// </summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}
