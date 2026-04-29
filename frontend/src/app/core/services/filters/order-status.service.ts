import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface OrderStatus {
  id: number | string;
  name: string;
}

/**
 * OrderStatusService - Sipariş durumu verilerini yöneten servis
 */
@Injectable({
  providedIn: 'root'
})
export class OrderStatusService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  orderStatuses = signal<OrderStatus[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedOrderStatuses = signal<OrderStatus[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Sipariş durumlarını API'den yükle
   */
  async loadOrderStatuses(): Promise<OrderStatus[]> {
    if (this.isLoading()) return this.orderStatuses();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/orderstatuses`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const orderStatuses: OrderStatus[] = result.resultData.map(os => ({
          id: os.orderStatusId,
          name: os.orderStatusName
        }));

        // İsme göre sırala
        orderStatuses.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.orderStatuses.set(orderStatuses);
        console.log(`[OrderStatusService] Loaded ${orderStatuses.length} order statuses`);
        return orderStatuses;
      }

      return [];
    } catch (error) {
      console.error('[OrderStatusService] Error loading order statuses:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Sipariş durumu seç
   */
  selectOrderStatus(orderStatus: OrderStatus): boolean {
    if (this.selectedOrderStatuses().some(os => os.id === orderStatus.id)) {
      console.log('[OrderStatusService] Order status already selected:', orderStatus.name);
      return false;
    }

    this.selectedOrderStatuses.update(statuses => [...statuses, orderStatus]);
    console.log('[OrderStatusService] Order status selected:', orderStatus.name);
    return true;
  }

  /**
   * Sipariş durumu seçimini kaldır (index ile)
   */
  deselectOrderStatusByIndex(index: number): OrderStatus | null {
    const orderStatuses = this.selectedOrderStatuses();
    if (index < 0 || index >= orderStatuses.length) {
      return null;
    }

    const removed = orderStatuses[index];
    this.selectedOrderStatuses.update(os => os.filter((_, i) => i !== index));
    console.log('[OrderStatusService] Order status deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedOrderStatuses().length;
    this.selectedOrderStatuses.set([]);
    console.log(`[OrderStatusService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Sipariş durumu seçin...';
  }

  getTitle(): string {
    return '<i class="fas fa-clipboard-check"></i> Sipariş Durumu Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Sipariş durumu ara
   */
  searchOrderStatuses(term: string): OrderStatus[] {
    if (!term.trim()) {
      return this.orderStatuses();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.orderStatuses().filter(os =>
      (os.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı sipariş durumu ara
   */
  getPagedOrderStatuses(term: string, page: number, pageSize: number = 50): { items: OrderStatus[]; hasMore: boolean } {
    const filtered = this.searchOrderStatuses(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}

