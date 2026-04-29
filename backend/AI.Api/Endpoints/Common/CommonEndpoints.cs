using AI.Api.Extensions;
using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Results;
using static Microsoft.AspNetCore.Http.Results;

namespace AI.Api.Endpoints.Common;

public static class CommonEndpoints
{
    public static void MapCommonEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/common")
            .WithTags("Common")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

        group.MapGet("/regions", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var territories = await metadataUseCase.GetTerritoriesAsync();
                return Ok(Result<List<TerritoryDto>>.Success(territories, "Bölgeler başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching territories");
                return BadRequest(Result<List<TerritoryDto>>.Error("Bölgeler getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/stores", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var stores = await metadataUseCase.GetStoresAsync();
                return Ok(Result<List<StoreDto>>.Success(stores, "Store'lar başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching stores");
                return BadRequest(Result<List<StoreDto>>.Error("Store'lar getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/categories", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var categories = await metadataUseCase.GetCategoriesAsync();
                return Ok(Result<List<DepartmentCategoryDto>>.Success(categories, "Kategoriler başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching categories");
                return BadRequest(Result<List<DepartmentCategoryDto>>.Error("Kategoriler getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/products", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var products = await metadataUseCase.GetProductsAsync();
                return Ok(Result<List<ProductDto>>.Success(products, "Ürünler başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching products");
                return BadRequest(Result<List<ProductDto>>.Error("Ürünler getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/promotions", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var promotions = await metadataUseCase.GetPromotionsAsync();
                return Ok(Result<List<PromotionDto>>.Success(promotions, "Kampanyalar başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching promotions");
                return BadRequest(Result<List<PromotionDto>>.Error("Kampanyalar getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/salespersons", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var salesPersons = await metadataUseCase.GetSalesPersonsAsync();
                return Ok(Result<List<SalesPersonDto>>.Success(salesPersons, "Satış temsilcileri başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching sales persons");
                return BadRequest(Result<List<SalesPersonDto>>.Error("Satış temsilcileri getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/customertypes", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var customerTypes = await metadataUseCase.GetCustomerTypesAsync();
                return Ok(Result<List<CustomerTypeDto>>.Success(customerTypes, "Müşteri tipleri başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching customer types");
                return BadRequest(Result<List<CustomerTypeDto>>.Error("Müşteri tipleri getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/orderstatuses", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var orderStatuses = await metadataUseCase.GetOrderStatusesAsync();
                return Ok(Result<List<OrderStatusDto>>.Success(orderStatuses, "Sipariş durumları başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching order statuses");
                return BadRequest(Result<List<OrderStatusDto>>.Error("Sipariş durumları getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/shipmethods", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var shipMethods = await metadataUseCase.GetShipMethodsAsync();
                return Ok(Result<List<ShipMethodDto>>.Success(shipMethods, "Teslimat yöntemleri başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching ship methods");
                return BadRequest(Result<List<ShipMethodDto>>.Error("Teslimat yöntemleri getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/currencies", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var currencies = await metadataUseCase.GetCurrenciesAsync();
                return Ok(Result<List<CurrencyDto>>.Success(currencies, "Para birimleri başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching currencies");
                return BadRequest(Result<List<CurrencyDto>>.Error("Para birimleri getirilirken bir hata oluştu."));
            }
        });

        group.MapGet("/salesreasons", async (
            IReportMetadataUseCase metadataUseCase,
            ILogger<Program> logger) =>
        {
            try
            {
                var salesReasons = await metadataUseCase.GetSalesReasonsAsync();
                return Ok(Result<List<SalesReasonDto>>.Success(salesReasons, "Satış nedenleri başarıyla getirildi."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching sales reasons");
                return BadRequest(Result<List<SalesReasonDto>>.Error("Satış nedenleri getirilirken bir hata oluştu."));
            }
        });
    }
}
