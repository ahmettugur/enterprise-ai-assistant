import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  DocumentCategory, 
  DocumentCategorySelect,
  CreateDocumentCategoryRequest, 
  UpdateDocumentCategoryRequest,
  ApiResult 
} from '../models/document.models';

/**
 * Döküman Kategori Servisi
 * category-manager.js'nin Angular karşılığı
 */
@Injectable({
  providedIn: 'root'
})
export class DocumentCategoryService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/v1/document-categories`;

  // State signals
  readonly categories = signal<DocumentCategory[]>([]);
  readonly selectedCategories = signal<DocumentCategory[]>([]);
  readonly isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Tüm kategorileri getir
   */
  getAll(includeInactive = false): Observable<DocumentCategory[]> {
    this.isLoading.set(true);
    
    return this.http.get<ApiResult<DocumentCategory[]>>(
      `${this.apiUrl}?includeInactive=${includeInactive}`
    ).pipe(
      map(result => {
        if (result.isSucceed && result.resultData) {
          // İsme göre sırala
          const sorted = result.resultData.sort((a, b) => 
            (a.displayName || '').localeCompare(b.displayName || '', 'tr')
          );
          this.categories.set(sorted);
          return sorted;
        }
        return [];
      }),
      tap(() => this.isLoading.set(false)),
      catchError(error => {
        console.error('[DocumentCategoryService] Error loading categories:', error);
        this.isLoading.set(false);
        return of([]);
      })
    );
  }

  /**
   * Kullanıcıya göre kategorileri getir
   */
  getAllByUserId(userId: string, includeInactive = false): Observable<DocumentCategory[]> {
    this.isLoading.set(true);
    
    return this.http.get<ApiResult<DocumentCategory[]>>(
      `${this.apiUrl}/by-user/${userId}?includeInactive=${includeInactive}`
    ).pipe(
      map(result => {
        if (result.isSucceed && result.resultData) {
          const sorted = result.resultData.sort((a, b) => 
            (a.displayName || '').localeCompare(b.displayName || '', 'tr')
          );
          this.categories.set(sorted);
          return sorted;
        }
        return [];
      }),
      tap(() => this.isLoading.set(false)),
      catchError(error => {
        console.error('[DocumentCategoryService] Error loading categories by user:', error);
        this.isLoading.set(false);
        return of([]);
      })
    );
  }

  /**
   * Select/Dropdown için kategorileri getir
   */
  getAllForSelect(): Observable<DocumentCategorySelect[]> {
    return this.http.get<ApiResult<DocumentCategorySelect[]>>(
      `${this.apiUrl}/select`
    ).pipe(
      map(result => result.isSucceed && result.resultData ? result.resultData : []),
      catchError(error => {
        console.error('[DocumentCategoryService] Error loading categories for select:', error);
        return of([]);
      })
    );
  }

  /**
   * ID'ye göre kategori getir
   */
  getById(id: string): Observable<DocumentCategory | null> {
    return this.http.get<ApiResult<DocumentCategory>>(
      `${this.apiUrl}/${id}`
    ).pipe(
      map(result => result.isSucceed && result.resultData ? result.resultData : null),
      catchError(error => {
        console.error('[DocumentCategoryService] Error loading category:', error);
        return of(null);
      })
    );
  }

  /**
   * Yeni kategori oluştur
   */
  create(request: CreateDocumentCategoryRequest): Observable<ApiResult<DocumentCategory>> {
    return this.http.post<ApiResult<DocumentCategory>>(this.apiUrl, request).pipe(
      tap(result => {
        if (result.isSucceed && result.resultData) {
          // Listeye ekle ve sırala
          const current = this.categories();
          const updated = [...current, result.resultData].sort((a, b) => 
            (a.displayName || '').localeCompare(b.displayName || '', 'tr')
          );
          this.categories.set(updated);
        }
      })
    );
  }

  /**
   * Kategori güncelle
   */
  update(id: string, request: UpdateDocumentCategoryRequest): Observable<ApiResult<DocumentCategory>> {
    return this.http.put<ApiResult<DocumentCategory>>(`${this.apiUrl}/${id}`, request).pipe(
      tap(result => {
        if (result.isSucceed && result.resultData) {
          // Listeyi güncelle
          const current = this.categories();
          const index = current.findIndex(c => c.id === id);
          if (index !== -1) {
            const updated = [...current];
            updated[index] = result.resultData;
            this.categories.set(updated);
          }
        }
      })
    );
  }

  /**
   * Kategori sil
   */
  delete(id: string): Observable<ApiResult<boolean>> {
    return this.http.delete<ApiResult<boolean>>(`${this.apiUrl}/${id}`).pipe(
      tap(result => {
        if (result.isSucceed) {
          // Listeden kaldır
          const current = this.categories();
          this.categories.set(current.filter(c => c.id !== id));
        }
      })
    );
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Kategori seç
   */
  selectCategory(category: DocumentCategory): boolean {
    const current = this.selectedCategories();
    
    // Zaten seçili mi kontrol et
    if (current.some(c => c.id === category.id)) {
      console.log('[DocumentCategoryService] Category already selected:', category.displayName);
      return false;
    }
    
    this.selectedCategories.set([...current, category]);
    console.log('[DocumentCategoryService] Category selected:', category.displayName);
    return true;
  }

  /**
   * Kategori seçimini kaldır (index ile)
   */
  deselectCategoryByIndex(index: number): DocumentCategory | null {
    const current = this.selectedCategories();
    
    if (index < 0 || index >= current.length) {
      return null;
    }
    
    const removed = current[index];
    this.selectedCategories.set(current.filter((_, i) => i !== index));
    console.log('[DocumentCategoryService] Category deselected:', removed.displayName);
    return removed;
  }

  /**
   * Kategori seçimini kaldır (id ile)
   */
  deselectCategoryById(id: string): DocumentCategory | null {
    const current = this.selectedCategories();
    const category = current.find(c => c.id === id);
    
    if (!category) {
      return null;
    }
    
    this.selectedCategories.set(current.filter(c => c.id !== id));
    console.log('[DocumentCategoryService] Category deselected:', category.displayName);
    return category;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedCategories().length;
    this.selectedCategories.set([]);
    console.log(`[DocumentCategoryService] Cleared ${count} selections`);
  }

  // ==================== HELPERS ====================

  /**
   * ID validasyonu (slug format)
   */
  validateCategoryId(id: string): boolean {
    return /^[a-z0-9-]+$/.test(id);
  }

  /**
   * Özet bilgi
   */
  getSummary(): { totalCategories: number; selectedCategories: number } {
    return {
      totalCategories: this.categories().length,
      selectedCategories: this.selectedCategories().length
    };
  }
}
