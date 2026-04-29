using AI.Domain.Identity;
using AI.Application.Configuration;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Infrastructure.Adapters.Persistence.Repositories;
using AI.Infrastructure.Adapters.External.Auth;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Infrastructure.Extensions;

/// <summary>
/// Authentication servislerinin DI extension'ları
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Authentication servislerini ve repository'leri kaydeder
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HttpContextAccessor - CurrentUserService için gerekli
        services.AddHttpContextAccessor();

        // Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ActiveDirectorySettings>(configuration.GetSection(ActiveDirectorySettings.SectionName));

        // Repositories — RefreshToken operations are part of IUserRepository (aggregate root)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        // AuthUseCase moved to Application layer - registered in ApplicationExtensions

        return services;
    }
}
