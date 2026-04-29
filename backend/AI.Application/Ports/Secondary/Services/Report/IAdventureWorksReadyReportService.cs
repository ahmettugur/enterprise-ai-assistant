using AI.Application.Results;
using AI.Application.DTOs.Reports;

namespace AI.Application.Ports.Secondary.Services.Report;

/// <summary>
/// AdventureWorks hazır raporlar Secondary Port interface'i
/// Secondary Port - SQL sorguları çalıştıran Adapter tarafından implement edilir
/// Implementation: AI.Infrastructure/Adapters/AI/ReadyReports/AdventureWorks/AdventureWorksReadyReportService.cs
/// </summary>
public interface IAdventureWorksReadyReportService
{
    /// <summary>
    /// En çok satan ürünleri getirir
    /// </summary>
    Task<Result<List<TopProductDto>>> GetTopProductsAsync(AdventureWorksReportFilter filter, int topCount = 10);
    
    /// <summary>
    /// En değerli müşterileri getirir
    /// </summary>
    Task<Result<List<TopCustomerDto>>> GetTopCustomersAsync(AdventureWorksReportFilter filter, int topCount = 10);
    
    /// <summary>
    /// Aylık satış trend verilerini getirir
    /// </summary>
    Task<Result<List<MonthlySalesTrendDto>>> GetMonthlySalesTrendAsync(AdventureWorksReportFilter filter);
    
    /// <summary>
    /// Ürün kategorisi karlılık verilerini getirir
    /// </summary>
    Task<Result<List<ProductCategoryProfitabilityDto>>> GetProductCategoryProfitabilityAsync(AdventureWorksReportFilter filter);
    
    /// <summary>
    /// Düşük stok seviyesi uyarılarını getirir
    /// </summary>
    Task<Result<List<LowStockAlertDto>>> GetLowStockAlertsAsync(AdventureWorksReportFilter filter);
    
    /// <summary>
    /// Departman bazında çalışan dağılımını getirir
    /// </summary>
    Task<Result<List<EmployeeDepartmentDistributionDto>>> GetEmployeeDepartmentDistributionAsync();
    
    /// <summary>
    /// Bölgeleri getirir (dropdown için)
    /// </summary>
    Task<Result<List<DropdownOptionDto>>> GetTerritoriesAsync();
    
    /// <summary>
    /// Ürün kategorilerini getirir (dropdown için)
    /// </summary>
    Task<Result<List<DropdownOptionDto>>> GetProductCategoriesAsync();
}
