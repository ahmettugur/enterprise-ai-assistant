using AI.Application.Common.Helpers;
using AI.Application.DTOs;
using AI.Application.DTOs.ChatMetadata;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Documents;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Document;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases;

public class DocumentMetadataUseCase : IDocumentMetadataUseCase
{
    private readonly IDocumentCacheService _cacheService;
    private readonly IDocumentCategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentMetadataUseCase> _logger;

    public DocumentMetadataUseCase(
        IDocumentCacheService cacheService,
        IDocumentCategoryRepository categoryRepository,
        ICurrentUserService currentUserService,
        ILogger<DocumentMetadataUseCase> logger)
    {
        _cacheService = cacheService;
        _categoryRepository = categoryRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<List<PromptDocumentInfo>> GetAllDocumentsAsync()
    {
        // Kullanıcı bazlı filtreleme - cache kullanmıyoruz çünkü her kullanıcının farklı sonucu var
        var userId = _currentUserService.UserId;

        _logger.LogDebug("Fetching documents for user: {UserId}", userId ?? "anonymous");

        // DB'den kullanıcıya ait dökümanları çek (UserId == null || UserId == userId)
        var dbDocuments = await _categoryRepository.GetAllDocumentsByUserIdAsync(userId, includeInactive: false);
        if (dbDocuments.Count == 0)
        {
            _logger.LogWarning("No documents found for user: {UserId}", userId ?? "anonymous");
            return new List<PromptDocumentInfo>();
        }

        _logger.LogDebug("Found {Count} documents for user: {UserId}", dbDocuments.Count, userId ?? "anonymous");

        return dbDocuments.Select(d => new PromptDocumentInfo
        {
            Name = d.FileName,
            DisplayName = d.DisplayName,
            DocumentType = d.DocumentType,
            IsActive = d.IsActive
        }).ToList();
    }

    public async Task<List<PromptDocumentCategory>> GetAllCategoriesAsync()
    {
        var userId = _currentUserService.UserId ?? "anonymous";

        // 1. Önce cache'e bak
        var cachedCategories = await _cacheService.GetCategoriesForUserAsync(userId);
        if (cachedCategories != null && cachedCategories.Count > 0)
        {
            _logger.LogDebug("Cache hit: Found {Count} categories for user: {UserId}", cachedCategories.Count, userId);
            return cachedCategories.Select(c => new PromptDocumentCategory
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Description = c.Description,
                IsActive = c.IsActive
            }).ToList();
        }

        _logger.LogDebug("Cache miss: Fetching categories from database for user: {UserId}", userId);

        // 2. DB'den kullanıcıya ait kategorileri çek (UserId == null || UserId == userId)
        var dbCategories = await _categoryRepository.GetAllByUserIdAsync(_currentUserService.UserId, includeInactive: false);
        if (dbCategories.Count == 0)
        {
            _logger.LogWarning("No categories found for user: {UserId}", userId);
            return new List<PromptDocumentCategory>();
        }

        // 3. Cache'e kaydet
        var categoriesToCache = dbCategories.Select(c => new DocumentCategoryDto
        {
            Id = c.Id,
            DisplayName = c.DisplayName,
            Description = c.Description,
            UserId = c.UserId,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        }).ToList();
        await _cacheService.SetCategoriesForUserAsync(userId, categoriesToCache);

        _logger.LogDebug("Found {Count} categories for user: {UserId} and cached", dbCategories.Count, userId);

        return dbCategories.Select(c => new PromptDocumentCategory
        {
            Id = c.Id,
            DisplayName = c.DisplayName,
            Description = c.Description,
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<List<PromptDocumentInfo>> GetDocumentsByCategoryAsync(string categoryId)
    {
        var allDocs = await GetAllDocumentsForUserAsync();
        if (allDocs == null || allDocs.Count == 0) return new List<PromptDocumentInfo>();

        return allDocs
            .Where(d => d.CategoryName?.Equals(categoryId, StringComparison.OrdinalIgnoreCase) == true)
            .Select(d => new PromptDocumentInfo
            {
                Name = d.FileName,
                DisplayName = d.DisplayName,
                DocumentType = d.DocumentType,
                IsActive = d.IsActive
            }).ToList();
    }

    /// <summary>
    /// Prompt'a inject edilecek markdown tablosu oluşturur
    /// </summary>
    public async Task<string> GenerateDocumentListForPromptAsync()
    {
        var documents = await GetAllDocumentsForUserAsync();
        if (documents == null || documents.Count == 0)
        {
            return "| Kategori | Doküman | Görünen Ad | Tip |\n|----------|---------|------------|-----|\n| - | Henüz döküman yok | - | - |";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| Kategori | Doküman | Görünen Ad | Tip |");
        sb.AppendLine("|----------|---------|------------|-----|");

        foreach (var doc in documents.Where(d => d.IsActive).OrderBy(d => d.CategoryName).ThenBy(d => d.FileName))
        {
            var docType = doc.DocumentType == Domain.Enums.DocumentType.QuestionAnswer ? "Q&A" : "Döküman";
            sb.AppendLine($"| {doc.CategoryName ?? "Genel"} | `{doc.FileName}` | {doc.DisplayName} | {docType} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Prompt'a inject edilecek kategori listesi formatını oluşturur
    /// </summary>
    public async Task<string> GenerateCategoryListForPromptAsync()
    {
        var categories = await GetAllCategoriesAsync();
        if (categories.Count == 0)
        {
            return "| onClick Değeri | Beklenen Action | templateName |\n|----------------|-----------------|--------------|";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| onClick Değeri | Beklenen Action | templateName |");
        sb.AppendLine("|----------------|-----------------|--------------|");

        foreach (var category in categories.Where(c => c.IsActive))
        {
            sb.AppendLine($"| `{category.Id}` | `ask_document_category` | `ask_document_category_{category.Id}` |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Dinamik doküman kartları template'i oluşturur
    /// </summary>
    public async Task<string> GenerateDynamicDocumentTemplateAsync(string categoryId)
    {
        var allDocs = await GetAllDocumentsForUserAsync();
        var documents = allDocs?
            .Where(d => d.IsActive && (string.IsNullOrEmpty(categoryId) || d.CategoryId?.Equals(categoryId, StringComparison.OrdinalIgnoreCase) == true))
            .ToList() ?? new List<DocumentDisplayInfoListDto>();

        if (documents.Count == 0)
        {
            return "<div class='no-documents'>Bu kategoride henüz döküman yok.</div>";
        }

        var cardTemplate = GetDocumentCardTemplate();
        var categories = await GetAllCategoriesAsync();
        var categoryDisplayName = categories.FirstOrDefault(c => c.Id == categoryId)?.DisplayName ?? categoryId;

        var optionsBuilder = new System.Text.StringBuilder();
        foreach (var doc in documents)
        {
            var card = cardTemplate
                .Replace("{{DOC_NAME}}", doc.FileName)
                .Replace("{{DISPLAY_NAME}}", doc.DisplayName)
                .Replace("{{DESCRIPTION}}", doc.Description ?? "");

            optionsBuilder.AppendLine(card);
        }

        var mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "fallback_document_selection.html");

        return mainTemplate
            .Replace("{{CATEGORY_NAME}}", categoryDisplayName)
            .Replace("{{DOCUMENT_OPTIONS}}", optionsBuilder.ToString());
    }

    /// <summary>
    /// Dinamik kategori seçim template'i oluşturur
    /// </summary>
    public async Task<string> GenerateDynamicCategorySelectionTemplateAsync()
    {
        var categories = await GetAllCategoriesAsync();

        if (categories.Count == 0)
        {
            return "<div class='no-categories'>Henüz kategori tanımlanmamış.</div>";
        }

        var cardTemplate = GetCategoryCardTemplate();

        var categoryOptionsBuilder = new System.Text.StringBuilder();
        foreach (var category in categories.Where(c => c.IsActive))
        {
            var card = cardTemplate
                .Replace("{{CATEGORY_ID}}", category.Id)
                .Replace("{{DISPLAY_NAME}}", category.DisplayName)
                .Replace("{{DESCRIPTION}}", category.Description);

            categoryOptionsBuilder.AppendLine(card);
        }

        var mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "ask_document.html");
        if (string.IsNullOrWhiteSpace(mainTemplate) || !mainTemplate.Contains("{{CATEGORY_OPTIONS}}"))
        {
            mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "fallback_category_selection.html");
        }

        return mainTemplate.Replace("{{CATEGORY_OPTIONS}}", categoryOptionsBuilder.ToString());
    }

    /// <summary>
    /// Document card template'ini dosyadan okur
    /// </summary>
    private static string GetDocumentCardTemplate()
    {
        var template = Helper.ReadFileContent("Common/Resources/Templates", "document_card_template.html");
        return template;
    }

    /// <summary>
    /// Category card template'ini dosyadan okur
    /// </summary>
    private static string GetCategoryCardTemplate()
    {
        var template = Helper.ReadFileContent("Common/Resources/Templates", "category_card_template.html");
        return template;
    }

    /// <summary>
    /// Kullanıcıya ait dökümanları önce cache'den, yoksa DB'den getirir
    /// Admin dökümanları (UserId == null) + kullanıcının kendi dökümanları
    /// </summary>
    private async Task<List<DocumentDisplayInfoListDto>?> GetAllDocumentsForUserAsync()
    {
        var userId = _currentUserService.UserId ?? "anonymous";

        // 1. Önce cache'e bak
        var cachedDocuments = await _cacheService.GetDocumentsForUserAsync(userId);
        if (cachedDocuments != null && cachedDocuments.Count > 0)
        {
            _logger.LogDebug("Cache hit: Found {Count} documents for user: {UserId}", cachedDocuments.Count, userId);
            return cachedDocuments;
        }

        _logger.LogDebug("Cache miss: Fetching documents from database for user: {UserId}", userId);

        // 2. DB'den kullanıcıya ait dökümanları çek (UserId == null || UserId == userId)
        var dbDocuments = await _categoryRepository.GetAllDocumentsByUserIdAsync(_currentUserService.UserId, includeInactive: false);
        if (dbDocuments.Count == 0)
        {
            _logger.LogWarning("No documents found for user: {UserId}", userId);
            return null;
        }

        var result = dbDocuments.Select(d => new DocumentDisplayInfoListDto
        {
            Id = d.Id,
            FileName = d.FileName,
            DisplayName = d.DisplayName,
            Description = d.Description,
            DocumentType = d.DocumentType,
            CategoryId = d.CategoryId,
            CategoryName = d.Category?.DisplayName,
            UserId = d.UserId,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt
        }).ToList();

        // 3. Cache'e kaydet
        await _cacheService.SetDocumentsForUserAsync(userId, result);

        _logger.LogDebug("Found {Count} documents for user: {UserId} and cached", result.Count, userId);

        return result;
    }
}