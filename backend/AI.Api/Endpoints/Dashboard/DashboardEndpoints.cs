using AI.Api.Extensions;
using AI.Application.DTOs.Dashboard;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Endpoints.Dashboard;

/// <summary>
/// Dashboard endpoints for feedback analytics and prompt improvements
/// Hexagonal Architecture: Uses IDashboardQueryUseCase (Primary Port) instead of repositories
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        // Overview statistics
        group.MapGet("/overview", GetOverview)
            .WithName("GetDashboardOverview")
            .WithDescription("Get comprehensive dashboard overview")
            .Produces<DashboardOverviewDto>(StatusCodes.Status200OK);

        // Feedback trends over time
        group.MapGet("/trends", GetFeedbackTrends)
            .WithName("GetFeedbackTrends")
            .WithDescription("Get feedback trends over time")
            .Produces<FeedbackTrendsDto>(StatusCodes.Status200OK);

        // Category breakdown
        group.MapGet("/categories", GetCategoryBreakdown)
            .WithName("GetCategoryBreakdown")
            .WithDescription("Get feedback breakdown by category")
            .Produces<List<CategoryBreakdownItemDto>>(StatusCodes.Status200OK);

        // Prompt improvements list
        group.MapGet("/improvements", GetPromptImprovements)
            .WithName("GetPromptImprovements")
            .WithDescription("Get list of prompt improvements")
            .Produces<PromptImprovementsResponseDto>(StatusCodes.Status200OK);

        // Update improvement status
        group.MapPatch("/improvements/{id:guid}/status", UpdateImprovementStatus)
            .WithName("UpdateImprovementStatus")
            .WithDescription("Update the status of a prompt improvement")
            .Produces<PromptImprovementDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Analysis reports history
        group.MapGet("/reports", GetAnalysisReports)
            .WithName("GetAnalysisReports")
            .WithDescription("Get historical analysis reports")
            .Produces<List<AnalysisReportSummaryDto>>(StatusCodes.Status200OK);

        // Single report detail
        group.MapGet("/reports/{id:guid}", GetAnalysisReportDetail)
            .WithName("GetAnalysisReportDetail")
            .WithDescription("Get detailed analysis report")
            .Produces<AnalysisReportDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetOverview(
        [FromServices] IDashboardQueryUseCase dashboardService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await dashboardService.GetOverviewAsync(cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dashboard overview");
            return Results.Problem("An error occurred while getting dashboard overview");
        }
    }

    private static async Task<IResult> GetFeedbackTrends(
        [FromQuery] int? days,
        [FromServices] IDashboardQueryUseCase dashboardService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await dashboardService.GetFeedbackTrendsAsync(days ?? 30, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting feedback trends");
            return Results.Problem("An error occurred while getting feedback trends");
        }
    }

    private static async Task<IResult> GetCategoryBreakdown(
        [FromServices] IDashboardQueryUseCase dashboardService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await dashboardService.GetCategoryBreakdownAsync(cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting category breakdown");
            return Results.Problem("An error occurred while getting category breakdown");
        }
    }

    private static async Task<IResult> GetPromptImprovements(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? limit,
        [FromServices] IDashboardQueryUseCase dashboardService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await dashboardService.GetImprovementsAsync(status, priority, limit ?? 50, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting prompt improvements");
            return Results.Problem("An error occurred while getting prompt improvements");
        }
    }

    private static async Task<IResult> UpdateImprovementStatus(
        Guid id,
        [FromBody] UpdateImprovementStatusRequest request,
        [FromServices] IDashboardQueryUseCase dashboardService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await dashboardService.UpdateImprovementStatusAsync(
                id, request.Status, userId, request.Notes, cancellationToken);
            
            if (result == null)
                return Results.NotFound(new { error = "Improvement not found" });

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating improvement status");
            return Results.Problem("An error occurred while updating improvement status");
        }
    }

    private static async Task<IResult> GetAnalysisReports(
        [FromQuery] int? limit,
        [FromServices] IDashboardQueryUseCase dashboardService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await dashboardService.GetAnalysisReportsAsync(limit ?? 20, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting analysis reports");
            return Results.Problem("An error occurred while getting analysis reports");
        }
    }

    private static async Task<IResult> GetAnalysisReportDetail(
        Guid id,
        [FromServices] IDashboardQueryUseCase dashboardService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await dashboardService.GetAnalysisReportDetailAsync(id, cancellationToken);
            
            if (result == null)
                return Results.NotFound(new { error = "Report not found" });

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting analysis report detail");
            return Results.Problem("An error occurred while getting analysis report detail");
        }
    }
}

#region Request DTOs

public class UpdateImprovementStatusRequest
{
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
}

#endregion
