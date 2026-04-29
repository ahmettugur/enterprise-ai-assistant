using System.Data;

namespace AI.Application.Ports.Secondary.Services.Database;


public interface ISqlServerConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}
