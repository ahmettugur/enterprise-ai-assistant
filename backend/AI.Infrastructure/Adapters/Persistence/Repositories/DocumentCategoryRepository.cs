using AI.Domain.Documents;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence.Repositories;

/// <summary>
/// PostgreSQL implementation of IDocumentCategoryRepository
/// </summary>
public sealed class DocumentCategoryRepository : IDocumentCategoryRepository
{
    private readonly ChatDbContext _dbContext;
    private readonly ILogger<DocumentCategoryRepository> _logger;

    public DocumentCategoryRepository(
        ChatDbContext dbContext,
        ILogger<DocumentCategoryRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentCategory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.DocumentCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document category by Id: {CategoryId}", id);
            throw;
        }
    }

    public async Task<List<DocumentCategory>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentCategories.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query
                .OrderBy(c => c.DisplayName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all document categories");
            throw;
        }
    }

    public async Task<List<DocumentCategory>> GetAllByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentCategories.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            // UserId null olanlar (sistem kategorileri) veya belirtilen userId'ye ait olanlar
            query = query.Where(c => c.UserId == null || c.UserId == userId);

            return await query
                .OrderBy(c => c.DisplayName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document categories for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<DocumentCategory>> GetByOwnerUserIdAsync(string userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentCategories.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            // Sadece belirtilen userId'ye ait olanlar (admin kategorileri hariç)
            query = query.Where(c => c.UserId == userId);

            return await query
                .OrderBy(c => c.DisplayName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document categories owned by user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<DocumentCategory>> GetAllWithDocumentCountAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentCategories
                .Include(c => c.Documents)
                .AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query
                .OrderBy(c => c.DisplayName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all document categories with document count");
            throw;
        }
    }

    public async Task<List<DocumentCategory>> GetAllWithDocumentCountByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentCategories
                .Include(c => c.Documents)
                .AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            // UserId null olanlar (sistem kategorileri) veya belirtilen userId'ye ait olanlar
            query = query.Where(c => c.UserId == null || c.UserId == userId);

            return await query
                .OrderBy(c => c.DisplayName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document categories with count for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<DocumentCategory>> GetWithDocumentCountByOwnerUserIdAsync(string userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentCategories
                .Include(c => c.Documents)
                .AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            // Sadece belirtilen userId'ye ait olanlar (admin kategorileri hariç)
            query = query.Where(c => c.UserId == userId);

            return await query
                .OrderBy(c => c.DisplayName)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document categories with count owned by user: {UserId}", userId);
            throw;
        }
    }

    public async Task<DocumentCategory> CreateAsync(DocumentCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbContext.DocumentCategories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document category created: {CategoryId}", category.Id);

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document category: {CategoryId}", category.Id);
            throw;
        }
    }

    public async Task<DocumentCategory> UpdateAsync(DocumentCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingCategory = await _dbContext.DocumentCategories
                .FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken);

            if (existingCategory == null)
            {
                throw new InvalidOperationException($"Category not found: {category.Id}");
            }

            existingCategory.Update(category.DisplayName, category.Description);
            if (category.IsActive && !existingCategory.IsActive)
                existingCategory.Activate();
            else if (!category.IsActive && existingCategory.IsActive)
                existingCategory.Deactivate();

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document category updated: {CategoryId}", category.Id);

            return existingCategory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document category: {CategoryId}", category.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _dbContext.DocumentCategories
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (category == null)
            {
                return false;
            }

            _dbContext.DocumentCategories.Remove(category);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document category deleted: {CategoryId}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document category: {CategoryId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.DocumentCategories
                .AnyAsync(c => c.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document category exists: {CategoryId}", id);
            throw;
        }
    }

    public async Task<DocumentCategory?> GetByIdWithDocumentsAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.DocumentCategories
                .Include(c => c.Documents)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document category with documents: {CategoryId}", id);
            throw;
        }
    }

    // ── Document (child entity) operations ──

    public async Task<DocumentDisplayInfo?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.DocumentDisplayInfos
                .Include(d => d.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document by Id: {DocumentId}", id);
            throw;
        }
    }

    public async Task<DocumentDisplayInfo?> GetDocumentByFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.DocumentDisplayInfos
                .Include(d => d.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.FileName == fileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document by FileName: {FileName}", fileName);
            throw;
        }
    }

    public async Task<List<DocumentDisplayInfo>> GetAllDocumentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentDisplayInfos.Include(d => d.Category).AsNoTracking();
            if (!includeInactive) query = query.Where(d => d.IsActive);
            return await query.OrderBy(d => d.DisplayName).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all documents");
            throw;
        }
    }

    public async Task<List<DocumentDisplayInfo>> GetAllDocumentsByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentDisplayInfos.Include(d => d.Category).AsNoTracking();
            if (!includeInactive) query = query.Where(d => d.IsActive);
            query = query.Where(d => d.UserId == null || d.UserId == userId);
            return await query.OrderBy(d => d.DisplayName).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<DocumentDisplayInfo>> GetDocumentsByOwnerUserIdAsync(string userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentDisplayInfos.Include(d => d.Category).AsNoTracking();
            if (!includeInactive) query = query.Where(d => d.IsActive);
            query = query.Where(d => d.UserId == userId);
            return await query.OrderBy(d => d.DisplayName).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents owned by user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<DocumentDisplayInfo>> GetDocumentsByCategoryAsync(string categoryId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentDisplayInfos.Include(d => d.Category).AsNoTracking()
                .Where(d => d.CategoryId == categoryId);
            if (!includeInactive) query = query.Where(d => d.IsActive);
            return await query.OrderBy(d => d.DisplayName).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<List<DocumentDisplayInfo>> GetDocumentsByCategoryAndUserIdAsync(string categoryId, string? userId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.DocumentDisplayInfos.Include(d => d.Category).AsNoTracking()
                .Where(d => d.CategoryId == categoryId);
            if (!includeInactive) query = query.Where(d => d.IsActive);
            query = query.Where(d => d.UserId == null || d.UserId == userId);
            return await query.OrderBy(d => d.DisplayName).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents by category {CategoryId} for user: {UserId}", categoryId, userId);
            throw;
        }
    }

    public async Task<DocumentDisplayInfo> SaveDocumentAsync(DocumentDisplayInfo document, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _dbContext.DocumentDisplayInfos
                .FirstOrDefaultAsync(d => d.Id == document.Id, cancellationToken);

            if (existing == null)
            {
                _dbContext.DocumentDisplayInfos.Add(document);
                _logger.LogInformation("Document created: {DocumentId} - {FileName}", document.Id, document.FileName);
            }
            else
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(document);
                _logger.LogInformation("Document updated: {DocumentId} - {FileName}", document.Id, document.FileName);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document: {DocumentId}", document.Id);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.DocumentDisplayInfos
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (entity == null) return false;

            _dbContext.DocumentDisplayInfos.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document deleted: {DocumentId} - {FileName}", id, entity.FileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {DocumentId}", id);
            throw;
        }
    }

    public async Task<bool> DocumentExistsByFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.DocumentDisplayInfos
                .AnyAsync(d => d.FileName == fileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if document exists: {FileName}", fileName);
            throw;
        }
    }
}
