using AI.Api.Extensions;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.Results;
using AI.Application.DTOs.Reports;
using Microsoft.AspNetCore.Mvc;

namespace AI.Api.Endpoints.Reports;

/// <summary>
/// AdventureWorks hazır rapor endpoint'leri
/// </summary>
public static class AdventureWorksReportEndpoints
{
    public static void MapAdventureWorksReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/ready-reports/adventureworks")
            .WithTags("Ready Reports - AdventureWorks")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        // En çok satan ürünler
        group.MapPost("/top-products", async (
            [FromBody] AdventureWorksReportFilter filter,
            [FromQuery] int topCount,
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetTopProductsAsync(filter, topCount > 0 ? topCount : 10);
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetTopProducts")
        .WithDescription("En çok satan ürünleri getirir")
        .Produces<Result<List<TopProductDto>>>()
        .ProducesProblem(400);

        // En değerli müşteriler
        group.MapPost("/top-customers", async (
            [FromBody] AdventureWorksReportFilter filter,
            [FromQuery] int topCount,
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetTopCustomersAsync(filter, topCount > 0 ? topCount : 10);
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetTopCustomers")
        .WithDescription("En değerli müşterileri getirir")
        .Produces<Result<List<TopCustomerDto>>>()
        .ProducesProblem(400);

        // Aylık satış trend
        group.MapPost("/monthly-sales-trend", async (
            [FromBody] AdventureWorksReportFilter filter,
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetMonthlySalesTrendAsync(filter);
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetMonthlySalesTrend")
        .WithDescription("Aylık satış trend verilerini getirir")
        .Produces<Result<List<MonthlySalesTrendDto>>>()
        .ProducesProblem(400);

        // Ürün kategorisi karlılık
        group.MapPost("/product-category-profitability", async (
            [FromBody] AdventureWorksReportFilter filter,
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetProductCategoryProfitabilityAsync(filter);
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetProductCategoryProfitability")
        .WithDescription("Ürün kategorisi karlılık verilerini getirir")
        .Produces<Result<List<ProductCategoryProfitabilityDto>>>()
        .ProducesProblem(400);

        // Düşük stok uyarıları
        group.MapPost("/low-stock-alerts", async (
            [FromBody] AdventureWorksReportFilter filter,
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetLowStockAlertsAsync(filter);
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetLowStockAlerts")
        .WithDescription("Düşük stok uyarılarını getirir")
        .Produces<Result<List<LowStockAlertDto>>>()
        .ProducesProblem(400);

        // Departman bazında çalışan dağılımı
        group.MapGet("/employee-department-distribution", async (
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetEmployeeDepartmentDistributionAsync();
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetEmployeeDepartmentDistribution")
        .WithDescription("Departman bazında çalışan dağılımını getirir")
        .Produces<Result<List<EmployeeDepartmentDistributionDto>>>()
        .ProducesProblem(400);

        // Bölgeler (dropdown için)
        group.MapGet("/territories", async (
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetTerritoriesAsync();
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetTerritories")
        .WithDescription("Bölgeleri getirir")
        .Produces<Result<List<DropdownOptionDto>>>()
        .ProducesProblem(400);

        // Ürün kategorileri (dropdown için)
        group.MapGet("/product-categories", async (
            [FromServices] IAdventureWorksReadyReportService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetProductCategoriesAsync();
            return result.IsSucceed
                ? Results.Ok(result)
                : Results.BadRequest(result);
        })
        .WithName("GetProductCategories")
        .WithDescription("Ürün kategorilerini getirir")
        .Produces<Result<List<DropdownOptionDto>>>()
        .ProducesProblem(400);
    }
}

