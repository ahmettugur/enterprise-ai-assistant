import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AdventureWorksReportFilter,
  TopProduct,
  TopCustomer,
  MonthlySalesTrend,
  ProductCategoryProfitability,
  LowStockAlert,
  EmployeeDepartmentDistribution,
  DropdownOption,
  ApiResponse
} from '../../models/adventureworks/adventureworks-report.model';

@Injectable({
  providedIn: 'root'
})
export class AdventureWorksReportService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/v1/ready-reports/adventureworks`;

  /**
   * En çok satan ürünleri getirir
   */
  getTopProducts(filter: AdventureWorksReportFilter, topCount: number = 10): Observable<TopProduct[]> {
    return this.http.post<ApiResponse<TopProduct[]>>(
      `${this.baseUrl}/top-products?topCount=${topCount}`,
      filter
    ).pipe(map(res => res.resultData || []));
  }

  /**
   * En değerli müşterileri getirir
   */
  getTopCustomers(filter: AdventureWorksReportFilter, topCount: number = 10): Observable<TopCustomer[]> {
    return this.http.post<ApiResponse<TopCustomer[]>>(
      `${this.baseUrl}/top-customers?topCount=${topCount}`,
      filter
    ).pipe(map(res => res.resultData || []));
  }

  /**
   * Aylık satış trend verilerini getirir
   */
  getMonthlySalesTrend(filter: AdventureWorksReportFilter): Observable<MonthlySalesTrend[]> {
    return this.http.post<ApiResponse<MonthlySalesTrend[]>>(
      `${this.baseUrl}/monthly-sales-trend`,
      filter
    ).pipe(map(res => res.resultData || []));
  }

  /**
   * Ürün kategorisi karlılık verilerini getirir
   */
  getProductCategoryProfitability(filter: AdventureWorksReportFilter): Observable<ProductCategoryProfitability[]> {
    return this.http.post<ApiResponse<ProductCategoryProfitability[]>>(
      `${this.baseUrl}/product-category-profitability`,
      filter
    ).pipe(map(res => res.resultData || []));
  }

  /**
   * Düşük stok uyarılarını getirir
   */
  getLowStockAlerts(filter: AdventureWorksReportFilter): Observable<LowStockAlert[]> {
    return this.http.post<ApiResponse<LowStockAlert[]>>(
      `${this.baseUrl}/low-stock-alerts`,
      filter
    ).pipe(map(res => res.resultData || []));
  }

  /**
   * Departman bazında çalışan dağılımını getirir
   */
  getEmployeeDepartmentDistribution(): Observable<EmployeeDepartmentDistribution[]> {
    return this.http.get<ApiResponse<EmployeeDepartmentDistribution[]>>(
      `${this.baseUrl}/employee-department-distribution`
    ).pipe(map(res => res.resultData || []));
  }

  /**
   * Bölgeleri getirir (dropdown için)
   */
  getTerritories(): Observable<DropdownOption[]> {
    return this.http.get<ApiResponse<DropdownOption[]>>(
      `${this.baseUrl}/territories`
    ).pipe(map(res => res.resultData || []));
  }

  /**
   * Ürün kategorilerini getirir (dropdown için)
   */
  getProductCategories(): Observable<DropdownOption[]> {
    return this.http.get<ApiResponse<DropdownOption[]>>(
      `${this.baseUrl}/product-categories`
    ).pipe(map(res => res.resultData || []));
  }
}

