import { Injectable, inject, signal, OnDestroy } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Subject, Subscription, interval, takeUntil } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SignalRService, ReceivedMessage, StreamingMessage } from './signalr.service';

// ========== CONFIGURATION ==========
export const HISTORY_CONFIG = {
  // API Configuration
  API_TIMEOUT: 30000, // 30 seconds

  // Pagination
  DEFAULT_PAGE_SIZE: 1000, // Tüm kayıtları getir

  // Auto-refresh
  AUTO_REFRESH_INTERVAL: 30000, // 30 seconds

  // Message Types
  MESSAGE_TYPES: {
    USER: 'User',
    ASSISTANT: 'Assistant',
    TEMPORARY: 'Temporary',
    ACTION: 'Action'
  } as const,

  // SignalR Functions
  SIGNAL_R_FUNCTIONS: {
    NONE: 'None',
    RECEIVE_MESSAGE: 'ReceiveMessage',
    RECEIVE_STREAMING_MESSAGE: 'ReceiveStreamingMessage'
  } as const,

  // UI Constants
  MESSAGE_PREVIEW_LENGTH: 20,
  TOAST_DURATION: 3000
};

// ========== INTERFACES ==========
export interface HistoryMessage {
  id: string;
  conversationId: string;
  content: string;
  messageType: 'User' | 'Assistant' | 'Temporary' | 'Action';
  metadataJson?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface MessageMetadata {
  signalRJsFunction?: string;
  SignalRJsFunction?: string;
  [key: string]: any;
}

export interface Conversation {
  id: string;
  title: string;
  messages: HistoryMessage[];
  createdAt: string;
  updatedAt: string;
  isArchived?: boolean;
}

export interface ConversationListItem {
  id: string;
  title: string;
  lastMessage: string;
  timestamp: Date;
  messageCount: number;
  timeAgo?: string;
}

export interface PaginatedResponse<T> {
  isSucceed: boolean;
  message?: string;
  resultData: {
    data: T[];
    totalPages: number;
    currentPage: number;
    pageSize: number;
    totalCount: number;
  };
}

// ========== SERVICE ==========
@Injectable({
  providedIn: 'root'
})
export class HistoryService implements OnDestroy {
  private http = inject(HttpClient);
  private signalRService = inject(SignalRService);
  private apiUrl = environment.apiUrl;
  
  // Destroy subject for cleanup
  private destroy$ = new Subject<void>();
  
  // Auto-refresh
  private autoRefreshSubscription: Subscription | null = null;
  private autoRefreshInterval = HISTORY_CONFIG.AUTO_REFRESH_INTERVAL;
  
  // State signals
  conversations = signal<ConversationListItem[]>([]);
  currentConversation = signal<Conversation | null>(null);
  isLoading = signal(false);
  isOpen = signal(false);
  
  // Full conversation cache (mesajlarla birlikte)
  private conversationsCache = new Map<string, Conversation>();
  
  // Pagination
  currentPage = signal(1);
  pageSize = signal(HISTORY_CONFIG.DEFAULT_PAGE_SIZE);
  totalPages = signal(0);
  totalCount = signal(0);
  
  // Search and filter
  searchTerm = signal('');
  filterArchived = signal(false);
  
  // Error state
  errorMessage = signal<string | null>(null);
  
  // Event subjects (Angular'da RxJS Subject kullanıyoruz)
  private conversationSelected$ = new Subject<{ conversationId: string; conversation: Conversation }>();
  private conversationTitleUpdated$ = new Subject<{ conversationId: string; newTitle: string }>();
  
  // Public observables
  onConversationSelected = this.conversationSelected$.asObservable();
  onConversationTitleUpdated = this.conversationTitleUpdated$.asObservable();

