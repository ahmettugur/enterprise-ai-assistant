using AI.Application.Results;
using AI.Application.DTOs.Chat;

namespace AI.Application.Ports.Secondary.Services.Report;

public interface IReportService
{
    Task<Result<LLmResponseModel>> GetReportsAsync(ChatRequest request);
    Task<Result<LLmResponseModel>> GetReportsWithHtmlAsync(ChatRequest request);
}
