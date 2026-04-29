namespace AI.Application.DTOs;

/// <summary>
/// Sayfalanmış sonuç DTO'su
/// </summary>
/// <typeparam name="T">Sonuç türü</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Sonuç listesi
    /// </summary>
    public List<T> Items { get; set; } = new();
    
    /// <summary>
    /// Toplam kayıt sayısı
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Sayfa numarası (1'den başlar)
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Sayfa boyutu
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Toplam sayfa sayısı
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    /// <summary>
    /// Önceki sayfa var mı?
    /// </summary>
    public bool HasPreviousPage => Page > 1;
    
    /// <summary>
    /// Sonraki sayfa var mı?
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
    
    /// <summary>
    /// Başlangıç kayıt numarası
    /// </summary>
    public int StartIndex => (Page - 1) * PageSize + 1;
    
    /// <summary>
    /// Bitiş kayıt numarası
    /// </summary>
    public int EndIndex => Math.Min(Page * PageSize, TotalCount);
    
    /// <summary>
    /// Boş sayfalanmış sonuç oluşturur
    /// </summary>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa boyutu</param>
    /// <returns>Boş sayfalanmış sonuç</returns>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 10)
    {
        return new PagedResult<T>
        {
            Items = new List<T>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
    }
    
    /// <summary>
    /// Sayfalanmış sonuç oluşturur
    /// </summary>
    /// <param name="items">Sonuç listesi</param>
    /// <param name="totalCount">Toplam kayıt sayısı</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa boyutu</param>
    /// <returns>Sayfalanmış sonuç</returns>
    public static PagedResult<T> Create(List<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}