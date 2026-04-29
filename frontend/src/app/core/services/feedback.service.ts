import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, catchError, of, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export type FeedbackType = 'positive' | 'negative';

export interface AddFeedbackRequest {
  type: FeedbackType;
  comment?: string;
}

export interface FeedbackResponse {
  id: string;
  messageId: string;
  conversationId: string;
  userId: string;
  type: FeedbackType;
  comment?: string;
  createdAt: string;
}

export interface FeedbackStatisticsResponse {
  totalPositive: number;
  totalNegative: number;
  totalCount: number;
  satisfactionRate: number;
}

export interface DashboardStatisticsResponse {
  totalFeedbacks: number;
  positiveFeedbacks: number;
  negativeFeedbacks: number;
  satisfactionRate: number;
  trendChange: number;
  trendDirection: 'up' | 'down' | 'stable';
  dailyStats: DailyStatResponse[];
  startDate: string;
  endDate: string;
}

export interface DailyStatResponse {
  date: string;
  positiveCount: number;
  negativeCount: number;
  satisfactionRate: number;
}

export interface MessageFeedbackState {
  messageId: string;
  type: FeedbackType | null;
  isSubmitting: boolean;
  showCommentModal?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  
  private apiUrl = environment.apiUrl;
  
  // Track feedback state for each message
  private messageFeedbacks = signal<Map<string, MessageFeedbackState>>(new Map());
  
  /**
   * Get current feedback state for a message
   */
  getFeedbackState(messageId: string): MessageFeedbackState | undefined {
    return this.messageFeedbacks().get(messageId);
  }
  
  /**
   * Check if message has feedback
   */
  hasFeedback(messageId: string): boolean {
    const state = this.messageFeedbacks().get(messageId);
    return state?.type != null;
  }
  
  /**
   * Get feedback type for a message
   */
  getFeedbackType(messageId: string): FeedbackType | null {
    return this.messageFeedbacks().get(messageId)?.type ?? null;
  }
  
  /**
   * Check if feedback is being submitted for a message
   */
  isSubmitting(messageId: string): boolean {
    return this.messageFeedbacks().get(messageId)?.isSubmitting ?? false;
  }

  /**
   * Add or update feedback for a message
   */
  addFeedback(messageId: string, type: FeedbackType, comment?: string): Observable<FeedbackResponse | null> {
    // Set submitting state
    this.updateFeedbackState(messageId, { isSubmitting: true });
    
    const headers = this.getHeaders();
    const request: AddFeedbackRequest = { type, comment };
    
    return this.http.post<FeedbackResponse>(
      `${this.apiUrl}/api/v1/feedback/messages/${messageId}`,
      request,
      { headers }
    ).pipe(
      catchError(error => {
        console.error('Failed to add feedback:', error);
        this.updateFeedbackState(messageId, { isSubmitting: false });
        return of(null);
      })
    );
  }
  
  /**
   * Get feedback for a message
   */
  getFeedback(messageId: string): Observable<FeedbackResponse | null> {
    const headers = this.getHeaders();
    
    return this.http.get<FeedbackResponse>(
      `${this.apiUrl}/api/v1/feedback/messages/${messageId}`,
      { headers }
    ).pipe(
      catchError(error => {
        // 404 means no feedback yet - this is normal
        if (error.status === 404) {
          return of(null);
        }
        console.error('Failed to get feedback:', error);
        return of(null);
      })
    );
  }
  
  /**
   * Delete feedback for a message
   */
  deleteFeedback(messageId: string): Observable<boolean> {
    const headers = this.getHeaders();
    
    return this.http.delete<void>(
      `${this.apiUrl}/api/v1/feedback/messages/${messageId}`,
      { headers }
    ).pipe(
      map(() => true),
      catchError(error => {
        console.error('Failed to delete feedback:', error);
        return of(false);
      })
    );
  }
  
  /**
   * Get all feedback for a conversation
   */
  getConversationFeedbacks(conversationId: string): Observable<FeedbackResponse[]> {
    const headers = this.getHeaders();
    
    return this.http.get<FeedbackResponse[]>(
      `${this.apiUrl}/api/v1/feedback/conversations/${conversationId}`,
      { headers }
    ).pipe(
      catchError(error => {
        console.error('Failed to get conversation feedbacks:', error);
        return of([]);
      })
    );
  }
  
