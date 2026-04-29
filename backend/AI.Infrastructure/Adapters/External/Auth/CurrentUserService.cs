
using System.Security.Claims;
using AI.Application.Ports.Secondary.Services.Auth;
using Microsoft.AspNetCore.Http;

namespace AI.Infrastructure.Adapters.External.Auth;

/// <summary>
/// HttpContext üzerinden mevcut kullanıcı bilgilerini sağlayan servis
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User?.FindFirst("sub")?.Value;

    /// <inheritdoc />
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value 
                         ?? User?.FindFirst("email")?.Value;

    /// <inheritdoc />
    public string? DisplayName => User?.FindFirst(ClaimTypes.Name)?.Value 
                               ?? User?.FindFirst("name")?.Value;

    /// <inheritdoc />
    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role)
                                            .Select(c => c.Value)
                                        ?? Enumerable.Empty<string>();

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    /// <inheritdoc />
    public bool IsAdmin => IsInRole("Admin");
}
