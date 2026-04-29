using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AI.Infrastructure.Logging;

public static class LoggingHelper
{
    public static Logger CustomLoggerConfigurationConsole()
    {
        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "TestApi")
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .MinimumLevel.Override("Swashbuckle.AspNetCore", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
            .WriteTo.Console()
            .CreateLogger();
    }
}
