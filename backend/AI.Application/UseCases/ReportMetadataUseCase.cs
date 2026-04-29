using AI.Application.Common.Helpers;
using AI.Application.DTOs;
using AI.Application.DTOs.ChatMetadata;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.Extensions.Logging;

namespace AI.Application.UseCases;

public class ReportMetadataUseCase : IReportMetadataUseCase
{
    private readonly ILogger<ReportMetadataUseCase> _logger;
    private readonly IDatabaseService? _databaseService;

    // Veritabanı tanımları
    private static readonly List<DatabaseInfo> _databases = new()
    {
        new DatabaseInfo
        {
            Id = "adventureworks",
            DisplayName = "AdventureWorks",
            Description = "Satış, Ürün, Müşteri verileri"
        }
    };

    // Rapor türleri (veritabanına göre)
    private static readonly List<ReportTypeInfo> _reportTypes = new()
    {
        new ReportTypeInfo
        {
            Id = "dynamic",
            DatabaseId = "adventureworks",
            DisplayName = "Dinamik Raporlar",
            Description = "AI destekli özel rapor oluşturma",
            OnClickValue = "Dinamik Raporlar"
        },
        new ReportTypeInfo
        {
            Id = "ready",
            DatabaseId = "adventureworks",
            DisplayName = "Hazır Raporlar",
            Description = "Önceden hazırlanmış analiz raporları",
            OnClickValue = "Hazır Raporlar"
        }
    };

    // Dinamik rapor kategorileri
    private static readonly List<DynamicReportCategory> _dynamicReportCategories = new()
    {
        new DynamicReportCategory
        {
            Id = "satis",
            DatabaseId = "adventureworks",
            DisplayName = "Satış Raporları",
            Description = "Satış performansı, gelir analizi",
            OnClickValue = "Satış Raporları",
            ReportName = "adventureworks"
        },
        new DynamicReportCategory
        {
            Id = "musteri",
            DatabaseId = "adventureworks",
            DisplayName = "Müşteri Raporları",
            Description = "Müşteri segmentasyonu, demografik analiz",
            OnClickValue = "Müşteri Raporları",
            ReportName = "adventureworks"
        },
        new DynamicReportCategory
        {
            Id = "urun",
            DatabaseId = "adventureworks",
            DisplayName = "Ürün Raporları",
            Description = "Ürün performansı, kategori analizi",
            OnClickValue = "Ürün Raporları",
            ReportName = "adventureworks"
        },
        new DynamicReportCategory
        {
            Id = "gelir",
            DatabaseId = "adventureworks",
            DisplayName = "Gelir Raporları",
            Description = "Finansal performans, kar analizi",
            OnClickValue = "Gelir Raporları",
            ReportName = "adventureworks"
        },
    };

    // Hazır raporlar (AdventureWorks)
    private static readonly List<ReadyReportInfo> _readyReports = new()
    {
        new ReadyReportInfo
        {
            Id = "top_products",
            DisplayName = "En Çok Satan Ürünler",
            Description = "Satış miktarına göre en çok satan 10 ürün raporu",
            Url = "/reports/adventureworks/top-products"
        },
        new ReadyReportInfo
        {
            Id = "top_customers",
            DisplayName = "En Değerli Müşteriler",
            Description = "Toplam alışveriş tutarına göre en değerli 10 müşteri raporu",
            Url = "/reports/adventureworks/top-customers"
        },
        new ReadyReportInfo
        {
            Id = "monthly_sales_trend",
            DisplayName = "Aylık Satış Trend Raporu",
            Description = "Son 12 ayın aylık satış tutarları ve büyüme oranları",
            Url = "/reports/adventureworks/monthly-sales-trend"
        },
        new ReadyReportInfo
        {
            Id = "product_category_profitability",
            DisplayName = "Ürün Kategorisi Karlılık Raporu",
            Description = "Kategori bazında kar marjı ve karlılık metrikleri",
            Url = "/reports/adventureworks/product-category-profitability"
        },
        new ReadyReportInfo
        {
            Id = "low_stock_alert",
            DisplayName = "Düşük Stok Seviyesi Uyarı Raporu",
            Description = "Güvenlik stok seviyesinin altında kalan ürünler",
            Url = "/reports/adventureworks/low-stock-alert"
        },
        new ReadyReportInfo
        {
            Id = "employee_department_distribution",
            DisplayName = "Departman Bazında Çalışan Dağılım Raporu",
            Description = "Her departmandaki aktif çalışan sayısı ve organizasyon yapısı",
            Url = "/reports/adventureworks/employee-department-distribution"
        }
    };