  /**
   * Get feedback statistics
   */
  getStatistics(): Observable<FeedbackStatisticsResponse | null> {
    const headers = this.getHeaders();
    
    return this.http.get<FeedbackStatisticsResponse>(
      `${this.apiUrl}/api/v1/feedback/statistics`,
      { headers }
    ).pipe(
      catchError(error => {
        console.error('Failed to get statistics:', error);
        return of(null);
      })
    );
  }

  /**
   * Get dashboard statistics with trends
   */
  getDashboardStatistics(days: number = 30): Observable<DashboardStatisticsResponse | null> {
    const headers = this.getHeaders();
    
    return this.http.get<DashboardStatisticsResponse>(
      `${this.apiUrl}/api/v1/feedback/dashboard?days=${days}`,
      { headers }
    ).pipe(
      catchError(error => {
        console.error('Failed to get dashboard statistics:', error);
        return of(null);
      })
    );
  }

  /**
   * Check if comment modal should be shown for a message
   */
  shouldShowCommentModal(messageId: string): boolean {
    return this.messageFeedbacks().get(messageId)?.showCommentModal ?? false;
  }

  /**
   * Show comment modal for negative feedback
   */
  showCommentModal(messageId: string): void {
    this.updateFeedbackState(messageId, { showCommentModal: true });
  }

  /**
   * Hide comment modal
   */
  hideCommentModal(messageId: string): void {
    this.updateFeedbackState(messageId, { showCommentModal: false });
  }
  
  /**
   * Submit feedback and update local state
   */
  async submitFeedback(messageId: string, type: FeedbackType, comment?: string): Promise<boolean> {
    // Optimistic update
    this.updateFeedbackState(messageId, { 
      type, 
      isSubmitting: true 
    });
    
    return new Promise((resolve) => {
      this.addFeedback(messageId, type, comment).subscribe({
        next: (response) => {
          if (response) {
            this.updateFeedbackState(messageId, { 
              type: response.type, 
              isSubmitting: false 
            });
            resolve(true);
          } else {
            // Revert on failure
            this.updateFeedbackState(messageId, { 
              type: null, 
              isSubmitting: false 
            });
            resolve(false);
          }
        },
        error: () => {
          // Revert on error
          this.updateFeedbackState(messageId, { 
            type: null, 
            isSubmitting: false 
          });
          resolve(false);
        }
      });
    });
  }
  
  /**
   * Toggle feedback (if same type, remove; otherwise set new type)
   */
  async toggleFeedback(messageId: string, type: FeedbackType): Promise<boolean> {
    const currentType = this.getFeedbackType(messageId);
    
    if (currentType === type) {
      // Remove feedback
      this.updateFeedbackState(messageId, { isSubmitting: true });
      
      return new Promise((resolve) => {
        this.deleteFeedback(messageId).subscribe({
          next: () => {
            this.updateFeedbackState(messageId, { 
              type: null, 
              isSubmitting: false 
            });
            resolve(true);
          },
          error: () => {
            this.updateFeedbackState(messageId, { isSubmitting: false });
            resolve(false);
          }
        });
      });
    } else {
      // Set new feedback
      return this.submitFeedback(messageId, type);
    }
  }
  
  /**
   * Load existing feedbacks for a conversation
   */
  loadConversationFeedbacks(conversationId: string): void {
    this.getConversationFeedbacks(conversationId).subscribe({
      next: (feedbacks) => {
        feedbacks.forEach(feedback => {
          this.updateFeedbackState(feedback.messageId, {
            type: feedback.type,
            isSubmitting: false
          });
        });
      }
    });
  }
  
  /**
   * Clear all feedback states (e.g., when starting new conversation)
   */
  clearFeedbackStates(): void {
    this.messageFeedbacks.set(new Map());
  }
  
  /**
   * Update feedback state for a message
   */
  private updateFeedbackState(
    messageId: string, 
    update: Partial<MessageFeedbackState>
  ): void {
    const current = this.messageFeedbacks();
    const newMap = new Map(current);
    
    const existing = newMap.get(messageId) || {
      messageId,
      type: null,
      isSubmitting: false
    };
    
    newMap.set(messageId, { ...existing, ...update, messageId });
    this.messageFeedbacks.set(newMap);
  }
  
  /**
   * Get auth headers
   */
  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }
}
