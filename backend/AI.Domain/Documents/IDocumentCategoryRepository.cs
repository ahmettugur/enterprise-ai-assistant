

namespace AI.Domain.Documents;

/// <summary>
/// Document Category repository interface — Aggregate Root repository.
/// DocumentDisplayInfo (child entity) erişimi de bu repository üzerinden yapılır.
/// </summary>
public interface IDocumentCategoryRepository
{
    // ── Category operations ──
    Task<DocumentCategory?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<DocumentCategory?> GetByIdWithDocumentsAsync(string id, CancellationToken cancellationToken = default);
    Task<List<DocumentCategory>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentCategory>> GetAllByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentCategory>> GetByOwnerUserIdAsync(string userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentCategory>> GetAllWithDocumentCountAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentCategory>> GetAllWithDocumentCountByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentCategory>> GetWithDocumentCountByOwnerUserIdAsync(string userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<DocumentCategory> CreateAsync(DocumentCategory category, CancellationToken cancellationToken = default);
    Task<DocumentCategory> UpdateAsync(DocumentCategory category, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    // ── Document (child entity) operations ──
    Task<DocumentDisplayInfo?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentDisplayInfo?> GetDocumentByFileNameAsync(string fileName, CancellationToken cancellationToken = default);
    Task<List<DocumentDisplayInfo>> GetAllDocumentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentDisplayInfo>> GetAllDocumentsByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentDisplayInfo>> GetDocumentsByOwnerUserIdAsync(string userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentDisplayInfo>> GetDocumentsByCategoryAsync(string categoryId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentDisplayInfo>> GetDocumentsByCategoryAndUserIdAsync(string categoryId, string? userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<DocumentDisplayInfo> SaveDocumentAsync(DocumentDisplayInfo document, CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DocumentExistsByFileNameAsync(string fileName, CancellationToken cancellationToken = default);
}