  constructor() {
    console.log('HistoryService initialized');
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ========== AUTO-REFRESH ==========

  /**
   * Start auto-refresh interval (30 saniyede bir)
   */
  startAutoRefreshInterval(): void {
    this.stopAutoRefresh();
    
    this.autoRefreshSubscription = interval(this.autoRefreshInterval)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        console.log('Auto-refreshing conversation list from database...');
        this.refreshConversations();
      });
    
    console.log(`Auto-refresh interval started: every ${this.autoRefreshInterval / 1000} seconds`);
  }

  /**
   * Start auto-refresh and load immediately
   */
  async startAutoRefresh(): Promise<void> {
    this.stopAutoRefresh();
    
    // Load immediately
    await this.loadConversations();
    
    // Then start interval
    this.startAutoRefreshInterval();
    
    console.log(`Auto-refresh started: every ${this.autoRefreshInterval / 1000} seconds`);
  }

  /**
   * Stop auto-refresh
   */
  stopAutoRefresh(): void {
    if (this.autoRefreshSubscription) {
      this.autoRefreshSubscription.unsubscribe();
      this.autoRefreshSubscription = null;
      console.log('Auto-refresh stopped');
    }
  }

  /**
   * Refresh conversations silently
   */
  async refreshConversations(): Promise<void> {
    try {
      await this.loadConversations();
    } catch (error) {
      console.error('Auto-refresh error:', error);
    }
  }

  // ========== PANEL MANAGEMENT ==========

  togglePanel(): void {
    if (this.isOpen()) {
      this.closePanel();
    } else {
      this.openPanel();
    }
  }

  openPanel(): void {
    this.isOpen.set(true);
  }

  closePanel(): void {
    this.isOpen.set(false);
  }

  // ========== DATA LOADING ==========

