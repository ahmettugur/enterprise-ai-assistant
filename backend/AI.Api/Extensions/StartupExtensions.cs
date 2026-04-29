using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using AI.Infrastructure.Adapters.Persistence;
using AI.Infrastructure.Extensions;
using AI.Application.Common.Helpers;
using AI.Application.DTOs.DocumentProcessing;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Documents;

namespace AI.Api.Extensions;

/// <summary>
/// Extensions for application startup and initialization
/// </summary>
public static class StartupExtensions
{
    /// <summary>
    /// Configures static file serving for multiple directories
    /// </summary>
    public static void ConfigureStaticFiles(this WebApplication app)
    {
        // Default wwwroot static files
        app.UseStaticFiles();

        // Output folder for generated content
        var outputFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output-folder");
        EnsureDirectoryExists(outputFolderPath);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(outputFolderPath),
            RequestPath = "/output-folder"
        });

        // Uploads folder
        var uploadsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");
        EnsureDirectoryExists(uploadsFolderPath);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadsFolderPath),
            RequestPath = "/uploads"
        });

        // Documents folder
        var documentsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documents");
        EnsureDirectoryExists(documentsFolderPath);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(documentsFolderPath),
            RequestPath = "/documents"
        });
    }

    /// <summary>
    /// Ensures database migration is applied
    /// </summary>
    public static async Task EnsureDatabaseMigrationAsync(this WebApplication app)
    {
        // Skip heavy DB operations in testing environment
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var dataContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            await dataContext.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully");
            
            // Seed default users (admin)
            await app.Services.SeedDefaultUsersAsync();
            logger.LogInformation("Default users seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database migration. Application will continue with fallback storage.");
        }
    }

    /// <summary>
    /// Ensures a directory exists, creates it if it doesn't
    /// </summary>
    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Initializes the user memory collection in Qdrant vector database.
    /// This ensures the collection exists before any memory operations are performed.
    /// </summary>
    public static async Task InitializeUserMemoryCollectionAsync(this WebApplication app)
    {
        // Skip in testing environment
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var userMemoryService = scope.ServiceProvider.GetService<IUserMemoryUseCase>();
            if (userMemoryService == null)
            {
                logger.LogWarning("IUserMemoryUseCase not registered, skipping memory collection initialization");
                return;
            }

            var result = await userMemoryService.InitializeCollectionAsync();
            if (result.IsSucceed)
            {
                logger.LogInformation("User memory collection initialized successfully");
            }
            else
            {
                logger.LogWarning("Failed to initialize user memory collection: {Error}", result.SystemMessage ?? result.UserMessage);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error initializing user memory collection. Memory features may not work correctly.");
        }
    }

    /// <summary>
    /// Application/Data klasöründeki sistem dokümanlarını Qdrant'a yükler ve veritabanına kaydeder.
    /// Dosya SHA256 hash'i ile Qdrant duplicate kontrolü, dosya adı ile DB duplicate kontrolü yapar.
    /// </summary>
    public static async Task InitializeSystemDocumentsAsync(this WebApplication app)
    {
        // Skip in testing environment
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var documentProcessingService = scope.ServiceProvider.GetService<IDocumentProcessingUseCase>();
            if (documentProcessingService == null)
            {
                logger.LogWarning("IDocumentProcessingUseCase not registered, skipping system documents initialization");
                return;
            }

            var categoryRepository = scope.ServiceProvider.GetService<IDocumentCategoryRepository>();
            if (categoryRepository == null)
            {
                logger.LogWarning("IDocumentCategoryRepository not registered, skipping system documents initialization");
                return;
            }

            // Application/Data klasörünü bul (build output'a kopyalanmış olmalı)
            var dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Application", "Data");

            if (!Directory.Exists(dataFolderPath))
            {
                logger.LogWarning("System documents data folder not found: {DataFolderPath}", dataFolderPath);
                return;
            }

            // Desteklenen dosya uzantıları
            var supportedExtensions = new[] { ".pdf", ".txt", ".docx", ".doc", ".json" };
            var files = Directory.GetFiles(dataFolderPath)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            if (files.Length == 0)
            {
                logger.LogInformation("No system documents found in {DataFolderPath}", dataFolderPath);
                return;
            }

            logger.LogInformation("Found {FileCount} system documents to process in {DataFolderPath}", files.Length, dataFolderPath);

            // "sistem" kategorisini bul veya oluştur
            const string systemCategoryId = "sistem";
            var systemCategory = await categoryRepository.GetByIdAsync(systemCategoryId);
            if (systemCategory == null)
            {
                systemCategory = DocumentCategory.Create(
                    id: systemCategoryId,
                    displayName: "Sistem Dokümanları",
                    description: "Sistem tarafından otomatik yüklenen dokümanlar",
                    userId: null // Tüm kullanıcılar görebilir
                );
                systemCategory = await categoryRepository.CreateAsync(systemCategory);
                logger.LogInformation("Created system document category: {CategoryId}", systemCategoryId);
            }

            var processedCount = 0;
            var skippedCount = 0;

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var sanitizedFileName = TurkishEncodingHelper.SanitizeFileName(fileName);

                try
                {
                    // DB'de dosya adı kontrolü yap
                    var existsInDb = await categoryRepository.DocumentExistsByFileNameAsync(sanitizedFileName);
                    if (existsInDb)
                    {
                        logger.LogInformation("System document already exists in database, skipping: {FileName}", sanitizedFileName);
                        skippedCount++;
                        continue;
                    }

                    // SHA256 hash hesapla
                    string fileHash;
                    await using (var hashStream = File.OpenRead(filePath))
                    {
                        using var sha256 = System.Security.Cryptography.SHA256.Create();
                        var hashBytes = await sha256.ComputeHashAsync(hashStream);
                        fileHash = Convert.ToBase64String(hashBytes);
                    }

                    // Dosyayı Qdrant'a yükle — mevcut pipeline'ı kullan
                    await using var fileStream = File.OpenRead(filePath);
                    var fileInfo = new FileInfo(filePath);

                    var uploadDto = new DocumentUploadDto
                    {
                        FileName = sanitizedFileName,
                        FileType = GetMimeType(fileName),
                        FileSize = fileInfo.Length,
                        FileHash = fileHash,
                        DocumentType = AI.Domain.Enums.DocumentType.Document,
                        Title = Path.GetFileNameWithoutExtension(fileName),
                        Description = "Sistem tarafından otomatik yüklenen doküman",
                        Category = systemCategoryId,
                        UserId = null,
                        UploadedBy = "System"
                    };

                    var result = await documentProcessingService.ProcessDocumentFromUploadAsync(uploadDto, fileStream);

                    if (result.Success)
                    {
                        // Veritabanına DocumentDisplayInfo kaydı oluştur
                        var displayName = Path.GetFileNameWithoutExtension(fileName);
                        var documentEntity = systemCategory.AddDocument(
                            fileName: sanitizedFileName,
                            displayName: displayName,
                            documentType: AI.Domain.Enums.DocumentType.Document,
                            description: "Sistem tarafından otomatik yüklenen doküman",
                            keywords: null,
                            userId: null, // Tüm kullanıcılar görebilir
                            createdBy: "System"
                        );

                        await categoryRepository.SaveDocumentAsync(documentEntity);

                        logger.LogInformation(
                            "System document processed and saved to DB: {FileName} ({ChunkCount} chunks, DocumentId: {DocumentId})",
                            sanitizedFileName, result.ProcessedChunks, documentEntity.Id);
                        processedCount++;
                    }
                    else
                    {
                        logger.LogWarning("Failed to process system document: {FileName} - {Error}",
                            sanitizedFileName, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing system document: {FileName}", fileName);
                }
            }

            logger.LogInformation(
                "System documents initialization completed. Processed: {ProcessedCount}, Skipped (already exists): {SkippedCount}, Total: {TotalCount}",
                processedCount, skippedCount, files.Length);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error initializing system documents. Application will continue without system documents.");
        }
    }

    /// <summary>
    /// Dosya uzantısından MIME type belirler
    /// </summary>
    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }
}
