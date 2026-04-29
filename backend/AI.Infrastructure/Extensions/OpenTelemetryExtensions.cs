using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Oracle.ManagedDataAccess.OpenTelemetry;

namespace AI.Infrastructure.Extensions;

public static class OpenTelemetryExtensions
{
    private static readonly string[] ExcludedPaths =
    {
        "/ai-hub/ping",
        "/ai-hub/negotiate",
        "/ai-hub"
    };

    private static readonly string[] ExcludedMethods = { "OPTIONS", "CONNECT" };

    private static readonly string[] ExcludedActivityNames =
    {
        "SignalRConnect",
        "SignalRDisConnect",
        "SignalRPing",
        "AI.Api.Presentation.Hubs.AIHub/Ping",
        "AI.Api.Presentation.Hubs.AIHub/OnConnectedAsync",
        "AI.Api.Presentation.Hubs.AIHub/OnDisconnectedAsync"
    };

    public static IServiceCollection AddOpenTelemetryTracing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otelConfig = configuration.GetSection("OpenTelemetry");

        if (!otelConfig.GetValue("Enabled", false))
        {
            return services;
        }

        var serviceName = otelConfig.GetValue("ServiceName", "AI.Api") ?? "AI.Api";
        var serviceVersion = otelConfig.GetValue("ServiceVersion", "1.0.0") ?? "1.0.0";
        var endpoint = otelConfig.GetSection("OtlpExporter").GetValue("Endpoint", "http://localhost:4317")
                       ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .ConfigureResource(resource =>
                        resource.AddService(serviceName, serviceVersion: serviceVersion))
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation(options => 
                    { 
                        options.Filter = ShouldTrace;
                        // SignalR hub metodlarını filtrele
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();

                // SQL Client Instrumentation (MSSQL)
                if (otelConfig.GetValue("SqlClientTracingEnabled", true))
                {
                    builder.AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithSqlCommand = (activity, command) =>
                        {
                            if (command is System.Data.Common.DbCommand dbCommand)
                            {
                                activity.SetTag("db.command.type", dbCommand.CommandType.ToString());
                                activity.SetTag("db.command.timeout", dbCommand.CommandTimeout);
                                activity.SetTag("db.command.parameter_count", dbCommand.Parameters.Count);
                                activity.SetTag("db.statement", dbCommand.CommandText);
                            }
                        };
                    });
                }

                if (otelConfig.GetValue("EntityFrameworkTracingEnabled", true))
                {
                    builder.AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            if (command is System.Data.Common.DbCommand dbCommand)
                            {
                                activity.SetTag("db.command.type", dbCommand.CommandType.ToString());
                                activity.SetTag("db.command.timeout", dbCommand.CommandTimeout);
                                activity.SetTag("db.command.parameter_count", dbCommand.Parameters.Count);
                                activity.SetTag("db.statement", dbCommand.CommandText);
                            }
                        };
                    });
                }

                // Redis Instrumentation
                if (otelConfig.GetValue("RedisTracingEnabled", true))
                {
                    builder.AddRedisInstrumentation(cfg => { cfg.SetVerboseDatabaseStatements = true; });
                }

                // Oracle Instrumentation
                if (otelConfig.GetValue("OracleTracingEnabled", true))
                {
                    builder.AddOracleDataProviderInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = true;
                        options.EnableConnectionLevelAttributes = true;
                    });
                }

                // Custom Activity Sources
                builder.AddSource("DocumentProcessing.ActivitySource");
                builder.AddSource("EmbeddingGeneration.ActivitySource");
                builder.AddSource("VectorSearch.ActivitySource");
                builder.AddSource("ChatHistory.ActivitySource");
                builder.AddSource("RagSearch.ActivitySource");
                builder.AddSource("Chat.ActivitySource");

                // OTLP Exporter
                builder.AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri(endpoint);
                    opt.Protocol = OtlpExportProtocol.Grpc;
                });

                // Add activity listener to filter out SignalR activities
                builder.AddProcessor(new ActivityFilteringProcessor());
            });

        return services;
    }

    private static bool ShouldTrace(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "";
        var method = httpContext.Request.Method;

        // Exclude specified paths
        if (ExcludedPaths.Any(pattern => path.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Exclude specified methods
        if (ExcludedMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private class AlwaysOnSampler : Sampler
    {
        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            return new SamplingResult(SamplingDecision.RecordAndSample);
        }
    }

    private class ActivityFilteringProcessor : BaseProcessor<Activity>
    {
        public override void OnStart(Activity activity)
        {
            // Filter out SignalR activities and hub methods
            if (ExcludedActivityNames.Any(name => 
                activity.DisplayName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                activity.OperationName.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            }
        }
    }
}
