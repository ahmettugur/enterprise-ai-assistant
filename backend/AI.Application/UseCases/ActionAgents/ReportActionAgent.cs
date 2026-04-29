using AI.Application.DTOs.AgentCore;
using AI.Application.DTOs.History;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AgentCore;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases.ActionAgents;

/// <summary>
/// Report action agent — Rapor oluşturma modunu yönetir
/// Hexagonal Architecture: Use Case Implementation (Agent)
/// </summary>
public class ReportActionAgent : IActionAgent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConversationUseCase _historyService;
    private readonly ILogger<ReportActionAgent> _logger;

    public ReportActionAgent(
        IServiceProvider serviceProvider,
        IConversationUseCase historyService,
        ILogger<ReportActionAgent> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ActionName => "report";

    public bool CanHandle(string action) => action == ActionName;

    public async Task<Result<dynamic>> HandleAsync(
        ActionContext context,
        CancellationToken cancellationToken = default)
    {
        var reportName = context.RoutingResult.ReportName;

        _logger.LogInformation("ReportActionAgent: Rapor oluşturuluyor '{ReportName}' - ConnectionId: {ConnectionId}",
            reportName, context.Request.ConnectionId);

        var reportService = GetReportService(reportName);

        await _historyService.AddUserMessageAsync(
            context.Request,
            $"Seçilen Rapor Türü: {reportName}",
            MessageType.Temporary,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        var reportResult = await reportService
            .GetReportsWithHtmlAsync(context.Request)
            .ConfigureAwait(false);

        return Result<dynamic>.Success(
            reportResult.ResultData!,
            reportResult.UserMessage,
            context.RoutingResult.Action);
    }

    /// <summary>
    /// Keyed service üzerinden rapor servisini çözümler
    /// </summary>
    private IReportService GetReportService(string reportType)
    {
        if (string.IsNullOrWhiteSpace(reportType))
            throw new ArgumentException("Rapor tipi boş olamaz", nameof(reportType));

        try
        {
            return _serviceProvider.GetRequiredKeyedService<IReportService>(reportType.ToLowerInvariant());
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException($"Rapor tipi bulunamadı: {reportType}");
        }
    }
}
