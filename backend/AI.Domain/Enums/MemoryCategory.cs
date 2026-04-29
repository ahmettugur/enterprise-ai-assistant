namespace AI.Domain.Enums;

/// <summary>
/// Hafıza kategorileri
/// </summary>
public enum MemoryCategory
{
    /// <summary>
    /// Tercihler (format, dil, bölge vb.)
    /// </summary>
    Preference = 0,

    /// <summary>
    /// Etkileşim geçmişi (son baktığı raporlar, sık kullandığı özellikler)
    /// </summary>
    Interaction = 1,

    /// <summary>
    /// Geri bildirimler (beğendiği/beğenmediği yanıtlar)
    /// </summary>
    Feedback = 2,

    /// <summary>
    /// İş bağlamı (çalıştığı projeler, ilgilendiği konular)
    /// </summary>
    WorkContext = 3
}
