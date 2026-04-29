import { Component, OnInit, OnDestroy, inject, signal, computed, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

// ApexCharts
import { NgApexchartsModule } from 'ng-apexcharts';
import { ApexChart, ApexNonAxisChartSeries, ApexDataLabels, ApexLegend, ApexPlotOptions, ApexXAxis, ApexYAxis, ApexFill, ApexTooltip, ApexGrid, ApexTitleSubtitle, ApexAxisChartSeries } from 'ng-apexcharts';

// Excel & PDF export
import * as XLSX from 'xlsx';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

// PrimeNG Imports
import { Card } from 'primeng/card';
import { MultiSelect } from 'primeng/multiselect';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DatePicker } from 'primeng/datepicker';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';

// Services
import { AdventureWorksReportService } from '../../../../core/services/adventureworks/adventureworks-report.service';
import { AuthService } from '../../../../core/services/auth.service';

// Models
import {
  AdventureWorksReportFilter,
  ProductCategoryProfitability,
  DropdownOption
} from '../../../../core/models/adventureworks/adventureworks-report.model';

export type ChartOptions = {
  series: ApexNonAxisChartSeries | ApexAxisChartSeries;
  chart: ApexChart;
  labels: any;
  dataLabels: ApexDataLabels;
  legend: ApexLegend;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis;
  fill: ApexFill;
  tooltip: ApexTooltip;
  grid: ApexGrid;
  title: ApexTitleSubtitle;
  colors: string[];
  plotOptions: ApexPlotOptions;
};

