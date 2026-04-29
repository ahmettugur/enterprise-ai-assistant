using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace AI.Infrastructure.Logging;

public class LogKeyConstants
{
    public const string Request = "Request";
    public const string Response = "Response";
    public const string Url = "RequestUrl";
    public const string RemoteIpAddress = "RemoteIpAddress";
    public const string TransactionId = "TransactionId";
}

public class HeaderKeyConstants
{
    public const string Url = "X-Url";
}

public static class LoggingExtension
{
    public static void CustomLog<TK>(
        this ILogger<TK> logger,
        Dictionary<string, StringValues>? header,
        string? requestData,
        string? responseData,
        string message,
        Dictionary<string, string>? additianolValues,
        LogLevel logLevel)
    {
        string requestPayload = string.Empty;
        string responsePayload = string.Empty;
        StringValues remoteIpAddress = string.Empty;
        StringValues url = string.Empty;
        if (requestData != null)
            requestPayload = requestData;

        if (responseData != null)
            responsePayload = responseData;

        if (header != null)
        {
            header.TryGetValue(HeaderKeyConstants.Url, out url);
        }

        var logContextProperties = new List<IDisposable>
        {
            LogContext.PushProperty(LogKeyConstants.Request, requestPayload),
            LogContext.PushProperty(LogKeyConstants.Response, responsePayload),
            LogContext.PushProperty(LogKeyConstants.Url, url)
        };

        if (additianolValues != null)
        {
            foreach (var kvp in additianolValues)
            {
                logContextProperties.Add(LogContext.PushProperty(kvp.Key, kvp.Value));
            }
        }

        using (new LogContextDisposable(logContextProperties))
        {
            try
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        logger.LogTrace(message);
                        break;
                    case LogLevel.Debug:
                        logger.LogDebug(message);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(message);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(message);
                        break;
                    case LogLevel.Error:
                        logger.LogError(message);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(message);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    private class LogContextDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables;

        public LogContextDisposable(List<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
