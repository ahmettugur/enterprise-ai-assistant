using AI.Application.Results;
using AI.Domain.Enums;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Kullanıcı uzun vadeli hafıza yönetim Use Case - Primary Port
/// </summary>
public interface IUserMemoryUseCase
{
    /// <summary>
    /// Kullanıcı için system prompt'a eklenecek memory context string'i oluşturur.
    /// L1 (CurrentUserService'den) + L2 (semantic search) birleştirir.
    /// </summary>
    /// <param name="query">Kullanıcının sorusu (semantic search için)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Prompt'a eklenecek context string (örn: "User: Ahmet, Sales Dept. Preferences: format: Excel")</returns>
    Task<string> BuildMemoryContextAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Semantic search ile kullanıcının ilgili hafızalarını getirir
    /// </summary>
    /// <param name="query">Arama sorgusu</param>
    /// <param name="topK">Maksimum sonuç sayısı</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>İlgili hafızalar</returns>
    Task<List<UserMemoryDto>> GetRelevantMemoriesAsync(string query, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının tüm hafızalarını getirir
    /// </summary>
    Task<List<UserMemoryDto>> GetAllMemoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Konuşmadan otomatik olarak kullanıcı bilgilerini çıkarır ve saklar
    /// </summary>
    /// <param name="userMessage">Kullanıcı mesajı</param>
    /// <param name="assistantResponse">AI yanıtı</param>
    /// <param name="userId">Kullanıcı ID (background task'larda CurrentUserService erişilemediği için)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task ExtractAndStoreMemoriesAsync(string userMessage, string assistantResponse, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manuel olarak hafıza ekler
    /// </summary>
    Task<ResultBase> AddMemoryAsync(string key, string value, MemoryCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hafızayı günceller
    /// </summary>
    Task<ResultBase> UpdateMemoryAsync(Guid id, string newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hafızayı siler
    /// </summary>
    Task<ResultBase> DeleteMemoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının tüm hafızasını siler (KVKK - Forget Me)
    /// </summary>
    Task<ResultBase> ForgetUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Qdrant'ta user_memories collection'ını oluşturur (ilk kurulum için)
    /// </summary>
    Task<ResultBase> InitializeCollectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Hafıza DTO
/// </summary>
public record UserMemoryDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public MemoryCategory Category { get; init; }
    public float Confidence { get; init; }
    public int UsageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastAccessedAt { get; init; }
}

/// <summary>
/// LLM'den çıkarılan hafıza bilgisi
/// </summary>
public record ExtractedMemoryDto
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Category { get; init; } = "Preference";
    public float Confidence { get; init; } = 0.8f;
}
