using Microsoft.AspNetCore.SignalR;

namespace AI.Application.Ports.Secondary.Services.Common;


/// <summary>
/// Abstraction for SignalR hub context to avoid circular dependency with API layer
/// </summary>
public interface ISignalRHubContext
{
    /// <summary>
    /// Get the hub clients for sending messages
    /// </summary>
    IHubClients Clients { get; }
    
    /// <summary>
    /// Get the hub groups for managing group membership
    /// </summary>
    IGroupManager Groups { get; }
}
