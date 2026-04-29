using AI.Application.DTOs.DynamicPrompt;

namespace AI.Application.Ports.Secondary.Services.AIChat;

/// <summary>
/// Kullanıcı sorgusuna göre dinamik SQL prompt'u oluşturan servis
/// </summary>
public interface IDynamicPromptBuilder
{
    /// <summary>
    /// Kullanıcı sorgusuna göre dinamik şema prompt'u oluşturur
    /// </summary>
    /// <param name="userQuery">Kullanıcının doğal dildeki sorusu</param>
    /// <param name="basePromptTemplate">Temel prompt şablonu (opsiyonel)</param>
    /// <returns>Dinamik prompt (şema + kurallar)</returns>
    Task<DynamicPromptResult> BuildPromptAsync(string userQuery, string? basePromptTemplate = null);
    
    /// <summary>
    /// Kullanıcı sorgusundan anahtar kelimeleri çıkarır
    /// </summary>
    /// <param name="userQuery">Kullanıcı sorgusu</param>
    /// <returns>Anahtar kelimeler</returns>
    Task<IEnumerable<string>> ExtractKeywordsAsync(string userQuery);
}