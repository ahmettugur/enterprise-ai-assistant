using AI.Api.Extensions;
using AI.Application.DTOs.Feedback;
using AI.Application.DTOs.FeedbackAnalysis;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Endpoints.Feedback;

/// <summary>
/// Message feedback endpoints - AI response quality measurement
/// Hexagonal Architecture: Uses IFeedbackUseCase (Primary Port) instead of repositories
/// </summary>
public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/feedback")
            .WithTags("Feedback")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        // Add feedback to a message
        group.MapPost("/messages/{messageId:guid}", AddFeedback)
            .WithName("AddMessageFeedback")
            .WithDescription("Add feedback (thumbs up/down) to an AI message")
            .Produces<FeedbackDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        // Get feedback for a message
        group.MapGet("/messages/{messageId:guid}", GetFeedbackForMessage)
            .WithName("GetMessageFeedback")
            .WithDescription("Get user's feedback for a specific message")
            .Produces<FeedbackDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // Get all feedbacks for a conversation
        group.MapGet("/conversations/{conversationId:guid}", GetFeedbacksForConversation)
            .WithName("GetConversationFeedbacks")
            .WithDescription("Get all feedbacks for a conversation")
            .Produces<List<FeedbackDto>>(StatusCodes.Status200OK);

        // Get feedback statistics
        group.MapGet("/statistics", GetStatistics)
            .WithName("GetFeedbackStatistics")
            .WithDescription("Get feedback statistics for a date range")
            .Produces<FeedbackStatisticsDto>(StatusCodes.Status200OK);

        // Delete feedback
        group.MapDelete("/messages/{messageId:guid}", DeleteFeedback)
            .WithName("DeleteMessageFeedback")
            .WithDescription("Delete user's feedback for a message")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // Dashboard statistics with trends
        group.MapGet("/dashboard", GetDashboardStatistics)
            .WithName("GetDashboardStatistics")
            .WithDescription("Get comprehensive dashboard statistics with daily trends")
            .Produces<DashboardStatisticsDto>(StatusCodes.Status200OK);

        // Negative feedbacks for review
        group.MapGet("/negative", GetNegativeFeedbacks)
            .WithName("GetNegativeFeedbacks")
            .WithDescription("Get negative feedbacks with comments for review")
            .Produces<List<NegativeFeedbackDto>>(StatusCodes.Status200OK);

        // Trigger feedback analysis (admin only)
        group.MapPost("/analyze", TriggerFeedbackAnalysis)
            .WithName("TriggerFeedbackAnalysis")
            .WithDescription("Trigger AI analysis of pending negative feedbacks")
            .Produces<FeedbackAnalysisResponseDto>(StatusCodes.Status200OK);

        // Get latest analysis result
        group.MapGet("/analysis", GetLatestAnalysis)
            .WithName("GetLatestAnalysis")
            .WithDescription("Get the latest feedback analysis result")
            .Produces<FeedbackAnalysisResponseDto>(StatusCodes.Status200OK);

        // Get analysis history
        group.MapGet("/analysis/history", GetAnalysisHistory)
            .WithName("GetAnalysisHistory")
            .WithDescription("Get feedback analysis history")
            .Produces<List<FeedbackAnalysisResponseDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> AddFeedback(
        Guid messageId,
        [FromBody] AddFeedbackRequest request,
        [FromServices] IFeedbackUseCase feedbackService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await feedbackService.AddFeedbackAsync(
                messageId, userId, request.Type, request.Comment, cancellationToken);

            if (result == null)
                return Results.BadRequest(new { error = "Message not found or invalid feedback type. Type must be 'positive' or 'negative'." });

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding feedback for message: {MessageId}", messageId);
            return Results.Problem("An error occurred while adding feedback");
        }
    }

    private static async Task<IResult> GetFeedbackForMessage(
        Guid messageId,
        [FromServices] IFeedbackUseCase feedbackService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await feedbackService.GetFeedbackForMessageAsync(messageId, userId, cancellationToken);
            if (result == null)
                return Results.NotFound(new { error = "Feedback not found" });

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting feedback for message: {MessageId}", messageId);
            return Results.Problem("An error occurred while getting feedback");
        }
    }

    private static async Task<IResult> GetFeedbacksForConversation(
        Guid conversationId,
        [FromServices] IFeedbackUseCase feedbackService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await feedbackService.GetFeedbacksForConversationAsync(conversationId, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting feedbacks for conversation: {ConversationId}", conversationId);
            return Results.Problem("An error occurred while getting feedbacks");
        }
    }

    private static async Task<IResult> GetStatistics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromServices] IFeedbackUseCase feedbackService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var result = await feedbackService.GetStatisticsAsync(start, end, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting feedback statistics");
            return Results.Problem("An error occurred while getting statistics");
        }
    }

    private static async Task<IResult> DeleteFeedback(
        Guid messageId,
        [FromServices] IFeedbackUseCase feedbackService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var deleted = await feedbackService.DeleteFeedbackAsync(messageId, userId, cancellationToken);
            if (!deleted)
                return Results.NotFound(new { error = "Feedback not found" });

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting feedback for message: {MessageId}", messageId);
            return Results.Problem("An error occurred while deleting feedback");
        }
    }

    private static async Task<IResult> GetDashboardStatistics(
        [FromQuery] int? days,
        [FromServices] IFeedbackUseCase feedbackService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await feedbackService.GetDashboardStatisticsAsync(days ?? 30, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dashboard statistics");
            return Results.Problem("An error occurred while getting dashboard statistics");
        }
    }

    private static async Task<IResult> GetNegativeFeedbacks(
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromServices] IFeedbackUseCase feedbackService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await feedbackService.GetNegativeFeedbacksAsync(skip, take == 0 ? 50 : take, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting negative feedbacks");
            return Results.Problem("An error occurred while getting negative feedbacks");
        }
    }

    private static async Task<IResult> TriggerFeedbackAnalysis(
        [FromServices] IFeedbackAnalysisUseCase analysisService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            logger.LogInformation("Manual feedback analysis triggered by user: {UserId}", userId);

            var result = await analysisService.AnalyzePendingFeedbacksAsync(cancellationToken);
            return Results.Ok(MapToResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error triggering feedback analysis");
            return Results.Problem("An error occurred while analyzing feedbacks");
        }
    }

    private static async Task<IResult> GetLatestAnalysis(
        [FromServices] IFeedbackAnalysisUseCase analysisService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await analysisService.GetLatestAnalysisAsync(cancellationToken);
            if (result == null)
                return Results.NotFound(new { error = "No analysis available" });

            return Results.Ok(MapToResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting latest analysis");
            return Results.Problem("An error occurred while getting analysis");
        }
    }

    private static async Task<IResult> GetAnalysisHistory(
        [FromQuery] int? limit,
        [FromServices] IFeedbackAnalysisUseCase analysisService,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await analysisService.GetAnalysisHistoryAsync(limit ?? 10, cancellationToken);
            return Results.Ok(result.Select(MapToResponse).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting analysis history");
            return Results.Problem("An error occurred while getting analysis history");
        }
    }

    private static FeedbackAnalysisResponseDto MapToResponse(FeedbackAnalysisResult result)
    {
        return new FeedbackAnalysisResponseDto
        {
            Id = result.Id,
            AnalyzedAt = result.AnalyzedAt,
            TotalFeedbacksAnalyzed = result.TotalFeedbacksAnalyzed,
            OverallSummary = result.OverallSummary,
            Categories = result.Categories.Select(c => new CategoryResponseDto
            {
                Name = c.Name,
                Description = c.Description,
                Count = c.Count,
                Percentage = c.Percentage,
                ExampleComments = c.ExampleComments
            }).ToList(),
            Suggestions = result.Suggestions.Select(s => new SuggestionResponseDto
            {
                Category = s.Category,
                Issue = s.Issue,
                Suggestion = s.Suggestion,
                Priority = s.Priority,
                PromptModification = s.PromptModification
            }).ToList()
        };
    }
}
