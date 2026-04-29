using AI.Api.Extensions;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Endpoints.Documents;

/// <summary>
/// Döküman görüntüleme bilgisi yönetimi endpoint'leri
/// </summary>
public static class DocumentDisplayInfoEndpoints
{
    public static void MapDocumentDisplayInfoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/document-display-info")
            .WithTags("Document Display Info")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        // Tüm dökümanları getir
        group.MapGet("/", async (
            [FromQuery] bool includeInactive,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var documents = await service.GetAllAsync(includeInactive, cancellationToken);
            return Results.Ok(Result<List<DocumentDisplayInfoListDto>>.Success(documents));
        })
        .WithName("GetAllDocumentDisplayInfos")
        .WithDescription("Tüm döküman görüntüleme bilgilerini getirir")
        .Produces<Result<List<DocumentDisplayInfoListDto>>>();

        // Kullanıcıya göre tüm dökümanları getir
        group.MapGet("/by-user/{userId}", async (
            string userId,
            [FromQuery] bool includeInactive,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var documents = await service.GetAllByUserIdAsync(userId, includeInactive, cancellationToken);
            return Results.Ok(Result<List<DocumentDisplayInfoListDto>>.Success(documents));
        })
        .WithName("GetDocumentDisplayInfosByUserId")
        .WithDescription("Kullanıcıya ait dökümanları getirir (UserId null olanlar + kullanıcının dökümanları)")
        .Produces<Result<List<DocumentDisplayInfoListDto>>>();

        // Select2 için dökümanları getir
        group.MapGet("/select", async (
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var documents = await service.GetAllForSelectAsync(cancellationToken);
            return Results.Ok(Result<List<DocumentDisplayInfoSelectDto>>.Success(documents));
        })
        .WithName("GetDocumentDisplayInfosForSelect")
        .WithDescription("Select2 dropdown için dökümanları getirir")
        .Produces<Result<List<DocumentDisplayInfoSelectDto>>>();

        // Kullanıcıya göre Select2 için dökümanları getir
        group.MapGet("/select/by-user/{userId}", async (
            string userId,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var documents = await service.GetAllForSelectByUserIdAsync(userId, cancellationToken);
            return Results.Ok(Result<List<DocumentDisplayInfoSelectDto>>.Success(documents));
        })
        .WithName("GetDocumentDisplayInfosForSelectByUserId")
        .WithDescription("Kullanıcıya göre Select2 dropdown için dökümanları getirir")
        .Produces<Result<List<DocumentDisplayInfoSelectDto>>>();

        // Kategoriye göre dökümanları getir
        group.MapGet("/by-category/{categoryId}", async (
            string categoryId,
            [FromQuery] bool includeInactive,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var documents = await service.GetByCategoryAsync(categoryId, includeInactive, cancellationToken);
            return Results.Ok(Result<List<DocumentDisplayInfoListDto>>.Success(documents));
        })
        .WithName("GetDocumentDisplayInfosByCategory")
        .WithDescription("Kategoriye göre dökümanları getirir")
        .Produces<Result<List<DocumentDisplayInfoListDto>>>();

        // Kategoriye ve kullanıcıya göre dökümanları getir
        group.MapGet("/by-category/{categoryId}/by-user/{userId}", async (
            string categoryId,
            string userId,
            [FromQuery] bool includeInactive,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var documents = await service.GetByCategoryByUserIdAsync(categoryId, userId, includeInactive, cancellationToken);
            return Results.Ok(Result<List<DocumentDisplayInfoListDto>>.Success(documents));
        })
        .WithName("GetDocumentDisplayInfosByCategoryAndUserId")
        .WithDescription("Kategoriye ve kullanıcıya göre dökümanları getirir")
        .Produces<Result<List<DocumentDisplayInfoListDto>>>();

        // Id'ye göre döküman getir
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var document = await service.GetByIdAsync(id, cancellationToken);

            if (document == null)
            {
                return Results.NotFound(Result<DocumentDisplayInfoDto>.Error($"Döküman bulunamadı: {id}"));
            }

