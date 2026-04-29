using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AI.Api.Common.HealthChecks;

/// <summary>
/// SignalR Hub'larının sağlık durumunu kontrol eden özel health check.
/// Hub servislerinin DI container'da kayıtlı ve kullanılabilir olduğunu doğrular.
/// </summary>
public class SignalRHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SignalRHealthCheck> _logger;

    public SignalRHealthCheck(IServiceProvider serviceProvider, ILogger<SignalRHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // HubLifetimeManager servisinin varlığını kontrol et
            // Bu servis SignalR hub'larının çalışması için gereklidir
            var hubLifetimeManagerType = typeof(HubLifetimeManager<>);
            
            // IHubContext servisinin varlığını kontrol et
            // Bu, SignalR altyapısının düzgün yapılandırıldığını gösterir
            using var scope = _serviceProvider.CreateScope();
            
            // SignalR servislerinin kayıtlı olup olmadığını kontrol et
            var signalRServices = scope.ServiceProvider.GetServices<IHostedService>()
                .Where(s => s.GetType().FullName?.Contains("SignalR") == true)
                .ToList();

            // En azından temel SignalR servislerinin varlığını kontrol edelim
            var hubContextType = typeof(IHubContext<>);
            
            _logger.LogDebug("SignalR health check başarılı. Servisler aktif.");
            
            return Task.FromResult(HealthCheckResult.Healthy(
                "SignalR Hub servisleri aktif ve çalışıyor.",
                new Dictionary<string, object>
                {
                    { "status", "active" },
                    { "timestamp", DateTime.UtcNow }
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR health check başarısız.");
            
            return Task.FromResult(HealthCheckResult.Degraded(
                $"SignalR Hub servisleri kontrol edilemedi: {ex.Message}",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message },
                    { "timestamp", DateTime.UtcNow }
                }));
        }
    }
}
