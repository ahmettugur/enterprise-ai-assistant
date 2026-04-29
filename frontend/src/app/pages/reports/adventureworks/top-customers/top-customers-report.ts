import { Component, OnInit, OnDestroy, inject, signal, computed, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';

// ApexCharts
import { NgApexchartsModule } from 'ng-apexcharts';
import { ApexChart, ApexNonAxisChartSeries, ApexResponsive, ApexDataLabels, ApexLegend, ApexPlotOptions, ApexXAxis, ApexYAxis, ApexFill, ApexTooltip, ApexGrid, ApexTitleSubtitle, ApexAxisChartSeries } from 'ng-apexcharts';

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
  TopCustomer,
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
  selector: 'app-top-customers-report',
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
  templateUrl: './top-customers-report.html',
  styleUrl: './top-customers-report.scss',
  host: {
    'style': 'display: block; width: 100%; height: 100vh; overflow: auto;'
  }
})
export class TopCustomersReport implements OnInit, OnDestroy, AfterViewInit {
  private reportService = inject(AdventureWorksReportService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  // Embedded mode
  isEmbedded = signal(false);

  // Loading states
  isLoadingFilters = signal(true);
  isLoadingData = signal(false);

  // Filter options
  territories = signal<DropdownOption[]>([]);

  // Selected filters
  selectedTerritories = signal<number[]>([]);
  rangeDates: Date[] | null = [new Date(2011, 0, 1), new Date(2014, 11, 31)]; // AdventureWorks default tarih aralığı
  topCount = signal(10);

  // Data
  customers = signal<TopCustomer[]>([]);

  // Chart options
  barChartOptions: Partial<ChartOptions> = {};
  pieChartOptions: Partial<ChartOptions> = {};

  // KPI Cards
  totalCustomers = computed(() => this.customers().length);
  totalPurchaseAmount = computed(() => 
    this.customers().reduce((sum, c) => sum + c.totalPurchaseAmount, 0)
  );
  averageOrderAmount = computed(() => {
    const customers = this.customers();
    if (customers.length === 0) return 0;
    return customers.reduce((sum, c) => sum + c.averageOrderAmount, 0) / customers.length;
  });
  totalOrderCount = computed(() => 
    this.customers().reduce((sum, c) => sum + c.orderCount, 0)
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

    this.reportService.getTopCustomers(filter, this.topCount())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.customers.set(data);
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
    const customers = this.customers();
    
    // Bar Chart - Top 10 Customers by Purchase Amount
    this.barChartOptions = {
      series: [{
        name: 'Toplam Alışveriş Tutarı',
        data: customers.slice(0, 10).map(c => c.totalPurchaseAmount)
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
        formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' })
      },
      xaxis: {
        categories: customers.slice(0, 10).map(c => c.customerName),
        labels: {
          rotate: -45,
          style: {
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        title: {
          text: 'Toplam Alışveriş Tutarı (USD)'
        },
        labels: {
          formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' })
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
        text: 'En Değerli 10 Müşteri (Toplam Alışveriş Tutarı)',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#10b981']
    };

    // Pie Chart - Top 5 Customers by Order Count
    this.pieChartOptions = {
      series: customers.slice(0, 5).map(c => c.orderCount),
      chart: {
        type: 'pie',
        height: 400,
        toolbar: { show: true },
        fontFamily: 'Inter, sans-serif'
      },
      labels: customers.slice(0, 5).map(c => c.customerName),
      dataLabels: {
        enabled: true,
        formatter: (val: number) => val.toFixed(1) + '%'
      },
      legend: {
        position: 'bottom'
      },
      tooltip: {
        y: {
          formatter: (val: number) => val + ' sipariş'
        }
      },
      title: {
        text: 'En Çok Sipariş Veren 5 Müşteri',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#10b981', '#6366f1', '#f59e0b', '#ef4444', '#8b5cf6']
    };
  }

  applyFilters(): void {
    this.loadData();
  }

  clearFilters(): void {
    this.selectedTerritories.set([]);
    this.rangeDates = [new Date(2011, 0, 1), new Date(2014, 11, 31)];
    this.topCount.set(10);
    this.loadData();
  }

  exportToExcel(): void {
    const data = this.customers().map(c => ({
      'Müşteri ID': c.customerId,
      'Müşteri Adı': c.customerName,
      'E-posta': c.emailAddress || '',
      'Bölge': c.territoryName || '',
      'Toplam Alışveriş Tutarı': c.totalPurchaseAmount,
      'Ortalama Sipariş Tutarı': c.averageOrderAmount,
      'Sipariş Sayısı': c.orderCount,
      'Son Sipariş Tarihi': c.lastOrderDate ? new Date(c.lastOrderDate).toLocaleDateString('tr-TR') : ''
    }));

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'En Değerli Müşteriler');
    XLSX.writeFile(wb, `en-degerli-musteriler-${new Date().toISOString().split('T')[0]}.xlsx`);
  }

  exportToPdf(): void {
    const doc = new jsPDF();
    
    doc.setFontSize(18);
    doc.text('En Değerli Müşteriler Raporu', 14, 20);
    
    doc.setFontSize(10);
    doc.text(`Tarih: ${new Date().toLocaleDateString('tr-TR')}`, 14, 30);

    const tableData = this.customers().map(c => [
      c.customerName,
      c.territoryName || '-',
      c.totalPurchaseAmount.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' }),
      c.orderCount.toString(),
      c.averageOrderAmount.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' })
    ]);

    autoTable(doc, {
      head: [['Müşteri Adı', 'Bölge', 'Toplam Tutar', 'Sipariş Sayısı', 'Ort. Sipariş Tutarı']],
      body: tableData,
      startY: 40,
      styles: { fontSize: 9 },
      headStyles: { fillColor: [16, 185, 129] }
    });

    doc.save(`en-degerli-musteriler-${new Date().toISOString().split('T')[0]}.pdf`);
  }

  formatCurrency(value: number): string {
    return value.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' });
  }

  formatNumber(value: number): string {
    return value.toLocaleString('tr-TR');
  }

  formatDate(value: string | undefined): string {
    if (!value) return '-';
    return new Date(value).toLocaleDateString('tr-TR');
  }
}

