import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface Category {
  id: number;
  name: string;
}

/**
 * CategoryService - Kategori/Departman verilerini yöneten servis
 * category-manager.js'in Angular versiyonu
 */
@Injectable({
  providedIn: 'root'
})
export class CategoryService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  categories = signal<Category[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedCategories = signal<Category[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Kategorileri API'den yükle
   */
  async loadCategories(): Promise<Category[]> {
    if (this.isLoading()) return this.categories();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/categories`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const categories: Category[] = result.resultData.map(cat => ({
          id: parseInt(cat.categoryId) || 0,
          name: cat.categoryName
        }));

        // İsme göre sırala
        categories.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.categories.set(categories);
        console.log(`[CategoryService] Loaded ${categories.length} categories`);
        return categories;
      }

      return [];
    } catch (error) {
      console.error('[CategoryService] Error loading categories:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Kategori seç
   */
  selectCategory(category: Category): boolean {
    if (this.selectedCategories().some(c => c.id === category.id)) {
      console.log('[CategoryService] Category already selected:', category.name);
      return false;
    }

    this.selectedCategories.update(categories => [...categories, category]);
    console.log('[CategoryService] Category selected:', category.name);
    return true;
  }

  /**
   * Kategori seçimini kaldır (index ile)
   */
  deselectCategoryByIndex(index: number): Category | null {
    const categories = this.selectedCategories();
    if (index < 0 || index >= categories.length) {
      return null;
    }

    const removed = categories[index];
    this.selectedCategories.update(c => c.filter((_, i) => i !== index));
    console.log('[CategoryService] Category deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedCategories().length;
    this.selectedCategories.set([]);
    console.log(`[CategoryService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Kategori seçin...';
  }

  getTitle(): string {
    return '<i class="fas fa-layer-group"></i> Kategori Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Kategori ara
   */
  searchCategories(term: string): Category[] {
    if (!term.trim()) {
      return this.categories();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.categories().filter(c =>
      (c.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı kategori ara
   */
  getPagedCategories(term: string, page: number, pageSize: number = 50): { items: Category[]; hasMore: boolean } {
    const filtered = this.searchCategories(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}
