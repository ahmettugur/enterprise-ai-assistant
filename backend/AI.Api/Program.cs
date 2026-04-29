using AI.Api.Extensions;
using AI.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ============================================================================
// KESTREL SERVER CONFIGURATION (Large file upload support)
// ============================================================================
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 150 * 1024 * 1024; // 150MB (Base64 encoded files ~1.37x larger)
});

// ============================================================================
// GLOBAL EXCEPTION HANDLING
// ============================================================================
builder.Services.AddGlobalExceptionHandling();

// ============================================================================
// OPENTELEMETRY TRACING CONFIGURATION
// ============================================================================
builder.Services.AddOpenTelemetryTracing(builder.Configuration);

// ============================================================================
// CHAT HISTORY STORAGE CONFIGURATION
// ============================================================================
builder.Services.AddChatConversationUseCases(builder.Configuration);

// ============================================================================
// RATE LIMITING CONFIGURATION
// ============================================================================
builder.Services.AddRateLimitingServices(builder.Configuration);

// ============================================================================
// HEALTH CHECKS CONFIGURATION
// ============================================================================
builder.Services.AddHealthCheckServices(builder.Configuration);

// ============================================================================
// SERVICES CONFIGURATION
// ============================================================================
builder.Services.ConfigureServices(builder.Configuration);

// ============================================================================
// BUILD AND CONFIGURE APPLICATION
// ============================================================================
var app = builder.Build();

// Global exception handler MUST be first in the pipeline
app.UseGlobalExceptionHandling();

app.UseCors();
app.UseApiVersionHeader();
app.UseRateLimitingMiddleware();
app.ConfigureStaticFiles();
app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAiApiEndpoints();

// ============================================================================
// APPLICATION INITIALIZATION
// ============================================================================
await app.EnsureDatabaseMigrationAsync();

// Varsayılan admin kullanıcısını seed et
await app.Services.SeedDefaultUsersAsync();

// User memory collection'ı Qdrant'ta oluştur
await app.InitializeUserMemoryCollectionAsync();

// Neo4j Schema Catalog'u başlat (yoksa oluştur)
await app.Services.InitializeNeo4jSchemaCatalogAsync(true);

// Sistem dokümanlarını Qdrant'a yükle (Data klasöründeki dosyalar, yoksa kaydedilir)
await app.InitializeSystemDocumentsAsync();

app.Run();
