namespace AI.Application.Configuration;

/// <summary>
/// ReAct (Reasoning + Acting) pattern konfigürasyonu
/// </summary>
public class ReActSettings
{
    /// <summary>
    /// ReAct özelliğini etkinleştirir/devre dışı bırakır
    /// false = Mevcut orchestrator akışı aynen çalışır
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Detaylı loglama aktif mi
    /// </summary>
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// ReAct adımlarını frontend'e SignalR ile gönder
    /// </summary>
    public bool SendStepsToFrontend { get; set; } = true;
}
