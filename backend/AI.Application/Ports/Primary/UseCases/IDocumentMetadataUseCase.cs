using AI.Application.DTOs.ChatMetadata;

namespace AI.Application.Ports.Primary.UseCases;


/// <summary>
/// Doküman metadata yönetimi için Use Case - Primary Port
/// Veritabanından doküman listesi çeker ve prompt'a inject eder
/// Kullanıcı bazlı filtreleme yapar (Admin dökümanları + kullanıcının kendi dökümanları)
/// </summary>
public interface IDocumentMetadataUseCase
{
    /// <summary>
    /// Tüm dokümanları kategorileriyle birlikte getirir (userId filtresi ile)
    /// </summary>
    Task<List<PromptDocumentInfo>> GetAllDocumentsAsync();
    
    /// <summary>
    /// Prompt'a inject edilecek doküman listesi formatını oluşturur
    /// </summary>
    Task<string> GenerateDocumentListForPromptAsync();
    
    /// <summary>
    /// Prompt'a inject edilecek kategori listesi formatını oluşturur
    /// </summary>
    Task<string> GenerateCategoryListForPromptAsync();
    
    /// <summary>
    /// Kategoriye göre dokümanları filtreler
    /// </summary>
    Task<List<PromptDocumentInfo>> GetDocumentsByCategoryAsync(string category);
    
    /// <summary>
    /// Dinamik template HTML'i oluşturur (doküman kartları)
    /// </summary>
    Task<string> GenerateDynamicDocumentTemplateAsync(string categoryId);
    
    /// <summary>
    /// Tüm kategorileri getirir (userId filtresi ile)
    /// </summary>
    Task<List<PromptDocumentCategory>> GetAllCategoriesAsync();
    
    /// <summary>
    /// Dinamik kategori seçim template'i oluşturur
    /// </summary>
    Task<string> GenerateDynamicCategorySelectionTemplateAsync();
}
