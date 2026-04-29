import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface SalesReason {
  id: number | string;
  name: string;
  reasonType?: string;
}

/**
 * SalesReasonService - Satış nedeni verilerini yöneten servis
 */
@Injectable({
  providedIn: 'root'
})
export class SalesReasonService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  salesReasons = signal<SalesReason[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedSalesReasons = signal<SalesReason[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Satış nedenlerini API'den yükle
   */
  async loadSalesReasons(): Promise<SalesReason[]> {
    if (this.isLoading()) return this.salesReasons();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/salesreasons`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const salesReasons: SalesReason[] = result.resultData.map(sr => ({
          id: sr.salesReasonId,
          name: sr.salesReasonName,
          reasonType: sr.reasonType
        }));

        // İsme göre sırala
        salesReasons.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.salesReasons.set(salesReasons);
        console.log(`[SalesReasonService] Loaded ${salesReasons.length} sales reasons`);
        return salesReasons;
      }

      return [];
    } catch (error) {
      console.error('[SalesReasonService] Error loading sales reasons:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Satış nedeni seç
   */
  selectSalesReason(salesReason: SalesReason): boolean {
    if (this.selectedSalesReasons().some(sr => sr.id === salesReason.id)) {
      console.log('[SalesReasonService] Sales reason already selected:', salesReason.name);
      return false;
    }

    this.selectedSalesReasons.update(reasons => [...reasons, salesReason]);
    console.log('[SalesReasonService] Sales reason selected:', salesReason.name);
    return true;
  }

  /**
   * Satış nedeni seçimini kaldır (index ile)
   */
  deselectSalesReasonByIndex(index: number): SalesReason | null {
    const salesReasons = this.selectedSalesReasons();
    if (index < 0 || index >= salesReasons.length) {
      return null;
    }

    const removed = salesReasons[index];
    this.selectedSalesReasons.update(sr => sr.filter((_, i) => i !== index));
    console.log('[SalesReasonService] Sales reason deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedSalesReasons().length;
    this.selectedSalesReasons.set([]);
    console.log(`[SalesReasonService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Satış nedeni seçin...';
  }

  getTitle(): string {
    return '<i class="fas fa-lightbulb"></i> Satış Nedeni Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Satış nedeni ara
   */
  searchSalesReasons(term: string): SalesReason[] {
    if (!term.trim()) {
      return this.salesReasons();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.salesReasons().filter(sr =>
      (sr.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm) ||
      (sr.reasonType || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı satış nedeni ara
   */
  getPagedSalesReasons(term: string, page: number, pageSize: number = 50): { items: SalesReason[]; hasMore: boolean } {
    const filtered = this.searchSalesReasons(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}

