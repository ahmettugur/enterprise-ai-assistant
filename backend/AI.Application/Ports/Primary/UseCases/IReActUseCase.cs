namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// ReAct (Reasoning + Acting) pattern için merkezi servis arayüzü.
/// Tüm kullanıcı-facing akışlarda THOUGHT ve OBSERVATION adımlarını yönetir.
/// ReAct devre dışı bırakıldığında tüm metodlar early return yapar.
/// </summary>
public interface IReActUseCase
{
    /// <summary>
    /// ReAct özelliğinin aktif olup olmadığını döndürür
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// THOUGHT adımını çalıştırır: Ayrı LLM çağrısı yaparak düşünce üretir ve frontend'e gönderir.
    /// ReAct devre dışıysa hiçbir şey yapmaz.
    /// </summary>
    /// <param name="connectionId">SignalR bağlantı ID'si</param>
    /// <param name="userPrompt">Kullanıcının orijinal promptu</param>
    /// <param name="flowContext">Akışa özel bağlam bilgisi (rapor listesi, döküman listesi vb.)</param>
    Task SendThoughtAsync(string connectionId, string userPrompt, string flowContext);

    /// <summary>
    /// OBSERVATION adımını frontend'e gönderir.
    /// ReAct devre dışıysa hiçbir şey yapmaz.
    /// </summary>
    /// <param name="connectionId">SignalR bağlantı ID'si</param>
    /// <param name="observationMessage">Gözlem mesajı (ör: "Rapor oluşturuluyor...")</param>
    Task SendObservationAsync(string connectionId, string observationMessage);
}
