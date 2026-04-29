using AI.Application.Common.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AI.Api.Hubs;

/// <summary>
/// AI uygulaması için SignalR Hub sınıfı
/// Connection yönetimi ve otomatik yeniden bağlanma desteği ile
/// </summary>
[Authorize]
public class AIHub : Hub
{
    private readonly ILogger<AIHub> _logger;

    public AIHub(ILogger<AIHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Client bağlandığında çalışır
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();

        using var activity = ActivitySources.ChatHistory.StartActivity("SignalRConnect");
        if (activity != null)
        {
            activity.SetTag("signalr.connection_id", connectionId);
            activity.SetTag("signalr.user_agent", userAgent);

            BaggageHelper.SetContextBaggage(requestId: connectionId);
            BaggageHelper.AddBaggageToActivity(activity);
        }

        _logger.LogInformation("SignalR bağlantısı kuruldu - ConnectionId: {ConnectionId}, UserAgent: {UserAgent}",
            connectionId, userAgent);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client bağlantısı kesildiğinde çalışır
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        if (exception != null)
        {
            _logger.LogWarning(exception, "SignalR bağlantısı hata ile kesildi - ConnectionId: {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogInformation("SignalR bağlantısı normal şekilde kesildi - ConnectionId: {ConnectionId}", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client'ın belirli bir gruba katılmasını sağlar
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client gruba eklendi - ConnectionId: {ConnectionId}, Group: {GroupName}", 
                Context.ConnectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gruba ekleme hatası - ConnectionId: {ConnectionId}, Group: {GroupName}", 
                Context.ConnectionId, groupName);
        }
    }

    /// <summary>
    /// Client'ın belirli bir gruptan ayrılmasını sağlar
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client gruptan çıkarıldı - ConnectionId: {ConnectionId}, Group: {GroupName}", 
                Context.ConnectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gruptan çıkarma hatası - ConnectionId: {ConnectionId}, Group: {GroupName}", 
                Context.ConnectionId, groupName);
        }
    }

    /// <summary>
    /// Client'ın bağlantı durumunu kontrol eder
    /// </summary>
    public async Task Ping()
    {
        try
        {
            await Clients.Caller.SendAsync("Pong", Context.ConnectionId);
            _logger.LogDebug("Ping-Pong başarılı - ConnectionId: {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping-Pong hatası - ConnectionId: {ConnectionId}", Context.ConnectionId);
        }
    }
}