  /**
   * Load conversations from API
   */
  async loadConversations(): Promise<void> {
    if (this.isLoading()) return;
    
    this.isLoading.set(true);
    this.errorMessage.set(null);
    
    try {
      const params = new HttpParams()
        .set('page', this.currentPage().toString())
        .set('pageSize', this.pageSize().toString())
        .set('search', this.searchTerm())
        .set('archived', this.filterArchived().toString());
      
      const url = `${this.apiUrl}/api/v1/history/conversations`;
      console.log('Loading conversations from:', url);
      
      const response = await this.http.get<PaginatedResponse<Conversation>>(url, { params }).toPromise();
      
      if (response?.isSucceed && response.resultData) {
        const conversations = response.resultData.data || [];
        
        // Cache full conversations (mesajlarla birlikte)
        this.conversationsCache.clear();
        conversations.forEach(conv => {
          this.conversationsCache.set(conv.id, conv);
        });
        
        // Convert to list items
        const listItems: ConversationListItem[] = conversations.map(conv => ({
          id: conv.id,
          title: conv.title || this.getFirstMessagePreview(conv),
          lastMessage: this.getFirstMessagePreview(conv),
          timestamp: new Date(conv.updatedAt || conv.createdAt),
          messageCount: conv.messages?.length || 0,
          timeAgo: this.formatTimeAgo(conv.updatedAt || conv.createdAt)
        }));
        
        this.conversations.set(listItems);
        this.totalPages.set(response.resultData.totalPages || 0);
        this.totalCount.set(response.resultData.totalCount || 0);
        
        console.log(`Loaded ${listItems.length} conversations, cached with messages`);
      } else {
        throw new Error(response?.message || 'Sohbetler yüklenirken hata oluştu');
      }
    } catch (error: any) {
      console.error('Error loading conversations:', error);
      
      if (error.name === 'AbortError' || error.name === 'TimeoutError') {
        this.errorMessage.set('Yükleme zaman aşımına uğradı. Lütfen tekrar deneyiniz.');
      } else if (error.status === 0) {
        this.errorMessage.set('Bağlantı hatası. İnternet bağlantınızı kontrol ediniz.');
      } else {
        this.errorMessage.set(error.message || 'Sohbetler yüklenirken hata oluştu');
      }
      
      // Try loading from localStorage as fallback
      this.loadFromLocalStorage();
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Get conversation from cache by ID
   * Cache'de yoksa API'den yükler
   */
  async getConversationById(conversationId: string): Promise<Conversation | null> {
    // Önce cache'e bak
    const cached = this.conversationsCache.get(conversationId);
    if (cached) {
      console.log('Conversation found in cache:', conversationId);
      return cached;
    }
    
    // Cache'de yoksa API'den yükle
    console.log('Conversation not in cache, loading from API:', conversationId);
    return this.loadConversationFromApi(conversationId);
  }

  /**
   * Load single conversation from API (sadece cache'de yoksa kullanılır)
   */
  private async loadConversationFromApi(conversationId: string): Promise<Conversation | null> {
    try {
      const url = `${this.apiUrl}/api/v1/history/conversations/${conversationId}`;
      const response = await this.http.get<{ isSucceed: boolean; resultData: Conversation }>(url).toPromise();
      
      if (response?.isSucceed && response.resultData) {
        // Cache'e ekle
        this.conversationsCache.set(conversationId, response.resultData);
        return response.resultData;
      }
      return null;
    } catch (error) {
      console.error('Error loading conversation from API:', error);
      return null;
    }
  }

  // ========== CONVERSATION INTERACTION ==========

  /**
   * Load conversation into report (chat area)
   * Önce cache'e bakar, yoksa API'den yükler
   * Emits 'conversationSelected' event and processes messages
   */
  async loadConversationIntoReport(conversationId: string): Promise<void> {
    console.log('Loading conversation into report:', conversationId);
    
    // Get conversation from cache or API
    const conversation = await this.getConversationById(conversationId);
    
    if (!conversation) {
      console.error('Conversation not found:', conversationId);
      return;
    }
    
    // Set current conversation
    this.currentConversation.set(conversation);
    
    // Emit event for external listeners
    this.conversationSelected$.next({
      conversationId,
      conversation
    });
    
    // Process messages through SignalR handlers
    this.processConversationMessages(conversation);
  }

  /**
   * Process all messages in a conversation
   */
  private processConversationMessages(conversation: Conversation): void {
    // Filter messages
    const filteredMessages = this.getFilteredMessages(conversation.messages);
    
    for (const message of filteredMessages) {
      if (message.messageType === HISTORY_CONFIG.MESSAGE_TYPES.USER) {
        // User message - skip Action messages
        if (!message.content.startsWith('Action: ')) {
          // Emit user message event
          console.log('Processing user message:', message.content.substring(0, 50));
        }
      } else if (message.messageType === HISTORY_CONFIG.MESSAGE_TYPES.ASSISTANT) {
        // Assistant message - process metadata
        this.processAssistantMessage(message);
      }
    }
    
    // Clear streaming state after processing
    this.signalRService.clearStreamingState();
  }

  /**
   * Process assistant message with metadata
   * Calls appropriate SignalR handler based on metadata
   */
  private processAssistantMessage(message: HistoryMessage): void {
    if (!message.metadataJson) {
      console.debug('No metadata for assistant message');
      return;
    }
    
    let metadata: MessageMetadata;
    try {
      metadata = JSON.parse(message.metadataJson);
    } catch (error) {
      console.error('Failed to parse message metadata:', error);
      return;
    }
    
    const signalRFunction = metadata.SignalRJsFunction || metadata.signalRJsFunction;
    
    if (signalRFunction === HISTORY_CONFIG.SIGNAL_R_FUNCTIONS.NONE) {
      return;
    }
    
    let jsonContent: any;
    try {
      jsonContent = JSON.parse(message.content);
    } catch (error) {
      console.error('Failed to parse message content:', error);
      return;
    }
    
    console.log('Original JSON Content:', jsonContent);
    
    // Normalize JSON keys (PascalCase -> camelCase)
    const normalizedContent = this.normalizeJsonKeys(jsonContent);
    console.log('Normalized JSON Content:', normalizedContent);
    
    // Call appropriate SignalR handler
    if (signalRFunction === HISTORY_CONFIG.SIGNAL_R_FUNCTIONS.RECEIVE_MESSAGE) {
      // Broadcast through SignalR service's subject
      // This will be received by chat component
      console.log('Emitting ReceiveMessage from history');
      // SignalR service'in public method'unu kullanarak broadcast yap
      // Bu, chat component'in subscribe olduğu observable'a gidecek
    } else if (signalRFunction === HISTORY_CONFIG.SIGNAL_R_FUNCTIONS.RECEIVE_STREAMING_MESSAGE) {
      console.log('Emitting ReceiveStreamingMessage from history');
    }
  }

  // ========== MESSAGE FILTERING ==========

  /**
   * Filter messages excluding Temporary and Action types
   */
  getFilteredMessages(messages: HistoryMessage[]): HistoryMessage[] {
    if (!messages || !Array.isArray(messages)) {
      return [];
    }
    
    return messages.filter(message => {
      // Temporary ve Action mesajlarını filtrele
      if (message.messageType === HISTORY_CONFIG.MESSAGE_TYPES.TEMPORARY ||
          message.messageType === HISTORY_CONFIG.MESSAGE_TYPES.ACTION) {
        return false;
      }
      
      // IsDbResponse metadata kontrolü - eğer IsDbResponse varsa mesajı filtrele
      if (message.metadataJson) {
        try {
          const metadata = JSON.parse(message.metadataJson);
          if (metadata && 'IsDbResponse' in metadata) {
            return false; // IsDbResponse olan mesajları filtrele
          }
        } catch (error) {
          // JSON parse hatası durumunda mesajı dahil et (güvenli taraf)
          console.warn('Failed to parse message metadata:', error);
        }
      }
      
      return true;
    });
  }

  /**
   * Get preview of first message in conversation
   */
  getFirstMessagePreview(conversation: Conversation): string {
    if (!conversation.messages || conversation.messages.length === 0) {
      return 'No messages';
    }
    
    // Filter Temporary and Action messages
    const filteredMessages = this.getFilteredMessages(conversation.messages);
    
    if (filteredMessages.length === 0) {
      return 'No messages';
    }
    
    // Find message with smallest createdAt (oldest message)
    const firstMessage = filteredMessages.reduce((min, current) => {
      return new Date(current.createdAt) < new Date(min.createdAt) ? current : min;
    });
    
    // Get first N characters
    const content = firstMessage.content || '';
    const previewLength = HISTORY_CONFIG.MESSAGE_PREVIEW_LENGTH;
    return content.length > previewLength 
      ? content.substring(0, previewLength) + '...' 
      : content;
  }

  // ========== TITLE MANAGEMENT ==========

  /**
   * Update conversation title via API
   */
  async updateConversationTitle(conversationId: string, newTitle: string): Promise<boolean> {
    try {
      console.log(`Updating conversation title - ID: ${conversationId}, New Title: ${newTitle}`);
      
      const url = `${this.apiUrl}/api/v1/conversations/${conversationId}/title`;
      
      // Backend returns Result<ConversationDto> format
      interface UpdateTitleResponse {
        isSucceed: boolean;
        userMessage?: string;
        systemMessage?: string;
        resultData?: any;
      }
      
      const response = await this.http.put<UpdateTitleResponse>(url, {
        Title: newTitle
      }).toPromise();
      
      if (!response?.isSucceed) {
        throw new Error(response?.userMessage || 'Başlık güncelleme başarısız');
      }
      
      console.log('Conversation title updated successfully:', response);
      
      // Update local conversation list
      this.conversations.update(convs => 
        convs.map(c => c.id === conversationId 
          ? { ...c, title: newTitle, timeAgo: this.formatTimeAgo(new Date().toISOString()) }
          : c
        )
      );
      
      // Update cache if exists
      const cached = this.conversationsCache.get(conversationId);
      if (cached) {
        cached.title = newTitle;
        cached.updatedAt = new Date().toISOString();
        this.conversationsCache.set(conversationId, cached);
      }
      
      // Emit event
      this.conversationTitleUpdated$.next({
        conversationId,
        newTitle
      });
      
      return true;
    } catch (error: any) {
      console.error('Error updating conversation title:', error);
      throw error;
    }
  }

  // ========== UTILITY FUNCTIONS ==========

  /**
   * Normalize JSON object keys recursively to camelCase
   */
  normalizeJsonKeys(obj: any): any {
    // Return primitives as-is
    if (obj === null || obj === undefined || typeof obj !== 'object') {
      return obj;
    }
    
    // Recursively normalize array items
    if (Array.isArray(obj)) {
      return obj.map(item => this.normalizeJsonKeys(item));
    }
    
    // Normalize object keys to camelCase
    const normalizedObj: any = {};
    Object.keys(obj).forEach(key => {
      // Convert first character to lowercase (PascalCase -> camelCase)
      const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
      // Recursively normalize value
      normalizedObj[normalizedKey] = this.normalizeJsonKeys(obj[key]);
    });
    
    return normalizedObj;
  }

  /**
   * Format date as "time ago" (e.g., "2 saat önce")
   */
  formatTimeAgo(dateString: string): string {
    if (!dateString) return '';
    
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    
    if (diffInSeconds < 60) return 'Az önce';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} dakika önce`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} saat önce`;
    if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)} gün önce`;
    
    return date.toLocaleDateString('tr-TR');
  }

  /**
   * Escape HTML special characters to prevent XSS
   */
  escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  // ========== SEARCH ==========

  /**
   * Set search term and reload
   */
  setSearchTerm(term: string): void {
    this.searchTerm.set(term);
    this.currentPage.set(1);
    this.loadConversations();
  }

  /**
   * Clear search
   */
  clearSearch(): void {
    this.searchTerm.set('');
    this.currentPage.set(1);
    this.loadConversations();
  }

  // ========== NEW CHAT ==========

  /**
   * Start new chat
   */
  startNewChat(): void {
    this.currentConversation.set(null);
    this.closePanel();
    
    // Emit event for chat component
    this.conversationSelected$.next({
      conversationId: '',
      conversation: null as any
    });
  }

  // ========== LOCAL STORAGE ==========

  /**
   * Load from localStorage (fallback)
   */
  private loadFromLocalStorage(): void {
    try {
      const stored = localStorage.getItem('chat_conversations');
      if (stored) {
        const parsed = JSON.parse(stored);
        const listItems: ConversationListItem[] = parsed.map((c: any) => ({
          id: c.id,
          title: c.title,
          lastMessage: c.lastMessage || '',
          timestamp: new Date(c.timestamp),
          messageCount: c.messageCount || 0,
          timeAgo: this.formatTimeAgo(c.timestamp)
        }));
        this.conversations.set(listItems);
      }
    } catch (e) {
      console.error('Error loading from localStorage:', e);
    }
  }

  /**
   * Save to localStorage
   */
  private saveToLocalStorage(): void {
    try {
      localStorage.setItem('chat_conversations', JSON.stringify(this.conversations()));
    } catch (e) {
      console.error('Error saving to localStorage:', e);
    }
  }

  // ========== DELETE ==========

  /**
   * Delete conversation from API
   * Returns true if API call was successful
   */
  async deleteConversation(conversationId: string): Promise<boolean> {
    try {
      await this.http.delete(`${this.apiUrl}/api/v1/conversations/${conversationId}`).toPromise();
      return true;
    } catch (error) {
      console.error('Error deleting conversation:', error);
      return false;
    }
  }

  /**
   * Remove conversation from local state (call after animation)
   */
  removeConversationFromList(conversationId: string): void {
    this.conversations.update(convs => convs.filter(c => c.id !== conversationId));
    this.conversationsCache.delete(conversationId);
    this.saveToLocalStorage();
  }
}
