import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { SignalRService } from './signalr.service';

export interface SendMessageRequest {
  message: string;
  conversationId?: string | null;
  file?: File | null;
  filters?: string[];
}

export interface ChatbotRequest {
  prompt: string;
  connectionId: string;
  conversationId: string;
  fileBase64: string;
  fileName: string;
}

export interface ChatbotResponse {
  isSucceed: boolean;
  systemMessage?: string;
  resultData?: {
    conversationId?: string;
    messageId?: string;
    htmlMessage?: string;
    summary?: string;
    suggestions?: string[];
  };
  message?: string;
}

export interface ChatMessage {
  id: string;
  content: string;
  isUser: boolean;
  timestamp: string;
  suggestions?: string[];
}

// File upload related
export interface AttachedFile {
  name: string;
  size: number;
  base64: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private signalRService = inject(SignalRService);
  private apiUrl = environment.apiUrl;

  // State
  conversationId = signal<string | null>(null);
  currentAction = signal<string>('');
  isLoading = signal(false);
  
  // File handling
  selectedFile = signal<File | null>(null);
  attachedFile = signal<AttachedFile | null>(null);
  
  // Supported file types
  readonly maxFileSize = 100 * 1024 * 1024; // 100MB
  readonly supportedExtensions = ['.xlsx', '.xls', '.csv', '.pdf', '.docx', '.doc', '.txt', '.pptx', '.ppt'];
  readonly supportedMimeTypes = [
    'application/vnd.ms-excel',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'text/csv',
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'text/plain',
    'application/vnd.ms-powerpoint',
    'application/vnd.openxmlformats-officedocument.presentationml.presentation'
  ];

  /**
   * Send message to chatbot API (report.js sendReportPrompt'tan taşındı)
   * SignalR üzerinden yanıt alınacak
   */
  async sendChatbotMessage(prompt: string): Promise<ChatbotResponse> {
    // SignalR bağlantı kontrolü
    if (!this.signalRService.isConnected()) {
      throw new Error('SignalR bağlantısı aktif değil. Lütfen bağlantının kurulmasını bekleyin.');
    }

    // Connection ID kontrolü
    const connectionId = this.signalRService.connectionId();
    if (!connectionId) {
      throw new Error('Connection ID bulunamadı. Lütfen sayfayı yenileyin.');
    }

    this.isLoading.set(true);

    let fileBase64 = '';
    let fileName = '';

    // Dosya varsa Base64'e çevir
    const file = this.selectedFile();
    if (file) {
      try {
        fileBase64 = await this.fileToBase64(file);
        fileName = file.name;

        // Dosya bilgisini attach olarak sakla
        this.attachedFile.set({
          name: fileName,
          size: file.size,
          base64: fileBase64
        });

        console.log(`Dosya attach edildi: ${fileName}`);
      } catch (error: any) {
        console.error('File conversion error:', error);
        this.isLoading.set(false);
        throw new Error(`Dosya dönüştürme hatası: ${error.message}`);
      }
    }

    const request: ChatbotRequest = {
      prompt,
      connectionId,
      conversationId: this.conversationId() || '',
      fileBase64,
      fileName
    };

    try {
      const response = await this.http.post<ChatbotResponse>(
        `${this.apiUrl}/api/v1/chatbot`,
        request
      ).toPromise();

      if (!response) {
        throw new Error('Boş yanıt alındı');
      }

      // Başarılı yanıt
      this.currentAction.set(response.systemMessage || '');
      
      if (response.isSucceed && response.resultData?.conversationId) {
        this.conversationId.set(response.resultData.conversationId);
      }

      console.log('ConversationId:', this.conversationId());
      
      return response;

    } catch (error: any) {
      console.error('Fetch Error:', error);
      throw error;
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Dosyayı Base64'e çevir
   */
  async fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = () => {
        try {
          const result = reader.result as string;
          // data:application/...;base64, kısmını kaldır
          const base64 = result.split(',')[1] || result;
          resolve(base64);
        } catch (error) {
          reject(error);
        }
      };

      reader.onerror = () => {
        reject(new Error('Dosya okuma hatası'));
      };

      reader.readAsDataURL(file);
    });
  }

  /**
   * Dosya validasyonu
   */
  validateFile(file: File): { valid: boolean; error?: string } {
    // Dosya türü kontrolü
    const hasValidExtension = this.supportedExtensions.some(ext =>
      file.name.toLowerCase().endsWith(ext)
    );

    const hasValidMimeType = this.supportedMimeTypes.includes(file.type) || hasValidExtension;

    if (!hasValidMimeType) {
      return {
        valid: false,
        error: 'Desteklenmeyen dosya türü. Lütfen Excel, PDF, CSV, Word, TXT veya PowerPoint dosyası seçin.'
      };
    }

    // Dosya boyutu kontrolü
    if (file.size > this.maxFileSize) {
      return {
        valid: false,
        error: `Dosya çok büyük. Maximum ${(this.maxFileSize / 1024 / 1024).toFixed(0)}MB olmalıdır.`
      };
    }

    return { valid: true };
  }

  /**
   * Dosya seç
   */
  selectFile(file: File): boolean {
    const validation = this.validateFile(file);
    if (!validation.valid) {
      console.error(validation.error);
      return false;
    }

    this.selectedFile.set(file);
    return true;
  }

  /**
   * Seçili dosyayı temizle
   */
  clearSelectedFile(): void {
    this.selectedFile.set(null);
    this.attachedFile.set(null);
  }

  /**
   * Yeni sohbet başlat
   */
  clearChat(): void {
    this.conversationId.set(null);
    this.currentAction.set('');
    this.selectedFile.set(null);
    this.attachedFile.set(null);
  }

  // Legacy methods (geriye uyumluluk için)
  
  // Send message via HTTP (SignalR will handle the response)
  async sendMessage(request: SendMessageRequest): Promise<void> {
    // sendChatbotMessage'ı kullan
    await this.sendChatbotMessage(request.message);
  }

  // Load conversation messages
  loadConversation(conversationId: string): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(`${this.apiUrl}/api/v1/conversations/${conversationId}/messages`);
  }

  // Get conversation by ID
  getConversation(conversationId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/api/v1/conversations/${conversationId}`);
  }

  // Delete conversation
  deleteConversation(conversationId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/v1/conversations/${conversationId}`);
  }

  // Upload file
  uploadFile(file: File): Observable<{ fileId: string; fileName: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ fileId: string; fileName: string }>(`${this.apiUrl}/api/v1/files/upload`, formData);
  }
}
