namespace AI.Application.DTOs.History;

/// <summary>
/// Context Summarization ayarları
/// </summary>
public class ContextSummarizationSettings
{
    /// <summary>
    /// Context summarization aktif mi?
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Özetleme tetiklenecek maksimum token sayısı
    /// Bu değer aşıldığında özetleme yapılır
    /// </summary>
    public int MaxTokenThreshold { get; set; } = 8000;

    /// <summary>
    /// Sliding window'da tutulacak son mesaj sayısı
    /// Bu mesajlar özeti takip eden detaylı mesajlardır
    /// </summary>
    public int SlidingWindowSize { get; set; } = 10;

    /// <summary>
    /// Özetin maksimum token uzunluğu
    /// </summary>
    public int SummaryMaxTokens { get; set; } = 500;

    /// <summary>
    /// Özet cache süresi (dakika)
    /// </summary>
    public int SummaryCacheTtlMinutes { get; set; } = 30;

    /// <summary>
    /// Özetleme için kullanılacak prompt şablonu
    /// </summary>
    public string SummaryPromptTemplate { get; set; } = @"
Aşağıdaki konuşma geçmişini kısa ve öz bir şekilde özetle.
Önemli noktaları, kararları ve bağlamı koru.
Özet, konuşmanın devamında yapay zekaya yeterli bağlam sağlamalıdır.

Özetlerken şunlara dikkat et:
- Kullanıcının ana soruları/istekleri
- Verilen önemli bilgiler (isimler, numaralar, tarihler)
- Alınan kararlar veya çözümler
- Devam eden konular

Konuşma:
{conversation}

Özet:";
}