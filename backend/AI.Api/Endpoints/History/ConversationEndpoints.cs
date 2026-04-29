using AI.Api.Extensions;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Results;
using AI.Application.DTOs.Chat;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.Results;

namespace AI.Api.Endpoints.History;

internal static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/conversations")
            .WithTags("Conversations")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        // Update conversation title
        group.MapPut("/{conversationId}/title", async (
            string conversationId,
            [FromBody] UpdateConversationTitleDto request,
            [FromServices] IConversationUseCase historyService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return BadRequest(Result<ConversationDto>.Error("Title cannot be empty."));
                }

                logger.LogInformation("Updating conversation title - ConversationId: {ConversationId}", conversationId);

                var resultDto = await historyService.UpdateConversationTitleAsync(conversationId, request.Title, cancellationToken);

                logger.LogInformation("Conversation title updated successfully - ConversationId: {ConversationId}", conversationId);

                return Ok(Result<ConversationDto>.Success(resultDto, "Conversation title updated successfully."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating conversation title - ConversationId: {ConversationId}", conversationId);
                return Problem(
                    detail: "An error occurred while updating the conversation title.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
            .WithName("UpdateConversationTitle")
            .WithSummary("Updates conversation title")
            .WithDescription("Updates the title of a specific conversation")
            .Accepts<UpdateConversationTitleDto>("application/json")
            .Produces<Result<ConversationDto>>(StatusCodes.Status200OK)
            .Produces<Result<string>>(StatusCodes.Status400BadRequest);

        // Delete conversation
        group.MapDelete("/{conversationId}", async (
            string conversationId,
            [FromServices] IConversationUseCase historyService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conversationId))
                {
                    return BadRequest(Result<bool>.Error("Conversation ID is required."));
                }

                logger.LogInformation("Deleting conversation - ConversationId: {ConversationId}", conversationId);

                var deleted = await historyService.RemoveConversationHistoryAsync(conversationId, cancellationToken);

                if (!deleted)
                {
                    logger.LogWarning("Conversation not found or could not be deleted - ConversationId: {ConversationId}", conversationId);
                    return NotFound(Result<bool>.Error("Conversation not found or could not be deleted."));
                }

                logger.LogInformation("Conversation deleted successfully - ConversationId: {ConversationId}", conversationId);

                return Ok(Result<bool>.Success(true, "Conversation deleted successfully."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting conversation - ConversationId: {ConversationId}", conversationId);
                return Problem(
                    detail: "An error occurred while deleting the conversation.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
            .WithName("DeleteConversation")
            .WithSummary("Deletes a conversation")
            .WithDescription("Deletes a specific conversation and all its messages")
            .Produces<Result<bool>>(StatusCodes.Status200OK)
            .Produces<Result<bool>>(StatusCodes.Status404NotFound);

        // Map Reports endpoint with rate limiting for AI operations
        app.MapPost("/api/v1/chatbot", async (
            IRouteConversationUseCase conversationOrchestrator,
            [FromBody] ChatRequest chatRequest,
            CancellationToken cancellationToken) =>
        {
            var response = await conversationOrchestrator.OrchestrateAsync(chatRequest, cancellationToken);
            return Ok(response);
        })
        .WithTags("AI.ChatBot")
        .RequireRateLimiting(RateLimitingExtensions.ChatPolicy);
    }
}
