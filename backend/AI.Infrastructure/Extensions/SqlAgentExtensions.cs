using AI.Application.Ports.Secondary.Services.Database;
using AI.Infrastructure.Adapters.AI.Agents.SqlAgents;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Infrastructure.Extensions;

/// <summary>
/// SQL Agent servisleri için DI extension'ları.
/// </summary>
public static class SqlAgentExtensions
{
    /// <summary>
    /// SQL Agent servislerini DI container'a ekler.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddSqlAgentServices(this IServiceCollection services)
    {
        // SQL Validation Agent
        services.AddScoped<ISqlValidationAgent, SqlValidationAgent>();
        
        // SQL Optimization Agent
        services.AddScoped<ISqlOptimizationAgent, SqlOptimizationAgent>();
        
        // SQL Agent Pipeline (orchestrator)
        services.AddScoped<ISqlAgentPipeline, SqlAgentPipeline>();

        return services;
    }
}
