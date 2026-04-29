using System.Text;
using AI.Application.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.IdentityModel.Tokens;

namespace AI.Api.Configuration;

/// <summary>
/// Authentication yapılandırma extension'ları
/// </summary>
public static class AuthenticationConfiguration
{
    /// <summary>
    /// JWT ve Windows Authentication'ı yapılandırır
    /// </summary>
    public static IServiceCollection AddAuthenticationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings are not configured");

        var adSettings = configuration.GetSection(ActiveDirectorySettings.SectionName).Get<ActiveDirectorySettings>();

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        // JWT Bearer Authentication
        authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false; // Development için, production'da true olmalı
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudiences = jwtSettings.Audiences,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
            };

            // SignalR için token'ı query string'den alma
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    // SignalR hub'larına bağlanırken token'ı query string'den al
                    if (!string.IsNullOrEmpty(accessToken) && 
                        (path.StartsWithSegments("/ai-hub") || path.StartsWithSegments("/hubs")))
                    {
                        context.Token = accessToken;
                    }
                    
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    // Token süresi dolmuşsa header'a bilgi ekle
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Windows Authentication (Active Directory SSO için)
        if (adSettings?.Enabled == true)
        {
            authBuilder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, options =>
            {
                // Windows Authentication varsayılan ayarları
            });
        }

        // Authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"))
            .AddPolicy("RequireOperatorRole", policy => policy.RequireRole("Admin", "Operator"))
            .AddPolicy("RequireUserRole", policy => policy.RequireRole("Admin", "Operator", "User"));

        return services;
    }
}
