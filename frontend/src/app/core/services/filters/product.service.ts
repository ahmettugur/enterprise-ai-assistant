import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface Product {
  id: number | string;
  name: string;
  code?: string;
}

/**
 * ProductService - Ürün verilerini yöneten servis
 * product-manager.js'in Angular versiyonu
 */
@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  products = signal<Product[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedProducts = signal<Product[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Ürünleri API'den yükle
   */
  async loadProducts(): Promise<Product[]> {
    if (this.isLoading()) return this.products();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/products`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const products: Product[] = result.resultData.map(product => ({
          id: product.productNumber,
          name: product.productName,
          code: product.productNumber
        }));

        // İsme göre sırala
        products.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.products.set(products);
        console.log(`[ProductService] Loaded ${products.length} products`);
        return products;
      }

      return [];
    } catch (error) {
      console.error('[ProductService] Error loading products:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Ürün seç
   */
  selectProduct(product: Product): boolean {
    if (this.selectedProducts().some(p => p.id === product.id)) {
      console.log('[ProductService] Product already selected:', product.name);
      return false;
    }

    this.selectedProducts.update(products => [...products, product]);
    console.log('[ProductService] Product selected:', product.name);
    return true;
  }

  /**
   * Ürün seçimini kaldır (index ile)
   */
  deselectProductByIndex(index: number): Product | null {
    const products = this.selectedProducts();
    if (index < 0 || index >= products.length) {
      return null;
    }

    const removed = products[index];
    this.selectedProducts.update(p => p.filter((_, i) => i !== index));
    console.log('[ProductService] Product deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedProducts().length;
    this.selectedProducts.set([]);
    console.log(`[ProductService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Ürün seçin veya arayın...';
  }

  getTitle(): string {
    return '<i class="fas fa-box"></i> Ürün Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Ürün ara
   */
  searchProducts(term: string): Product[] {
    if (!term.trim()) {
      return this.products();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.products().filter(p =>
      (p.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm) ||
      (p.code?.toString() || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı ürün ara
   */
  getPagedProducts(term: string, page: number, pageSize: number = 50): { items: Product[]; hasMore: boolean } {
    const filtered = this.searchProducts(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}
