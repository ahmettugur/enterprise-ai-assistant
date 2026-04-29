using System.Security.Cryptography;
using AI.Api.Extensions;
using AI.Application.Common.Helpers;
using AI.Application.DTOs;
using AI.Application.DTOs.DocumentProcessing;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Results;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.Results;

namespace AI.Api.Endpoints.Documents;

/// <summary>
/// Doküman yönetimi endpoint'leri
/// </summary>
public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.DocumentUploadPolicy);

        // Doküman yükleme
        group.MapPost("/upload", async (
            [FromForm] DocumentUploadRequest request,
            [FromServices] IDocumentProcessingUseCase documentProcessingService,
            [FromServices] ILogger<Program> logger) =>
        {
            try
            {
                // Dosya validasyonu
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(Result<DocumentUploadResultDto>.Error("Dosya seçilmedi."));
                }

                // Dosya boyutu kontrolü (150MB)
                const long maxFileSize = 150 * 1024 * 1024;
                if (request.File.Length > maxFileSize)
                {
                    return BadRequest(Result<DocumentUploadResultDto>.Error("Dosya boyutu 150MB'dan büyük olamaz."));
                }

                // Dosya türü kontrolü
                var allowedExtensions = new[] { ".pdf", ".txt", ".docx", ".doc", ".xlsx", ".xls", ".csv", ".pptx", ".ppt" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(Result<DocumentUploadResultDto>.Error("Desteklenmeyen dosya türü. Sadece PDF, TXT, DOCX, DOC, Excel, CSV ve PowerPoint dosyaları kabul edilir."));
                }

                // Dosya hash'i oluştur
                using var stream = request.File.OpenReadStream();
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(stream);
                var fileHash = Convert.ToBase64String(hashBytes);

                // DTO oluştur (entity oluşturma UseCase'de yapılır)
                var uploadDto = new DocumentUploadDto
                {
                    FileName = request.File.FileName,
                    FileType = request.File.ContentType ?? "",
                    FileSize = request.File.Length,
                    FileHash = fileHash,
                    Title = request.Title ?? Path.GetFileNameWithoutExtension(request.File.FileName),
                    Description = request.Description,
                    Category = request.Category ?? "Genel",
                    UploadedBy = request.UploadedBy ?? "Anonim"
                };

                // Stream'i başa al
                stream.Position = 0;

                logger.LogInformation("Doküman yükleme başlatıldı: {FileName}", request.File.FileName);

                var result = await documentProcessingService.ProcessDocumentFromUploadAsync(uploadDto, stream);

                if (result.Success)
                {
                    logger.LogInformation("Doküman başarıyla yüklendi: {DocumentId}", result.Id);
                    return Ok(Result<DocumentUploadResultDto>.Success(result, "Doküman başarıyla yüklendi."));
                }
                else
                {
                    return BadRequest(Result<DocumentUploadResultDto>.Error(result.ErrorMessage ?? "Doküman işlenirken hata oluştu"));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Doküman yükleme sırasında hata oluştu");
                return Problem(
                    detail: "Doküman yükleme sırasında beklenmeyen bir hata oluştu.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
            .WithName("UploadDocument")
            .WithSummary("Doküman yükler")
            .WithDescription("Yeni bir doküman yükler ve işler")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<Result<DocumentUploadResultDto>>(StatusCodes.Status200OK)
            .Produces<Result<string>>(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();

        // Doküman silme
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IDocumentProcessingUseCase documentProcessingService,
            [FromServices] ILogger logger) =>
        {
            try
            {
                logger.LogInformation("Doküman siliniyor: {DocumentId}", id);

                var success = await documentProcessingService.DeleteDocumentAsync(id);

                if (!success)
                {
                    return NotFound(Result<string>.Error("Doküman bulunamadı."));
                }

                logger.LogInformation("Doküman başarıyla silindi: {DocumentId}", id);
                return Ok(Result<string>.Success("Doküman başarıyla silindi.", "Doküman silindi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Doküman silme sırasında hata oluştu: {DocumentId}", id);
                return Problem("Doküman silme sırasında beklenmeyen bir hata oluştu.");
            }
        })
            .WithName("DeleteDocument")
            .WithSummary("Dokümanı siler")
            .WithDescription("Belirtilen ID'ye sahip dokümanı siler")
            .Produces<Result<string>>(StatusCodes.Status200OK)
            .Produces<Result<string>>(StatusCodes.Status404NotFound);

        // Base64 formatında dosya yükleme
        group.MapPost("/upload-base64", async (
            [FromBody] DocumentBase64UploadRequest request,
            [FromServices] IDocumentProcessingUseCase documentProcessingService,
            [FromServices] ILogger logger) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FileContent))
                    return BadRequest(Result<DocumentUploadResultDto>.Error("Dosya içeriği boş olamaz."));

                if (string.IsNullOrWhiteSpace(request.FileName))
                    return BadRequest(Result<DocumentUploadResultDto>.Error("Dosya adı boş olamaz."));

                if (!Helper.IsFileExtensionSupported(request.FileName))
                    return BadRequest(Result<DocumentUploadResultDto>.Error("Desteklenmeyen dosya formatı. Sadece PDF, TXT, DOC ve DOCX dosyaları kabul edilir."));

                logger.LogInformation("Base64 dosya yükleme işlemi başlatıldı: {FileName}", request.FileName);

                using var fileStream = Helper.ConvertBase64ToStream(request.FileContent);

                var mimeType = !string.IsNullOrWhiteSpace(request.MimeType)
                    ? request.MimeType
                    : Helper.GetMimeTypeFromFileName(request.FileName);

                // Dosya hash'i oluştur
                fileStream.Position = 0;
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(fileStream);
                var fileHash = Convert.ToBase64String(hashBytes);

                // DTO oluştur (entity oluşturma UseCase'de yapılır)
                var uploadDto = new DocumentUploadDto
                {
                    FileName = request.FileName,
                    FileType = mimeType,
                    FileSize = fileStream.Length,
                    FileHash = fileHash,
                    Title = request.Title ?? Path.GetFileNameWithoutExtension(request.FileName),
                    Description = request.Description,
                    Category = request.Category ?? "Genel",
                    UploadedBy = request.UploadedBy ?? "Anonim"
                };

                fileStream.Position = 0;

                var result = await documentProcessingService.ProcessDocumentFromUploadAsync(uploadDto, fileStream);

                logger.LogInformation("Base64 dosya başarıyla yüklendi ve işlendi: {DocumentId}", result.Id);

                if (result.Success)
                {
                    return Ok(Result<DocumentUploadResultDto>.Success(result, "Dosya başarıyla yüklendi ve işlendi."));
                }
                else
                {
                    return Problem(result.ErrorMessage ?? "Dosya işleme sırasında hata oluştu.");
                }
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Base64 dosya yükleme sırasında geçersiz parametre: {Message}", ex.Message);
                return BadRequest(Result<DocumentUploadResultDto>.Error(ex.Message));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Base64 dosya yükleme sırasında hata oluştu: {FileName}", request.FileName);
                return Problem("Dosya yükleme sırasında beklenmeyen bir hata oluştu.");
            }
        })
            .WithName("UploadDocumentBase64")
            .WithSummary("Base64 formatında dosya yükler")
            .WithDescription("Base64 string olarak kodlanmış dosyayı yükler ve embedding işlemi için işler")
            .Accepts<DocumentBase64UploadRequest>("application/json")
            .Produces<Result<DocumentUploadResultDto>>(StatusCodes.Status200OK)
            .Produces<Result<DocumentUploadResultDto>>(StatusCodes.Status400BadRequest);
    }
}