@Component({
  selector: 'app-product-category-profitability-report',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Card,
    MultiSelect,
    Button,
    TableModule,
    DatePicker,
    Skeleton,
    Tag,
    NgApexchartsModule
  ],
  templateUrl: './product-category-profitability-report.html',
  styleUrl: './product_category_profitability_report.scss',
  host: {
    'style': 'display: block; width: 100%; height: 100vh; overflow: auto;'
  }
})
export class ProductCategoryProfitabilityReport implements OnInit, OnDestroy, AfterViewInit {
  private reportService = inject(AdventureWorksReportService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  isEmbedded = signal(false);
  isLoadingFilters = signal(true);
  isLoadingData = signal(false);

  territories = signal<DropdownOption[]>([]);
  selectedTerritories = signal<number[]>([]);
  rangeDates: Date[] | null = [new Date(2011, 0, 1), new Date(2014, 11, 31)]; // AdventureWorks default tarih aralığı

  categories = signal<ProductCategoryProfitability[]>([]);

  barChartOptions: Partial<ChartOptions> = {};
  pieChartOptions: Partial<ChartOptions> = {};

  totalRevenue = computed(() => 
    this.categories().reduce((sum, c) => sum + c.totalRevenue, 0)
  );
  totalProfit = computed(() => 
    this.categories().reduce((sum, c) => sum + c.totalProfit, 0)
  );
  averageProfitMargin = computed(() => {
    const categories = this.categories();
    if (categories.length === 0) return 0;
    return categories.reduce((sum, c) => sum + c.profitMarginPercent, 0) / categories.length;
  });
  totalProducts = computed(() => 
    this.categories().reduce((sum, c) => sum + c.productCount, 0)
  );

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['embedded'] === 'true') {
        this.isEmbedded.set(true);
      }
    });
    this.loadFilters();
  }

  ngAfterViewInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadFilters(): void {
    this.isLoadingFilters.set(true);
    this.reportService.getTerritories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.territories.set(data);
          this.isLoadingFilters.set(false);
        },
        error: (error) => {
          console.error('Error loading filters:', error);
          this.isLoadingFilters.set(false);
        }
      });
  }

  loadData(): void {
    this.isLoadingData.set(true);
    const filter: AdventureWorksReportFilter = {
      startDate: this.rangeDates?.[0] ? this.rangeDates[0].toISOString().split('T')[0] : null,
      endDate: this.rangeDates?.[1] ? this.rangeDates[1].toISOString().split('T')[0] : null,
      territoryIds: this.selectedTerritories().length > 0 ? this.selectedTerritories() : undefined
    };

    this.reportService.getProductCategoryProfitability(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.categories.set(data);
          this.updateCharts();
          this.isLoadingData.set(false);
        },
        error: (error) => {
          console.error('Error loading data:', error);
          this.isLoadingData.set(false);
        }
      });
  }

  updateCharts(): void {
    const categories = this.categories();
    
    // Bar Chart - Profit by Category
    this.barChartOptions = {
      series: [{
        name: 'Toplam Kar',
        data: categories.map(c => c.totalProfit)
      }],
      chart: {
        type: 'bar',
        height: 400,
        toolbar: { show: true },
        fontFamily: 'Inter, sans-serif'
      },
      plotOptions: {
        bar: {
          borderRadius: 4,
          horizontal: false,
          dataLabels: {
            position: 'top'
          }
        }
      },
      dataLabels: {
        enabled: true,
        formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
      },
      xaxis: {
        categories: categories.map(c => c.categoryName),
        labels: {
          rotate: -45,
          style: {
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        title: {
          text: 'Toplam Kar (USD)'
        },
        labels: {
          formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
        }
      },
      fill: {
        colors: ['#10b981']
      },
      tooltip: {
        y: {
          formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' })
        }
      },
      title: {
        text: 'Kategori Bazında Toplam Kar',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#10b981']
    };

    // Pie Chart - Profit Margin by Category
    this.pieChartOptions = {
      series: categories.map(c => c.profitMarginPercent),
      chart: {
        type: 'pie',
        height: 400,
        toolbar: { show: true },
        fontFamily: 'Inter, sans-serif'
      },
      labels: categories.map(c => c.categoryName),
      dataLabels: {
        enabled: true,
        formatter: (val: number) => val.toFixed(1) + '%'
      },
      legend: {
        position: 'bottom'
      },
      tooltip: {
        y: {
          formatter: (val: number) => val.toFixed(2) + '%'
        }
      },
      title: {
        text: 'Kategori Bazında Kar Marjı (%)',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#10b981', '#6366f1', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4', '#84cc16']
    };
  }

  applyFilters(): void {
    this.loadData();
  }

  clearFilters(): void {
    this.selectedTerritories.set([]);
    this.rangeDates = [new Date(2011, 0, 1), new Date(2014, 11, 31)];
    this.loadData();
  }

  exportToExcel(): void {
    const data = this.categories().map(c => ({
      'Kategori': c.categoryName,
      'Ürün Sayısı': c.productCount,
      'Toplam Gelir': c.totalRevenue,
      'Toplam Maliyet': c.totalCost,
      'Toplam Kar': c.totalProfit,
      'Kar Marjı (%)': c.profitMarginPercent,
      'Ortalama Birim Kar': c.averageUnitProfit,
      'Toplam Satış Miktarı': c.totalSalesQuantity
    }));

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Kategori Karlılık');
    XLSX.writeFile(wb, `kategori-karlilik-${new Date().toISOString().split('T')[0]}.xlsx`);
  }

  exportToPdf(): void {
    const doc = new jsPDF();
    doc.setFontSize(18);
    doc.text('Ürün Kategorisi Karlılık Raporu', 14, 20);
    doc.setFontSize(10);
    doc.text(`Tarih: ${new Date().toLocaleDateString('tr-TR')}`, 14, 30);

    const tableData = this.categories().map(c => [
      c.categoryName,
      c.productCount.toString(),
      c.totalRevenue.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' }),
      c.totalProfit.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' }),
      c.profitMarginPercent.toFixed(2) + '%'
    ]);

    autoTable(doc, {
      head: [['Kategori', 'Ürün Sayısı', 'Toplam Gelir', 'Toplam Kar', 'Kar Marjı (%)']],
      body: tableData,
      startY: 40,
      styles: { fontSize: 9 },
      headStyles: { fillColor: [16, 185, 129] }
    });

    doc.save(`kategori-karlilik-${new Date().toISOString().split('T')[0]}.pdf`);
  }

  formatCurrency(value: number): string {
    return value.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' });
  }

  formatNumber(value: number): string {
    return value.toLocaleString('tr-TR');
  }

  formatPercent(value: number): string {
    return value.toFixed(2) + '%';
  }
}

