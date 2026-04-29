import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface Currency {
  id: string;
  name: string;
}

/**
 * CurrencyService - Para birimi verilerini yöneten servis
 */
@Injectable({
  providedIn: 'root'
})
export class CurrencyService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  currencies = signal<Currency[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedCurrencies = signal<Currency[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Para birimlerini API'den yükle
   */
  async loadCurrencies(): Promise<Currency[]> {
    if (this.isLoading()) return this.currencies();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/currencies`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const currencies: Currency[] = result.resultData.map(curr => ({
          id: curr.currencyCode,
          name: curr.currencyName
        }));

        // Kod'a göre sırala
        currencies.sort((a, b) => (a.id || '').localeCompare(b.id || '', 'tr'));

        this.currencies.set(currencies);
        console.log(`[CurrencyService] Loaded ${currencies.length} currencies`);
        return currencies;
      }

      return [];
    } catch (error) {
      console.error('[CurrencyService] Error loading currencies:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Para birimi seç
   */
  selectCurrency(currency: Currency): boolean {
    if (this.selectedCurrencies().some(c => c.id === currency.id)) {
      console.log('[CurrencyService] Currency already selected:', currency.name);
      return false;
    }

    this.selectedCurrencies.update(currencies => [...currencies, currency]);
    console.log('[CurrencyService] Currency selected:', currency.name);
    return true;
  }

  /**
   * Para birimi seçimini kaldır (index ile)
   */
  deselectCurrencyByIndex(index: number): Currency | null {
    const currencies = this.selectedCurrencies();
    if (index < 0 || index >= currencies.length) {
      return null;
    }

    const removed = currencies[index];
    this.selectedCurrencies.update(c => c.filter((_, i) => i !== index));
    console.log('[CurrencyService] Currency deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedCurrencies().length;
    this.selectedCurrencies.set([]);
    console.log(`[CurrencyService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Para birimi seçin...';
  }

  getTitle(): string {
    return '<i class="fas fa-dollar-sign"></i> Para Birimi Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Para birimi ara
   */
  searchCurrencies(term: string): Currency[] {
    if (!term.trim()) {
      return this.currencies();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.currencies().filter(c =>
      (c.id || '').toLocaleLowerCase('tr-TR').includes(searchTerm) ||
      (c.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı para birimi ara
   */
  getPagedCurrencies(term: string, page: number, pageSize: number = 50): { items: Currency[]; hasMore: boolean } {
    const filtered = this.searchCurrencies(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}

