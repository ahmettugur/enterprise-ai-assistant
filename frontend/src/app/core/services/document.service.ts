import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpEventType, HttpEvent } from '@angular/common/http';
import { Observable, map, tap, catchError, of, Subject, filter } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { 
  DocumentDisplayInfo,
  DocumentDisplayInfoList,
  DocumentDisplayInfoSelect,
  CreateDocumentDisplayInfoRequest,
  UpdateDocumentDisplayInfoRequest,
  DocumentUploadData,
  DocumentType,
  UploadProgress,
  ApiResult 
} from '../models/document.models';

/**
 * Döküman Servisi
 * document-manager.js'nin Angular karşılığı
 */
@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly apiUrl = `${environment.apiUrl}/api/v1/document-display-info`;

  // State signals
  readonly documents = signal<DocumentDisplayInfoList[]>([]);
  readonly isLoading = signal(false);
  readonly uploadProgress = signal<UploadProgress>({
    percent: 0,
    status: 'idle',
    message: ''
  });

  // Filtered documents (computed)
  private searchTerm = signal('');
  private categoryFilter = signal('');

  readonly filteredDocuments = computed(() => {
    let docs = this.documents();
    const search = this.searchTerm().toLowerCase();
    const category = this.categoryFilter();

    if (search) {
      docs = docs.filter(doc => 
        doc.displayName.toLowerCase().includes(search) ||
        doc.fileName.toLowerCase().includes(search)
      );
    }

    if (category) {
      docs = docs.filter(doc => doc.categoryId === category);
    }

    return docs;
  });

  // ==================== API CALLS ====================

  /**
   * Kullanıcı rolüne göre dökümanları getir
   * - Admin: userId null olan (genel) dökümanları getirir
   * - Normal kullanıcı: kendi dökümanlarını getirir
   */
  getAll(includeInactive = false): Observable<DocumentDisplayInfoList[]> {
    this.isLoading.set(true);
    
    const isAdmin = this.authService.isAdmin();
    const currentUser = this.authService.currentUser();
    const url = `${this.apiUrl}?includeInactive=${includeInactive}`;
    
    console.log('[DocumentService] Fetching documents, isAdmin:', isAdmin, 'userId:', currentUser?.id);
    
    return this.http.get<ApiResult<DocumentDisplayInfoList[]>>(url).pipe(
      map(result => {
        console.log('[DocumentService] API Response:', result);
        if (result.isSucceed && result.resultData) {
          let docs = result.resultData;
          
          // Admin için: userId null olan (genel) dökümanları göster
          // Normal kullanıcı için: kendi dökümanlarını göster
          if (isAdmin) {
            docs = docs.filter(doc => doc.userId === null || doc.userId === undefined);
            console.log('[DocumentService] Admin - filtered to null userId docs:', docs.length);
          } else if (currentUser?.id) {
            docs = docs.filter(doc => doc.userId === currentUser.id);
            console.log('[DocumentService] User - filtered to own docs:', docs.length);
          }
          
          this.documents.set(docs);
          return docs;
        }
        return [];
      }),
      tap(() => this.isLoading.set(false)),
      catchError(error => {
        console.error('[DocumentService] Error loading documents:', error);
        this.isLoading.set(false);
        return of([]);
      })
    );
  }

  /**
   * Kullanıcıya göre dökümanları getir
   */
  getAllByUserId(userId: string, includeInactive = false): Observable<DocumentDisplayInfoList[]> {
    this.isLoading.set(true);
    
    return this.http.get<ApiResult<DocumentDisplayInfoList[]>>(
      `${this.apiUrl}/by-user/${userId}?includeInactive=${includeInactive}`
    ).pipe(
      map(result => {
        if (result.isSucceed && result.resultData) {
          this.documents.set(result.resultData);
          return result.resultData;
        }
        return [];
      }),
      tap(() => this.isLoading.set(false)),
      catchError(error => {
        console.error('[DocumentService] Error loading documents by user:', error);
        this.isLoading.set(false);
        return of([]);
      })
    );
  }

  /**
   * Kategoriye göre dökümanları getir
   */
  getByCategory(categoryId: string, includeInactive = false): Observable<DocumentDisplayInfoList[]> {
    this.isLoading.set(true);
    
    return this.http.get<ApiResult<DocumentDisplayInfoList[]>>(
      `${this.apiUrl}/by-category/${categoryId}?includeInactive=${includeInactive}`
    ).pipe(
      map(result => {
        if (result.isSucceed && result.resultData) {
          return result.resultData;
        }
        return [];
      }),
      tap(() => this.isLoading.set(false)),
      catchError(error => {
        console.error('[DocumentService] Error loading documents by category:', error);
        this.isLoading.set(false);
        return of([]);
      })
    );
  }

  /**
   * Select/Dropdown için dökümanları getir
   */
  getAllForSelect(): Observable<DocumentDisplayInfoSelect[]> {
    return this.http.get<ApiResult<DocumentDisplayInfoSelect[]>>(
      `${this.apiUrl}/select`
    ).pipe(
      map(result => result.isSucceed && result.resultData ? result.resultData : []),
      catchError(error => {
        console.error('[DocumentService] Error loading documents for select:', error);
        return of([]);
      })
    );
  }

  /**
   * ID'ye göre döküman getir
   */
  getById(id: string): Observable<DocumentDisplayInfo | null> {
    return this.http.get<ApiResult<DocumentDisplayInfo>>(
      `${this.apiUrl}/${id}`
    ).pipe(
      map(result => result.isSucceed && result.resultData ? result.resultData : null),
      catchError(error => {
        console.error('[DocumentService] Error loading document:', error);
        return of(null);
      })
    );
  }

  /**
   * Dosya adına göre döküman getir
   */
  getByFileName(fileName: string): Observable<DocumentDisplayInfo | null> {
    return this.http.get<ApiResult<DocumentDisplayInfo>>(
      `${this.apiUrl}/by-filename/${fileName}`
    ).pipe(
      map(result => result.isSucceed && result.resultData ? result.resultData : null),
      catchError(error => {
        console.error('[DocumentService] Error loading document by filename:', error);
        return of(null);
      })
    );
  }

  /**
   * Döküman yükle (FormData ile)
   */
  upload(data: DocumentUploadData): Observable<ApiResult<DocumentDisplayInfo>> {
    const formData = new FormData();
    formData.append('File', data.file);
    formData.append('DisplayName', data.displayName);
    formData.append('DocumentType', data.documentType.toString());
    
    if (data.categoryId) {
      formData.append('CategoryId', data.categoryId);
    }
    if (data.description) {
      formData.append('Description', data.description);
    }
    if (data.keywords) {
      formData.append('Keywords', data.keywords);
    }

    // Progress tracking başlat
    this.uploadProgress.set({
      percent: 0,
      status: 'uploading',
      message: 'Dosya yükleniyor...'
    });

    return this.http.post<ApiResult<DocumentDisplayInfo>>(
      `${this.apiUrl}/upload`,
      formData,
      {
        reportProgress: true,
        observe: 'events'
      }
    ).pipe(
      tap((event: HttpEvent<ApiResult<DocumentDisplayInfo>>) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          const percent = Math.round((event.loaded / event.total) * 100);
          let status: UploadProgress['status'] = 'uploading';
          let message = 'Dosya yükleniyor...';

          if (percent > 30) {
            status = 'processing';
            message = 'Döküman işleniyor...';
          }
          if (percent > 60) {
            status = 'embedding';
            message = 'Embedding oluşturuluyor...';
          }
          if (percent >= 100) {
            status = 'completed';
            message = 'Tamamlandı!';
          }

          this.uploadProgress.set({ percent, status, message });
        }
      }),
      filter((event): event is HttpEvent<ApiResult<DocumentDisplayInfo>> & { type: HttpEventType.Response } => 
        event.type === HttpEventType.Response
      ),
      map(event => {
        const result = event.body as ApiResult<DocumentDisplayInfo>;
        if (result?.isSucceed && result.resultData) {
          // Listeye ekle
          const current = this.documents();
          const newDoc: DocumentDisplayInfoList = {
            id: result.resultData.id,
            fileName: result.resultData.fileName,
            documentType: result.resultData.documentType,
            displayName: result.resultData.displayName,
            description: result.resultData.description,
            categoryId: result.resultData.categoryId,
            categoryName: result.resultData.categoryName,
            userId: result.resultData.userId,
            isActive: result.resultData.isActive,
            hasEmbeddings: result.resultData.hasEmbeddings,
            chunkCount: result.resultData.chunkCount,
            createdAt: result.resultData.createdAt
          };
          this.documents.set([newDoc, ...current]);
          
          this.uploadProgress.set({
            percent: 100,
            status: 'completed',
            message: 'Tamamlandı!'
          });
        }
        return result;
      }),
      catchError(error => {
        console.error('[DocumentService] Error uploading document:', error);
        this.uploadProgress.set({
          percent: 0,
          status: 'error',
          message: 'Yükleme hatası!'
        });
        return of({
          isSucceed: false,
          userMessage: 'Döküman yüklenirken bir hata oluştu.'
        } as ApiResult<DocumentDisplayInfo>);
      })
    );
  }

  /**
   * Döküman güncelle (sadece metadata)
   */
  update(id: string, request: UpdateDocumentDisplayInfoRequest): Observable<ApiResult<DocumentDisplayInfo>> {
    return this.http.put<ApiResult<DocumentDisplayInfo>>(`${this.apiUrl}/${id}`, request).pipe(
      tap(result => {
        if (result.isSucceed && result.resultData) {
          // Listeyi güncelle
          const current = this.documents();
          const index = current.findIndex(d => d.id === id);
          if (index !== -1) {
            const updated = [...current];
            updated[index] = {
              ...updated[index],
              displayName: result.resultData.displayName,
              description: result.resultData.description,
              categoryId: result.resultData.categoryId,
              categoryName: result.resultData.categoryName,
              isActive: result.resultData.isActive
            };
            this.documents.set(updated);
          }
        }
      })
    );
  }

  /**
   * Döküman sil
   */
  delete(id: string): Observable<ApiResult<boolean>> {
    return this.http.delete<ApiResult<boolean>>(`${this.apiUrl}/${id}`).pipe(
      tap(result => {
        if (result.isSucceed) {
          // Listeden kaldır
          const current = this.documents();
          this.documents.set(current.filter(d => d.id !== id));
        }
      })
    );
  }

  // ==================== FILTERING ====================

  /**
   * Arama terimini ayarla
   */
  setSearchTerm(term: string): void {
    this.searchTerm.set(term);
  }

  /**
   * Kategori filtresini ayarla
   */
  setCategoryFilter(categoryId: string): void {
    this.categoryFilter.set(categoryId);
  }

  /**
   * Filtreleri temizle
   */
  clearFilters(): void {
    this.searchTerm.set('');
    this.categoryFilter.set('');
  }

  // ==================== VALIDATION ====================

  /**
   * Dosya validasyonu
   */
  validateFile(file: File): { valid: boolean; error?: string } {
    const allowedTypes = ['.pdf', '.txt', '.docx', '.doc', '.json'];
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    
    if (!allowedTypes.includes(ext)) {
      return {
        valid: false,
        error: 'Desteklenmeyen dosya türü. PDF, TXT, DOCX, DOC veya JSON yükleyin.'
      };
    }
    
    const maxSize = 50 * 1024 * 1024; // 50MB
    if (file.size > maxSize) {
      return {
        valid: false,
        error: 'Dosya boyutu 50MB\'dan büyük olamaz.'
      };
    }
    
    return { valid: true };
  }

  /**
   * Upload progress'i sıfırla
   */
  resetUploadProgress(): void {
    this.uploadProgress.set({
      percent: 0,
      status: 'idle',
      message: ''
    });
  }

  // ==================== HELPERS ====================

  /**
   * Döküman tipi label'ı
   */
  getDocumentTypeLabel(type: DocumentType): string {
    return type === DocumentType.Document ? 'Döküman' : 'Soru-Cevap';
  }

  /**
   * Döküman tipi CSS class'ı
   */
  getDocumentTypeClass(type: DocumentType): string {
    return type === DocumentType.Document ? 'type-document' : 'type-qa';
  }

  /**
   * Durum label'ı
   */
  getStatusLabel(doc: DocumentDisplayInfoList): string {
    return doc.hasEmbeddings ? `Aktif (${doc.chunkCount} chunk)` : 'İşlenmedi';
  }

  /**
   * Durum CSS class'ı
   */
  getStatusClass(doc: DocumentDisplayInfoList): string {
    return doc.hasEmbeddings ? 'status-active' : 'status-pending';
  }
}
