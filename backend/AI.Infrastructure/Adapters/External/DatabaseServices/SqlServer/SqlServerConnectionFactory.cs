using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using AI.Application.Ports.Secondary.Services.Database;

namespace AI.Infrastructure.Adapters.External.DatabaseServices.SqlServer;

public sealed class SqlServerConnectionFactory(IConfiguration configuration, string connectionName) : ISqlServerConnectionFactory
{
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection(configuration.GetConnectionString(connectionName));
        await connection.OpenAsync();

        return connection;
    }
}