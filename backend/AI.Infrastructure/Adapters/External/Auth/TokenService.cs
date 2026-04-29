
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AI.Application.Configuration;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AI.Infrastructure.Adapters.External.Auth;

/// <summary>
/// JWT Token servisi implementasyonu
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudiences = _jwtSettings.Audiences,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(_jwtSettings.ClockSkewSeconds)
        };
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var jwtId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("name", user.DisplayName),
            new("auth_source", user.AuthenticationSource.ToString())
        };

        // AD kullanıcısı ise ek bilgiler
        if (!string.IsNullOrEmpty(user.AdUsername))
        {
            claims.Add(new Claim("ad_username", user.AdUsername));
        }
        if (!string.IsNullOrEmpty(user.AdDomain))
        {
            claims.Add(new Claim("ad_domain", user.AdDomain));
        }
        if (!string.IsNullOrEmpty(user.Department))
        {
            claims.Add(new Claim("department", user.Department));
        }
        if (!string.IsNullOrEmpty(user.Title))
        {
            claims.Add(new Claim("title", user.Title));
        }

        // Roller
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audiences.FirstOrDefault(),
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Süresi dolmuş token'ları da doğrula
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudiences = _jwtSettings.Audiences,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey))
        };

        try
        {
            var principal = _tokenHandler.ValidateToken(accessToken, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool ValidateAccessToken(string accessToken)
    {
        try
        {
            _tokenHandler.ValidateToken(accessToken, _validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string? GetJtiFromToken(string accessToken)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(accessToken);
            return principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public Guid? GetUserIdFromToken(string accessToken)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(accessToken);
            var userIdClaim = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
