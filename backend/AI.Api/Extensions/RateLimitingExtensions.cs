using System.Threading.RateLimiting;
using AI.Application.Configuration;
using Microsoft.AspNetCore.RateLimiting;

namespace AI.Api.Extensions;

/// <summary>
/// Rate limiting yapılandırması için extension metodları
/// </summary>
public static class RateLimitingExtensions
{
    // Policy isimleri
    public const string FixedWindowPolicy = "fixed";
    public const string SlidingWindowPolicy = "sliding";
    public const string TokenBucketPolicy = "token";
    public const string ConcurrencyPolicy = "concurrency";
    public const string ChatPolicy = "chat";
    public const string DocumentUploadPolicy = "document-upload";
    public const string SearchPolicy = "search";

    /// <summary>
    /// Yapılandırılabilir policy'ler ile rate limiting servislerini ekler
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimiting").Get<RateLimitSettings>() 
                       ?? new RateLimitSettings();

        if (!settings.Enabled)
        {
            // Devre dışı bırakılmışsa boş rate limiter ekle
            services.AddRateLimiter(_ => { });
            return services;
        }

        services.AddRateLimiter(options =>
        {
            // Global reddetme durum kodu
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Rate limit aşıldığında özel yanıt
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue.TotalSeconds
                    : 60;

                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString("F0");

                var response = new
                {
                    success = false,
                    message = "Çok fazla istek gönderdiniz. Lütfen bekleyin.",
                    retryAfterSeconds = retryAfter
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };

            // Sabit Pencere Policy - Genel API kullanımı için
            // Örnek: 60 saniye içinde maksimum 100 istek yapılabilir.
            // Pencere bittiğinde sayaç sıfırlanır.
            // Kullanım: Basit API endpoint'leri için idealdir.
            options.AddFixedWindowLimiter(FixedWindowPolicy, opt =>
            {
                opt.PermitLimit = settings.FixedWindow.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(settings.FixedWindow.WindowSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.FixedWindow.QueueLimit;
            });

            // Kayan Pencere Policy - Daha hassas kontrol için
            // Örnek: Son 60 saniye içinde maksimum 100 istek (6 segment ile).
            // Her 10 saniyede bir segment kayar, ani yük dalgalanmalarını yumuşatır.
            // Kullanım: Daha adil rate limiting gerektiğinde tercih edilir.
            options.AddSlidingWindowLimiter(SlidingWindowPolicy, opt =>
            {
                opt.PermitLimit = settings.SlidingWindow.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(settings.SlidingWindow.WindowSeconds);
                opt.SegmentsPerWindow = settings.SlidingWindow.SegmentsPerWindow;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.SlidingWindow.QueueLimit;
            });

            // Token Kovası Policy - Ani yük artışlarına uyumlu
            // Örnek: Kovada 100 token var, her 10 saniyede 20 token eklenir.
            // Kullanıcı ani bir yük gönderebilir (burst), sonra beklemeli.
            // Kullanım: API'lerin kısa süreli yoğun kullanımına izin vermek için.
            options.AddTokenBucketLimiter(TokenBucketPolicy, opt =>
            {
                opt.TokenLimit = settings.TokenBucket.TokenLimit;
                opt.TokensPerPeriod = settings.TokenBucket.TokensPerPeriod;
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(settings.TokenBucket.ReplenishmentPeriodSeconds);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.TokenBucket.QueueLimit;
            });

            // Eşzamanlılık Policy - Eşzamanlı istek sayısını sınırlar
            // Örnek: Aynı anda maksimum 50 istek işlenebilir.
            // 51. istek kuyruğa alınır veya reddedilir.
            // Kullanım: Sunucu kaynaklarını korumak için (CPU, bellek, bağlantı havuzu).
            options.AddConcurrencyLimiter(ConcurrencyPolicy, opt =>
            {
                opt.PermitLimit = settings.Concurrency.PermitLimit;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = settings.Concurrency.QueueLimit;
            });

            // Chat Policy - AI sohbet için daha sıkı limitler (maliyetli işlemler)
            // Örnek: Kullanıcı hızlıca 20 mesaj gönderebilir (burst),
            // sonra her 10 saniyede sadece 5 mesaj gönderebilir.
            // Kullanım: OpenAI API maliyetlerini kontrol altında tutmak için.
            options.AddTokenBucketLimiter(ChatPolicy, opt =>
            {
                opt.TokenLimit = 20;           // Maksimum 20 istek ani yük
                opt.TokensPerPeriod = 5;       // Periyot başına 5 istek
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);  // Her 10 saniyede
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            // Doküman Yükleme Policy - Çok sıkı (ağır işlem)
            // Örnek: 1 dakikada maksimum 10 dosya yüklenebilir.
            // PDF işleme, embedding oluşturma gibi ağır işlemler için.
            // Kullanım: Sunucu kaynaklarını ve depolama maliyetlerini korumak için.
            options.AddFixedWindowLimiter(DocumentUploadPolicy, opt =>
            {
                opt.PermitLimit = 10;          // Pencere başına 10 yükleme
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });

            // Arama Policy - Orta seviye limitler
            // Örnek: 1 dakikada maksimum 60 arama (saniyede ortalama 1 arama).
            // Kayan pencere ile ani aramalar yumuşatılır.
            // Kullanım: Vektör veritabanı ve embedding API maliyetlerini kontrol için.
            options.AddSlidingWindowLimiter(SearchPolicy, opt =>
            {
                opt.PermitLimit = 60;          // Dakikada 60 arama
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });

            // İstemci IP'sine göre global limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                
                return RateLimitPartition.GetTokenBucketLimiter(clientIp, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 200,
                    TokensPerPeriod = 50,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                });
            });
        });

        return services;
    }

    /// <summary>
    /// Rate limiting middleware'ini kullanır
    /// </summary>
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }
}