            return Results.Ok(Result<DocumentDisplayInfoDto>.Success(document));
        })
        .WithName("GetDocumentDisplayInfoById")
        .WithDescription("Id'ye göre döküman görüntüleme bilgisi getirir")
        .Produces<Result<DocumentDisplayInfoDto>>()
        .Produces<Result<DocumentDisplayInfoDto>>(StatusCodes.Status404NotFound);

        // Dosya adına göre döküman getir
        group.MapGet("/by-filename/{fileName}", async (
            string fileName,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var document = await service.GetByFileNameAsync(fileName, cancellationToken);

            if (document == null)
            {
                return Results.NotFound(Result<DocumentDisplayInfoDto>.Error($"Döküman bulunamadı: {fileName}"));
            }

            return Results.Ok(Result<DocumentDisplayInfoDto>.Success(document));
        })
        .WithName("GetDocumentDisplayInfoByFileName")
        .WithDescription("Dosya adına göre döküman görüntüleme bilgisi getirir")
        .Produces<Result<DocumentDisplayInfoDto>>()
        .Produces<Result<DocumentDisplayInfoDto>>(StatusCodes.Status404NotFound);

        // Dosya yükle ve işle
        group.MapPost("/upload", async (
            [FromForm] DocumentDisplayInfoUploadRequest request,
            [FromServices] IDocumentDisplayInfoUseCase service,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Dosya validasyonu
                if (request.File == null || request.File.Length == 0)
                {
                    return Results.BadRequest(Result<DocumentDisplayInfoDto>.Error("Dosya seçilmedi."));
                }

                // Dosya boyutu kontrolü (50MB)
                const long maxFileSize = 150 * 1024 * 1024; // 150MB
                if (request.File.Length > maxFileSize)
                {
                    return Results.BadRequest(Result<DocumentDisplayInfoDto>.Error("Dosya boyutu 150MB'dan büyük olamaz."));
                }

                // Dosya türü kontrolü
                var allowedExtensions = new[] { ".pdf", ".txt", ".docx", ".doc", ".json" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Results.BadRequest(Result<DocumentDisplayInfoDto>.Error("Desteklenmeyen dosya türü. Sadece PDF, TXT, DOCX, DOC ve JSON dosyaları kabul edilir."));
                }

                using var stream = request.File.OpenReadStream();

                var createRequest = new CreateDocumentDisplayInfoRequest
                {
                    DisplayName = request.DisplayName,
                    DocumentType = request.DocumentType,
                    Description = request.Description,
                    Keywords = request.Keywords,
                    CategoryId = request.CategoryId
                };

                var document = await service.UploadAndProcessAsync(
                    stream,
                    request.File.FileName,
                    request.File.ContentType ?? "application/octet-stream",
                    createRequest,
                    cancellationToken);

                logger.LogInformation("Document uploaded successfully: {DocumentId} - {FileName}", document.Id, document.FileName);

                return Results.Created($"/api/document-display-info/{document.Id}",
                    Result<DocumentDisplayInfoDto>.Success(document, "Döküman başarıyla yüklendi ve işlendi."));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to upload document");
                return Results.BadRequest(Result<DocumentDisplayInfoDto>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading document");
                return Results.Problem(
                    detail: "Döküman yüklenirken bir hata oluştu.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("UploadDocumentDisplayInfo")
        .WithDescription("Döküman yükler, embedding oluşturur ve veritabanına kaydeder")
        .DisableAntiforgery()
        .Produces<Result<DocumentDisplayInfoDto>>(StatusCodes.Status201Created)
        .Produces<Result<DocumentDisplayInfoDto>>(StatusCodes.Status400BadRequest);

        // Döküman güncelle (sadece metadata)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateDocumentDisplayInfoRequest request,
            [FromServices] IDocumentDisplayInfoUseCase service,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var document = await service.UpdateAsync(id, request, cancellationToken);
                return Results.Ok(Result<DocumentDisplayInfoDto>.Success(document, "Döküman başarıyla güncellendi."));
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to update document: {DocumentId}", id);
                return Results.NotFound(Result<DocumentDisplayInfoDto>.Error(ex.Message));
            }
        })
        .WithName("UpdateDocumentDisplayInfo")
        .WithDescription("Döküman metadata'sını günceller (embedding'lere dokunmaz)")
        .Produces<Result<DocumentDisplayInfoDto>>()
        .Produces<Result<DocumentDisplayInfoDto>>(StatusCodes.Status404NotFound);

        // Döküman sil (veritabanı + Qdrant)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IDocumentDisplayInfoUseCase service,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(id, cancellationToken);

            if (!result)
            {
                return Results.NotFound(Result<bool>.Error($"Döküman bulunamadı: {id}"));
            }

            logger.LogInformation("Document deleted: {DocumentId}", id);
            return Results.Ok(Result<bool>.Success(true, "Döküman ve embedding'leri başarıyla silindi."));
        })
        .WithName("DeleteDocumentDisplayInfo")
        .WithDescription("Dökümanı ve Qdrant'taki embedding'lerini siler")
        .Produces<Result<bool>>()
        .Produces<Result<bool>>(StatusCodes.Status404NotFound);

        // Embedding durumunu kontrol et
        group.MapGet("/{id:guid}/embeddings-status", async (
            Guid id,
            [FromServices] IDocumentDisplayInfoUseCase service,
            CancellationToken cancellationToken) =>
        {
            var document = await service.GetByIdAsync(id, cancellationToken);

            if (document == null)
            {
                return Results.NotFound(Result<object>.Error($"Döküman bulunamadı: {id}"));
            }

            var status = new
            {
                document.Id,
                document.FileName,
                document.HasEmbeddings,
                document.ChunkCount
            };

            return Results.Ok(Result<object>.Success(status));
        })
        .WithName("GetDocumentEmbeddingsStatus")
        .WithDescription("Dökümanın Qdrant'taki embedding durumunu kontrol eder")
        .Produces<Result<object>>()
        .Produces<Result<object>>(StatusCodes.Status404NotFound);
    }
}
