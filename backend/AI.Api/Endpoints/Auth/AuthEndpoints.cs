using AI.Application.DTOs.Auth;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Auth;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Endpoints.Auth;

/// <summary>
/// Authentication API Endpoints
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Login with email/password
        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithDescription("Email ve şifre ile giriş yapar");

        // Windows Authentication login
        group.MapPost("/windows-login", WindowsLoginAsync)
            .RequireAuthorization(policy => policy.AddAuthenticationSchemes(NegotiateDefaults.AuthenticationScheme).RequireAuthenticatedUser())
            .WithName("WindowsLogin")
            .WithDescription("Windows Authentication (SSO) ile giriş yapar");

        // Refresh token
        group.MapPost("/refresh", RefreshTokenAsync)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithDescription("Access token'ı yeniler");

        // Logout
        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .WithName("Logout")
            .WithDescription("Oturumu kapatır");

        // Logout all sessions
        group.MapPost("/logout-all", LogoutAllAsync)
            .RequireAuthorization()
            .WithName("LogoutAll")
            .WithDescription("Tüm oturumları kapatır");

        // Register (local user)
        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("Register")
            .WithDescription("Yeni kullanıcı kaydı yapar");

        // Get current user info
        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithDescription("Mevcut kullanıcı bilgilerini getirir");

        // Change password
        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .WithName("ChangePassword")
            .WithDescription("Şifre değiştirir");

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] IAuthUseCase authService,
        HttpContext httpContext)
    {
        try
        {
            var ipAddress = GetIpAddress(httpContext);
            var userAgent = GetUserAgent(httpContext);
            
            var response = await authService.LoginAsync(request, ipAddress, userAgent);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> WindowsLoginAsync(
        [FromBody] WindowsLoginRequest request,
        [FromServices] IAuthUseCase authService,
        HttpContext httpContext)
    {
        try
        {
            var windowsIdentity = httpContext.User.Identity;
            if (windowsIdentity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            // Windows kimliğinden kullanıcı adı ve domain'i çıkar
            var windowsName = windowsIdentity.Name; // Format: DOMAIN\username
            var parts = windowsName?.Split('\\');
            
            if (parts is null || parts.Length != 2)
            {
                return Results.Problem("Geçersiz Windows kimliği formatı");
            }

            var domain = parts[0];
            var username = parts[1];

            var ipAddress = GetIpAddress(httpContext);
            var userAgent = GetUserAgent(httpContext);

            var response = await authService.WindowsLoginAsync(
                username, 
                domain, 
                request.RememberMe, 
                ipAddress, 
                userAgent);

            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IAuthUseCase authService,
        HttpContext httpContext)
    {
        try
        {
            var ipAddress = GetIpAddress(httpContext);
            var userAgent = GetUserAgent(httpContext);
            
            var response = await authService.RefreshTokenAsync(request, ipAddress, userAgent);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] string refreshToken,
        [FromServices] IAuthUseCase authService,
        HttpContext httpContext)
    {
        try
        {
            var ipAddress = GetIpAddress(httpContext);
            await authService.LogoutAsync(refreshToken, ipAddress);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> LogoutAllAsync(
        [FromServices] IAuthUseCase authService,
        [FromServices] ICurrentUserService currentUserService,
        HttpContext httpContext)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var ipAddress = GetIpAddress(httpContext);
            await authService.LogoutAllAsync(userId, ipAddress);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        [FromServices] IAuthUseCase authService,
        HttpContext httpContext)
    {
        try
        {
            var ipAddress = GetIpAddress(httpContext);
            var userAgent = GetUserAgent(httpContext);
            
            var response = await authService.RegisterAsync(request, ipAddress, userAgent);
            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> GetCurrentUserAsync(
        [FromServices] IAuthUseCase authService,
        [FromServices] ICurrentUserService currentUserService)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var userInfo = await authService.GetUserInfoAsync(userId);
            if (userInfo is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(userInfo);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        [FromServices] IAuthUseCase authService,
        [FromServices] ICurrentUserService currentUserService)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            await authService.ChangePasswordAsync(userId, request);
            return Results.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static string? GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static string? GetUserAgent(HttpContext context)
    {
        return context.Request.Headers.UserAgent.ToString();
    }
}
