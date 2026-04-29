import { Injectable, signal, computed, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { Subject } from 'rxjs';

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
  isStreaming?: boolean;
  suggestions?: string[];
  htmlMessage?: string;
}

export interface ReceivedMessage {
  systemMessage: string;
  resultData: {
    htmlMessage?: string;
    summary?: string;
    messageId?: string;
    conversationId?: string;
    suggestions?: string[];
    data?: any[];
    isSuccess?: boolean;  // Template menus have false, real AI responses have true
  };
  isSucceed: boolean;
}

export interface StreamingMessage {
  userMessage?: string;
  resultData?: {
    htmlMessage?: string;
    data?: Array<{
      content: string;
      metadata?: {
        filePath?: string;
        fileName?: string;
      };
    }>;
  };
}

export interface AnalysisProgress {
  stage: 'Chunking' | 'ChunkAnalysis' | 'Aggregation' | 'FinalAnalysis';
  currentChunk: number;
  totalChunks: number;
  completedChunks: number;
  inProgressChunks: number;
  percentComplete: number;
  estimatedSecondsRemaining: number;
  message?: string;
}

export interface ReActStep {
  stepNumber: number;
  stepType: 'thought' | 'action' | 'observation';
  content: string;
  action?: string;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;

  // Ping mekanizması
  private pingInterval = 60000; // 60 saniye
  private pingTimer: any = null;
  private lastPingTime: Date | null = null;
  private pingFailureCount = 0;
  private maxPingFailures = 5;
  private isLongRunningOperation = false;

  // Reconnect
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private reconnectInterval = 3000;
  private isConnecting = false;

  // Streaming
  private fullStreamingMessage = '';
  private streamingContainerCounter = 0;

  // State signals
  private connectionStateSignal = signal<ConnectionState>('disconnected');
  private connectionIdSignal = signal<string | null>(null);
  private messagesSignal = signal<ChatMessage[]>([]);
  private currentStreamingMessageSignal = signal<string>('');
  private isStreamingSignal = signal<boolean>(false);
  private lastMessageSignal = signal<any>(null);
  private streamingMessageSignal = signal<string>('');
  private isConnectedSignal = signal<boolean>(false);
  private loadingMessageSignal = signal<string>('');
  private errorMessageSignal = signal<string | null>(null);
  private analysisProgressSignal = signal<AnalysisProgress | null>(null);
  private reactStepsSignal = signal<ReActStep[]>([]);

  // RxJS Subjects for event broadcasting (chat ve history için)
  private messageReceived$ = new Subject<ReceivedMessage>();
  private streamingReceived$ = new Subject<StreamingMessage>();
  private loadingReceived$ = new Subject<string>();
  private errorReceived$ = new Subject<string>();
  private progressReceived$ = new Subject<AnalysisProgress>();
  private reactStepReceived$ = new Subject<ReActStep>();

  // Public computed signals
  connectionState = computed(() => this.connectionStateSignal());
  connectionId = computed(() => this.connectionIdSignal());
  messages = computed(() => this.messagesSignal());
  currentStreamingMessage = computed(() => this.currentStreamingMessageSignal());
  isStreaming = computed(() => this.isStreamingSignal());
  lastMessage = computed(() => this.lastMessageSignal());
  streamingMessage = computed(() => this.streamingMessageSignal());
  isConnected = computed(() => this.isConnectedSignal());
  loadingMessage = computed(() => this.loadingMessageSignal());
  errorMessage = computed(() => this.errorMessageSignal());
  analysisProgress = computed(() => this.analysisProgressSignal());
  reactSteps = computed(() => this.reactStepsSignal());

  // Observable streams for components to subscribe
  onMessageReceived = this.messageReceived$.asObservable();
  onStreamingReceived = this.streamingReceived$.asObservable();
  onLoadingReceived = this.loadingReceived$.asObservable();
  onErrorReceived = this.errorReceived$.asObservable();
  onProgressReceived = this.progressReceived$.asObservable();
  onReActStepReceived = this.reactStepReceived$.asObservable();

