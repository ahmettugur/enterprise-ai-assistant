using AI.Application.DTOs.Dashboard;
using AI.Application.DTOs.Chat;

namespace AI.Application.Ports.Secondary.Services.Common;

public interface IFileSaver
{
    Task<(string projectPath, string outputApiUrl)> SaveDashboardFiles(DashboardFiles files, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output");
    
    /// <summary>
    /// Template-based hızlı dashboard kaydetme - LLM sadece config üretir, template dosyaları kopyalanır
    /// </summary>
    Task<(string projectPath, string outputApiUrl)> SaveTemplateDashboard(DashboardConfig config, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output");
}
