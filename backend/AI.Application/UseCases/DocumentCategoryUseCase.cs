using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Documents;
using AI.Application.Ports.Secondary.Services.Auth;
using AI.Application.Ports.Secondary.Services.Document;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases;

/// <summary>
/// Document Category Use Case implementation
/// Redis cache ile veritabanı senkronizasyonu yapar
/// </summary>
public sealed class DocumentCategoryUseCase : IDocumentCategoryUseCase
{
    private readonly IDocumentCategoryRepository _repository;
    private readonly IDocumentCacheService _cacheService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentCategoryUseCase> _logger;

    public DocumentCategoryUseCase(
        IDocumentCategoryRepository repository,
        IDocumentCacheService cacheService,
        ICurrentUserService currentUserService,
        ILogger<DocumentCategoryUseCase> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentCategoryDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Tek kayıt için cache kullanmıyoruz, direkt DB'den
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<DocumentCategoryDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // Kullanıcının sadece kendi eklediği kategorileri getir (Admin kategorileri hariç)
        var userId = _currentUserService.UserId;
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GetAllAsync called without authenticated user");
            return new List<DocumentCategoryDto>();
        }

        // User bazlı sorgu - her kullanıcının farklı sonucu olduğu için cache kullanmıyoruz
        var entities = await _repository.GetWithDocumentCountByOwnerUserIdAsync(userId, includeInactive, cancellationToken);
        var result = entities.Select(MapToDto).ToList();

        _logger.LogDebug("Categories loaded for user {UserId} ({Count} items)", userId, result.Count);

        return result;
    }

    public async Task<List<DocumentCategoryDto>> GetAllByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // User bazlı sorgu için cache kullanmıyoruz - her kullanıcının farklı sonucu var
        var entities = await _repository.GetAllWithDocumentCountByUserIdAsync(userId, includeInactive, cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<DocumentCategorySelectDto>> GetAllForSelectAsync(CancellationToken cancellationToken = default)
    {
        // Kullanıcı bazlı filtreleme: Admin kategorileri (UserId = null) + kullanıcının kendi kategorileri
        var userId = _currentUserService.UserId;
        
        // User bazlı sorgu - her kullanıcının farklı sonucu olduğu için cache kullanmıyoruz
        var entities = await _repository.GetAllByUserIdAsync(userId, includeInactive: false, cancellationToken);
        
        _logger.LogDebug("Categories for select loaded for user {UserId} ({Count} items)", userId ?? "anonymous", entities.Count);
        
        return entities.Select(e => new DocumentCategorySelectDto
        {
            Id = e.Id,
            Text = e.DisplayName,
            Description = e.Description
        }).ToList();
    }

    public async Task<List<DocumentCategorySelectDto>> GetAllForSelectByUserIdAsync(string? userId, CancellationToken cancellationToken = default)
    {
        // User bazlı sorgu için cache kullanmıyoruz
        var entities = await _repository.GetAllByUserIdAsync(userId, includeInactive: false, cancellationToken);
        return entities.Select(e => new DocumentCategorySelectDto
        {
            Id = e.Id,
            Text = e.DisplayName,
            Description = e.Description
        }).ToList();
    }

    public async Task<DocumentCategoryDto> CreateAsync(CreateDocumentCategoryRequest request, CancellationToken cancellationToken = default)
    {
        // Check if category already exists
        if (await _repository.ExistsAsync(request.Id, cancellationToken))
        {
            throw new InvalidOperationException($"Category with Id '{request.Id}' already exists.");
        }

        // Admin tarafından oluşturulan kategoriler herkes tarafından görülebilir (UserId = null)
        // Normal kullanıcılar tarafından oluşturulan kategoriler sadece o kullanıcı tarafından görülebilir
        var effectiveUserId = _currentUserService.IsAdmin ? null : _currentUserService.UserId;

        var entity = DocumentCategory.Create(
            id: request.Id,
            displayName: request.DisplayName,
            description: request.Description,
            userId: effectiveUserId
        );

        var created = await _repository.CreateAsync(entity, cancellationToken);

        // Cache'i invalidate et ve yeniden yükle
        await InvalidateAndReloadCacheAsync(cancellationToken);
        
        _logger.LogInformation("Document category created: {CategoryId} - {DisplayName}", created.Id, created.DisplayName);

        return MapToDto(created);
    }

    public async Task<DocumentCategoryDto> UpdateAsync(string id, UpdateDocumentCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new InvalidOperationException($"Category with Id '{id}' not found.");
        }

        existing.Update(request.DisplayName, request.Description);
        if (request.IsActive && !existing.IsActive)
            existing.Activate();
        else if (!request.IsActive && existing.IsActive)
            existing.Deactivate();

        var updated = await _repository.UpdateAsync(existing, cancellationToken);

        // Cache'i invalidate et ve yeniden yükle
        await InvalidateAndReloadCacheAsync(cancellationToken);

        _logger.LogInformation("Document category updated: {CategoryId} - {DisplayName}", updated.Id, updated.DisplayName);

        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeleteAsync(id, cancellationToken);
        
        if (result)
        {
            // Cache'i invalidate et ve yeniden yükle
            await InvalidateAndReloadCacheAsync(cancellationToken);
            _logger.LogInformation("Document category deleted: {CategoryId}", id);
        }

        return result;
    }

    /// <summary>
    /// Cache'i invalidate edip veritabanından yeniden yükler
    /// </summary>
    private async Task InvalidateAndReloadCacheAsync(CancellationToken cancellationToken)
    {
        // Önce tüm cache'leri temizle
        await _cacheService.InvalidateCategoryCacheAsync(cancellationToken);
        
        // Kullanıcı bazlı cache'i de temizle
        var userId = _currentUserService.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            await _cacheService.InvalidateUserCacheAsync(userId, cancellationToken);
            _logger.LogDebug("Invalidated user cache for: {UserId}", userId);
        }

        // Veritabanından tüm verileri çek ve cache'e yükle
        var entities = await _repository.GetAllWithDocumentCountAsync(includeInactive: false, cancellationToken);
        var allCategories = entities.Select(MapToDto).ToList();
        await _cacheService.SetAllCategoriesAsync(allCategories, cancellationToken);

        var selectCategories = entities.Select(e => new DocumentCategorySelectDto
        {
            Id = e.Id,
            Text = e.DisplayName
        }).ToList();
        await _cacheService.SetCategoriesForSelectAsync(selectCategories, cancellationToken);

        _logger.LogInformation("Category cache invalidated and reloaded with {Count} categories", allCategories.Count);
    }

    private static DocumentCategoryDto MapToDto(DocumentCategory entity)
    {
        return new DocumentCategoryDto
        {
            Id = entity.Id,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            UserId = entity.UserId,
            IsActive = entity.IsActive,
            DocumentCount = entity.Documents.Count,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
