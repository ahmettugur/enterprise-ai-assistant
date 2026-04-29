using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AI.Api.Extensions;

/// <summary>
/// Extension methods for configuring health checks with UI
/// </summary>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Adds comprehensive health check services
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // PostgreSQL health check
        var postgresConnectionString = configuration.GetConnectionString("PostgreSQL");
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            healthChecksBuilder.AddNpgSql(
                postgresConnectionString,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "postgresql", "ready" });
        }

        // Redis health check
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "cache", "redis", "ready" });
        }

        // SQL Server health check (AdventureWorks)
        var sqlServerConnectionString = configuration.GetConnectionString("AdventureWorks2022");
        if (!string.IsNullOrEmpty(sqlServerConnectionString))
        {
            healthChecksBuilder.AddSqlServer(
                sqlServerConnectionString,
                name: "sqlserver-adventureworks",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "db", "sqlserver", "ready" });
        }

        // Qdrant health check (custom)
        var qdrantHost = configuration["Qdrant:Host"];
        var qdrantPort = configuration["Qdrant:Port"];
        if (!string.IsNullOrEmpty(qdrantHost))
        {
            healthChecksBuilder.AddUrlGroup(
                new Uri($"http://{qdrantHost}:{qdrantPort ?? "6333"}/healthz"),
                name: "qdrant",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "vector-db", "qdrant", "ready" });
        }

        // Elasticsearch health check
        var elasticUri = configuration["ElasticSearch:Uri"];
        if (!string.IsNullOrEmpty(elasticUri))
        {
            healthChecksBuilder.AddUrlGroup(
                new Uri($"{elasticUri}/_cluster/health"),
                name: "elasticsearch",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "search", "elasticsearch", "ready" });
        }

        // SignalR Hub health check
        // SignalR servislerinin DI container'da kayıtlı ve çalışır durumda olduğunu kontrol eder
        healthChecksBuilder.AddCheck<AI.Api.Common.HealthChecks.SignalRHealthCheck>(
            name: "signalr-hub",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "signalr", "realtime", "ready" });

        // Kibana health check
        // Kibana dashboard'unun erişilebilir olduğunu kontrol eder (varsayılan port: 5601)
        var kibanaUri = configuration["Kibana:Uri"] ?? "http://localhost:5601";
        healthChecksBuilder.AddUrlGroup(
            new Uri($"{kibanaUri}/api/status"),
            name: "kibana",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "monitoring", "kibana", "ready" });

        // OpenTelemetry Collector health check
        // OTEL Collector'ın çalışır durumda olduğunu kontrol eder (varsayılan port: 13133)
        var otelCollectorUri = configuration["OpenTelemetry:OtlpExporter:HealthEndpoint"] ?? "http://localhost:13133";
        healthChecksBuilder.AddUrlGroup(
            new Uri($"{otelCollectorUri}/health"),
            name: "otel-collector",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "telemetry", "otel", "ready" });

        // Jaeger health check
        // Jaeger tracing backend'inin erişilebilir olduğunu kontrol eder (varsayılan port: 14269)
        var jaegerUri = configuration["Jaeger:Uri"] ?? "http://localhost:14269";
        healthChecksBuilder.AddUrlGroup(
            new Uri($"{jaegerUri}/"),
            name: "jaeger",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "tracing", "jaeger", "ready" });

        // Self health check
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy("API çalışıyor"), 
            tags: new[] { "self", "live" });

        return services;
    }

    /// <summary>
    /// Maps health check endpoints with detailed UI
    /// </summary>
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Liveness probe - simple check if app is running
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse
        });

        // Readiness probe - check if app is ready to receive traffic
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        // Full health check - all checks
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        // Health UI endpoint (HTML page)
        endpoints.MapGet("/health-ui", async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(GetHealthCheckHtmlPage());
        });

        return endpoints;
    }

    /// <summary>
    /// Writes a detailed JSON response for health checks
    /// </summary>
    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds,
                tags = entry.Value.Tags,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data.Count > 0 ? entry.Value.Data : null
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    /// <summary>
    /// Returns an HTML page for health check visualization
    /// </summary>
    private static string GetHealthCheckHtmlPage()
    {
        return """
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>AI.Api - Health Check Dashboard</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" rel="stylesheet">
    <style>
        .status-healthy { background: linear-gradient(135deg, #10b981 0%, #059669 100%); }
        .status-degraded { background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); }
        .status-unhealthy { background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); }
        .pulse { animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite; }
        @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: .5; } }
        .card-hover { transition: transform 0.2s, box-shadow 0.2s; }
        .card-hover:hover { transform: translateY(-2px); box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1); }
    </style>
</head>
<body class="bg-gray-100 min-h-screen">
    <div class="container mx-auto px-4 py-8">
        <!-- Header -->
        <div class="text-center mb-8">
            <h1 class="text-3xl font-bold text-gray-800 mb-2">
                <i class="fas fa-heartbeat text-red-500 mr-2"></i>
                AI.Api Health Dashboard
            </h1>
            <p class="text-gray-600">Sistem sağlık durumu ve bileşen izleme</p>
        </div>

        <!-- Overall Status -->
        <div id="overall-status" class="mb-8 p-6 rounded-xl shadow-lg text-white text-center">
            <div class="text-5xl mb-2">
                <i id="overall-icon" class="fas fa-spinner fa-spin"></i>
            </div>
            <h2 id="overall-text" class="text-2xl font-bold">Kontrol ediliyor...</h2>
            <p id="overall-duration" class="text-sm opacity-80 mt-1"></p>
        </div>

        <!-- Health Checks Grid -->
        <div id="health-checks" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <!-- Loading placeholder -->
            <div class="bg-white rounded-xl p-6 shadow animate-pulse">
                <div class="h-4 bg-gray-200 rounded w-3/4 mb-4"></div>
                <div class="h-8 bg-gray-200 rounded w-1/2"></div>
            </div>
        </div>

        <!-- Last Updated -->
        <div class="text-center mt-8 text-gray-500 text-sm">
            <p>Son güncelleme: <span id="last-updated">-</span></p>
            <button onclick="refreshHealth()" class="mt-2 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition">
                <i class="fas fa-sync-alt mr-1"></i> Yenile
            </button>
        </div>

        <!-- API Endpoints Info -->
        <div class="mt-8 bg-white rounded-xl p-6 shadow">
            <h3 class="text-lg font-semibold text-gray-800 mb-4">
                <i class="fas fa-link text-blue-500 mr-2"></i>Health Check Endpoints
            </h3>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                <a href="/health" target="_blank" class="block p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition">
                    <code class="text-blue-600">/health</code>
                    <p class="text-sm text-gray-600 mt-1">Tüm kontroller (JSON)</p>
                </a>
                <a href="/health/ready" target="_blank" class="block p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition">
                    <code class="text-green-600">/health/ready</code>
                    <p class="text-sm text-gray-600 mt-1">Readiness probe</p>
                </a>
                <a href="/health/live" target="_blank" class="block p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition">
                    <code class="text-purple-600">/health/live</code>
                    <p class="text-sm text-gray-600 mt-1">Liveness probe</p>
                </a>
            </div>
        </div>
    </div>

    <script>
        const statusConfig = {
            'Healthy': { icon: 'fa-check-circle', class: 'status-healthy', text: 'Sağlıklı' },
            'Degraded': { icon: 'fa-exclamation-triangle', class: 'status-degraded', text: 'Düşük Performans' },
            'Unhealthy': { icon: 'fa-times-circle', class: 'status-unhealthy', text: 'Sorunlu' }
        };

        async function refreshHealth() {
            try {
                const response = await fetch('/health');
                const data = await response.json();
                
                // Update overall status
                const overallStatus = document.getElementById('overall-status');
                const overallIcon = document.getElementById('overall-icon');
                const overallText = document.getElementById('overall-text');
                const overallDuration = document.getElementById('overall-duration');
                
                const config = statusConfig[data.status] || statusConfig['Unhealthy'];
                overallStatus.className = `mb-8 p-6 rounded-xl shadow-lg text-white text-center ${config.class}`;
                overallIcon.className = `fas ${config.icon}`;
                overallText.textContent = `Sistem Durumu: ${config.text}`;
                overallDuration.textContent = `Toplam süre: ${data.totalDuration.toFixed(2)}ms`;

                // Update health checks grid
                const checksContainer = document.getElementById('health-checks');
                checksContainer.innerHTML = data.checks.map(check => {
                    const checkConfig = statusConfig[check.status] || statusConfig['Unhealthy'];
                    const tags = check.tags ? check.tags.join(', ') : '';
                    
                    return `
                        <div class="bg-white rounded-xl p-6 shadow card-hover">
                            <div class="flex items-center justify-between mb-4">
                                <h3 class="font-semibold text-gray-800">${check.name}</h3>
                                <span class="px-3 py-1 rounded-full text-white text-xs ${checkConfig.class}">
                                    <i class="fas ${checkConfig.icon} mr-1"></i>${checkConfig.text}
                                </span>
                            </div>
                            <div class="text-sm text-gray-600">
                                <p><i class="fas fa-clock mr-1"></i> ${check.duration.toFixed(2)}ms</p>
                                ${tags ? `<p class="mt-1"><i class="fas fa-tags mr-1"></i> ${tags}</p>` : ''}
                                ${check.description ? `<p class="mt-1"><i class="fas fa-info-circle mr-1"></i> ${check.description}</p>` : ''}
                                ${check.exception ? `<p class="mt-1 text-red-500"><i class="fas fa-bug mr-1"></i> ${check.exception}</p>` : ''}
                            </div>
                        </div>
                    `;
                }).join('');

                // Update timestamp
                document.getElementById('last-updated').textContent = new Date().toLocaleString('tr-TR');
                
            } catch (error) {
                console.error('Health check error:', error);
                document.getElementById('overall-status').className = 'mb-8 p-6 rounded-xl shadow-lg text-white text-center status-unhealthy';
                document.getElementById('overall-icon').className = 'fas fa-times-circle';
                document.getElementById('overall-text').textContent = 'Bağlantı Hatası';
            }
        }

        // Initial load
        refreshHealth();
        
        // Auto-refresh every 30 seconds
        setInterval(refreshHealth, 30000);
    </script>
</body>
</html>
""";
    }
}
