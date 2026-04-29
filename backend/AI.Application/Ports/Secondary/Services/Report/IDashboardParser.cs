using AI.Application.DTOs.Dashboard;

namespace AI.Application.Ports.Secondary.Services.Report;


public interface IDashboardParser
{
    ParseResult ParseResponse(string response);
}

