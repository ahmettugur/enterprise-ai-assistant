import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then(m => m.Login)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/chat/chat').then(m => m.Chat)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard)
  },
  // AdventureWorks Ready Reports Routes
  {
    path: 'reports',
    canActivate: [authGuard],
    children: [
      {
        path: 'adventureworks/top-products',
        loadComponent: () => import('./pages/reports/adventureworks/top-products/top-products-report').then(m => m.TopProductsReport)
      },
      {
        path: 'adventureworks/top-customers',
        loadComponent: () => import('./pages/reports/adventureworks/top-customers/top-customers-report').then(m => m.TopCustomersReport)
      },
      {
        path: 'adventureworks/monthly-sales-trend',
        loadComponent: () => import('./pages/reports/adventureworks/monthly-sales-trend/monthly-sales-trend-report').then(m => m.MonthlySalesTrendReport)
      },
      {
        path: 'adventureworks/product-category-profitability',
        loadComponent: () => import('./pages/reports/adventureworks/product-category-profitability/product-category-profitability-report').then(m => m.ProductCategoryProfitabilityReport)
      },
      {
        path: 'adventureworks/low-stock-alert',
        loadComponent: () => import('./pages/reports/adventureworks/low-stock-alert/low-stock-alert-report').then(m => m.LowStockAlertReport)
      },
      {
        path: 'adventureworks/employee-department-distribution',
        loadComponent: () => import('./pages/reports/adventureworks/employee-department-distribution/employee-department-distribution-report').then(m => m.EmployeeDepartmentDistributionReport)
      },
      {
        path: '',
        redirectTo: 'adventureworks/top-products',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
