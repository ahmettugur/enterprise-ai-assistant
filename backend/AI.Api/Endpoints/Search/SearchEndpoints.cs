using AI.Api.Extensions;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Results;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.Results;

namespace AI.Api.Endpoints.Search;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/search")
            .WithTags("Search")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.SearchPolicy);

        group.MapPost("/semantic", async (
            [FromBody] SearchRequestDto request,
            [FromServices] IRagSearchUseCase ragSearchService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(Result<List<SearchResultDto>>.Error("Search query cannot be empty."));
                }

                if (request.Query.Length > 1000)
                {
                    return BadRequest(Result<List<SearchResultDto>>.Error("Search query cannot be longer than 1000 characters."));
                }

                logger.LogInformation("Semantic search started: {Query}", request.Query);

                // Request doğrudan kullanılabilir (SearchRequestDto)
                var searchRequest = request;

                var response = await ragSearchService.SearchAsync(searchRequest, cancellationToken);

                logger.LogInformation("Semantic search completed: {ResultCount} results found", response.Results.Count);

                var searchResultDtos = response.Results.Select(r => new SearchResultDto
                {
                    DocumentTitle = r.DocumentTitle,
                    Content = r.Content,
                    Score = r.SimilarityScore,
                    Metadata = r.Metadata
                }).ToList();

                return Ok(Result<List<SearchResultDto>>.Success(searchResultDtos, $"{searchResultDtos.Count} search results found."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during semantic search. Query: {Query}", request?.Query);
                return Problem(
                    detail: "An error occurred during the search operation.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
            .WithName("SemanticSearch")
            .WithSummary("Performs semantic search")
            .WithDescription("Performs semantic search with the given query")
            .Accepts<SearchRequestDto>("application/json")
            .Produces<Result<List<SearchResultDto>>>(StatusCodes.Status200OK)
            .Produces<Result<string>>(StatusCodes.Status400BadRequest);
    }
}