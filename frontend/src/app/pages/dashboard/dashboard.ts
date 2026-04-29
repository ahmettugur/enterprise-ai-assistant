import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// PrimeNG imports (v21 standalone components)
import { Card } from 'primeng/card';
import { Button } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Select } from 'primeng/select';
import { ProgressBar } from 'primeng/progressbar';
import { Tooltip } from 'primeng/tooltip';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { Skeleton } from 'primeng/skeleton';
import { Chip } from 'primeng/chip';
import { Badge } from 'primeng/badge';
import { Divider } from 'primeng/divider';
import { Paginator } from 'primeng/paginator';

// Chart.js
import { Chart, registerables } from 'chart.js';

// Services
import {
  DashboardService,
  DashboardOverview,
  FeedbackTrends,
  CategoryBreakdown,
  PromptImprovementsResponse,
  PromptImprovementItem,
  AnalysisReportSummary,
  AnalysisReportDetail,
  ImprovementStatus,
  ImprovementPriority
} from '../../core/services/dashboard.service';
import { ToastService } from '../../core/services/toast.service';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Card,
    Button,
    TableModule,
    Tag,
    Select,
    ProgressBar,
    Tooltip,
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    Dialog,
    Textarea,
    Skeleton,
    Chip,
    Badge,
    Divider,
    Paginator
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit, OnDestroy {
  private dashboardService = inject(DashboardService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  
  // Loading states
  isLoading = signal(true);
  isLoadingTrends = signal(true);
  isLoadingImprovements = signal(true);
  isLoadingReports = signal(true);
  
  // Data signals
  overview = signal<DashboardOverview | null>(null);
  trends = signal<FeedbackTrends | null>(null);
  categories = signal<CategoryBreakdown | null>(null);
  improvementsResponse = signal<PromptImprovementsResponse | null>(null);
  reports = signal<AnalysisReportSummary[]>([]);
  selectedReport = signal<AnalysisReportDetail | null>(null);
  
  // Computed values
  improvements = computed(() => this.improvementsResponse()?.improvements ?? []);
  improvementStats = computed(() => this.improvementsResponse()?.statistics);
  
  // Filter states
  selectedDays = signal(30);
  selectedStatus = signal<ImprovementStatus | null>(null);
  selectedPriority = signal<ImprovementPriority | null>(null);
  
  // Pagination
  currentPage = signal(1);
  pageSize = signal(10);
  totalRecords = signal(0);
  
  // Modal states
  showReportModal = signal(false);
  showApproveModal = signal(false);
  selectedImprovement = signal<PromptImprovementItem | null>(null);
  reviewNotes = signal('');
  
  // Chart instances
  private trendsChart: Chart | null = null;
  private categoryChart: Chart | null = null;
  
  // Dropdown options
  daysOptions = [
    { label: 'Son 7 gün', value: 7 },
    { label: 'Son 30 gün', value: 30 },
    { label: 'Son 90 gün', value: 90 }
  ];
  
  statusOptions = [
    { label: 'Tümü', value: null },
    { label: 'Beklemede', value: 'Pending' },
    { label: 'İnceleniyor', value: 'UnderReview' },
    { label: 'Uygulandı', value: 'Applied' },
    { label: 'Reddedildi', value: 'Rejected' }
  ];
  
  priorityOptions = [
    { label: 'Tümü', value: null },
    { label: 'Yüksek', value: 'High' },
    { label: 'Orta', value: 'Medium' },
    { label: 'Düşük', value: 'Low' }
  ];
  
  ngOnInit(): void {
    this.loadAllData();
  }
  
  ngOnDestroy(): void {
    this.destroyCharts();
  }
  
  private destroyCharts(): void {
    if (this.trendsChart) {
      this.trendsChart.destroy();
      this.trendsChart = null;
    }
    if (this.categoryChart) {
      this.categoryChart.destroy();
      this.categoryChart = null;
    }
  }
  
  loadAllData(): void {
    this.loadOverview();
    this.loadTrends();
    this.loadCategories();
    this.loadImprovements();
    this.loadReports();
  }
  
  loadOverview(): void {
    this.isLoading.set(true);
    this.dashboardService.getOverview(this.selectedDays()).subscribe({
      next: (data) => {
        this.overview.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading overview:', error);
        this.toastService.error('Dashboard verileri yüklenirken hata oluştu');
        this.isLoading.set(false);
      }
    });
  }
  
  loadTrends(): void {
    this.isLoadingTrends.set(true);
    this.dashboardService.getTrends(this.selectedDays()).subscribe({
      next: (data) => {
        this.trends.set(data);
        this.isLoadingTrends.set(false);
        setTimeout(() => this.renderTrendsChart(), 100);
      },
      error: (error) => {
        console.error('Error loading trends:', error);
        this.isLoadingTrends.set(false);
      }
    });
  }
  
  loadCategories(): void {
    this.dashboardService.getCategories(this.selectedDays()).subscribe({
      next: (data) => {
        this.categories.set(data);
        setTimeout(() => this.renderCategoryChart(), 100);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }
  
  loadImprovements(): void {
    this.isLoadingImprovements.set(true);
    this.dashboardService.getImprovements(
      this.selectedStatus() ?? undefined,
      this.selectedPriority() ?? undefined,
      this.currentPage(),
      this.pageSize()
    ).subscribe({
      next: (data) => {
        this.improvementsResponse.set(data);
        this.totalRecords.set(data.totalCount);
        this.isLoadingImprovements.set(false);
      },
      error: (error) => {
        console.error('Error loading improvements:', error);
        this.isLoadingImprovements.set(false);
      }
    });
  }
  
  loadReports(): void {
    this.isLoadingReports.set(true);
    this.dashboardService.getReports(1, 5).subscribe({
      next: (data) => {
        this.reports.set(data.reports);
        this.isLoadingReports.set(false);
      },
      error: (error) => {
        console.error('Error loading reports:', error);
        this.isLoadingReports.set(false);
      }
    });
  }
  
  onDaysChange(days: number): void {
    this.selectedDays.set(days);
    this.loadOverview();
    this.loadTrends();
    this.loadCategories();
  }
  
  onStatusChange(status: ImprovementStatus | null): void {
    this.selectedStatus.set(status);
    this.currentPage.set(1);
    this.loadImprovements();
  }
  
  onPriorityChange(priority: ImprovementPriority | null): void {
    this.selectedPriority.set(priority);
    this.currentPage.set(1);
    this.loadImprovements();
  }
  
  onPageChange(event: { page?: number; rows?: number; first?: number }): void {
    if (event.page !== undefined) {
      this.currentPage.set(event.page + 1);
    }
    if (event.rows !== undefined) {
      this.pageSize.set(event.rows);
    }
    this.loadImprovements();
  }
  
  // Chart rendering
  private renderTrendsChart(): void {
    const canvas = document.getElementById('trendsChart') as HTMLCanvasElement;
    if (!canvas || !this.trends()) return;
    
    if (this.trendsChart) {
      this.trendsChart.destroy();
    }
    
    const data = this.trends()!;
    const dailyList = data.dailyData ?? [];
    if (dailyList.length === 0) return;
    
    const labels = dailyList.map(d => this.formatDate(d.date));
    const positiveData = dailyList.map(d => d.positiveCount);
    const negativeData = dailyList.map(d => d.negativeCount);
    
    this.trendsChart = new Chart(canvas, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Olumlu',
            data: positiveData,
            borderColor: '#22c55e',
            backgroundColor: 'rgba(34, 197, 94, 0.1)',
            fill: true,
            tension: 0.4
          },
          {
            label: 'Olumsuz',
            data: negativeData,
            borderColor: '#ef4444',
            backgroundColor: 'rgba(239, 68, 68, 0.1)',
            fill: true,
            tension: 0.4
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top'
          }
        },
        scales: {
          y: {
            beginAtZero: true
          }
        }
      }
    });
  }
  
  private renderCategoryChart(): void {
    const canvas = document.getElementById('categoryChart') as HTMLCanvasElement;
    if (!canvas || !this.categories()) return;
    
    if (this.categoryChart) {
      this.categoryChart.destroy();
    }
    
    const data = this.categories()!;
    const categoryList = data.categories ?? [];
    if (categoryList.length === 0) return;
    
    const labels = categoryList.map(c => c.category);
    const values = categoryList.map(c => c.count);
    const colors = ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];
    
    this.categoryChart = new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels,
        datasets: [{
          data: values,
          backgroundColor: colors.slice(0, labels.length),
          borderWidth: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'right'
          }
        }
      }
    });
  }
  
  // Actions
  openReportDetail(report: AnalysisReportSummary): void {
    this.dashboardService.getReportDetail(report.id).subscribe({
      next: (detail) => {
        this.selectedReport.set(detail);
        this.showReportModal.set(true);
      },
      error: (error) => {
        console.error('Error loading report detail:', error);
        this.toastService.error('Rapor detayları yüklenirken hata oluştu');
      }
    });
  }
  
  openApproveModal(improvement: PromptImprovementItem): void {
    this.selectedImprovement.set(improvement);
    this.reviewNotes.set('');
    this.showApproveModal.set(true);
  }
  
  approveImprovement(): void {
    const improvement = this.selectedImprovement();
    if (!improvement) return;
    
    this.dashboardService.updateImprovementStatus(improvement.id, {
      status: 'Applied',
      reviewNotes: this.reviewNotes(),
      reviewedBy: 'Admin' // TODO: Get from auth service
    }).subscribe({
      next: () => {
        this.toastService.success('İyileştirme onaylandı');
        this.showApproveModal.set(false);
        this.loadImprovements();
      },
      error: (error) => {
        console.error('Error approving improvement:', error);
        this.toastService.error('İşlem sırasında hata oluştu');
      }
    });
  }
  
  rejectImprovement(improvement: PromptImprovementItem): void {
    this.dashboardService.updateImprovementStatus(improvement.id, {
      status: 'Rejected',
      reviewedBy: 'Admin'
    }).subscribe({
      next: () => {
        this.toastService.success('İyileştirme reddedildi');
        this.loadImprovements();
      },
      error: (error) => {
        console.error('Error rejecting improvement:', error);
        this.toastService.error('İşlem sırasında hata oluştu');
      }
    });
  }
  
  startReview(improvement: PromptImprovementItem): void {
    this.dashboardService.updateImprovementStatus(improvement.id, {
      status: 'UnderReview',
      reviewedBy: 'Admin'
    }).subscribe({
      next: () => {
        this.toastService.info('İnceleme başlatıldı');
        this.loadImprovements();
      },
      error: (error) => {
        console.error('Error starting review:', error);
        this.toastService.error('İşlem sırasında hata oluştu');
      }
    });
  }
  
  // Helpers
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });
  }
  
  formatDateTime(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('tr-TR', {
      day: '2-digit',
      month: 'long',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
  
  getStatusSeverity(status: ImprovementStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'Applied': return 'success';
      case 'UnderReview': return 'info';
      case 'Pending': return 'warn';
      case 'Rejected': return 'danger';
      default: return 'secondary';
    }
  }
  
  getStatusLabel(status: ImprovementStatus): string {
    switch (status) {
      case 'Applied': return 'Uygulandı';
      case 'UnderReview': return 'İnceleniyor';
      case 'Pending': return 'Beklemede';
      case 'Rejected': return 'Reddedildi';
      default: return status;
    }
  }
  
  getPrioritySeverity(priority: ImprovementPriority): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (priority) {
      case 'High': return 'danger';
      case 'Medium': return 'warn';
      case 'Low': return 'info';
      default: return 'secondary';
    }
  }
  
  getPriorityLabel(priority: ImprovementPriority): string {
    switch (priority) {
      case 'High': return 'Yüksek';
      case 'Medium': return 'Orta';
      case 'Low': return 'Düşük';
      default: return priority;
    }
  }
  
  getTrendIcon(): string {
    const direction = this.overview()?.trendDirection;
    switch (direction) {
      case 'up': return 'pi pi-arrow-up';
      case 'down': return 'pi pi-arrow-down';
      default: return 'pi pi-minus';
    }
  }
  
  getTrendClass(): string {
    const direction = this.overview()?.trendDirection;
    switch (direction) {
      case 'up': return 'text-green-500';
      case 'down': return 'text-red-500';
      default: return 'text-gray-500';
    }
  }
  
  goBack(): void {
    this.router.navigate(['/']);
  }
}
