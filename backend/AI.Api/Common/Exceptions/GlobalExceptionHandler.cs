using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Common.Exceptions;

/// <summary>
/// Global exception handler implementing IExceptionHandler (.NET 8+)
/// Provides RFC 7807 ProblemDetails responses for all unhandled exceptions
/// Does NOT interfere with service-level retry mechanisms (Polly, custom retry)
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Get trace ID for correlation
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        // Determine exception type and create appropriate response
        var (statusCode, title, detail) = MapException(exception);

        // Log the exception with structured logging
        LogException(exception, statusCode, traceId, httpContext.Request.Path);

        // Create RFC 7807 ProblemDetails response
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() ? exception.Message : detail,
            Instance = httpContext.Request.Path,
            Type = GetProblemType(statusCode)
        };

        // Add trace ID for correlation
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        // Add exception details in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace?.Split(Environment.NewLine).Take(10).ToArray(),
                innerException = exception.InnerException?.Message
            };
        }

        // Set response
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Exception was handled
    }

    /// <summary>
    /// Maps exception types to appropriate HTTP status codes and messages
    /// </summary>
    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            // Validation / Bad Request
            ArgumentNullException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Request",
                "One or more required parameters were not provided."),

            ArgumentException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Argument",
                "One or more parameters contain invalid values."),

            FormatException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Format",
                "The provided data format is invalid."),

            // Not Found
            KeyNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                "The requested resource was not found."),

            FileNotFoundException => (
                (int)HttpStatusCode.NotFound,
                "File Not Found",
                "The requested file was not found."),

            // Timeout / Service Unavailable
            TimeoutException => (
                (int)HttpStatusCode.GatewayTimeout,
                "Request Timeout",
                "The operation timed out. Please try again later."),

            TaskCanceledException tcEx when tcEx.CancellationToken.IsCancellationRequested => (
                (int)HttpStatusCode.BadRequest,
                "Request Cancelled",
                "The request was cancelled by the client."),

            TaskCanceledException => (
                (int)HttpStatusCode.GatewayTimeout,
                "Request Timeout",
                "The operation timed out. Please try again later."),

            OperationCanceledException ocEx when ocEx.CancellationToken.IsCancellationRequested => (
                (int)HttpStatusCode.BadRequest,
                "Request Cancelled",
                "The request was cancelled by the client."),

            OperationCanceledException => (
                (int)HttpStatusCode.GatewayTimeout,
                "Request Timeout",
                "The operation timed out. Please try again later."),

            // HTTP Client errors
            HttpRequestException httpEx when httpEx.StatusCode.HasValue => (
                (int)httpEx.StatusCode.Value,
                "External Service Error",
                $"An error occurred while communicating with an external service."),

            HttpRequestException => (
                (int)HttpStatusCode.BadGateway,
                "External Service Error",
                "Failed to communicate with an external service."),

            // Authorization
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You are not authorized to perform this action."),

            // Invalid Operation
            InvalidOperationException => (
                (int)HttpStatusCode.Conflict,
                "Invalid Operation",
                "The operation is not valid in the current state."),

            // Not Implemented
            NotImplementedException => (
                (int)HttpStatusCode.NotImplemented,
                "Not Implemented",
                "This feature is not yet implemented."),

            NotSupportedException => (
                (int)HttpStatusCode.NotImplemented,
                "Not Supported",
                "This operation is not supported."),

            // Database / Infrastructure errors
            // Note: Since we moved this to AI.Api, we might not have direct access to Microsoft.EntityFrameworkCore
            // But AI.Infrastructure is referenced, so we might via transitive dependency.
            // If not, we can check by type name string to avoid hard dependency if needed, 
            // but for now I will keep it as user requested "don't break system".
            // However, AI.Api project does not reference EF Core directly. 
            // Let's use full name matching if compilation fails, but let's try direct reference first assuming transitive works.
            Microsoft.EntityFrameworkCore.DbUpdateException => (
                (int)HttpStatusCode.Conflict,
                "Database Error",
                "A database error occurred while processing your request."),

            // Default: Internal Server Error
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later.")
        };
    }

    /// <summary>
    /// Returns RFC 7807 problem type URI based on status code
    /// </summary>
    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            501 => "https://tools.ietf.org/html/rfc7231#section-6.6.2",
            502 => "https://tools.ietf.org/html/rfc7231#section-6.6.3",
            503 => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            504 => "https://tools.ietf.org/html/rfc7231#section-6.6.5",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }

    /// <summary>
    /// Logs exception with appropriate log level based on status code
    /// </summary>
    private void LogException(Exception exception, int statusCode, string traceId, string path)
    {
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 and < 500 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, StatusCode: {StatusCode}, ExceptionType: {ExceptionType}",
            traceId,
            path,
            statusCode,
            exception.GetType().Name);
    }
}
