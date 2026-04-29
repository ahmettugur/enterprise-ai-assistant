using AI.Application.DTOs.Database;

namespace AI.Application.Ports.Secondary.Services.Database;


public interface IDatabaseService
{
    Task ExecuteCommandAsync(string query, string userConnectionId);
    Task<DbResponseModel> GetDataTableWithExpandoObjectAsync(string query,string userConnectionId);
}
