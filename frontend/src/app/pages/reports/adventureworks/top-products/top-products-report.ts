import { Component, OnInit, OnDestroy, inject, signal, computed, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';

// ApexCharts
import { NgApexchartsModule } from 'ng-apexcharts';
import { ApexChart, ApexNonAxisChartSeries, ApexAxisChartSeries, ApexResponsive, ApexDataLabels, ApexLegend, ApexStroke, ApexPlotOptions, ApexXAxis, ApexYAxis, ApexFill, ApexTooltip, ApexGrid, ApexTitleSubtitle } from 'ng-apexcharts';

// Excel & PDF export
import * as XLSX from 'xlsx';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

// PrimeNG Imports
import { Card } from 'primeng/card';
import { Select } from 'primeng/select';
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
  TopProduct,
  DropdownOption
} from '../../../../core/models/adventureworks/adventureworks-report.model';

export type ChartOptions = {
  series: ApexNonAxisChartSeries | ApexAxisChartSeries;
  chart: ApexChart;
  responsive: ApexResponsive[];
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
  selector: 'app-top-products-report',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Card,
    Select,
    MultiSelect,
    Button,
    TableModule,
    DatePicker,
    Skeleton,
    Tag,
    NgApexchartsModule
  ],
  templateUrl: './top-products-report.html',
  styleUrl: './top-products-report.scss',
  host: {
    'style': 'display: block; width: 100%; height: 100vh; overflow: auto;'
  }
})
export class TopProductsReport implements OnInit, OnDestroy, AfterViewInit {
  private reportService = inject(AdventureWorksReportService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  // Embedded mode (iframe için)
  isEmbedded = signal(false);

  // Loading states
  isLoadingFilters = signal(true);
  isLoadingData = signal(false);

  // Filter options
  territories = signal<DropdownOption[]>([]);
  productCategories = signal<DropdownOption[]>([]);

  // Selected filters
  selectedTerritories = signal<number[]>([]);
  selectedProductCategories = signal<number[]>([]);
  rangeDates: Date[] | null = [new Date(2011, 0, 1), new Date(2014, 11, 31)]; // AdventureWorks default tarih aralığı
  topCount = signal(10);

  // Data
  products = signal<TopProduct[]>([]);

  // Chart options
  barChartOptions: Partial<ChartOptions> = {};
  pieChartOptions: Partial<ChartOptions> = {};

  // KPI Cards
  totalProducts = computed(() => this.products().length);
  totalSalesQuantity = computed(() => 
    this.products().reduce((sum, p) => sum + p.totalSalesQuantity, 0)
  );
  totalSalesAmount = computed(() => 
    this.products().reduce((sum, p) => sum + p.totalSalesAmount, 0)
  );
  averageUnitPrice = computed(() => {
    const products = this.products();
    if (products.length === 0) return 0;
    return products.reduce((sum, p) => sum + p.averageUnitPrice, 0) / products.length;
  });

  ngOnInit(): void {
    // Check if embedded
    this.route.queryParams.subscribe(params => {
      if (params['embedded'] === 'true') {
        this.isEmbedded.set(true);
      }
    });

    this.loadFilters();
  }

  ngAfterViewInit(): void {
    // Initial data load
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Filtreleri yükle
   */
  loadFilters(): void {
    this.isLoadingFilters.set(true);

    forkJoin({
      territories: this.reportService.getTerritories(),
      categories: this.reportService.getProductCategories()
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.territories.set(data.territories);
          this.productCategories.set(data.categories);
          this.isLoadingFilters.set(false);
        },
        error: (error) => {
          console.error('Error loading filters:', error);
          this.isLoadingFilters.set(false);
        }
      });
  }

  /**
   * Rapor verilerini yükle
   */
  loadData(): void {
    this.isLoadingData.set(true);

    const filter: AdventureWorksReportFilter = {
      startDate: this.rangeDates?.[0] ? this.rangeDates[0].toISOString().split('T')[0] : null,
      endDate: this.rangeDates?.[1] ? this.rangeDates[1].toISOString().split('T')[0] : null,
      territoryIds: this.selectedTerritories().length > 0 ? this.selectedTerritories() : undefined,
      productCategoryIds: this.selectedProductCategories().length > 0 ? this.selectedProductCategories() : undefined
    };

    this.reportService.getTopProducts(filter, this.topCount())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.products.set(data);
          this.updateCharts();
          this.isLoadingData.set(false);
        },
        error: (error) => {
          console.error('Error loading data:', error);
          this.isLoadingData.set(false);
        }
      });
  }

  /**
   * Grafikleri güncelle
   */
  updateCharts(): void {
    const products = this.products();
    
    // Bar Chart - Top 10 Products by Sales Quantity
    this.barChartOptions = {
      series: [{
        name: 'Satış Miktarı',
        data: products.slice(0, 10).map(p => p.totalSalesQuantity)
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
        formatter: (val: number) => val.toLocaleString('tr-TR')
      },
      xaxis: {
        categories: products.slice(0, 10).map(p => p.productName),
        labels: {
          rotate: -45,
          style: {
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        title: {
          text: 'Satış Miktarı'
        },
        labels: {
          formatter: (val: number) => val.toLocaleString('tr-TR')
        }
      },
      fill: {
        colors: ['#6366f1']
      },
      tooltip: {
        y: {
          formatter: (val: number) => val.toLocaleString('tr-TR') + ' adet'
        }
      },
      title: {
        text: 'En Çok Satan 10 Ürün (Satış Miktarı)',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#6366f1']
    };

    // Pie Chart - Top 5 Products by Sales Amount
    this.pieChartOptions = {
      series: products.slice(0, 5).map(p => p.totalSalesAmount),
      chart: {
        type: 'pie',
        height: 400,
        toolbar: { show: true },
        fontFamily: 'Inter, sans-serif'
      },
      labels: products.slice(0, 5).map(p => p.productName),
      dataLabels: {
        enabled: true,
        formatter: (val: number) => val.toFixed(1) + '%'
      },
      legend: {
        position: 'bottom'
      },
      tooltip: {
        y: {
          formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' })
        }
      },
      title: {
        text: 'En Çok Gelir Getiren 5 Ürün',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6']
    };
  }

  /**
   * Filtreleri uygula
   */
  applyFilters(): void {
    this.loadData();
  }

  /**
   * Filtreleri temizle
   */
  clearFilters(): void {
    this.selectedTerritories.set([]);
    this.selectedProductCategories.set([]);
    this.rangeDates = [new Date(2011, 0, 1), new Date(2014, 11, 31)];
    this.topCount.set(10);
    this.loadData();
  }

  /**
   * Excel'e aktar
   */
  exportToExcel(): void {
    const data = this.products().map(p => ({
      'Ürün ID': p.productId,
      'Ürün Adı': p.productName,
      'Ürün Kodu': p.productNumber,
      'Kategori': p.categoryName || '',
      'Toplam Satış Miktarı': p.totalSalesQuantity,
      'Toplam Satış Tutarı': p.totalSalesAmount,
      'Ortalama Birim Fiyat': p.averageUnitPrice,
      'Sipariş Sayısı': p.orderCount
    }));

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'En Çok Satan Ürünler');
    XLSX.writeFile(wb, `en-cok-satan-urunler-${new Date().toISOString().split('T')[0]}.xlsx`);
  }

  /**
   * PDF'e aktar
   */
  exportToPdf(): void {
    const doc = new jsPDF();
    
    // Title
    doc.setFontSize(18);
    doc.text('En Çok Satan Ürünler Raporu', 14, 20);
    
    // Date
    doc.setFontSize(10);
    doc.text(`Tarih: ${new Date().toLocaleDateString('tr-TR')}`, 14, 30);

    // Table
    const tableData = this.products().map(p => [
      p.productName,
      p.categoryName || '-',
      p.totalSalesQuantity.toString(),
      p.totalSalesAmount.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' }),
      p.averageUnitPrice.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' })
    ]);

    autoTable(doc, {
      head: [['Ürün Adı', 'Kategori', 'Satış Miktarı', 'Toplam Tutar', 'Ort. Birim Fiyat']],
      body: tableData,
      startY: 40,
      styles: { fontSize: 9 },
      headStyles: { fillColor: [99, 102, 241] }
    });

    doc.save(`en-cok-satan-urunler-${new Date().toISOString().split('T')[0]}.pdf`);
  }

  /**
   * Format currency
   */
  formatCurrency(value: number): string {
    return value.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' });
  }

  /**
   * Format number
   */
  formatNumber(value: number): string {
    return value.toLocaleString('tr-TR');
  }
}

