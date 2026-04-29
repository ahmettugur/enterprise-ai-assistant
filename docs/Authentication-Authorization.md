# 🔐 Authentication & Authorization Sistemi

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Mimari Yapı](#mimari-yapı)
- [Dosya Yapısı](#dosya-yapısı)
- [Authentication Akışları](#authentication-akışları)
- [JWT Token Yönetimi](#jwt-token-yönetimi)
- [Active Directory Entegrasyonu](#active-directory-entegrasyonu)
- [API Endpoints](#api-endpoints)
- [Domain Entities](#domain-entities)
- [Güvenlik Önlemleri](#güvenlik-önlemleri)
- [Konfigürasyon](#konfigürasyon)

---

## Genel Bakış

Sistem, **dual-authentication** mimarisi kullanır:

| Özellik | Değer |
|---------|-------|
| **Auth Yöntemleri** | Local (email/password) + Active Directory (Windows SSO) |
| **Token Türü** | JWT Bearer Token (HMAC-SHA256) |
| **Access Token Süresi** | 60 dakika (konfigürasyondan) |
| **Refresh Token Süresi** | 7 gün |
| **Refresh Token Boyutu** | 64 byte (Base64 encoded) |
| **Şifreleme** | BCrypt (domain entity içinde) |
| **Rol Tabanlı Yetkilendirme** | Admin, User (varsayılan) |
| **DDD Pattern** | Aggregate Root (User), Entity (RefreshToken) |

---

## Mimari Yapı

```
Frontend (Angular)
       │
       ▼
┌──────────────────────────────────────────────────────┐
│                    AI.Api Layer                        │
│  ┌─────────────────────────────────────────────────┐ │
│  │ AuthEndpoints.cs (300 satır)                    │ │
│  │ POST /api/auth/login                            │ │
│  │ POST /api/auth/windows-login                    │ │
│  │ POST /api/auth/refresh                          │ │
│  │ POST /api/auth/register                         │ │
│  │ POST /api/auth/logout                           │ │
│  │ POST /api/auth/logout-all                       │ │
│  │ GET  /api/auth/me                               │ │
│  │ POST /api/auth/change-password                  │ │
│  └──────────────────┬──────────────────────────────┘ │
│                     │ IAuthUseCase                    │
├─────────────────────┼────────────────────────────────┤
│              Application Layer                        │
│  ┌──────────────────▼──────────────────────────────┐ │
│  │ AuthUseCase.cs (318 satır)                      │ │
│  │ ├─ LoginAsync()                                 │ │
│  │ ├─ WindowsLoginAsync()                          │ │
│  │ ├─ RefreshTokenAsync()                          │ │
│  │ ├─ RegisterAsync()                              │ │
│  │ ├─ LogoutAsync() / LogoutAllAsync()             │ │
│  │ ├─ ChangePasswordAsync()                        │ │
│  │ └─ GetUserInfoAsync()                           │ │
│  └──────────────────┬──────────────────────────────┘ │
│          ┌──────────┼──────────┐                     │
│          ▼          ▼          ▼                     │
│   ITokenService  IUserRepo  IRoleRepo                │
├─────────────────────┼────────────────────────────────┤
│           Infrastructure Layer                        │
│  ┌──────────────────▼──────────────────────────────┐ │
│  │ TokenService.cs (180 satır)                     │ │
│  │ ├─ GenerateAccessToken() — JWT + Claims         │ │
│  │ ├─ GenerateRefreshTokenString() — Crypto RNG    │ │
│  │ ├─ GetPrincipalFromExpiredToken()               │ │
│  │ ├─ ValidateAccessToken()                        │ │
│  │ └─ GetUserIdFromToken()                         │ │
│  ├─────────────────────────────────────────────────┤ │
│  │ CurrentUserService.cs (48 satır)                │ │
│  │ HttpContext → UserId, Email, Roles, IsAdmin     │ │
│  └─────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────┘
```

---

## Dosya Yapısı

```
AI.Application/
├── Ports/Primary/UseCases/
│   └── IAuthUseCase.cs                    # Auth interface
├── Ports/Secondary/Services/Auth/
│   ├── ITokenService.cs                   # Token işlemleri interface
│   └── ICurrentUserService.cs             # Mevcut kullanıcı bilgisi interface
├── UseCases/
│   └── AuthUseCase.cs                     # Auth implementasyonu (318 satır)
├── Configuration/
│   ├── JwtSettings.cs                     # JWT konfigürasyonu
│   └── ActiveDirectorySettings.cs         # AD konfigürasyonu
└── DTOs/Auth/
    ├── LoginRequest.cs                    # Email + Password
    ├── RegisterRequest.cs                 # Email + Password + DisplayName
    ├── RefreshTokenRequest.cs             # AccessToken + RefreshToken
    ├── ChangePasswordRequest.cs           # CurrentPassword + NewPassword
    ├── WindowsLoginRequest.cs             # RememberMe flag
    ├── AuthResponse.cs                    # Tokens + UserInfo
    └── UserInfo.cs                        # Id, Email, DisplayName, Roles

AI.Infrastructure/Adapters/External/Auth/
├── TokenService.cs                        # JWT token üretimi (180 satır)
└── CurrentUserService.cs                  # HttpContext claim okuma (48 satır)

AI.Domain/Identity/
├── User.cs                                # Aggregate Root — CreateLocalUser, CreateFromActiveDirectory
├── Role.cs                                # Role entity — Admin, User
├── RefreshToken.cs                        # Entity — Token, JwtId, ExpiresAt, Revoke()
├── IUserRepository.cs                     # Repository interface
└── IRoleRepository.cs                     # Repository interface

AI.Domain/Enums/
└── AuthenticationSource.cs                # Local | ActiveDirectory

AI.Api/Endpoints/Auth/
└── AuthEndpoints.cs                       # 8 REST endpoint (300 satır)
```

---

## Authentication Akışları

### 1. Local Login (Email/Password)

```
Kullanıcı → POST /api/auth/login {email, password}
    │
    ├─ User bulunamadı → 401 Unauthorized
    ├─ AuthSource ≠ Local → 401 "AD ile giriş yapmalı"
    ├─ IsActive = false → 401 "Hesap devre dışı"
    ├─ VerifyPassword() başarısız → 401
    │
    └─ Başarılı:
       ├─ Roles yükle
       ├─ Access Token oluştur (JWT + claims)
       ├─ Refresh Token oluştur (64 byte crypto)
       ├─ User.AddRefreshToken() — DDD pattern
       ├─ DB'ye kaydet + LastLogin güncelle
       └─ AuthResponse döndür
```

### 2. Active Directory Login (Windows SSO)

```
Kullanıcı → POST /api/auth/windows-login (Negotiate auth)
    │
    ├─ AD devre dışı → 400 "AD not enabled"
    ├─ Windows Identity parse: DOMAIN\username
    │
    ├─ User bulunamadı + AutoCreate=false → 401
    ├─ User bulunamadı + AutoCreate=true:
    │   ├─ User.CreateFromActiveDirectory()
    │   ├─ Varsayılan AD rolü ata
    │   └─ DB'ye kaydet
    │
    ├─ IsActive = false → 401
    └─ Başarılı → CreateAuthResponseAsync()
```

### 3. Token Refresh

```
Client → POST /api/auth/refresh {accessToken, refreshToken}
    │
    ├─ GetPrincipalFromExpiredToken() — expired token'dan claim oku
    ├─ JTI çıkar — token eşleşme kontrolü
    ├─ RefreshToken DB'den getir
    │   ├─ Bulunamadı → 401
    │   ├─ IsActive = false → 401 "Süresi dolmuş"
    │   └─ JwtId ≠ JTI → 401 "Token eşleşmesi başarısız"
    │
    ├─ User kontrol (var mı, aktif mi)
    ├─ Eski refresh token iptal (Revoke)
    └─ Yeni token pair oluştur → AuthResponse
```

### 4. Şifre Değişikliği

```
Client → POST /api/auth/change-password {currentPassword, newPassword, confirmNewPassword}
    │
    ├─ Şifreler eşleşmiyor → 400
    ├─ AuthSource ≠ Local → 400 "AD kullanıcıları değiştiremez"
    ├─ Mevcut şifre yanlış → 401
    │
    └─ Başarılı:
       ├─ user.SetPassword(newPassword)
       ├─ Tüm refresh token'ları iptal (güvenlik)
       └─ 200 OK
```

---

## JWT Token Yönetimi

### Access Token Claims

```csharp
// AI.Infrastructure/Adapters/External/Auth/TokenService.cs
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, user.Id),       // Kullanıcı ID
    new(JwtRegisteredClaimNames.Email, user.Email),  // Email
    new(JwtRegisteredClaimNames.Jti, jwtId),         // Unique token ID
    new(JwtRegisteredClaimNames.Iat, timestamp),     // Issued at
    new("name", user.DisplayName),                   // Görünen ad
    new("auth_source", "Local|ActiveDirectory"),     // Auth kaynağı
    new("ad_username", user.AdUsername),              // AD kullanıcı (opsiyonel)
    new("ad_domain", user.AdDomain),                 // AD domain (opsiyonel)
    new("department", user.Department),              // Departman (opsiyonel)
    new("title", user.Title),                        // Ünvan (opsiyonel)
    new(ClaimTypes.Role, "Admin"),                   // Roller (çoklu)
};
```

### Token Doğrulama Parametreleri

```csharp
// TokenService constructor
_validationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = _jwtSettings.Issuer,
    ValidAudiences = _jwtSettings.Audiences,        // Çoklu audience desteği
    IssuerSigningKey = new SymmetricSecurityKey(...), // HMAC-SHA256
    ClockSkew = TimeSpan.FromSeconds(clockSkewSeconds)
};
```

### Refresh Token — DDD Pattern

```csharp
// AI.Domain/Identity/User.cs (Aggregate Root)
var refreshToken = user.AddRefreshToken(
    tokenString,    // 64 byte crypto random (Base64)
    jti,            // Access token JTI — eşleşme için
    expiryDays: 7,  // 7 gün geçerli
    ipAddress,      // Client IP (audit)
    userAgent       // Client user agent (audit)
);
```

---

## Active Directory Entegrasyonu

### Konfigürasyon

```json
{
  "ActiveDirectory": {
    "Enabled": true,
    "AutoCreateUsers": true,
    "DefaultRole": "User",
    "Domain": "CORP"
  }
}
```

### Özellikler

- **Windows Negotiate Authentication** — Tarayıcı SSO (NTLM/Kerberos)
- **Auto-Create Users** — İlk girişte otomatik kullanıcı oluşturma
- **Domain Mapping** — `DOMAIN\username` → `username@domain.local`
- **Varsayılan Rol** — Konfigürasyondan alınır

---

## API Endpoints

| Metod | Endpoint | Auth | Açıklama |
|-------|----------|------|----------|
| `POST` | `/api/auth/login` | ❌ | Email/password ile giriş |
| `POST` | `/api/auth/windows-login` | 🪟 Negotiate | Windows SSO ile giriş |
| `POST` | `/api/auth/refresh` | ❌ | Token yenileme |
| `POST` | `/api/auth/register` | ❌ | Yeni kullanıcı kaydı |
| `POST` | `/api/auth/logout` | ✅ | Tek oturumu kapat |
| `POST` | `/api/auth/logout-all` | ✅ | Tüm oturumları kapat |
| `GET` | `/api/auth/me` | ✅ | Kullanıcı bilgileri |
| `POST` | `/api/auth/change-password` | ✅ | Şifre değiştir |

---

## Domain Entities

### User (Aggregate Root)

```csharp
// AI.Domain/Identity/User.cs
public class User : AggregateRoot<string>
{
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? Department { get; private set; }
    public string? Title { get; private set; }
    public AuthenticationSource AuthenticationSource { get; private set; }
    public string? AdUsername { get; private set; }
    public string? AdDomain { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Factory Methods
    public static User CreateLocalUser(email, displayName, password);
    public static User CreateFromActiveDirectory(email, displayName, adUsername, adDomain);

    // Domain Methods
    public bool VerifyPassword(string password);
    public void SetPassword(string newPassword);
    public RefreshToken AddRefreshToken(token, jti, expiryDays, ip, userAgent);
}
```

### RefreshToken (Entity)

```csharp
// AI.Domain/Identity/RefreshToken.cs
public class RefreshToken : Entity<Guid>
{
    public string Token { get; private set; }
    public string JwtId { get; private set; }        // Access token ile eşleşme
    public string UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? CreatedByIp { get; private set; }  // Audit trail
    public string? UserAgent { get; private set; }     // Audit trail
    public bool IsRevoked { get; private set; }
    public string? RevokedByIp { get; private set; }

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
    public void Revoke(string? ipAddress);
}
```

### CurrentUserService

```csharp
// AI.Infrastructure/Adapters/External/Auth/CurrentUserService.cs
public sealed class CurrentUserService : ICurrentUserService
{
    // HttpContext.User claim'lerinden okur:
    public string? UserId { get; }       // sub claim
    public string? Email { get; }        // email claim
    public string? DisplayName { get; }  // name claim
    public IEnumerable<string> Roles     // role claims
    public bool IsAuthenticated { get; }
    public bool IsAdmin { get; }         // "Admin" role check
}
```

---

## Güvenlik Önlemleri

| Önlem | Implementasyon |
|-------|----------------|
| **Şifre Hash** | BCrypt (domain entity içinde — `VerifyPassword`, `SetPassword`) |
| **Token Eşleşme** | Refresh token JwtId ↔ Access token JTI |
| **Token İptali** | `Revoke()` ile IP kaydı tutulur |
| **Şifre Değişikliğinde** | Tüm refresh token'lar otomatik iptal |
| **IP Tracking** | Login, refresh, logout işlemlerinde IP kaydı |
| **User Agent Tracking** | Oturum bazlı cihaz takibi |
| **Inactive Check** | Her auth işleminde `IsActive` kontrolü |
| **AD Kullanıcı Kısıtlama** | AD kullanıcıları local şifre değiştiremez |

---

## Konfigürasyon

### appsettings.json

```json
{
  "JwtSettings": {
    "SecretKey": "...",
    "Issuer": "AI.Api",
    "Audiences": ["AI.Frontend", "AI.Scheduler"],
    "AccessTokenExpirationMinutes": 60,
    "ClockSkewSeconds": 30
  },
  "ActiveDirectory": {
    "Enabled": true,
    "AutoCreateUsers": true,
    "DefaultRole": "User"
  }
}
```

---

## İlgili Dosyalar

| Dosya | Satır | Açıklama |
|-------|-------|----------|
| `AuthUseCase.cs` | 318 | Auth business logic |
| `AuthEndpoints.cs` | 300 | 8 REST endpoint |
| `TokenService.cs` | 180 | JWT üretimi ve doğrulama |
| `CurrentUserService.cs` | 48 | HttpContext → user bilgisi |
| `User.cs` | — | DDD Aggregate Root |
| `RefreshToken.cs` | — | Token entity |
| `Role.cs` | — | Rol entity |

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [System-Overview.md](System-Overview.md) | Genel sistem analizi |
| [Hexagonal-Architecture.md](Hexagonal-Architecture.md) | Port/Adapter mimarisi |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
| [Infrastructure-Cross-Cutting.md](Infrastructure-Cross-Cutting.md) | Rate limiting, error handling |
