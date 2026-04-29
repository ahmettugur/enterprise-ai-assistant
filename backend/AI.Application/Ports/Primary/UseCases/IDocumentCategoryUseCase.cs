using AI.Application.DTOs;

namespace AI.Application.Ports.Primary.UseCases;


/// <summary>
/// Document Category Use Case interface
/// Primary Port - API'den doğrudan çağrılır (DocumentCategoryEndpoints.cs)
/// </summary>
public interface IDocumentCategoryUseCase
{
    Task<DocumentCategoryDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<DocumentCategoryDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentCategoryDto>> GetAllByUserIdAsync(string? userId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<List<DocumentCategorySelectDto>> GetAllForSelectAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentCategorySelectDto>> GetAllForSelectByUserIdAsync(string? userId, CancellationToken cancellationToken = default);
    Task<DocumentCategoryDto> CreateAsync(CreateDocumentCategoryRequest request, CancellationToken cancellationToken = default);
    Task<DocumentCategoryDto> UpdateAsync(string id, UpdateDocumentCategoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
