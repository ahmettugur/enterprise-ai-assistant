using AI.Api.Extensions;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Endpoints.Documents;

/// <summary>
/// Döküman kategori yönetimi endpoint'leri
/// </summary>
public static class DocumentCategoryEndpoints
{
    public static void MapDocumentCategoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/document-categories")
            .WithTags("Document Categories")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        // Tüm kategorileri getir
        group.MapGet("/", async (
            [FromQuery] bool includeInactive,
            [FromServices] IDocumentCategoryUseCase categoryService,
            CancellationToken cancellationToken) =>
        {
            var categories = await categoryService.GetAllAsync(includeInactive, cancellationToken);
            return Results.Ok(Result<List<DocumentCategoryDto>>.Success(categories));
        })
        .WithName("GetAllDocumentCategories")
        .WithDescription("Tüm döküman kategorilerini getirir")
        .Produces<Result<List<DocumentCategoryDto>>>();

        // Kullanıcıya göre kategorileri getir
        group.MapGet("/by-user/{userId}", async (
            string userId,
            [FromQuery] bool includeInactive,
            [FromServices] IDocumentCategoryUseCase categoryService,
            CancellationToken cancellationToken) =>
        {
            var categories = await categoryService.GetAllByUserIdAsync(userId, includeInactive, cancellationToken);
            return Results.Ok(Result<List<DocumentCategoryDto>>.Success(categories));
        })
        .WithName("GetDocumentCategoriesByUserId")
        .WithDescription("Kullanıcıya ait döküman kategorilerini getirir (UserId null olanlar + kullanıcının kategorileri)")
        .Produces<Result<List<DocumentCategoryDto>>>();

        // Select2 için kategorileri getir
        group.MapGet("/select", async (
            [FromServices] IDocumentCategoryUseCase categoryService,
            CancellationToken cancellationToken) =>
        {
            var categories = await categoryService.GetAllForSelectAsync(cancellationToken);
            return Results.Ok(Result<List<DocumentCategorySelectDto>>.Success(categories));
        })
        .WithName("GetDocumentCategoriesForSelect")
        .WithDescription("Select2 dropdown için kategorileri getirir")
        .Produces<Result<List<DocumentCategorySelectDto>>>();

        // Kullanıcıya göre Select2 için kategorileri getir
        group.MapGet("/select/by-user/{userId}", async (
            string userId,
            [FromServices] IDocumentCategoryUseCase categoryService,
            CancellationToken cancellationToken) =>
        {
            var categories = await categoryService.GetAllForSelectByUserIdAsync(userId, cancellationToken);
            return Results.Ok(Result<List<DocumentCategorySelectDto>>.Success(categories));
        })
        .WithName("GetDocumentCategoriesForSelectByUserId")
        .WithDescription("Kullanıcıya göre Select2 dropdown için kategorileri getirir")
        .Produces<Result<List<DocumentCategorySelectDto>>>();

        // Id'ye göre kategori getir
        group.MapGet("/{id}", async (
            string id,
            [FromServices] IDocumentCategoryUseCase categoryService,
            CancellationToken cancellationToken) =>
        {
            var category = await categoryService.GetByIdAsync(id, cancellationToken);

            if (category == null)
            {
                return Results.NotFound(Result<DocumentCategoryDto>.Error($"Kategori bulunamadı: {id}"));
            }

            return Results.Ok(Result<DocumentCategoryDto>.Success(category));
        })
        .WithName("GetDocumentCategoryById")
        .WithDescription("Id'ye göre döküman kategorisi getirir")
        .Produces<Result<DocumentCategoryDto>>()
        .Produces<Result<DocumentCategoryDto>>(StatusCodes.Status404NotFound);

        // Yeni kategori oluştur
        group.MapPost("/", async (
            [FromBody] CreateDocumentCategoryRequest request,
            [FromServices] IDocumentCategoryUseCase categoryService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var category = await categoryService.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/document-categories/{category.Id}", 
                    Result<DocumentCategoryDto>.Success(category, "Kategori başarıyla oluşturuldu."));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to create document category: {CategoryId}", request.Id);
                return Results.BadRequest(Result<DocumentCategoryDto>.Error(ex.Message));
            }
        })
        .WithName("CreateDocumentCategory")
        .WithDescription("Yeni döküman kategorisi oluşturur")
        .Produces<Result<DocumentCategoryDto>>(StatusCodes.Status201Created)
        .Produces<Result<DocumentCategoryDto>>(StatusCodes.Status400BadRequest);

        // Kategori güncelle
        group.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateDocumentCategoryRequest request,
            [FromServices] IDocumentCategoryUseCase categoryService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var category = await categoryService.UpdateAsync(id, request, cancellationToken);
                return Results.Ok(Result<DocumentCategoryDto>.Success(category, "Kategori başarıyla güncellendi."));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to update document category: {CategoryId}", id);
                return Results.NotFound(Result<DocumentCategoryDto>.Error(ex.Message));
            }
        })
        .WithName("UpdateDocumentCategory")
        .WithDescription("Döküman kategorisini günceller")
        .Produces<Result<DocumentCategoryDto>>()
        .Produces<Result<DocumentCategoryDto>>(StatusCodes.Status404NotFound);

        // Kategori sil
        group.MapDelete("/{id}", async (
            string id,
            [FromServices] IDocumentCategoryUseCase categoryService,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            var result = await categoryService.DeleteAsync(id, cancellationToken);

            if (!result)
            {
                return Results.NotFound(Result<bool>.Error($"Kategori bulunamadı: {id}"));
            }

            return Results.Ok(Result<bool>.Success(true, "Kategori başarıyla silindi."));
        })
        .WithName("DeleteDocumentCategory")
        .WithDescription("Döküman kategorisini siler")
        .Produces<Result<bool>>()
        .Produces<Result<bool>>(StatusCodes.Status404NotFound);
    }
}
