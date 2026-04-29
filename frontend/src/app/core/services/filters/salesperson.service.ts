import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface SalesPerson {
  id: number | string;
  name: string;
}

/**
 * SalesPersonService - Satış temsilcisi verilerini yöneten servis
 */
@Injectable({
  providedIn: 'root'
})
export class SalesPersonService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  salesPersons = signal<SalesPerson[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedSalesPersons = signal<SalesPerson[]>([]);

  // ==================== LOADING STATES ====================

  isLoading = signal(false);

  // ==================== API CALLS ====================

  /**
   * Satış temsilcilerini API'den yükle
   */
  async loadSalesPersons(): Promise<SalesPerson[]> {
    if (this.isLoading()) return this.salesPersons();
    
    this.isLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/salespersons`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const salesPersons: SalesPerson[] = result.resultData.map(sp => ({
          id: sp.salesPersonId,
          name: sp.salesPersonName
        }));

        // İsme göre sırala
        salesPersons.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.salesPersons.set(salesPersons);
        console.log(`[SalesPersonService] Loaded ${salesPersons.length} sales persons`);
        return salesPersons;
      }

      return [];
    } catch (error) {
      console.error('[SalesPersonService] Error loading sales persons:', error);
      return [];
    } finally {
      this.isLoading.set(false);
    }
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Satış temsilcisi seç
   */
  selectSalesPerson(salesPerson: SalesPerson): boolean {
    if (this.selectedSalesPersons().some(sp => sp.id === salesPerson.id)) {
      console.log('[SalesPersonService] Sales person already selected:', salesPerson.name);
      return false;
    }

    this.selectedSalesPersons.update(sps => [...sps, salesPerson]);
    console.log('[SalesPersonService] Sales person selected:', salesPerson.name);
    return true;
  }

  /**
   * Satış temsilcisi seçimini kaldır (index ile)
   */
  deselectSalesPersonByIndex(index: number): SalesPerson | null {
    const salesPersons = this.selectedSalesPersons();
    if (index < 0 || index >= salesPersons.length) {
      return null;
    }

    const removed = salesPersons[index];
    this.selectedSalesPersons.update(sp => sp.filter((_, i) => i !== index));
    console.log('[SalesPersonService] Sales person deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    const count = this.selectedSalesPersons().length;
    this.selectedSalesPersons.set([]);
    console.log(`[SalesPersonService] Cleared ${count} selections`);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Satış temsilcisi seçin...';
  }

  getTitle(): string {
    return '<i class="fas fa-user-tie"></i> Satış Temsilcisi Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Satış temsilcisi ara
   */
  searchSalesPersons(term: string): SalesPerson[] {
    if (!term.trim()) {
      return this.salesPersons();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.salesPersons().filter(sp =>
      (sp.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı satış temsilcisi ara
   */
  getPagedSalesPersons(term: string, page: number, pageSize: number = 50): { items: SalesPerson[]; hasMore: boolean } {
    const filtered = this.searchSalesPersons(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}

