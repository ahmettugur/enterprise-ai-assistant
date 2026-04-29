// AdventureWorks Report Models

export interface AdventureWorksReportFilter {
  startDate?: string | null;
  endDate?: string | null;
  territoryIds?: number[];
  productCategoryIds?: number[];
  productIds?: number[];
}

export interface TopProduct {
  productId: number;
  productName: string;
  productNumber: string;
  categoryName?: string;
  totalSalesQuantity: number;
  totalSalesAmount: number;
  averageUnitPrice: number;
  orderCount: number;
}

export interface TopCustomer {
  customerId: number;
  customerName: string;
  emailAddress?: string;
  orderCount: number;
  totalPurchaseAmount: number;
  averageOrderAmount: number;
  lastOrderDate?: string;
  territoryName?: string;
}

export interface MonthlySalesTrend {
  year: number;
  month: number;
  monthName: string;
  monthlySales: number;
  orderCount: number;
  averageOrderAmount: number;
  previousMonthSales?: number;
  growthRate?: number;
}

export interface ProductCategoryProfitability {
  categoryId: number;
  categoryName: string;
  productCount: number;
  totalRevenue: number;
  totalCost: number;
  totalProfit: number;
  profitMarginPercent: number;
  averageUnitProfit: number;
  totalSalesQuantity: number;
}

export interface LowStockAlert {
  productId: number;
  productName: string;
  productNumber: string;
  categoryName?: string;
  currentStock: number;
  safetyStockLevel: number;
  reorderPoint: number;
  shortageAmount: number;
  locationName?: string;
}

export interface EmployeeDepartmentDistribution {
  departmentId: number;
  departmentName: string;
  groupName?: string;
  employeeCount: number;
  oldestHireDate?: string;
  newestHireDate?: string;
  averageYearsOfService?: number;
}

export interface DropdownOption {
  value: string;
  label: string;
}

export interface ApiResponse<T> {
  isSucceed: boolean;
  resultData: T;
  errorMessage?: string;
}

