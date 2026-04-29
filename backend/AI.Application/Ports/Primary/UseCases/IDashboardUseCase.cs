using AI.Application.DTOs;
using AI.Application.DTOs.Dashboard;
using AI.Application.DTOs.Chat;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Dashboard işleme Use Case - Primary Port
/// HTML dashboard oluşturma işlemleri
/// </summary>
public interface IDashboardUseCase
{
    Task<DashboardProcessResult> ProcessDashboardResponse(string promptResponse, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output", string insightHtml = "");

    /// <summary>
    /// Template-based hızlı dashboard işleme - LLM sadece config üretir, template dosyaları kullanılır
    /// Mevcut yöntemden 5-10 kat daha hızlı
    /// </summary>
    Task<DashboardProcessResult> ProcessTemplateDashboard(DashboardConfig config, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output");
}
