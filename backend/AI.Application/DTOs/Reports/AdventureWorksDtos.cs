namespace AI.Application.DTOs.Reports;

/// <summary>
/// AdventureWorks hazır raporlar için ortak filtre
/// </summary>
public class AdventureWorksReportFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<int>? TerritoryIds { get; set; }
    public List<int>? ProductCategoryIds { get; set; }
    public List<int>? ProductIds { get; set; }
}

/// <summary>
/// En çok satan ürünler raporu için DTO
/// </summary>
public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int TotalSalesQuantity { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal AverageUnitPrice { get; set; }
    public int OrderCount { get; set; }
}

/// <summary>
/// En değerli müşteriler raporu için DTO
/// </summary>
public class TopCustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public string? TerritoryName { get; set; }
}

/// <summary>
/// Aylık satış trend raporu için DTO
/// </summary>
public class MonthlySalesTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal MonthlySales { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public decimal? PreviousMonthSales { get; set; }
    public decimal? GrowthRate { get; set; }
}

/// <summary>
/// Ürün kategorisi karlılık raporu için DTO
/// </summary>
public class ProductCategoryProfitabilityDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    public decimal AverageUnitProfit { get; set; }
    public int TotalSalesQuantity { get; set; }
}

/// <summary>
/// Düşük stok seviyesi uyarı raporu için DTO
/// </summary>
public class LowStockAlertDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int CurrentStock { get; set; }
    public int SafetyStockLevel { get; set; }
    public int ReorderPoint { get; set; }
    public int ShortageAmount { get; set; }
    public string? LocationName { get; set; }
}

/// <summary>
/// Departman bazında çalışan dağılım raporu için DTO
/// </summary>
public class EmployeeDepartmentDistributionDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime? OldestHireDate { get; set; }
    public DateTime? NewestHireDate { get; set; }
    public decimal? AverageYearsOfService { get; set; }
}

/// <summary>
/// Dropdown seçenekleri için DTO
/// </summary>
public class DropdownOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
