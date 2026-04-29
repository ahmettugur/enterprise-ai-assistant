using AI.Api.Hubs;
using AI.Application.Ports.Secondary.Services.Common;
using Microsoft.AspNetCore.SignalR;

namespace AI.Api.Common;

/// <summary>
/// SignalR hub context wrapper that implements ISignalRHubContext
/// Provides abstraction for Application layer services
/// </summary>
public sealed class SignalRHubContextWrapper : ISignalRHubContext
{
    private readonly IHubContext<AIHub> _hubContext;

    public SignalRHubContextWrapper(IHubContext<AIHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public IHubClients Clients => _hubContext.Clients;
    
    public IGroupManager Groups => _hubContext.Groups;
}
