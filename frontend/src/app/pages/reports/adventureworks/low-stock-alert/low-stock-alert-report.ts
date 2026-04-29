import { Component, OnInit, OnDestroy, inject, signal, computed, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';

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
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';

// Services
import { AdventureWorksReportService } from '../../../../core/services/adventureworks/adventureworks-report.service';
import { AuthService } from '../../../../core/services/auth.service';

// Models
import {
  AdventureWorksReportFilter,
  LowStockAlert,
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
  selector: 'app-low-stock-alert-report',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Card,
    MultiSelect,
    Button,
    TableModule,
    Skeleton,
    Tag,
    NgApexchartsModule
  ],
  templateUrl: './low-stock-alert-report.html',
  styleUrl: './low_stock_alert_report.scss',
  host: {
    'style': 'display: block; width: 100%; height: 100vh; overflow: auto;'
  }
})
export class LowStockAlertReport implements OnInit, OnDestroy, AfterViewInit {
  private reportService = inject(AdventureWorksReportService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  isEmbedded = signal(false);
  isLoadingFilters = signal(true);
  isLoadingData = signal(false);

  productCategories = signal<DropdownOption[]>([]);
  selectedProductCategories = signal<number[]>([]);

  alerts = signal<LowStockAlert[]>([]);

  barChartOptions: Partial<ChartOptions> = {};

  totalAlerts = computed(() => this.alerts().length);
  totalShortage = computed(() => 
    this.alerts().reduce((sum, a) => sum + a.shortageAmount, 0)
  );
  averageShortage = computed(() => {
    const alerts = this.alerts();
    if (alerts.length === 0) return 0;
    return alerts.reduce((sum, a) => sum + a.shortageAmount, 0) / alerts.length;
  });
  criticalAlerts = computed(() => 
    this.alerts().filter(a => a.shortageAmount > 50).length
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
    this.reportService.getProductCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.productCategories.set(data);
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
      productCategoryIds: this.selectedProductCategories().length > 0 ? this.selectedProductCategories() : undefined
    };

    this.reportService.getLowStockAlerts(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.alerts.set(data);
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
    const alerts = this.alerts();
    const topAlerts = alerts.slice(0, 10);
    
    this.barChartOptions = {
      series: [{
        name: 'Eksik Miktar',
        data: topAlerts.map(a => a.shortageAmount)
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
          },
          colors: {
            ranges: [{
              from: 0,
              to: 50,
              color: '#f59e0b'
            }, {
              from: 50,
              to: 1000,
              color: '#ef4444'
            }]
          }
        }
      },
      dataLabels: {
        enabled: true,
        formatter: (val: number) => val.toString()
      },
      xaxis: {
        categories: topAlerts.map(a => a.productName),
        labels: {
          rotate: -45,
          style: {
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        title: {
          text: 'Eksik Miktar'
        }
      },
      tooltip: {
        y: {
          formatter: (val: number) => val + ' adet eksik'
        }
      },
      title: {
        text: 'En Kritik Düşük Stok Uyarıları (Top 10)',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#ef4444']
    };
  }

  applyFilters(): void {
    this.loadData();
  }

  clearFilters(): void {
    this.selectedProductCategories.set([]);
    this.loadData();
  }

  exportToExcel(): void {
    const data = this.alerts().map(a => ({
      'Ürün ID': a.productId,
      'Ürün Adı': a.productName,
      'Ürün Kodu': a.productNumber,
      'Kategori': a.categoryName || '',
      'Mevcut Stok': a.currentStock,
      'Güvenlik Stok Seviyesi': a.safetyStockLevel,
      'Yeniden Sipariş Noktası': a.reorderPoint,
      'Eksik Miktar': a.shortageAmount,
      'Lokasyon': a.locationName || ''
    }));

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Düşük Stok Uyarıları');
    XLSX.writeFile(wb, `dusuk-stok-uyarilari-${new Date().toISOString().split('T')[0]}.xlsx`);
  }

  exportToPdf(): void {
    const doc = new jsPDF();
    doc.setFontSize(18);
    doc.text('Düşük Stok Seviyesi Uyarı Raporu', 14, 20);
    doc.setFontSize(10);
    doc.text(`Tarih: ${new Date().toLocaleDateString('tr-TR')}`, 14, 30);

    const tableData = this.alerts().map(a => [
      a.productName,
      a.categoryName || '-',
      a.currentStock.toString(),
      a.safetyStockLevel.toString(),
      a.shortageAmount.toString(),
      a.locationName || '-'
    ]);

    autoTable(doc, {
      head: [['Ürün Adı', 'Kategori', 'Mevcut Stok', 'Güvenlik Seviyesi', 'Eksik Miktar', 'Lokasyon']],
      body: tableData,
      startY: 40,
      styles: { fontSize: 9 },
      headStyles: { fillColor: [239, 68, 68] }
    });

    doc.save(`dusuk-stok-uyarilari-${new Date().toISOString().split('T')[0]}.pdf`);
  }

  formatNumber(value: number): string {
    return value.toLocaleString('tr-TR');
  }

  getSeverity(shortageAmount: number): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' | null {
    if (shortageAmount > 50) return 'danger';
    if (shortageAmount > 20) return 'warn';
    return 'info';
  }
}

