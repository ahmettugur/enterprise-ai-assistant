import { Component, OnInit, OnDestroy, inject, signal, computed, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

// ApexCharts
import { NgApexchartsModule } from 'ng-apexcharts';
import { ApexChart, ApexNonAxisChartSeries, ApexDataLabels, ApexLegend, ApexPlotOptions, ApexXAxis, ApexYAxis, ApexFill, ApexTooltip, ApexGrid, ApexTitleSubtitle, ApexAxisChartSeries, ApexStroke } from 'ng-apexcharts';

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
  MonthlySalesTrend,
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
  stroke: ApexStroke;
};

@Component({
  selector: 'app-monthly-sales-trend-report',
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
  templateUrl: './monthly-sales-trend-report.html',
  styleUrl: './monthly_sales_trend_report.scss',
  host: {
    'style': 'display: block; width: 100%; height: 100vh; overflow: auto;'
  }
})
export class MonthlySalesTrendReport implements OnInit, OnDestroy, AfterViewInit {
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

  trends = signal<MonthlySalesTrend[]>([]);

  lineChartOptions: Partial<ChartOptions> = {};
  barChartOptions: Partial<ChartOptions> = {};

  totalSales = computed(() => 
    this.trends().reduce((sum, t) => sum + t.monthlySales, 0)
  );
  averageMonthlySales = computed(() => {
    const trends = this.trends();
    if (trends.length === 0) return 0;
    return trends.reduce((sum, t) => sum + t.monthlySales, 0) / trends.length;
  });
  totalOrders = computed(() => 
    this.trends().reduce((sum, t) => sum + t.orderCount, 0)
  );
  averageGrowthRate = computed(() => {
    const trends = this.trends().filter(t => t.growthRate !== undefined && t.growthRate !== null);
    if (trends.length === 0) return 0;
    return trends.reduce((sum, t) => sum + (t.growthRate || 0), 0) / trends.length;
  });

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

    this.reportService.getMonthlySalesTrend(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.trends.set(data);
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
    const trends = this.trends();
    const categories = trends.map(t => `${t.monthName} ${t.year}`);
    
    // Line Chart - Monthly Sales Trend
    this.lineChartOptions = {
      series: [{
        name: 'Aylık Satış',
        data: trends.map(t => t.monthlySales)
      }],
      chart: {
        type: 'line',
        height: 400,
        toolbar: { show: true },
        fontFamily: 'Inter, sans-serif'
      },
      stroke: {
        curve: 'smooth',
        width: 3
      },
      dataLabels: {
        enabled: true,
        formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
      },
      xaxis: {
        categories: categories,
        labels: {
          rotate: -45,
          style: {
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        title: {
          text: 'Satış Tutarı (USD)'
        },
        labels: {
          formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
        }
      },
      fill: {
        type: 'gradient',
        gradient: {
          shade: 'light',
          type: 'vertical',
          shadeIntensity: 0.5,
          gradientToColors: ['#6366f1'],
          inverseColors: false,
          opacityFrom: 0.8,
          opacityTo: 0.2,
          stops: [0, 100]
        }
      },
      tooltip: {
        y: {
          formatter: (val: number) => val.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' })
        }
      },
      title: {
        text: 'Aylık Satış Trendi',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#6366f1']
    };

    // Bar Chart - Growth Rate
    const growthData = trends.filter(t => t.growthRate !== undefined && t.growthRate !== null);
    if (growthData.length > 0) {
      this.barChartOptions = {
        series: [{
          name: 'Büyüme Oranı',
          data: growthData.map(t => t.growthRate || 0)
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
            colors: {
              ranges: [{
                from: -100,
                to: 0,
                color: '#ef4444'
              }, {
                from: 0,
                to: 100,
                color: '#10b981'
              }]
            }
          }
        },
        dataLabels: {
          enabled: true,
          formatter: (val: number) => val.toFixed(1) + '%'
        },
        xaxis: {
          categories: growthData.map(t => `${t.monthName} ${t.year}`),
          labels: {
            rotate: -45,
            style: {
              fontSize: '12px'
            }
          }
        },
        yaxis: {
          title: {
            text: 'Büyüme Oranı (%)'
          },
          labels: {
            formatter: (val: number) => val.toFixed(1) + '%'
          }
        },
        tooltip: {
          y: {
            formatter: (val: number) => val.toFixed(2) + '%'
          }
        },
        title: {
          text: 'Aylık Büyüme Oranı',
          align: 'center',
          style: {
            fontSize: '16px',
            fontWeight: '600'
          }
        }
      };
    }
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
    const data = this.trends().map(t => ({
      'Yıl': t.year,
      'Ay': t.monthName,
      'Aylık Satış': t.monthlySales,
      'Sipariş Sayısı': t.orderCount,
      'Ortalama Sipariş Tutarı': t.averageOrderAmount,
      'Önceki Ay Satış': t.previousMonthSales || 0,
      'Büyüme Oranı (%)': t.growthRate ? t.growthRate.toFixed(2) : '-'
    }));

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Aylık Satış Trendi');
    XLSX.writeFile(wb, `aylik-satis-trendi-${new Date().toISOString().split('T')[0]}.xlsx`);
  }

  exportToPdf(): void {
    const doc = new jsPDF();
    doc.setFontSize(18);
    doc.text('Aylık Satış Trend Raporu', 14, 20);
    doc.setFontSize(10);
    doc.text(`Tarih: ${new Date().toLocaleDateString('tr-TR')}`, 14, 30);

    const tableData = this.trends().map(t => [
      `${t.monthName} ${t.year}`,
      t.monthlySales.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' }),
      t.orderCount.toString(),
      t.averageOrderAmount.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' }),
      t.growthRate ? t.growthRate.toFixed(2) + '%' : '-'
    ]);

    autoTable(doc, {
      head: [['Dönem', 'Aylık Satış', 'Sipariş Sayısı', 'Ort. Sipariş Tutarı', 'Büyüme Oranı']],
      body: tableData,
      startY: 40,
      styles: { fontSize: 9 },
      headStyles: { fillColor: [99, 102, 241] }
    });

    doc.save(`aylik-satis-trendi-${new Date().toISOString().split('T')[0]}.pdf`);
  }

  formatCurrency(value: number): string {
    return value.toLocaleString('tr-TR', { style: 'currency', currency: 'USD' });
  }

  formatNumber(value: number): string {
    return value.toLocaleString('tr-TR');
  }

  formatPercent(value: number | undefined): string {
    if (value === undefined || value === null) return '-';
    return value.toFixed(2) + '%';
  }
}