  constructor(private authService: AuthService) { }

  async connect(): Promise<void> {
    return this.startConnection();
  }

  async disconnect(): Promise<void> {
    return this.stopConnection();
  }

  async startConnection(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connectionStateSignal.set('connecting');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/ai-hub`, {
        accessTokenFactory: () => this.authService.getToken() || '',
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();

    try {
      this.isConnecting = true;
      await this.hubConnection.start();
      this.connectionStateSignal.set('connected');
      this.connectionIdSignal.set(this.hubConnection.connectionId);
      this.isConnectedSignal.set(true);
      this.reconnectAttempts = 0;

      // Connection ID'yi kaydet
      if (this.hubConnection.connectionId) {
        this.saveConnectionId(this.hubConnection.connectionId);
      }

      // Ping mekanizmasını başlat
      this.startPing();

      console.log('SignalR Connected:', this.hubConnection.connectionId);
    } catch (error) {
      console.error('SignalR Connection Error:', error);
      this.connectionStateSignal.set('disconnected');
      this.isConnectedSignal.set(false);
      this.reconnectAttempts++;
      throw error;
    } finally {
      this.isConnecting = false;
    }
  }

  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Önce mevcut handler'ları temizle (duplicate önlemek için)
    this.hubConnection.off('ReceiveMessage');
    this.hubConnection.off('ReceiveLoadingMessage');
    this.hubConnection.off('ReceiveStreamingMessage');
    this.hubConnection.off('ReceiveErrorMessage');
    this.hubConnection.off('OnProgress');
    this.hubConnection.off('ReceiveReActStep');
    this.hubConnection.off('Pong');
    this.hubConnection.off('Error');

    console.log('Setting up SignalR event handlers...');

    // ReceiveMessage - AI'dan tam mesaj alındığında
    this.hubConnection.on('ReceiveMessage', (responseMessage: ReceivedMessage) => {
      console.log('ReceiveMessage event triggered:', responseMessage);
      this.handleReceiveMessage(responseMessage);
    });

    // ReceiveLoadingMessage - Yükleme durumu mesajı
    this.hubConnection.on('ReceiveLoadingMessage', (loadingMessage: string) => {
      console.log('ReceiveLoadingMessage event triggered:', loadingMessage);
      this.handleLoadingMessage(loadingMessage);
    });

    // ReceiveStreamingMessage - Streaming mesaj parçaları
    this.hubConnection.on('ReceiveStreamingMessage', (streamingMessage: StreamingMessage) => {
      console.log('ReceiveStreamingMessage event triggered');
      this.handleReceiveStreamingMessage(streamingMessage);
    });

    // ReceiveErrorMessage - Hata mesajı
    this.hubConnection.on('ReceiveErrorMessage', (errorData: any) => {
      console.log('ReceiveErrorMessage event triggered');
      const errorMsg = errorData?.error || 'Bir hata oluştu';
      this.showErrorMessage(errorMsg);
    });

    // OnProgress - Chunk analizi ilerleme bildirimi
    this.hubConnection.on('OnProgress', (progress: AnalysisProgress) => {
      console.log('OnProgress event triggered:', progress);
      this.handleProgressMessage(progress);
    });

    // ReceiveReActStep - ReAct pattern düşünce adımları
    this.hubConnection.on('ReceiveReActStep', (step: ReActStep) => {
      console.log('ReceiveReActStep event triggered:', step);
      this.handleReActStep(step);
    });

    // Ping-Pong mekanizması
    this.hubConnection.on('Pong', (connectionId: string) => {
      const now = new Date();
      const latency = this.lastPingTime ? now.getTime() - this.lastPingTime.getTime() : 0;
      console.log(`Pong received from: ${connectionId}, latency: ${latency}ms`);
      this.pingFailureCount = 0;
    });

    // Hata yönetimi
    this.hubConnection.on('Error', (errorMessage: string) => {
      console.error('Hub error received:', errorMessage);
      this.showErrorMessage(errorMessage);
    });

    // Reconnection events
    this.hubConnection.onreconnecting((error) => {
      console.log('SignalR reconnecting:', error);
      this.connectionStateSignal.set('reconnecting');
      this.isConnectedSignal.set(false);
      this.stopPing();
    });

    this.hubConnection.onreconnected((connectionId: string | undefined) => {
      console.log('SignalR reconnected with ID:', connectionId);
      this.connectionStateSignal.set('connected');
      this.connectionIdSignal.set(connectionId || null);
      this.isConnectedSignal.set(true);
      this.reconnectAttempts = 0;
      this.saveConnectionId(connectionId || '');
      this.startPing();
    });

    this.hubConnection.onclose((error) => {
      console.log('SignalR connection closed:', error);
      this.connectionStateSignal.set('disconnected');
      this.connectionIdSignal.set(null);
      this.isConnectedSignal.set(false);
      this.stopPing();
    });

    console.log('SignalR event handlers registered successfully');
  }

  // ========== MESSAGE HANDLERS (report.js'den taşındı) ==========

  /**
   * ReceiveMessage handler - Tam mesaj alındığında
   * report.js handleReceiveMessage fonksiyonu ile birebir aynı
   * NOT: Asıl işlem chat.ts'de yapılıyor, burada sadece broadcast ediyoruz
   */
  private handleReceiveMessage(responseMessage: ReceivedMessage): void {
    console.log('ReceiveMessage called:', responseMessage);

    // report.js ile aynı: Uzun işlem bittiğini işaretle (ping'i yeniden başlat)
    this.isLongRunningOperation = false;
    console.log('Long running operation completed - ping resumed');

    // Streaming state'i temizle
    this.isStreamingSignal.set(false);
    this.currentStreamingMessageSignal.set('');
    this.fullStreamingMessage = '';

    // Signal güncelle (son mesaj referansı için)
    this.lastMessageSignal.set(responseMessage.resultData || responseMessage);

    // Subject ile broadcast et (chat.ts dinleyecek ve işleyecek)
    // NOT: Burada messagesSignal'e ekleme yapmıyoruz, chat.ts yapacak (report.js gibi tek yerde işleme)
    this.messageReceived$.next(responseMessage);
  }

  /**
   * ReceiveLoadingMessage handler - Yükleme mesajı
   * report.js handleLoadingMessage fonksiyonu ile birebir aynı
   */
  private handleLoadingMessage(loadingMessage: string): void {
    console.log('Loading message received:', loadingMessage);

    // report.js ile aynı: Uzun işlem başladığını işaretle (ping'i duraklat)
    this.isLongRunningOperation = true;
    console.log('Long running operation started - ping paused');

    // Loading message signal güncelle
    this.loadingMessageSignal.set(loadingMessage);

    // Subject ile broadcast et
    this.loadingReceived$.next(loadingMessage);

    // report.js ile aynı: DOM'daki typing-message elementini güncelle
    setTimeout(() => {
      const typingMessage = document.getElementById('typing-message');
      if (typingMessage) {
        typingMessage.textContent = loadingMessage;
      }
    }, 100);
  }

  /**
   * OnProgress handler - Chunk analizi ilerleme bildirimi
   */
  private handleProgressMessage(progress: AnalysisProgress): void {
    console.log('Progress message received:', progress);

    // Progress signal güncelle
    this.analysisProgressSignal.set(progress);

    // Subject ile broadcast et
    this.progressReceived$.next(progress);

    // Loading message'ı progress ile güncelle
    if (progress.message) {
      this.loadingMessageSignal.set(progress.message);
    }
  }

  /**
   * ReceiveReActStep handler - ReAct pattern düşünce adımları
   */
  private handleReActStep(step: ReActStep): void {
    console.log('ReAct step received:', step);

    // Mevcut adımlara yeni adımı ekle
    const currentSteps = this.reactStepsSignal();
    this.reactStepsSignal.set([...currentSteps, step]);

    // Subject ile broadcast et
    this.reactStepReceived$.next(step);

    // THOUGHT adımında loading mesajını güncelle
    if (step.stepType === 'thought') {
      this.loadingMessageSignal.set(`🤔 ${step.content}`);
    } else if (step.stepType === 'observation') {
      this.loadingMessageSignal.set(`👁️ ${step.content}`);
    }
  }

  /**
   * ReAct adımlarını temizle - Yeni mesaj gönderildiğinde çağrılmalı
   */
  clearReActSteps(): void {
    this.reactStepsSignal.set([]);
  }

  /**
   * Progress HTML oluştur
   */
  private buildProgressHtml(progress: AnalysisProgress): string {
    const stageLabels: Record<string, string> = {
      'Chunking': 'Veri bölümleniyor...',
      'ChunkAnalysis': 'Veri analiz ediliyor...',
      'Aggregation': 'Sonuçlar birleştiriliyor...',
      'FinalAnalysis': 'Final analiz yapılıyor...'
    };

    const stageLabel = stageLabels[progress.stage] || progress.stage;
    const timeRemaining = progress.estimatedSecondsRemaining > 0
      ? `~${progress.estimatedSecondsRemaining} saniye`
      : '';

    return `
      <div class="flex items-center gap-3 p-3 bg-indigo-50 rounded-lg border border-indigo-200">
        <div class="flex-shrink-0">
          <svg class="animate-spin h-5 w-5 text-indigo-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </div>
        <div class="flex-1 min-w-0">
          <div class="flex justify-between items-center mb-1">
            <span class="text-sm font-medium text-indigo-700">${stageLabel}</span>
            <span class="text-sm text-indigo-600">${progress.percentComplete}%</span>
          </div>
          <div class="w-full bg-indigo-200 rounded-full h-2">
            <div class="bg-indigo-600 h-2 rounded-full transition-all duration-300" style="width: ${progress.percentComplete}%"></div>
          </div>
          <div class="flex justify-between items-center mt-1">
            <span class="text-xs text-indigo-500">Parça: ${progress.completedChunks}/${progress.totalChunks}</span>
            ${timeRemaining ? `<span class="text-xs text-indigo-500">${timeRemaining}</span>` : ''}
          </div>
        </div>
      </div>
    `;
  }

  /**
   * ReceiveStreamingMessage handler - Streaming mesaj parçaları
   * report.js handleReceiveStreamingMessage fonksiyonu
   */
  private handleReceiveStreamingMessage(streamingMessage: StreamingMessage): void {
    console.log('handleReceiveStreamingMessage called');

    this.isStreamingSignal.set(true);

    let content = '';

    if (streamingMessage.resultData) {
      if (Array.isArray(streamingMessage.resultData.data)) {
        // Array ise her item için döngü yap
        for (const item of streamingMessage.resultData.data) {
          content += item.content || '';
        }
      } else if (streamingMessage.resultData.htmlMessage) {
        // htmlMessage varsa kullan
        this.fullStreamingMessage += streamingMessage.resultData.htmlMessage;
        content = this.fullStreamingMessage;
      }
    } else if (streamingMessage.userMessage) {
      // userMessage varsa ekle
      this.fullStreamingMessage += streamingMessage.userMessage;
      content = this.fullStreamingMessage;
    }

    // Signal güncelle
    this.streamingMessageSignal.set(content);
    this.currentStreamingMessageSignal.set(content);

    // Subject ile broadcast et
    this.streamingReceived$.next(streamingMessage);
  }

  /**
   * Hata mesajı göster
   * report.js showErrorMessage fonksiyonu
   */
  showErrorMessage(message: string): void {
    console.error('SignalR Error:', message);

    // Signal güncelle
    this.errorMessageSignal.set(message);

    // Subject ile broadcast et
    this.errorReceived$.next(message);

    // 5 saniye sonra temizle
    setTimeout(() => {
      this.errorMessageSignal.set(null);
    }, 5000);
  }

  // ========== PING MECHANISM ==========

  private startPing(): void {
    this.stopPing();
    this.pingFailureCount = 0;

    this.pingTimer = setInterval(async () => {
      try {
        // Uzun işlem sırasında ping gönderme
        if (this.isLongRunningOperation) {
          console.log('Skipping ping during long running operation');
          return;
        }

        if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
          this.lastPingTime = new Date();
          await this.hubConnection.invoke('Ping');
          console.log('Ping sent at:', this.lastPingTime.toLocaleTimeString());
        }
      } catch (error) {
        console.error('Ping error:', error);
        this.handlePingFailure();
      }
    }, this.pingInterval);
  }

  private stopPing(): void {
    if (this.pingTimer) {
      clearInterval(this.pingTimer);
      this.pingTimer = null;
    }
    this.pingFailureCount = 0;
  }

  private handlePingFailure(): void {
    this.pingFailureCount++;
    console.warn(`Ping failure ${this.pingFailureCount}/${this.maxPingFailures}`);

    if (this.pingFailureCount >= this.maxPingFailures) {
      console.error('Max ping failures reached, attempting to reconnect...');
      this.stopPing();
      this.reconnect();
    }
  }

  // ========== CONNECTION ID MANAGEMENT ==========

  private saveConnectionId(connectionId: string): void {
    if (connectionId) {
      localStorage.setItem('signalr-connection-id', connectionId);
    }
  }

  private loadSavedConnectionId(): string | null {
    return localStorage.getItem('signalr-connection-id');
  }

  // ========== RECONNECT ==========

  async reconnect(): Promise<void> {
    if (this.isConnecting) {
      console.log('Already connecting...');
      return;
    }

    console.log('Reconnecting SignalR...');
    this.reconnectAttempts = 0;

    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
      } catch (e) {
        console.warn('Error stopping connection:', e);
      }
    }

    await this.startConnection();
  }

  // ========== CLEAR STATE ==========

  clearStreamingState(): void {
    this.fullStreamingMessage = '';
    this.currentStreamingMessageSignal.set('');
    this.isStreamingSignal.set(false);
    this.streamingMessageSignal.set('');
  }

  async sendMessage(message: string, conversationId?: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    // Add user message
    this.messagesSignal.update(msgs => [...msgs, {
      role: 'user',
      content: message,
      timestamp: new Date()
    }]);

    try {
      await this.hubConnection.invoke('SendMessage', message, conversationId);
    } catch (error) {
      console.error('Error sending message:', error);
      throw error;
    }
  }

  async sendReportPrompt(prompt: string, filters?: any): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('Not connected to hub');
    }

    // Add user message
    this.messagesSignal.update(msgs => [...msgs, {
      role: 'user',
      content: prompt,
      timestamp: new Date()
    }]);

    try {
      await this.hubConnection.invoke('SendReportPrompt', prompt, filters);
    } catch (error) {
      console.error('Error sending report prompt:', error);
      throw error;
    }
  }

  clearMessages(): void {
    this.messagesSignal.set([]);
    this.currentStreamingMessageSignal.set('');
    this.isStreamingSignal.set(false);
  }

  async stopConnection(): Promise<void> {
    // Ping'i durdur
    this.stopPing();

    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
      } catch (e) {
        console.warn('Error stopping connection:', e);
      }
      this.hubConnection = null;
      this.connectionStateSignal.set('disconnected');
      this.connectionIdSignal.set(null);
      this.isConnectedSignal.set(false);
    }
  }

  ngOnDestroy(): void {
    this.stopPing();
    this.stopConnection();
  }
}
