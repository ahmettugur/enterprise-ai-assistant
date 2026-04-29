using AI.Infrastructure.Extensions;
using AI.Scheduler.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/scheduler-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("AI.Scheduler başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog'u kullan
    builder.Host.UseSerilog();

    // LLM Provider (Semantic Kernel)
    builder.Services.AddLLMProvider(builder.Configuration);

    // PostgreSQL DbContext (required by Infrastructure services)
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "PostgreSQL connection string not found. Please add 'ConnectionStrings:PostgreSQL' to appsettings.json");
    }
    
    builder.Services.AddPooledDbContextFactory<AI.Infrastructure.Adapters.Persistence.ChatDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
        });
        options.UseQueryTrackingBehavior(Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking);
    });
    
    // Register scoped DbContext from factory
    builder.Services.AddScoped<AI.Infrastructure.Adapters.Persistence.ChatDbContext>(sp => 
        sp.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<AI.Infrastructure.Adapters.Persistence.ChatDbContext>>().CreateDbContext());

    // Infrastructure servisleri (DbContext, Repositories) - Artık doğrudan eklenebilir
    // Çünkü projeye Infrastructure referansını geri ekledik.
    // Composition Root (Program.cs) her şeyi bilmek zorundadır.
    // Ancak Business Logic (Job sınıfları) içinde Infrastructure referansı kullanılmayacaktır.
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Hangfire servisleri
    builder.Services.AddHangfireServices(builder.Configuration);

    // Health checks
    builder.Services.AddHealthChecks();

    // OpenAPI / Swagger
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Development ortamında Swagger
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Hangfire Dashboard ve Recurring Jobs
    app.UseHangfireServices(app.Configuration);

    // Health check endpoint
    app.MapHealthChecks("/health");

    // Root endpoint
    app.MapGet("/", () => Results.Redirect("/hangfire"));

    Log.Information("AI.Scheduler başarıyla başlatıldı - Hangfire Dashboard: /hangfire");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AI.Scheduler başlatılırken kritik hata oluştu");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

