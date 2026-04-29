namespace AI.Api.Extensions;

/// <summary>
/// Extension methods for API versioning
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Current API version
    /// </summary>
    public const string CurrentVersion = "v1";
    
    /// <summary>
    /// Supported API versions
    /// </summary>
    public static readonly string[] SupportedVersions = { "v1" };

    /// <summary>
    /// Adds API version header to response
    /// </summary>
    public static IApplicationBuilder UseApiVersionHeader(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Api-Version"] = CurrentVersion;
                context.Response.Headers["X-Api-Supported-Versions"] = string.Join(", ", SupportedVersions);
                return Task.CompletedTask;
            });
            
            await next();
        });
    }

    /// <summary>
    /// Maps API version info endpoint
    /// </summary>
    public static IEndpointRouteBuilder MapApiVersionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/version", () => new
        {
            currentVersion = CurrentVersion,
            supportedVersions = SupportedVersions,
            deprecatedVersions = Array.Empty<string>(),
            documentation = "/swagger"
        })
        .WithName("GetApiVersion")
        .WithTags("API Info")
        .WithSummary("Returns API version information");

        return endpoints;
    }
}