    public ReportMetadataUseCase(
        ILogger<ReportMetadataUseCase> logger,
        IDatabaseService? databaseService = null)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public Task<List<DatabaseInfo>> GetAllDatabasesAsync() => Task.FromResult(_databases.ToList());

    public Task<List<ReportTypeInfo>> GetReportTypesByDatabaseAsync(string databaseId)
    {
        var filtered = _reportTypes
            .Where(r => r.DatabaseId.Equals(databaseId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(filtered);
    }

    public Task<List<DynamicReportCategory>> GetDynamicReportCategoriesAsync() =>
        Task.FromResult(_dynamicReportCategories.ToList());

    public Task<List<ReadyReportInfo>> GetReadyReportsAsync() =>
        Task.FromResult(_readyReports.ToList());

    public string GenerateDatabaseListForPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| onClick Değeri | Veritabanı | Açıklama |");
        sb.AppendLine("|----------------|------------|----------|");
        foreach (var db in _databases)
            sb.AppendLine($"| `{db.Id}` | {db.DisplayName} | {db.Description} |");
        return sb.ToString();
    }

    public string GenerateReportTypeListForPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| Veritabanı | onClick Değeri | Rapor Türü | Açıklama |");
        sb.AppendLine("|------------|----------------|------------|----------|");
        foreach (var rt in _reportTypes)
            sb.AppendLine($"| {rt.DatabaseId} | `{rt.OnClickValue}` | {rt.DisplayName} | {rt.Description} |");
        return sb.ToString();
    }

    public string GenerateDynamicReportCategoryListForPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| Veritabanı | onClick Değeri | Kategori | reportName | Açıklama |");
        sb.AppendLine("|------------|----------------|----------|------------|----------|");
        foreach (var cat in _dynamicReportCategories)
            sb.AppendLine($"| {cat.DatabaseId} | `{cat.OnClickValue}` | {cat.DisplayName} | `{cat.ReportName}` | {cat.Description} |");
        return sb.ToString();
    }

    public string GenerateDynamicDatabaseSelectionTemplate()
    {
        var cardTemplate = GetOptionCardTemplate();
        var optionsBuilder = new System.Text.StringBuilder();
        foreach (var db in _databases)
        {
            optionsBuilder.AppendLine(cardTemplate
                .Replace("{{ONCLICK_VALUE}}", db.Id)
                .Replace("{{DISPLAY_NAME}}", db.DisplayName)
                .Replace("{{DESCRIPTION}}", db.Description));
        }
        var mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "ask_report.html");
        if (string.IsNullOrWhiteSpace(mainTemplate) || !mainTemplate.Contains("{{DATABASE_OPTIONS}}"))
            mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "fallback_database_selection.html");
        return mainTemplate.Replace("{{DATABASE_OPTIONS}}", optionsBuilder.ToString());
    }

    public string GenerateDynamicReportTypeTemplate(string databaseId)
    {
        var cardTemplate = GetOptionCardTemplate();
        var reportTypes = _reportTypes.Where(r => r.DatabaseId.Equals(databaseId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (reportTypes.Count == 0)
            return $"<p><strong>{databaseId} veritabanı için rapor türü bulunamadı.</strong></p>";

        var dbName = _databases.FirstOrDefault(d => d.Id.Equals(databaseId, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? databaseId;
        var optionsBuilder = new System.Text.StringBuilder();
        foreach (var rt in reportTypes)
        {
            optionsBuilder.AppendLine(cardTemplate
                .Replace("{{ONCLICK_VALUE}}", rt.OnClickValue)
                .Replace("{{DISPLAY_NAME}}", rt.DisplayName)
                .Replace("{{DESCRIPTION}}", rt.Description));
        }
        var mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "ask_report_type.html");
        if (string.IsNullOrWhiteSpace(mainTemplate) || !mainTemplate.Contains("{{REPORT_TYPE_OPTIONS}}"))
            mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "fallback_report_type_selection.html");
        return mainTemplate.Replace("{{DATABASE_NAME}}", dbName).Replace("{{REPORT_TYPE_OPTIONS}}", optionsBuilder.ToString());
    }

    public string GenerateDynamicReportCategoryTemplate(string databaseId)
    {
        var cardTemplate = GetOptionCardTemplate();
        var categories = _dynamicReportCategories.Where(c => c.DatabaseId.Equals(databaseId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (categories.Count == 0)
            return $"<p><strong>{databaseId} veritabanı için dinamik rapor kategorisi bulunamadı.</strong></p>";

        var dbName = _databases.FirstOrDefault(d => d.Id.Equals(databaseId, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? databaseId;
        var optionsBuilder = new System.Text.StringBuilder();
        foreach (var cat in categories)
        {
            optionsBuilder.AppendLine(cardTemplate
                .Replace("{{ONCLICK_VALUE}}", cat.OnClickValue)
                .Replace("{{DISPLAY_NAME}}", cat.DisplayName)
                .Replace("{{DESCRIPTION}}", cat.Description));
        }
        var mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "ask_dynamic_report_type.html");
        if (string.IsNullOrWhiteSpace(mainTemplate) || !mainTemplate.Contains("{{REPORT_CATEGORY_OPTIONS}}"))
            mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "fallback_dynamic_report_category.html");
        return mainTemplate.Replace("{{DATABASE_NAME}}", dbName).Replace("{{REPORT_CATEGORY_OPTIONS}}", optionsBuilder.ToString());
    }

    public string GenerateReadyReportTemplate()
    {
        var cardTemplate = GetReadyReportCardTemplate();
        var optionsBuilder = new System.Text.StringBuilder();
        foreach (var report in _readyReports)
        {
            optionsBuilder.AppendLine(cardTemplate
                .Replace("{{URL}}", report.Url)
                .Replace("{{DISPLAY_NAME}}", report.DisplayName)
                .Replace("{{DESCRIPTION}}", report.Description));
        }
        var mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "ask_ready_report.html");
        if (string.IsNullOrWhiteSpace(mainTemplate) || !mainTemplate.Contains("{{READY_REPORT_OPTIONS}}"))
            mainTemplate = Helper.ReadFileContent("Common/Resources/Templates", "fallback_ready_report.html");
        return mainTemplate.Replace("{{READY_REPORT_OPTIONS}}", optionsBuilder.ToString());
    }

    private static string GetOptionCardTemplate() =>
        Helper.ReadFileContent("Common/Resources/Templates", "option_card_template.html");

    private static string GetReadyReportCardTemplate() =>
        Helper.ReadFileContent("Common/Resources/Templates", "ready_report_card_template.html");

    #region AdventureWorks Lookup Data Methods

    private const string TerritoriesQuery = """
        SELECT TerritoryID AS "TerritoryId", Name AS "TerritoryName", [Group] AS "TerritoryGroup"
        FROM Sales.SalesTerritory ORDER BY Name
        """;
    private const string StoresQuery = """
        SELECT CAST(s.BusinessEntityID AS VARCHAR) AS "StoreNumber", s.Name AS "StoreName",
               CAST(ISNULL(sp.TerritoryID, 0) AS VARCHAR) AS "RegionNumber",
               CAST(CASE WHEN s.BusinessEntityID IS NOT NULL THEN 1 ELSE 0 END AS VARCHAR) AS "IsActive"
        FROM Sales.Store s LEFT JOIN Sales.SalesPerson sp ON s.SalesPersonID = sp.BusinessEntityID
        WHERE s.BusinessEntityID IN (SELECT DISTINCT StoreID FROM Sales.Customer WHERE StoreID IS NOT NULL)
        ORDER BY s.Name
        """;
    private const string CategoriesQuery = """
        SELECT CAST(ProductCategoryID AS VARCHAR) AS "CategoryId", Name AS "CategoryName"
        FROM Production.ProductCategory ORDER BY Name
        """;
    private const string ProductsQuery = """
        SELECT Name AS "ProductName", ProductNumber AS "ProductNumber"
        FROM Production.Product WHERE Name IS NOT NULL AND ProductNumber IS NOT NULL ORDER BY Name
        """;
    private const string PromotionsQuery = """
        SELECT CAST(SpecialOfferID AS VARCHAR) AS "PromotionNumber", Description AS "PromotionName"
        FROM Sales.SpecialOffer WHERE SpecialOfferID IN (SELECT DISTINCT SpecialOfferID FROM Sales.SalesOrderDetail WHERE SpecialOfferID > 1)
        ORDER BY Description
        """;
    private const string SalesPersonsQuery = """
        SELECT CAST(sp.BusinessEntityID AS VARCHAR) AS "SalesPersonId", p.FirstName + ' ' + p.LastName AS "SalesPersonName"
        FROM Sales.SalesPerson sp INNER JOIN Person.Person p ON sp.BusinessEntityID = p.BusinessEntityID
        WHERE sp.BusinessEntityID IN (SELECT DISTINCT SalesPersonID FROM Sales.SalesOrderHeader WHERE SalesPersonID IS NOT NULL)
        ORDER BY p.LastName, p.FirstName
        """;
    private const string CustomerTypesQuery = """
        SELECT 'Individual' AS "CustomerTypeId", 'Individual' AS "CustomerTypeName"
        UNION ALL SELECT 'Store' AS "CustomerTypeId", 'Store' AS "CustomerTypeName"
        """;
    private const string OrderStatusesQuery = """
        SELECT CAST(Status AS VARCHAR) AS "OrderStatusId",
               CASE Status WHEN 1 THEN 'In Process' WHEN 2 THEN 'Approved' WHEN 3 THEN 'Backordered'
               WHEN 4 THEN 'Rejected' WHEN 5 THEN 'Shipped' WHEN 6 THEN 'Cancelled' ELSE 'Unknown' END AS "OrderStatusName"
        FROM (SELECT DISTINCT Status FROM Sales.SalesOrderHeader) AS StatusList ORDER BY Status
        """;
    private const string ShipMethodsQuery = """
        SELECT CAST(ShipMethodID AS VARCHAR) AS "ShipMethodId", Name AS "ShipMethodName"
        FROM Purchasing.ShipMethod WHERE ShipMethodID IN (SELECT DISTINCT ShipMethodID FROM Sales.SalesOrderHeader) ORDER BY Name
        """;
    private const string CurrenciesQuery = """
        SELECT CurrencyCode AS "CurrencyCode", Name AS "CurrencyName"
        FROM Sales.Currency WHERE CurrencyCode IN (SELECT DISTINCT CurrencyCode FROM Sales.CountryRegionCurrency) ORDER BY CurrencyCode
        """;
    private const string SalesReasonsQuery = """
        SELECT CAST(SalesReasonID AS VARCHAR) AS "SalesReasonId", Name AS "SalesReasonName", ReasonType AS "ReasonType"
        FROM Sales.SalesReason WHERE SalesReasonID IN (SELECT DISTINCT SalesReasonID FROM Sales.SalesOrderHeaderSalesReason) ORDER BY Name
        """;

    private async Task<List<T>> ExecuteLookupAsync<T>(string query, Func<IDictionary<string, object>, T> mapper)
    {
        if (_databaseService == null)
            throw new InvalidOperationException("IDatabaseService is not available for lookup queries.");
        var result = await _databaseService.GetDataTableWithExpandoObjectAsync(query, "system");
        return result.Data!.Select(item => mapper((IDictionary<string, object>)item!)).ToList();
    }

    public Task<List<TerritoryDto>> GetTerritoriesAsync() =>
        ExecuteLookupAsync(TerritoriesQuery, d => new TerritoryDto
        {
            TerritoryId = d.TryGetValue("TerritoryId", out var v) ? v?.ToString() : null,
            TerritoryName = d.TryGetValue("TerritoryName", out var n) ? n?.ToString() : null,
            TerritoryGroup = d.TryGetValue("TerritoryGroup", out var g) ? g?.ToString() : null
        });

    public Task<List<StoreDto>> GetStoresAsync() =>
        ExecuteLookupAsync(StoresQuery, d => new StoreDto
        {
            StoreNumber = d.TryGetValue("StoreNumber", out var v) ? v?.ToString() : null,
            StoreName = d.TryGetValue("StoreName", out var n) ? n?.ToString() : null,
            IsActive = d.TryGetValue("IsActive", out var a) ? a?.ToString() : null,
            RegionNumber = d.TryGetValue("RegionNumber", out var r) ? r?.ToString() : null
        });

    public Task<List<DepartmentCategoryDto>> GetCategoriesAsync() =>
        ExecuteLookupAsync(CategoriesQuery, d => new DepartmentCategoryDto
        {
            CategoryId = d.TryGetValue("CategoryId", out var v) ? v?.ToString() : null,
            CategoryName = d.TryGetValue("CategoryName", out var n) ? n?.ToString() : null
        });

    public Task<List<ProductDto>> GetProductsAsync() =>
        ExecuteLookupAsync(ProductsQuery, d => new ProductDto
        {
            ProductName = d.TryGetValue("ProductName", out var v) ? v?.ToString() : null,
            ProductNumber = d.TryGetValue("ProductNumber", out var n) ? n?.ToString() : null
        });

    public Task<List<PromotionDto>> GetPromotionsAsync() =>
        ExecuteLookupAsync(PromotionsQuery, d => new PromotionDto
        {
            PromotionNumber = d.TryGetValue("PromotionNumber", out var v) ? v?.ToString() : null,
            PromotionName = d.TryGetValue("PromotionName", out var n) ? n?.ToString() : null
        });

    public Task<List<SalesPersonDto>> GetSalesPersonsAsync() =>
        ExecuteLookupAsync(SalesPersonsQuery, d => new SalesPersonDto
        {
            SalesPersonId = d.TryGetValue("SalesPersonId", out var v) ? v?.ToString() : null,
            SalesPersonName = d.TryGetValue("SalesPersonName", out var n) ? n?.ToString() : null
        });

    public Task<List<CustomerTypeDto>> GetCustomerTypesAsync() =>
        ExecuteLookupAsync(CustomerTypesQuery, d => new CustomerTypeDto
        {
            CustomerTypeId = d.TryGetValue("CustomerTypeId", out var v) ? v?.ToString() : null,
            CustomerTypeName = d.TryGetValue("CustomerTypeName", out var n) ? n?.ToString() : null
        });

    public Task<List<OrderStatusDto>> GetOrderStatusesAsync() =>
        ExecuteLookupAsync(OrderStatusesQuery, d => new OrderStatusDto
        {
            OrderStatusId = d.TryGetValue("OrderStatusId", out var v) ? v?.ToString() : null,
            OrderStatusName = d.TryGetValue("OrderStatusName", out var n) ? n?.ToString() : null
        });

    public Task<List<ShipMethodDto>> GetShipMethodsAsync() =>
        ExecuteLookupAsync(ShipMethodsQuery, d => new ShipMethodDto
        {
            ShipMethodId = d.TryGetValue("ShipMethodId", out var v) ? v?.ToString() : null,
            ShipMethodName = d.TryGetValue("ShipMethodName", out var n) ? n?.ToString() : null
        });

    public Task<List<CurrencyDto>> GetCurrenciesAsync() =>
        ExecuteLookupAsync(CurrenciesQuery, d => new CurrencyDto
        {
            CurrencyCode = d.TryGetValue("CurrencyCode", out var v) ? v?.ToString() : null,
            CurrencyName = d.TryGetValue("CurrencyName", out var n) ? n?.ToString() : null
        });

    public Task<List<SalesReasonDto>> GetSalesReasonsAsync() =>
        ExecuteLookupAsync(SalesReasonsQuery, d => new SalesReasonDto
        {
            SalesReasonId = d.TryGetValue("SalesReasonId", out var v) ? v?.ToString() : null,
            SalesReasonName = d.TryGetValue("SalesReasonName", out var n) ? n?.ToString() : null,
            ReasonType = d.TryGetValue("ReasonType", out var r) ? r?.ToString() : null
        });

    #endregion
}