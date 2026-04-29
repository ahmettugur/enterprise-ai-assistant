using AI.Api.Extensions;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Endpoints.Reports;

/// <summary>
/// Zamanlanmış rapor yönetimi endpoint'leri
/// </summary>
public static class ScheduledReportEndpoints
{
    public static void MapScheduledReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/scheduled-reports")
            .WithTags("Scheduled Reports")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        #region CRUD Operations

        // Kullanıcının tüm zamanlanmış raporlarını getir
        group.MapGet("/", async (
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetMyReportsAsync(cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.BadRequest(result);
        })
        .WithName("GetMyScheduledReports")
        .WithDescription("Kullanıcının tüm zamanlanmış raporlarını getirir")
        .Produces<Result<List<ScheduledReportDto>>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Id'ye göre zamanlanmış rapor getir
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetByIdAsync(id, cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.NotFound(result);
        })
        .WithName("GetScheduledReportById")
        .WithDescription("Id'ye göre zamanlanmış rapor getirir")
        .Produces<Result<ScheduledReportDto>>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Id'ye göre zamanlanmış rapor detayı getir (loglarla birlikte)
        group.MapGet("/{id:guid}/details", async (
            Guid id,
            [FromQuery] int logLimit = 10,
            [FromServices] IScheduledReportUseCase service = null!,
            CancellationToken cancellationToken = default) =>
        {
            var result = await service.GetByIdWithLogsAsync(id, logLimit, cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.NotFound(result);
        })
        .WithName("GetScheduledReportDetails")
        .WithDescription("Id'ye göre zamanlanmış rapor detayını loglarla birlikte getirir")
        .Produces<Result<ScheduledReportDetailDto>>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // Yeni zamanlanmış rapor oluştur
        group.MapPost("/", async (
            [FromBody] CreateScheduledReportDto request,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return result.IsSucceed 
                ? Results.Created($"/api/v1/scheduled-reports/{result.ResultData?.Id}", result) 
                : Results.BadRequest(result);
        })
        .WithName("CreateScheduledReport")
        .WithDescription("Yeni zamanlanmış rapor oluşturur")
        .Produces<Result<ScheduledReportDto>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Mevcut bir mesajdan zamanlanmış rapor oluştur (Frontend'den kullanılır)
        group.MapPost("/from-message", async (
            [FromBody] CreateScheduledReportFromMessageDto request,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateFromMessageAsync(request, cancellationToken);
            return result.IsSucceed 
                ? Results.Created($"/api/v1/scheduled-reports/{result.ResultData?.Id}", result) 
                : Results.BadRequest(result);
        })
        .WithName("CreateScheduledReportFromMessage")
        .WithDescription("Mevcut bir mesajdan zamanlanmış rapor oluşturur. MessageId'den SQL sorgusu otomatik çekilir.")
        .Produces<Result<ScheduledReportDto>>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Zamanlanmış raporu güncelle
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateScheduledReportDto request,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(id, request, cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.BadRequest(result);
        })
        .WithName("UpdateScheduledReport")
        .WithDescription("Zamanlanmış raporu günceller")
        .Produces<Result<ScheduledReportDto>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Zamanlanmış raporu sil
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.NotFound(result);
        })
        .WithName("DeleteScheduledReport")
        .WithDescription("Zamanlanmış raporu siler")
        .Produces<Result<string>>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        #endregion

        #region Status Operations

        // Raporu duraklat
        group.MapPost("/{id:guid}/pause", async (
            Guid id,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.PauseAsync(id, cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.BadRequest(result);
        })
        .WithName("PauseScheduledReport")
        .WithDescription("Zamanlanmış raporu duraklatır")
        .Produces<Result<ScheduledReportDto>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Raporu devam ettir
        group.MapPost("/{id:guid}/resume", async (
            Guid id,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ResumeAsync(id, cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.BadRequest(result);
        })
        .WithName("ResumeScheduledReport")
        .WithDescription("Duraklatılmış zamanlanmış raporu devam ettirir")
        .Produces<Result<ScheduledReportDto>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        #endregion

        #region Execution Operations

        // Raporu hemen çalıştır
        group.MapPost("/{id:guid}/run-now", async (
            Guid id,
            [FromServices] IScheduledReportUseCase service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RunNowAsync(id, cancellationToken);
            return result.IsSucceed 
                ? Results.Accepted(value: result) 
                : Results.BadRequest(result);
        })
        .WithName("RunScheduledReportNow")
        .WithDescription("Zamanlanmış raporu hemen çalıştırır")
        .Produces<Result<ScheduledReportLogDto>>(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // Rapor loglarını getir
        group.MapGet("/{id:guid}/logs", async (
            Guid id,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            [FromServices] IScheduledReportUseCase service = null!,
            CancellationToken cancellationToken = default) =>
        {
            var result = await service.GetLogsAsync(id, skip, take, cancellationToken);
            return result.IsSucceed 
                ? Results.Ok(result) 
                : Results.BadRequest(result);
        })
        .WithName("GetScheduledReportLogs")
        .WithDescription("Zamanlanmış rapor çalışma loglarını getirir")
        .Produces<Result<List<ScheduledReportLogDto>>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        #endregion

        #region Utility Endpoints

        // Cron expression'ları listele (yardımcı)
        group.MapGet("/cron-presets", () =>
        {
            var presets = new List<CronPresetDto>
            {
                new() { Value = "0 * * * *", Label = "Her saat başı", Description = "Her saat başında çalışır" },
                new() { Value = "0 0 * * *", Label = "Her gün gece yarısı", Description = "Her gün 00:00'da çalışır" },
                new() { Value = "0 9 * * *", Label = "Her gün saat 09:00", Description = "Her gün sabah 9'da çalışır" },
                new() { Value = "0 9 * * 1", Label = "Her pazartesi 09:00", Description = "Her pazartesi sabah 9'da çalışır" },
                new() { Value = "0 9 * * 1-5", Label = "Hafta içi 09:00", Description = "Pazartesi-Cuma sabah 9'da çalışır" },
                new() { Value = "0 0 1 * *", Label = "Her ayın 1'inde", Description = "Her ayın 1'inde gece yarısı çalışır" },
                new() { Value = "0 9 1 * *", Label = "Her ayın 1'inde 09:00", Description = "Her ayın 1'inde sabah 9'da çalışır" },
                new() { Value = "0 0 * * 0", Label = "Her pazar gece yarısı", Description = "Her pazar 00:00'da çalışır" }
            };
            return Results.Ok(Result<List<CronPresetDto>>.Success(presets));
        })
        .WithName("GetCronPresets")
        .WithDescription("Hazır cron expression şablonlarını getirir")
        .Produces<Result<List<CronPresetDto>>>();

        #endregion
    }
}

/// <summary>
/// Cron preset DTO
/// </summary>
public sealed class CronPresetDto
{
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Description { get; set; } = null!;
}
