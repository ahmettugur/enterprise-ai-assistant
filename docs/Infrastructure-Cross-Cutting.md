# 🏗️ Infrastructure & Cross-Cutting Concerns

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Global Exception Handling](#global-exception-handling)
- [Rate Limiting](#rate-limiting)
- [Health Checks](#health-checks)
- [Cache Stratejisi (L1 + L2)](#cache-stratejisi-l1--l2)
- [API Versiyonlama](#api-versiyonlama)
- [Dosya Yapısı](#dosya-yapısı)

---

## Genel Bakış

Backend altyapısını oluşturan cross-cutting concern'ler:

| Bileşen | Dosya | Satır | Açıklama |
|---------|-------|-------|----------|
| **Exception Handling** | `GlobalExceptionHandler.cs` | 232 | RFC 7807 ProblemDetails |
| **Rate Limiting** | `RateLimitingExtensions.cs` | 179 | 7 policy, IP-based global limiter |
| **Health Checks** | `HealthChecksExtensions.cs` | 339 | 9 check + HTML dashboard |
| **Cache** | `DocumentCacheService.cs` | 285 | L1 Memory + L2 Redis |
| **API Versioning** | `ApiVersioningExtensions.cs` | 55 | Header-based versioning |
| **SignalR Health** | `SignalRHealthCheck.cs` | 68 | Custom health check |

---

## Global Exception Handling

**Dosya:** `AI.Api/Common/Exceptions/GlobalExceptionHandler.cs` (232 satır)

.NET 8+ `IExceptionHandler` implementasyonu — tüm unhandled exception'ları yakalar ve **RFC 7807 ProblemDetails** formatında döner.

### Exception → HTTP Status Mapping

```
Exception Tipi                    → Status Code         → Title
──────────────────────────────────────────────────────────────────────────────────
ArgumentNullException             → 400 Bad Request     → "Invalid Request"
ArgumentException                 → 400 Bad Request     → "Invalid Argument"
FormatException                   → 400 Bad Request     → "Invalid Format"
KeyNotFoundException              → 404 Not Found       → "Resource Not Found"
FileNotFoundException             → 404 Not Found       → "File Not Found"
UnauthorizedAccessException       → 401 Unauthorized    → "Unauthorized"
InvalidOperationException         → 409 Conflict        → "Invalid Operation"
DbUpdateException                 → 409 Conflict        → "Database Error"
TimeoutException                  → 504 Gateway Timeout → "Request Timeout"
TaskCanceledException (client)    → 400 Bad Request     → "Request Cancelled"
TaskCanceledException (timeout)   → 504 Gateway Timeout → "Request Timeout"
HttpRequestException (with code)  → (status code)       → "External Service Error"
HttpRequestException (no code)    → 502 Bad Gateway     → "External Service Error"
NotImplementedException           → 501 Not Implemented → "Not Implemented"
(diğer tüm exception'lar)        → 500 Internal Error   → "Internal Server Error"
```

### Response Formatı (RFC 7807)

```json
{
  "status": 400,
  "title": "Invalid Argument",
  "detail": "One or more parameters contain invalid values.",
  "instance": "/api/v1/chatbot",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "traceId": "00-abc123...",
  "timestamp": "2026-03-29T14:00:00Z",
  "exception": {
    "type": "ArgumentException",
    "message": "...",
    "stackTrace": ["..."],
    "innerException": "..."
  }
}
```

> **Not:** `exception` detayları sadece `Development` ortamında görünür.

### Loglama Stratejisi

```
Status Code >= 500  → LogLevel.Error    (kritik hatalar)
Status Code 400-499 → LogLevel.Warning  (kullanıcı hataları)
Diğer               → LogLevel.Information
```

### Kayıt

```csharp
// AI.Api/Extensions/ExceptionHandlingExtensions.cs (47 satır)
services.AddGlobalExceptionHandling();  // DI
app.UseGlobalExceptionHandling();       // Middleware (pipeline başında)
```

---

## Rate Limiting

**Dosya:** `AI.Api/Extensions/RateLimitingExtensions.cs` (179 satır)

7 farklı rate limiting policy + IP-based global limiter.

### Policy'ler

| Policy | Algoritma | Limit | Pencere | Kullanım |
|--------|-----------|-------|---------|----------|
| `fixed` | Fixed Window | 100 req | 60s | Genel API endpoint'leri |
| `sliding` | Sliding Window | 100 req | 60s (6 segment) | Hassas kontrol gereken API'ler |
| `token` | Token Bucket | 100 token, 20/periyot | 10s | Burst'e izin veren API'ler |
| `concurrency` | Concurrency | 50 eşzamanlı | — | Sunucu kaynak koruması |
| `chat` | Token Bucket | 20 token, 5/periyot | 10s | AI sohbet (OpenAI maliyet kontrolü) |
| `document-upload` | Fixed Window | 10 req | 60s | Dosya yükleme (ağır işlem) |
| `search` | Sliding Window | 60 req | 60s (6 segment) | Vektör arama |

### Global IP-Based Limiter

```csharp
// Her client IP için ayrı token bucket
options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetTokenBucketLimiter(clientIp, _ => new TokenBucketRateLimiterOptions
    {
        TokenLimit = 200,
        TokensPerPeriod = 50,
        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
        QueueLimit = 20
    });
});
```

### Rate Limit Aşıldığında

```json
HTTP 429 Too Many Requests
Retry-After: 60

{
  "success": false,
  "message": "Çok fazla istek gönderdiniz. Lütfen bekleyin.",
  "retryAfterSeconds": 60
}
```

### Konfigürasyon

```json
{
  "RateLimiting": {
    "Enabled": true,
    "FixedWindow": { "PermitLimit": 100, "WindowSeconds": 60, "QueueLimit": 10 },
    "SlidingWindow": { "PermitLimit": 100, "WindowSeconds": 60, "SegmentsPerWindow": 6, "QueueLimit": 10 },
    "TokenBucket": { "TokenLimit": 100, "TokensPerPeriod": 20, "ReplenishmentPeriodSeconds": 10, "QueueLimit": 10 },
    "Concurrency": { "PermitLimit": 50, "QueueLimit": 20 }
  }
}
```

`"Enabled": false` ile tamamen devre dışı bırakılabilir.

---

## Health Checks

**Dosya:** `AI.Api/Extensions/HealthChecksExtensions.cs` (339 satır)

### Kontrol Edilen Servisler (9 adet)

| Servis | Tag'ler | Fail Durumu | Açıklama |
|--------|---------|-------------|----------|
| **PostgreSQL** | db, postgresql, ready | Unhealthy | Ana veritabanı |
| **Redis** | cache, redis, ready | Degraded | Cache katmanı |
| **SQL Server** | db, sqlserver, ready | Degraded | AdventureWorks2022 |
| **Qdrant** | vector-db, qdrant, ready | Degraded | Vektör veritabanı |
| **Elasticsearch** | search, elasticsearch, ready | Degraded | Full-text arama |
| **SignalR Hub** | signalr, realtime, ready | Degraded | Custom health check |
| **Kibana** | monitoring, kibana, ready | Degraded | Log dashboard |
| **OTEL Collector** | telemetry, otel, ready | Degraded | OpenTelemetry |
| **Jaeger** | tracing, jaeger, ready | Degraded | Distributed tracing |
| **Self** | self, live | — | API çalışıyor mu? |

### Endpoint'ler

| Endpoint | Açıklama | Filter |
|----------|----------|--------|
| `/health` | Tüm kontroller (JSON) | Hepsi |
| `/health/ready` | Readiness probe | `ready` tag |
| `/health/live` | Liveness probe | `live` tag |
| `/health-ui` | HTML Dashboard | — |

### JSON Response Formatı

```json
{
  "status": "Healthy",
  "totalDuration": 123.45,
  "timestamp": "2026-03-29T14:00:00Z",
  "checks": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "duration": 12.3,
      "tags": ["db", "postgresql", "ready"]
    },
    {
      "name": "redis",
      "status": "Degraded",
      "duration": 5.1,
      "tags": ["cache", "redis", "ready"],
      "exception": "Connection refused"
    }
  ]
}
```

### HTML Dashboard (`/health-ui`)

TailwindCSS + Font Awesome ile görsel health check sayfası:

- Her bileşen için renkli kart (yeşil/sarı/kırmızı)
- 30 saniyede otomatik yenileme
- Responsive grid layout
- Detaylı süre ve hata bilgisi

### SignalR Custom Health Check

```csharp
// AI.Api/Common/HealthChecks/SignalRHealthCheck.cs (68 satır)
public class SignalRHealthCheck : IHealthCheck
{
    // DI container'da SignalR servislerinin varlığını kontrol eder
    // HubLifetimeManager ve IHubContext servislerini doğrular
}
```

---

## Cache Stratejisi (L1 + L2)

**Dosya:** `AI.Infrastructure/Adapters/External/Caching/DocumentCacheService.cs` (285 satır)

### İki Katmanlı Cache Mimarisi

```
İstek → L1 (Memory Cache) → L2 (Redis) → Veritabanı
         30 dk TTL            24 saat TTL
```

### Okuma Akışı

```
GetFromCacheAsync<T>(cacheKey):
    │
    ├─ L1 Memory Cache hit → return (en hızlı)
    │
    ├─ L2 Redis hit:
    │   ├─ Deserialize JSON
    │   ├─ L1'e yaz (30 dk TTL)
    │   └─ return
    │
    └─ Cache miss → null (caller DB'den okuyacak)
```

### Yazma Akışı

```
SetToCacheAsync<T>(cacheKey, value):
    │
    ├─ L2 Redis'e yaz (24 saat TTL)
    └─ L1 Memory'ye yaz (30 dk TTL)
```

### Cache Key Yapısı

| Prefix | Örnek | Açıklama |
|--------|-------|----------|
| `doc:category:` | `doc:category:all` | Tüm kategoriler |
| `doc:category:` | `doc:category:select` | Dropdown için kategoriler |
| `doc:display:` | `doc:display:all` | Tüm dokümanlar |
| `doc:display:` | `doc:display:category:{id}` | Kategoriye göre dokümanlar |
| `doc:display:` | `doc:display:select` | Dropdown için dokümanlar |
| `doc:user:` | `doc:user:{userId}:documents` | Kullanıcı bazlı dokümanlar |
| `doc:user:` | `doc:user:{userId}:categories` | Kullanıcı bazlı kategoriler |

### Cache Invalidation

```csharp
// Kategori değiştiğinde
await InvalidateCategoryCacheAsync();    // doc:category:all + doc:category:select

// Doküman değiştiğinde
await InvalidateDocumentCacheAsync();    // doc:display:all + doc:display:select

// Kullanıcı bazlı
await InvalidateUserCacheAsync(userId);  // doc:user:{userId}:*

// Tümünü temizle
await InvalidateAllAsync();              // Hepsi
```

### TTL Değerleri

| Katman | Süre | Nedeni |
|--------|------|--------|
| **L1 (Memory)** | 30 dakika | Bellek tüketimini sınırla |
| **L2 (Redis)** | 24 saat | Dokümanlar sık değişmez |

### Hata Toleransı

```csharp
// Cache hataları loglanır ama exception fırlatılmaz
catch (Exception ex)
{
    _logger.LogError(ex, "Error getting from cache: {CacheKey}", cacheKey);
    return null; // Graceful degradation — DB'den okumaya devam
}
```

---

## API Versiyonlama

**Dosya:** `AI.Api/Extensions/ApiVersioningExtensions.cs` (55 satır)

### Mevcut Durum

```csharp
public const string CurrentVersion = "v1";
public static readonly string[] SupportedVersions = { "v1" };
```

### Response Header'ları

Her API yanıtına otomatik eklenir:

```
X-Api-Version: v1
X-Api-Supported-Versions: v1
```

### Versiyon Bilgi Endpoint'i

```
GET /api/version
→ {
    "currentVersion": "v1",
    "supportedVersions": ["v1"],
    "deprecatedVersions": [],
    "documentation": "/swagger"
  }
```

---

## Dosya Yapısı

```
AI.Api/
├── Common/
│   ├── Exceptions/
│   │   └── GlobalExceptionHandler.cs      # RFC 7807 handler (232 satır)
│   ├── HealthChecks/
│   │   └── SignalRHealthCheck.cs           # Custom health check (68 satır)
│   └── SignalRHubContextWrapper.cs         # SignalR wrapper
│
├── Extensions/
│   ├── ExceptionHandlingExtensions.cs     # Exception DI + middleware (47 satır)
│   ├── RateLimitingExtensions.cs          # 7 rate limit policy (179 satır)
│   ├── HealthChecksExtensions.cs          # 9 health check + HTML UI (339 satır)
│   ├── ApiVersioningExtensions.cs         # Header versioning (55 satır)
│   ├── DependencyInjectionExtensions.cs   # Ana DI container
│   ├── ConversationServiceExtensions.cs   # Chat DI
│   └── StartupExtensions.cs              # Startup pipeline

AI.Infrastructure/Adapters/External/Caching/
└── DocumentCacheService.cs                # L1+L2 cache (285 satır)

AI.Application/
├── Configuration/
│   ├── CacheSettings.cs                   # Cache TTL konfigürasyonu
│   └── RateLimitSettings.cs               # Rate limit konfigürasyonu
├── Common/Constants/
│   └── CacheKeys.cs                       # Tutarlı cache key'ler
└── Ports/Secondary/Services/Cache/
    ├── IChatCacheService.cs               # Chat cache interface
    └── IDocumentCacheService.cs           # Document cache interface (Port)
```

---

## İlgili Dosyalar

| Dosya | Satır | Açıklama |
|-------|-------|----------|
| `GlobalExceptionHandler.cs` | 232 | RFC 7807 exception handler |
| `RateLimitingExtensions.cs` | 179 | 7 rate limit policy |
| `HealthChecksExtensions.cs` | 339 | 9 health check + HTML dashboard |
| `DocumentCacheService.cs` | 285 | L1 Memory + L2 Redis cache |
| `ApiVersioningExtensions.cs` | 55 | Header-based versioning |
| `ExceptionHandlingExtensions.cs` | 47 | Exception handler DI |
| `SignalRHealthCheck.cs` | 68 | Custom SignalR health check |

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [System-Overview.md](System-Overview.md) | Genel sistem mimarisi |
| [Hexagonal-Architecture.md](Hexagonal-Architecture.md) | Port/Adapter yapısı |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı |
| [Chat-System.md](Chat-System.md) | SignalR + streaming |
| [Authentication-Authorization.md](Authentication-Authorization.md) | Auth sistemi |
