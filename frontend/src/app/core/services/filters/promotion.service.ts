import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface Promotion {
  id: number;
  name: string;
}

/**
 * PromotionService - Kampanya verilerini yöneten servis
 * promotion-manager.js'in Angular versiyonu
 */
@Injectable({
  providedIn: 'root'
})
export class PromotionService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  promotions = signal<Promotion[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedPromotions = signal<Promotion[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Kampanyaları API'den yükle
   */
  async loadPromotions(): Promise<Promotion[]> {
    if (this.isLoading()) return this.promotions();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/promotions`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const promotions: Promotion[] = result.resultData.map(promo => ({
          id: promo.promotionNumber,
          name: promo.promotionName
        }));

        // İsme göre sırala
        promotions.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.promotions.set(promotions);
        console.log(`[PromotionService] Loaded ${promotions.length} promotions`);
        return promotions;
      }

      return [];
    } catch (error) {
      console.error('[PromotionService] Error loading promotions:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Kampanya seç
   */
  selectPromotion(promotion: Promotion): boolean {
    if (this.selectedPromotions().some(p => p.id === promotion.id)) {
      console.log('[PromotionService] Promotion already selected:', promotion.name);
      return false;
    }

    this.selectedPromotions.update(promotions => [...promotions, promotion]);
    console.log('[PromotionService] Promotion selected:', promotion.name);
    return true;
  }

  /**
   * Kampanya seçimini kaldır (index ile)
   */
  deselectPromotionByIndex(index: number): Promotion | null {
    const promotions = this.selectedPromotions();
    if (index < 0 || index >= promotions.length) {
      return null;
    }

    const removed = promotions[index];
    this.selectedPromotions.update(p => p.filter((_, i) => i !== index));
    console.log('[PromotionService] Promotion deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedPromotions().length;
    this.selectedPromotions.set([]);
    console.log(`[PromotionService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Kampanya seçin veya arayın...';
  }

  getTitle(): string {
    return '<i class="fas fa-bullhorn"></i> Kampanya Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Kampanya ara
   */
  searchPromotions(term: string): Promotion[] {
    if (!term.trim()) {
      return this.promotions();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.promotions().filter(p =>
      (p.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı kampanya ara
   */
  getPagedPromotions(term: string, page: number, pageSize: number = 50): { items: Promotion[]; hasMore: boolean } {
    const filtered = this.searchPromotions(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}
