
using AI.Application.Results;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.Ports.Secondary.Services.Database;
using AI.Application.DTOs.Reports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.ReadyReports.AdventureWorks;

/// <summary>
/// AdventureWorks hazır raporlar servisi
/// </summary>
public class AdventureWorksReadyReportService : IAdventureWorksReadyReportService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<AdventureWorksReadyReportService> _logger;

    public AdventureWorksReadyReportService(
        [FromKeyedServices("adventureworks")] IDatabaseService databaseService,
        ILogger<AdventureWorksReadyReportService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task<Result<List<TopProductDto>>> GetTopProductsAsync(AdventureWorksReportFilter filter, int topCount = 10)
    {
        try
        {
            var whereConditions = new List<string> { "soh.Status = 5" }; // Sadece teslim edilmiş siparişler (Shipped)
            
            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToString("yyyy-MM-dd");
                whereConditions.Add($"soh.OrderDate >= '{startDate}'");
            }
            
            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToString("yyyy-MM-dd");
                whereConditions.Add($"soh.OrderDate <= '{endDate}'");
            }
            
            if (filter.TerritoryIds != null && filter.TerritoryIds.Any())
            {
                var territoryIds = string.Join(", ", filter.TerritoryIds);
                whereConditions.Add($"soh.TerritoryID IN ({territoryIds})");
            }
            
            if (filter.ProductCategoryIds != null && filter.ProductCategoryIds.Any())
            {
                var categoryIds = string.Join(", ", filter.ProductCategoryIds);
                whereConditions.Add($"pc.ProductCategoryID IN ({categoryIds})");
            }
            
            if (filter.ProductIds != null && filter.ProductIds.Any())
            {
                var productIds = string.Join(", ", filter.ProductIds);
                whereConditions.Add($"p.ProductID IN ({productIds})");
            }

            var whereClause = string.Join(" AND ", whereConditions);

            var query = $"""
                SELECT TOP {topCount}
                    p.ProductID AS ProductId,
                    p.Name AS ProductName,
                    p.ProductNumber AS ProductNumber,
                    pc.Name AS CategoryName,
                    SUM(sod.OrderQty) AS TotalSalesQuantity,
                    SUM(sod.LineTotal) AS TotalSalesAmount,
                    AVG(sod.UnitPrice) AS AverageUnitPrice,
                    COUNT(DISTINCT sod.SalesOrderID) AS OrderCount
                FROM Production.Product p
                JOIN Sales.SalesOrderDetail sod ON p.ProductID = sod.ProductID
                JOIN Sales.SalesOrderHeader soh ON sod.SalesOrderID = soh.SalesOrderID
                LEFT JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                LEFT JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                WHERE {whereClause}
                GROUP BY p.ProductID, p.Name, p.ProductNumber, pc.Name
                ORDER BY TotalSalesQuantity DESC
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var products = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new TopProductDto
                {
                    ProductId = Convert.ToInt32(dict["ProductId"]),
                    ProductName = dict["ProductName"]?.ToString() ?? string.Empty,
                    ProductNumber = dict["ProductNumber"]?.ToString() ?? string.Empty,
                    CategoryName = dict["CategoryName"]?.ToString(),
                    TotalSalesQuantity = Convert.ToInt32(dict["TotalSalesQuantity"]),
                    TotalSalesAmount = Convert.ToDecimal(dict["TotalSalesAmount"]),
                    AverageUnitPrice = Convert.ToDecimal(dict["AverageUnitPrice"]),
                    OrderCount = Convert.ToInt32(dict["OrderCount"])
                };
            }).ToList();

            return Result<List<TopProductDto>>.Success(products, "En çok satan ürünler başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top products");
            return Result<List<TopProductDto>>.Error("En çok satan ürünler getirilirken bir hata oluştu.");
        }
    }

    public async Task<Result<List<TopCustomerDto>>> GetTopCustomersAsync(AdventureWorksReportFilter filter, int topCount = 10)
    {
        try
        {
            var whereConditions = new List<string> { "soh.Status = 5" }; // Sadece teslim edilmiş siparişler (Shipped)
            
            if (filter.StartDate.HasValue)
            {
                var startDate = filter.StartDate.Value.ToString("yyyy-MM-dd");
                whereConditions.Add($"soh.OrderDate >= '{startDate}'");
            }
            
            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.ToString("yyyy-MM-dd");
                whereConditions.Add($"soh.OrderDate <= '{endDate}'");
            }
            
            if (filter.TerritoryIds != null && filter.TerritoryIds.Any())
            {
                var territoryIds = string.Join(", ", filter.TerritoryIds);
                whereConditions.Add($"soh.TerritoryID IN ({territoryIds})");
            }

            var whereClause = string.Join(" AND ", whereConditions);

            var query = $"""
                SELECT TOP {topCount}
                    c.CustomerID AS CustomerId,
                    CONCAT(p.FirstName, ' ', p.LastName) AS CustomerName,
                    pe.EmailAddress AS EmailAddress,
                    COUNT(soh.SalesOrderID) AS OrderCount,
                    SUM(soh.TotalDue) AS TotalPurchaseAmount,
                    AVG(soh.TotalDue) AS AverageOrderAmount,
                    MAX(soh.OrderDate) AS LastOrderDate,
                    st.Name AS TerritoryName
                FROM Sales.Customer c
                JOIN Person.Person p ON c.PersonID = p.BusinessEntityID
                LEFT JOIN Person.EmailAddress pe ON p.BusinessEntityID = pe.BusinessEntityID
                JOIN Sales.SalesOrderHeader soh ON c.CustomerID = soh.CustomerID
                LEFT JOIN Sales.SalesTerritory st ON soh.TerritoryID = st.TerritoryID
                WHERE {whereClause}
                GROUP BY c.CustomerID, p.FirstName, p.LastName, pe.EmailAddress, st.Name
                ORDER BY TotalPurchaseAmount DESC
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var customers = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new TopCustomerDto
                {
                    CustomerId = Convert.ToInt32(dict["CustomerId"]),
                    CustomerName = dict["CustomerName"]?.ToString() ?? string.Empty,
                    EmailAddress = dict["EmailAddress"]?.ToString(),
                    OrderCount = Convert.ToInt32(dict["OrderCount"]),
                    TotalPurchaseAmount = Convert.ToDecimal(dict["TotalPurchaseAmount"]),
                    AverageOrderAmount = Convert.ToDecimal(dict["AverageOrderAmount"]),
                    LastOrderDate = dict["LastOrderDate"] != null ? Convert.ToDateTime(dict["LastOrderDate"]) : null,
                    TerritoryName = dict["TerritoryName"]?.ToString()
                };
            }).ToList();

            return Result<List<TopCustomerDto>>.Success(customers, "En değerli müşteriler başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top customers");
            return Result<List<TopCustomerDto>>.Error("En değerli müşteriler getirilirken bir hata oluştu.");
        }
    }

    public async Task<Result<List<MonthlySalesTrendDto>>> GetMonthlySalesTrendAsync(AdventureWorksReportFilter filter)
    {
        try
        {
            // AdventureWorks veritabanı 2011-2014 arası veri içerir
            var startDate = filter.StartDate ?? new DateTime(2011, 1, 1);
            var endDate = filter.EndDate ?? new DateTime(2014, 12, 31);
            
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");

            var whereConditions = new List<string>
            {
                $"soh.OrderDate >= '{startDateStr}'",
                $"soh.OrderDate <= '{endDateStr}'",
                "soh.Status = 5" // Sadece teslim edilmiş siparişler (Shipped)
            };

            if (filter.TerritoryIds != null && filter.TerritoryIds.Any())
            {
                var territoryIds = string.Join(", ", filter.TerritoryIds);
                whereConditions.Add($"soh.TerritoryID IN ({territoryIds})");
            }

            var whereClause = string.Join(" AND ", whereConditions);

            var query = $"""
                WITH MonthlyData AS (
                    SELECT 
                        YEAR(soh.OrderDate) AS Year,
                        MONTH(soh.OrderDate) AS Month,
                        DATENAME(MONTH, soh.OrderDate) AS MonthName,
                        SUM(soh.TotalDue) AS MonthlySales,
                        COUNT(soh.SalesOrderID) AS OrderCount,
                        AVG(soh.TotalDue) AS AverageOrderAmount
                    FROM Sales.SalesOrderHeader soh
                    WHERE {whereClause}
                    GROUP BY YEAR(soh.OrderDate), MONTH(soh.OrderDate), DATENAME(MONTH, soh.OrderDate)
                )
                SELECT 
                    Year,
                    Month,
                    MonthName,
                    MonthlySales,
                    OrderCount,
                    AverageOrderAmount,
                    LAG(MonthlySales) OVER (ORDER BY Year, Month) AS PreviousMonthSales,
                    CASE 
                        WHEN LAG(MonthlySales) OVER (ORDER BY Year, Month) > 0 
                        THEN ROUND(((MonthlySales - LAG(MonthlySales) OVER (ORDER BY Year, Month)) / LAG(MonthlySales) OVER (ORDER BY Year, Month)) * 100, 2) 
                        ELSE 0 
                    END AS GrowthRate
                FROM MonthlyData
                ORDER BY Year DESC, Month DESC
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var trends = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new MonthlySalesTrendDto
                {
                    Year = Convert.ToInt32(dict["Year"]),
                    Month = Convert.ToInt32(dict["Month"]),
                    MonthName = dict["MonthName"]?.ToString() ?? string.Empty,
                    MonthlySales = Convert.ToDecimal(dict["MonthlySales"]),
                    OrderCount = Convert.ToInt32(dict["OrderCount"]),
                    AverageOrderAmount = Convert.ToDecimal(dict["AverageOrderAmount"]),
                    PreviousMonthSales = dict["PreviousMonthSales"] != null ? Convert.ToDecimal(dict["PreviousMonthSales"]) : null,
                    GrowthRate = dict["GrowthRate"] != null ? Convert.ToDecimal(dict["GrowthRate"]) : null
                };
            }).ToList();

            return Result<List<MonthlySalesTrendDto>>.Success(trends, "Aylık satış trend verileri başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly sales trend");
            return Result<List<MonthlySalesTrendDto>>.Error("Aylık satış trend verileri getirilirken bir hata oluştu.");
        }
    }

    public async Task<Result<List<ProductCategoryProfitabilityDto>>> GetProductCategoryProfitabilityAsync(AdventureWorksReportFilter filter)
    {
        try
        {
            // AdventureWorks veritabanı 2011-2014 arası veri içerir
            var startDate = filter.StartDate ?? new DateTime(2011, 1, 1);
            var endDate = filter.EndDate ?? new DateTime(2014, 12, 31);
            
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");

            var whereConditions = new List<string>
            {
                "p.StandardCost > 0",
                $"soh.OrderDate >= '{startDateStr}'",
                $"soh.OrderDate <= '{endDateStr}'",
                "soh.Status = 5" // Sadece teslim edilmiş siparişler (Shipped)
            };

            if (filter.TerritoryIds != null && filter.TerritoryIds.Any())
            {
                var territoryIds = string.Join(", ", filter.TerritoryIds);
                whereConditions.Add($"soh.TerritoryID IN ({territoryIds})");
            }

            var whereClause = string.Join(" AND ", whereConditions);

            var query = $"""
                SELECT 
                    pc.ProductCategoryID AS CategoryId,
                    pc.Name AS CategoryName,
                    COUNT(DISTINCT p.ProductID) AS ProductCount,
                    SUM(sod.LineTotal) AS TotalRevenue,
                    SUM(sod.OrderQty * p.StandardCost) AS TotalCost,
                    SUM(sod.LineTotal) - SUM(sod.OrderQty * p.StandardCost) AS TotalProfit,
                    CASE 
                        WHEN SUM(sod.LineTotal) > 0 
                        THEN ROUND(((SUM(sod.LineTotal) - SUM(sod.OrderQty * p.StandardCost)) / SUM(sod.LineTotal)) * 100, 2) 
                        ELSE 0 
                    END AS ProfitMarginPercent,
                    AVG(sod.UnitPrice - p.StandardCost) AS AverageUnitProfit,
                    SUM(sod.OrderQty) AS TotalSalesQuantity
                FROM Production.ProductCategory pc
                JOIN Production.ProductSubcategory psc ON pc.ProductCategoryID = psc.ProductCategoryID
                JOIN Production.Product p ON psc.ProductSubcategoryID = p.ProductSubcategoryID
                JOIN Sales.SalesOrderDetail sod ON p.ProductID = sod.ProductID
                JOIN Sales.SalesOrderHeader soh ON sod.SalesOrderID = soh.SalesOrderID
                WHERE {whereClause}
                GROUP BY pc.ProductCategoryID, pc.Name
                ORDER BY TotalProfit DESC
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var profitability = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new ProductCategoryProfitabilityDto
                {
                    CategoryId = Convert.ToInt32(dict["CategoryId"]),
                    CategoryName = dict["CategoryName"]?.ToString() ?? string.Empty,
                    ProductCount = Convert.ToInt32(dict["ProductCount"]),
                    TotalRevenue = Convert.ToDecimal(dict["TotalRevenue"]),
                    TotalCost = Convert.ToDecimal(dict["TotalCost"]),
                    TotalProfit = Convert.ToDecimal(dict["TotalProfit"]),
                    ProfitMarginPercent = Convert.ToDecimal(dict["ProfitMarginPercent"]),
                    AverageUnitProfit = Convert.ToDecimal(dict["AverageUnitProfit"]),
                    TotalSalesQuantity = Convert.ToInt32(dict["TotalSalesQuantity"])
                };
            }).ToList();

            return Result<List<ProductCategoryProfitabilityDto>>.Success(profitability, "Ürün kategorisi karlılık verileri başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product category profitability");
            return Result<List<ProductCategoryProfitabilityDto>>.Error("Ürün kategorisi karlılık verileri getirilirken bir hata oluştu.");
        }
    }

    public async Task<Result<List<LowStockAlertDto>>> GetLowStockAlertsAsync(AdventureWorksReportFilter filter)
    {
        try
        {
            var whereConditions = new List<string> { "p.SafetyStockLevel IS NOT NULL" };
            
            if (filter.ProductCategoryIds != null && filter.ProductCategoryIds.Any())
            {
                var categoryIds = string.Join(", ", filter.ProductCategoryIds);
                whereConditions.Add($"pc.ProductCategoryID IN ({categoryIds})");
            }
            
            if (filter.ProductIds != null && filter.ProductIds.Any())
            {
                var productIds = string.Join(", ", filter.ProductIds);
                whereConditions.Add($"p.ProductID IN ({productIds})");
            }

            var whereClause = string.Join(" AND ", whereConditions);

            var query = $"""
                SELECT 
                    p.ProductID AS ProductId,
                    p.Name AS ProductName,
                    p.ProductNumber AS ProductNumber,
                    pc.Name AS CategoryName,
                    SUM(pi.Quantity) AS CurrentStock,
                    p.SafetyStockLevel AS SafetyStockLevel,
                    p.ReorderPoint AS ReorderPoint,
                    (p.SafetyStockLevel - SUM(pi.Quantity)) AS ShortageAmount,
                    l.Name AS LocationName
                FROM Production.Product p
                JOIN Production.ProductInventory pi ON p.ProductID = pi.ProductID
                JOIN Production.Location l ON pi.LocationID = l.LocationID
                LEFT JOIN Production.ProductSubcategory psc ON p.ProductSubcategoryID = psc.ProductSubcategoryID
                LEFT JOIN Production.ProductCategory pc ON psc.ProductCategoryID = pc.ProductCategoryID
                WHERE {whereClause}
                GROUP BY p.ProductID, p.Name, p.ProductNumber, p.SafetyStockLevel, p.ReorderPoint, pc.Name, l.LocationID, l.Name
                HAVING SUM(pi.Quantity) < p.SafetyStockLevel
                ORDER BY ShortageAmount DESC
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var alerts = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new LowStockAlertDto
                {
                    ProductId = Convert.ToInt32(dict["ProductId"]),
                    ProductName = dict["ProductName"]?.ToString() ?? string.Empty,
                    ProductNumber = dict["ProductNumber"]?.ToString() ?? string.Empty,
                    CategoryName = dict["CategoryName"]?.ToString(),
                    CurrentStock = Convert.ToInt32(dict["CurrentStock"]),
                    SafetyStockLevel = Convert.ToInt32(dict["SafetyStockLevel"]),
                    ReorderPoint = Convert.ToInt32(dict["ReorderPoint"]),
                    ShortageAmount = Convert.ToInt32(dict["ShortageAmount"]),
                    LocationName = dict["LocationName"]?.ToString()
                };
            }).ToList();

            return Result<List<LowStockAlertDto>>.Success(alerts, "Düşük stok uyarıları başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock alerts");
            return Result<List<LowStockAlertDto>>.Error("Düşük stok uyarıları getirilirken bir hata oluştu.");
        }
    }

    public async Task<Result<List<EmployeeDepartmentDistributionDto>>> GetEmployeeDepartmentDistributionAsync()
    {
        try
        {
            var query = """
                SELECT 
                    d.DepartmentID AS DepartmentId,
                    d.Name AS DepartmentName,
                    d.GroupName AS GroupName,
                    COUNT(DISTINCT edh.BusinessEntityID) AS EmployeeCount,
                    MIN(e.HireDate) AS OldestHireDate,
                    MAX(e.HireDate) AS NewestHireDate,
                    AVG(DATEDIFF(YEAR, e.HireDate, GETDATE())) AS AverageYearsOfService
                FROM HumanResources.Department d
                JOIN HumanResources.EmployeeDepartmentHistory edh ON d.DepartmentID = edh.DepartmentID
                JOIN HumanResources.Employee e ON edh.BusinessEntityID = e.BusinessEntityID
                WHERE edh.EndDate IS NULL 
                    AND e.CurrentFlag = 1
                GROUP BY d.DepartmentID, d.Name, d.GroupName
                ORDER BY EmployeeCount DESC
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var distribution = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new EmployeeDepartmentDistributionDto
                {
                    DepartmentId = Convert.ToInt32(dict["DepartmentId"]),
                    DepartmentName = dict["DepartmentName"]?.ToString() ?? string.Empty,
                    GroupName = dict["GroupName"]?.ToString(),
                    EmployeeCount = Convert.ToInt32(dict["EmployeeCount"]),
                    OldestHireDate = dict["OldestHireDate"] != null ? Convert.ToDateTime(dict["OldestHireDate"]) : null,
                    NewestHireDate = dict["NewestHireDate"] != null ? Convert.ToDateTime(dict["NewestHireDate"]) : null,
                    AverageYearsOfService = dict["AverageYearsOfService"] != null ? Convert.ToDecimal(dict["AverageYearsOfService"]) : null
                };
            }).ToList();

            return Result<List<EmployeeDepartmentDistributionDto>>.Success(distribution, "Departman bazında çalışan dağılımı başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee department distribution");
            return Result<List<EmployeeDepartmentDistributionDto>>.Error("Departman bazında çalışan dağılımı getirilirken bir hata oluştu.");
        }
    }

    public async Task<Result<List<DropdownOptionDto>>> GetTerritoriesAsync()
    {
        try
        {
            var query = """
                SELECT 
                    CAST(TerritoryID AS VARCHAR) AS Value,
                    Name AS Label
                FROM Sales.SalesTerritory
                ORDER BY Name
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var territories = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new DropdownOptionDto
                {
                    Value = dict["Value"]?.ToString() ?? string.Empty,
                    Label = dict["Label"]?.ToString() ?? string.Empty
                };
            }).ToList();

            return Result<List<DropdownOptionDto>>.Success(territories, "Bölgeler başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting territories");
            return Result<List<DropdownOptionDto>>.Error("Bölgeler getirilirken bir hata oluştu.");
        }
    }

    public async Task<Result<List<DropdownOptionDto>>> GetProductCategoriesAsync()
    {
        try
        {
            var query = """
                SELECT 
                    CAST(ProductCategoryID AS VARCHAR) AS Value,
                    Name AS Label
                FROM Production.ProductCategory
                ORDER BY Name
                """;

            var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
            
            var categories = result.Data!.Select(item =>
            {
                var dict = (IDictionary<string, object>)item!;
                return new DropdownOptionDto
                {
                    Value = dict["Value"]?.ToString() ?? string.Empty,
                    Label = dict["Label"]?.ToString() ?? string.Empty
                };
            }).ToList();

            return Result<List<DropdownOptionDto>>.Success(categories, "Ürün kategorileri başarıyla getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product categories");
            return Result<List<DropdownOptionDto>>.Error("Ürün kategorileri getirilirken bir hata oluştu.");
        }
    }
}

