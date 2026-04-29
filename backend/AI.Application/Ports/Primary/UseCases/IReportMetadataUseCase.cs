using AI.Application.DTOs;
using AI.Application.DTOs.ChatMetadata;

namespace AI.Application.Ports.Primary.UseCases;


/// <summary>
/// Rapor ve veritabanı metadata yönetimi için Use Case - Primary Port
/// Dinamik rapor seçenekleri oluşturur
/// </summary>
public interface IReportMetadataUseCase
{
    /// <summary>
    /// Tüm veritabanlarını getirir
    /// </summary>
    Task<List<DatabaseInfo>> GetAllDatabasesAsync();

    /// <summary>
    /// Veritabanına göre rapor türlerini getirir
    /// </summary>
    Task<List<ReportTypeInfo>> GetReportTypesByDatabaseAsync(string databaseId);

    /// <summary>
    /// Dinamik rapor kategorilerini getirir
    /// </summary>
    Task<List<DynamicReportCategory>> GetDynamicReportCategoriesAsync();

    /// <summary>
    /// Hazır raporları getirir
    /// </summary>
    Task<List<ReadyReportInfo>> GetReadyReportsAsync();

    /// <summary>
    /// Prompt'a inject edilecek veritabanı listesi formatını oluşturur
    /// </summary>
    string GenerateDatabaseListForPrompt();

    /// <summary>
    /// Prompt'a inject edilecek rapor türü listesi formatını oluşturur
    /// </summary>
    string GenerateReportTypeListForPrompt();

    /// <summary>
    /// Prompt'a inject edilecek dinamik rapor kategorileri formatını oluşturur
    /// </summary>
    string GenerateDynamicReportCategoryListForPrompt();

    /// <summary>
    /// Dinamik veritabanı seçim template'i oluşturur
    /// </summary>
    string GenerateDynamicDatabaseSelectionTemplate();

    /// <summary>
    /// Dinamik rapor türü seçim template'i oluşturur (veritabanına göre)
    /// </summary>
    string GenerateDynamicReportTypeTemplate(string databaseId);

    /// <summary>
    /// Dinamik rapor kategorisi seçim template'i oluşturur (veritabanına göre)
    /// </summary>
    string GenerateDynamicReportCategoryTemplate(string databaseId);

    /// <summary>
    /// Hazır rapor seçim template'i oluşturur
    /// </summary>
    string GenerateReadyReportTemplate();

    // ── AdventureWorks Lookup Data Methods ────────────────────────
    // CommonEndpoints tarafından kullanılır — doğrudan IDatabaseService erişimi yerine use case üzerinden.

    Task<List<TerritoryDto>> GetTerritoriesAsync();
    Task<List<StoreDto>> GetStoresAsync();
    Task<List<DepartmentCategoryDto>> GetCategoriesAsync();
    Task<List<ProductDto>> GetProductsAsync();
    Task<List<PromotionDto>> GetPromotionsAsync();
    Task<List<SalesPersonDto>> GetSalesPersonsAsync();
    Task<List<CustomerTypeDto>> GetCustomerTypesAsync();
    Task<List<OrderStatusDto>> GetOrderStatusesAsync();
    Task<List<ShipMethodDto>> GetShipMethodsAsync();
    Task<List<CurrencyDto>> GetCurrenciesAsync();
    Task<List<SalesReasonDto>> GetSalesReasonsAsync();
}
