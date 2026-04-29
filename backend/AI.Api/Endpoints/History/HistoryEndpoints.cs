using AI.Api.Extensions;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Results;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.Results;

namespace AI.Api.Endpoints.History;

/// <summary>
/// History yönetimi endpoint'leri
/// </summary>
public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/history")
            .WithTags("History")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        // Tüm conversation'ları mesajları ile birlikte listele
        group.MapGet("/conversations", async (
            [FromServices] IConversationUseCase conversationUseCase,
            [FromServices] ICurrentUserService currentUserService,
            [FromServices] ILogger<Program> logger,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            try
            {
                var userId = currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await conversationUseCase.GetConversationsWithMessagesAsync(userId, page, pageSize);
                return Ok(Result<object>.Success(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Conversation listesi alınırken hata oluştu");
                return BadRequest(Result<object>.Error("Conversation listesi alınamadı."));
            }
        })
        .WithName("GetConversations")
        .WithSummary("Tüm conversation'ları mesajları ile birlikte listeler");

        // Belirli bir conversation'ın detaylarını getir
        group.MapGet("/conversations/{conversationId}", async (
            [FromRoute] string conversationId,
            [FromServices] IConversationUseCase conversationUseCase,
            [FromServices] ICurrentUserService currentUserService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conversationId))
                {
                    return BadRequest(Result<object>.Error("ConversationId gereklidir."));
                }

                var userId = currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                if (!Guid.TryParse(conversationId, out var guid))
                {
                    return BadRequest(Result<object>.Error("Geçersiz ConversationId formatı."));
                }

                var result = await conversationUseCase.GetConversationDetailAsync(guid, userId, currentUserService.IsAdmin);
                if (result == null)
                {
                    return NotFound(Result<object>.Error("Conversation bulunamadı."));
                }

                return Ok(Result<object>.Success(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Conversation detayları alınırken hata oluştu - ConversationId: {ConversationId}", conversationId);
                return BadRequest(Result<object>.Error("Conversation detayları alınamadı."));
            }
        })
        .WithName("GetConversationDetails")
        .WithSummary("Belirli bir conversation'ın detaylarını getirir");

        // Conversation'ın mesajlarını sayfalı olarak getir
        group.MapGet("/conversations/{conversationId}/messages", async (
            [FromRoute] string conversationId,
            [FromServices] IConversationUseCase conversationUseCase,
            [FromServices] ICurrentUserService currentUserService,
            [FromServices] ILogger<Program> logger,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conversationId))
                {
                    return BadRequest(Result<object>.Error("ConversationId gereklidir."));
                }

                var userId = currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                if (!Guid.TryParse(conversationId, out var guid))
                {
                    return BadRequest(Result<object>.Error("Geçersiz ConversationId formatı."));
                }

                var result = await conversationUseCase.GetConversationMessagesPagedAsync(
                    guid, userId, currentUserService.IsAdmin, page, pageSize);
                if (result == null)
                {
                    return NotFound(Result<object>.Error("Conversation bulunamadı."));
                }

                return Ok(Result<object>.Success(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mesajlar alınırken hata oluştu - ConversationId: {ConversationId}", conversationId);
                return BadRequest(Result<object>.Error("Mesajlar alınamadı."));
            }
        })
        .WithName("GetConversationMessages")
        .WithSummary("Conversation'ın mesajlarını sayfalı olarak getirir");

        // Conversation istatistikleri
        group.MapGet("/stats", async (
            [FromServices] IConversationUseCase conversationUseCase,
            [FromServices] ICurrentUserService currentUserService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                var userId = currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await conversationUseCase.GetConversationStatsAsync(userId);
                return Ok(Result<object>.Success(result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "İstatistikler alınırken hata oluştu");
                return BadRequest(Result<object>.Error("İstatistikler alınamadı."));
            }
        })
        .WithName("GetHistoryStats")
        .WithSummary("History istatistiklerini getirir");
    }
}