import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

// DTOs matching backend responses
export interface DashboardOverview {
  totalFeedbacks: number;
  positiveFeedbacks: number;
  negativeFeedbacks: number;
  satisfactionRate: number;
  totalAnalysisReports: number;
  pendingImprovements: number;
  appliedImprovements: number;
  trendChange: number;
  trendDirection: 'up' | 'down' | 'stable';
}

export interface DailyTrendItem {
  date: string;
  positiveCount: number;
  negativeCount: number;
  satisfactionRate: number;
}

export interface FeedbackTrends {
  startDate: string;
  endDate: string;
  dailyData: DailyTrendItem[];
  averageSatisfactionRate: number;
}

export interface CategoryItem {
  category: string;
  count: number;
  percentage: number;
}

export interface CategoryBreakdown {
  categories: CategoryItem[];
  totalFeedbacks: number;
}

export type ImprovementStatus = 'Pending' | 'UnderReview' | 'Applied' | 'Rejected';
export type ImprovementPriority = 'High' | 'Medium' | 'Low';

export interface PromptImprovementItem {
  id: string;
  category: string;
  issue: string;
  suggestion: string;
  priority: ImprovementPriority;
  status: ImprovementStatus;
  reviewNotes?: string;
  reviewedBy?: string;
  reviewedAt?: string;
  createdAt: string;
}

export interface PromptImprovementStatistics {
  total: number;
  pending: number;
  underReview: number;
  applied: number;
  rejected: number;
}

export interface PromptImprovementsResponse {
  improvements: PromptImprovementItem[];
  statistics: PromptImprovementStatistics;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface AnalysisReportSummary {
  id: string;
  analysisDate: string;
  totalFeedbacksAnalyzed: number;
  satisfactionScore: number;
  createdAt: string;
}

export interface AnalysisReportDetail {
  id: string;
  analysisDate: string;
  totalFeedbacksAnalyzed: number;
  positiveFeedbacks: number;
  negativeFeedbacks: number;
  satisfactionScore: number;
  summary: string;
  keyInsights: string[];
  improvements: PromptImprovementItem[];
  createdAt: string;
}

export interface UpdateImprovementStatusRequest {
  status: ImprovementStatus;
  reviewNotes?: string;
  reviewedBy?: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  
  private apiUrl = `${environment.apiUrl}/api/v1/dashboard`;
  
  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'Content-Type': 'application/json'
    });
  }
  
  /**
   * Get dashboard overview statistics
   */
  getOverview(days: number = 30): Observable<DashboardOverview> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<DashboardOverview>(`${this.apiUrl}/overview`, {
      headers: this.getHeaders(),
      params
    }).pipe(
      catchError(error => {
        console.error('Error fetching dashboard overview:', error);
        throw error;
      })
    );
  }
  
  /**
   * Get feedback trends over time
   */
  getTrends(days: number = 30): Observable<FeedbackTrends> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<FeedbackTrends>(`${this.apiUrl}/trends`, {
      headers: this.getHeaders(),
      params
    }).pipe(
      catchError(error => {
        console.error('Error fetching feedback trends:', error);
        throw error;
      })
    );
  }
  
  /**
   * Get feedback category breakdown
   */
  getCategories(days: number = 30): Observable<CategoryBreakdown> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<CategoryBreakdown>(`${this.apiUrl}/categories`, {
      headers: this.getHeaders(),
      params
    }).pipe(
      catchError(error => {
        console.error('Error fetching category breakdown:', error);
        throw error;
      })
    );
  }
  
  /**
   * Get prompt improvements with filtering
   */
  getImprovements(
    status?: ImprovementStatus,
    priority?: ImprovementPriority,
    page: number = 1,
    pageSize: number = 20
  ): Observable<PromptImprovementsResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (status) {
      params = params.set('status', status);
    }
    if (priority) {
      params = params.set('priority', priority);
    }
    
    return this.http.get<PromptImprovementsResponse>(`${this.apiUrl}/improvements`, {
      headers: this.getHeaders(),
      params
    }).pipe(
      catchError(error => {
        console.error('Error fetching improvements:', error);
        throw error;
      })
    );
  }
  
  /**
   * Update improvement status (approve/reject)
   */
  updateImprovementStatus(
    id: string,
    request: UpdateImprovementStatusRequest
  ): Observable<PromptImprovementItem> {
    return this.http.patch<PromptImprovementItem>(
      `${this.apiUrl}/improvements/${id}/status`,
      request,
      { headers: this.getHeaders() }
    ).pipe(
      catchError(error => {
        console.error('Error updating improvement status:', error);
        throw error;
      })
    );
  }
  
  /**
   * Get analysis reports list
   */
  getReports(page: number = 1, pageSize: number = 10): Observable<{
    reports: AnalysisReportSummary[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
  }> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    return this.http.get<{
      reports: AnalysisReportSummary[];
      totalCount: number;
      pageNumber: number;
      pageSize: number;
    }>(`${this.apiUrl}/reports`, {
      headers: this.getHeaders(),
      params
    }).pipe(
      catchError(error => {
        console.error('Error fetching reports:', error);
        throw error;
      })
    );
  }
  
  /**
   * Get detailed analysis report
   */
  getReportDetail(id: string): Observable<AnalysisReportDetail> {
    return this.http.get<AnalysisReportDetail>(`${this.apiUrl}/reports/${id}`, {
      headers: this.getHeaders()
    }).pipe(
      catchError(error => {
        console.error('Error fetching report detail:', error);
        throw error;
      })
    );
  }
}
