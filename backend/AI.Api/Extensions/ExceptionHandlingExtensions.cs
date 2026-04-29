using AI.Api.Common.Exceptions;

namespace AI.Api.Extensions;

/// <summary>
/// Exception handling extension methods for DI and middleware configuration
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Registers global exception handling services
    /// </summary>
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        // Register ProblemDetails service for RFC 7807 support
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                // Add trace ID to all problem details responses
                context.ProblemDetails.Extensions["traceId"] = 
                    context.HttpContext.TraceIdentifier;
                
                context.ProblemDetails.Extensions["timestamp"] = 
                    DateTimeOffset.UtcNow;
            };
        });

        // Register our custom exception handler
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    /// <summary>
    /// Configures the exception handling middleware in the request pipeline
    /// MUST be called early in the pipeline (before routing, endpoints, etc.)
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        // UseExceptionHandler activates the IExceptionHandler implementations
        app.UseExceptionHandler();

        return app;
    }
}
