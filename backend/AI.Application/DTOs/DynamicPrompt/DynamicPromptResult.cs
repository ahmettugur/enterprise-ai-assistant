namespace AI.Application.DTOs.DynamicPrompt;

/// <summary>
/// Dinamik prompt oluşturma sonucu
/// </summary>
public class DynamicPromptResult
{
    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Oluşturulan dinamik prompt
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    
    /// <summary>
    /// Prompt'a dahil edilen tablo sayısı
    /// </summary>
    public int TableCount { get; set; }
    
    /// <summary>
    /// Prompt'a dahil edilen kolon sayısı
    /// </summary>
    public int ColumnCount { get; set; }
    
    /// <summary>
    /// Bulunan JOIN path sayısı
    /// </summary>
    public int JoinPathCount { get; set; }
    
    /// <summary>
    /// Tahmini token sayısı
    /// </summary>
    public int EstimatedTokens { get; set; }
    
    /// <summary>
    /// İlgili bulunan tablo adları
    /// </summary>
    public List<string> RelevantTables { get; set; } = new();
    
    /// <summary>
    /// Çıkarılan anahtar kelimeler
    /// </summary>
    public List<string> ExtractedKeywords { get; set; } = new();
    
    /// <summary>
    /// Hata mesajı (başarısız ise)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Static prompt'a fallback yapıldı mı?
    /// </summary>
    public bool UsedFallback { get; set; }
}