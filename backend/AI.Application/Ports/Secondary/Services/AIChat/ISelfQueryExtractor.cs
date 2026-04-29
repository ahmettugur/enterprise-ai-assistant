using AI.Application.DTOs.AdvancedRag;

namespace AI.Application.Ports.Secondary.Services.AIChat;

/// Self-Query: Kullanıcı sorgusundan metadata filtrelerini otomatik çıkarma servisi
/// </summary>
public interface ISelfQueryExtractor
{
    /// <summary>
    /// Kullanıcı sorgusundan semantic query ve metadata filtrelerini ayırır
    /// </summary>
    /// <param name="userQuery">Orijinal kullanıcı sorgusu</param>
    /// <param name="availableMetadataFields">Kullanılabilir metadata alanları</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Ayrıştırılmış sorgu ve filtreler</returns>
    Task<SelfQueryResult> ExtractAsync(
        string userQuery,
        List<MetadataFieldInfo>? availableMetadataFields = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-Query aktif mi kontrolü
    /// </summary>
    bool IsEnabled { get; }
}
