import { Component, OnInit, OnDestroy, inject, signal, computed, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

// ApexCharts
import { NgApexchartsModule } from 'ng-apexcharts';
import { ApexChart, ApexNonAxisChartSeries, ApexAxisChartSeries, ApexDataLabels, ApexLegend, ApexPlotOptions, ApexXAxis, ApexYAxis, ApexFill, ApexTooltip, ApexGrid, ApexTitleSubtitle } from 'ng-apexcharts';

// Excel & PDF export
import * as XLSX from 'xlsx';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

// PrimeNG Imports
import { Card } from 'primeng/card';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';

// Services
import { AdventureWorksReportService } from '../../../../core/services/adventureworks/adventureworks-report.service';
import { AuthService } from '../../../../core/services/auth.service';

// Models
import {
  EmployeeDepartmentDistribution
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
  selector: 'app-employee-department-distribution-report',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Card,
    Button,
    TableModule,
    Skeleton,
    Tag,
    NgApexchartsModule
  ],
  templateUrl: './employee-department-distribution-report.html',
  styleUrl: './employee_department_distribution_report.scss',
  host: {
    'style': 'display: block; width: 100%; height: 100vh; overflow: auto;'
  }
})
export class EmployeeDepartmentDistributionReport implements OnInit, OnDestroy, AfterViewInit {
  private reportService = inject(AdventureWorksReportService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  isEmbedded = signal(false);
  isLoadingData = signal(false);

  departments = signal<EmployeeDepartmentDistribution[]>([]);

  barChartOptions: Partial<ChartOptions> = {};
  pieChartOptions: Partial<ChartOptions> = {};

  totalEmployees = computed(() => 
    this.departments().reduce((sum, d) => sum + d.employeeCount, 0)
  );
  totalDepartments = computed(() => this.departments().length);
  averageEmployeesPerDept = computed(() => {
    const departments = this.departments();
    if (departments.length === 0) return 0;
    return departments.reduce((sum, d) => sum + d.employeeCount, 0) / departments.length;
  });
  averageYearsOfService = computed(() => {
    const departments = this.departments().filter(d => d.averageYearsOfService !== undefined && d.averageYearsOfService !== null);
    if (departments.length === 0) return 0;
    return departments.reduce((sum, d) => sum + (d.averageYearsOfService || 0), 0) / departments.length;
  });

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['embedded'] === 'true') {
        this.isEmbedded.set(true);
      }
    });
  }

  ngAfterViewInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(): void {
    this.isLoadingData.set(true);

    this.reportService.getEmployeeDepartmentDistribution()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.departments.set(data);
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
    const departments = this.departments();
    
    // Bar Chart - Employee Count by Department
    this.barChartOptions = {
      series: [{
        name: 'Çalışan Sayısı',
        data: departments.map(d => d.employeeCount)
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
        formatter: (val: number) => val.toString()
      },
      xaxis: {
        categories: departments.map(d => d.departmentName),
        labels: {
          rotate: -45,
          style: {
            fontSize: '12px'
          }
        }
      },
      yaxis: {
        title: {
          text: 'Çalışan Sayısı'
        }
      },
      fill: {
        colors: ['#6366f1']
      },
      tooltip: {
        y: {
          formatter: (val: number) => val + ' çalışan'
        }
      },
      title: {
        text: 'Departman Bazında Çalışan Dağılımı',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#6366f1']
    };

    // Pie Chart - Employee Distribution
    this.pieChartOptions = {
      series: departments.map(d => d.employeeCount),
      chart: {
        type: 'pie',
        height: 400,
        toolbar: { show: true },
        fontFamily: 'Inter, sans-serif'
      },
      labels: departments.map(d => d.departmentName),
      dataLabels: {
        enabled: true,
        formatter: (val: number) => val.toFixed(1) + '%'
      },
      legend: {
        position: 'bottom'
      },
      tooltip: {
        y: {
          formatter: (val: number) => val + ' çalışan'
        }
      },
      title: {
        text: 'Çalışan Dağılım Oranları',
        align: 'center',
        style: {
          fontSize: '16px',
          fontWeight: '600'
        }
      },
      colors: ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4', '#84cc16', '#ec4899']
    };
  }

  exportToExcel(): void {
    const data = this.departments().map(d => ({
      'Departman': d.departmentName,
      'Grup': d.groupName || '',
      'Çalışan Sayısı': d.employeeCount,
      'En Eski İşe Giriş': d.oldestHireDate ? new Date(d.oldestHireDate).toLocaleDateString('tr-TR') : '',
      'En Yeni İşe Giriş': d.newestHireDate ? new Date(d.newestHireDate).toLocaleDateString('tr-TR') : '',
      'Ortalama Hizmet Yılı': d.averageYearsOfService ? d.averageYearsOfService.toFixed(2) : ''
    }));

    const ws = XLSX.utils.json_to_sheet(data);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Departman Dağılımı');
    XLSX.writeFile(wb, `departman-calisan-dagilimi-${new Date().toISOString().split('T')[0]}.xlsx`);
  }

  exportToPdf(): void {
    const doc = new jsPDF();
    doc.setFontSize(18);
    doc.text('Departman Bazında Çalışan Dağılım Raporu', 14, 20);
    doc.setFontSize(10);
    doc.text(`Tarih: ${new Date().toLocaleDateString('tr-TR')}`, 14, 30);

    const tableData = this.departments().map(d => [
      d.departmentName,
      d.groupName || '-',
      d.employeeCount.toString(),
      d.averageYearsOfService ? d.averageYearsOfService.toFixed(2) + ' yıl' : '-'
    ]);

    autoTable(doc, {
      head: [['Departman', 'Grup', 'Çalışan Sayısı', 'Ort. Hizmet Yılı']],
      body: tableData,
      startY: 40,
      styles: { fontSize: 9 },
      headStyles: { fillColor: [99, 102, 241] }
    });

    doc.save(`departman-calisan-dagilimi-${new Date().toISOString().split('T')[0]}.pdf`);
  }

  formatNumber(value: number): string {
    return value.toLocaleString('tr-TR');
  }

  formatDate(value: string | undefined): string {
    if (!value) return '-';
    return new Date(value).toLocaleDateString('tr-TR');
  }

  formatYears(value: number | undefined): string {
    if (value === undefined || value === null) return '-';
    return value.toFixed(2) + ' yıl';
  }
}

