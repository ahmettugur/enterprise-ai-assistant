import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface ShipMethod {
  id: number | string;
  name: string;
}

/**
 * ShipMethodService - Teslimat yöntemi verilerini yöneten servis
 */
@Injectable({
  providedIn: 'root'
})
export class ShipMethodService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  shipMethods = signal<ShipMethod[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedShipMethods = signal<ShipMethod[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Teslimat yöntemlerini API'den yükle
   */
  async loadShipMethods(): Promise<ShipMethod[]> {
    if (this.isLoading()) return this.shipMethods();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/shipmethods`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const shipMethods: ShipMethod[] = result.resultData.map(sm => ({
          id: sm.shipMethodId,
          name: sm.shipMethodName
        }));

        // İsme göre sırala
        shipMethods.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.shipMethods.set(shipMethods);
        console.log(`[ShipMethodService] Loaded ${shipMethods.length} ship methods`);
        return shipMethods;
      }

      return [];
    } catch (error) {
      console.error('[ShipMethodService] Error loading ship methods:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Teslimat yöntemi seç
   */
  selectShipMethod(shipMethod: ShipMethod): boolean {
    if (this.selectedShipMethods().some(sm => sm.id === shipMethod.id)) {
      console.log('[ShipMethodService] Ship method already selected:', shipMethod.name);
      return false;
    }

    this.selectedShipMethods.update(methods => [...methods, shipMethod]);
    console.log('[ShipMethodService] Ship method selected:', shipMethod.name);
    return true;
  }

  /**
   * Teslimat yöntemi seçimini kaldır (index ile)
   */
  deselectShipMethodByIndex(index: number): ShipMethod | null {
    const shipMethods = this.selectedShipMethods();
    if (index < 0 || index >= shipMethods.length) {
      return null;
    }

    const removed = shipMethods[index];
    this.selectedShipMethods.update(sm => sm.filter((_, i) => i !== index));
    console.log('[ShipMethodService] Ship method deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedShipMethods().length;
    this.selectedShipMethods.set([]);
    console.log(`[ShipMethodService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Teslimat yöntemi seçin...';
  }

  getTitle(): string {
    return '<i class="fas fa-truck"></i> Teslimat Yöntemi Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Teslimat yöntemi ara
   */
  searchShipMethods(term: string): ShipMethod[] {
    if (!term.trim()) {
      return this.shipMethods();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.shipMethods().filter(sm =>
      (sm.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı teslimat yöntemi ara
   */
  getPagedShipMethods(term: string, page: number, pageSize: number = 50): { items: ShipMethod[]; hasMore: boolean } {
    const filtered = this.searchShipMethods(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}

