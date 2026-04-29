import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { firstValueFrom } from 'rxjs';

// ==================== INTERFACES ====================

export interface Region {
  id: number;
  name: string;
}

export interface Store {
  id: number | string;
  name: string;
  code?: string;
  isActive: boolean;
  regionNumber: number;
}

/**
 * RegionStoreService - Bölge ve Mağaza verilerini yöneten servis
 * region-store-manager.js'in Angular versiyonu
 */
@Injectable({
  providedIn: 'root'
})
export class RegionStoreService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  private readonly apiUrl = 'https://localhost:7041';

  // ==================== DATA STORES ====================

  regions = signal<Region[]>([]);
  stores = signal<Store[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedRegions = signal<Region[]>([]);
  selectedStores = signal<Store[]>([]);

  // ==================== LOADING STATES ====================

  isRegionsLoading = signal(false);
  isStoresLoading = signal(false);

  // ==================== COMPUTED VALUES ====================

  /**
   * Filtrelenmiş mağazalar (bölgeye göre)
   */
  filteredStores = computed(() => {
    const selectedRegs = this.selectedRegions();
    const allStores = this.stores();

    if (selectedRegs.length === 0) {
      return allStores;
    }

    const selectedRegionIds = selectedRegs.map(r => r.id);
    return allStores.filter(store =>
      selectedRegionIds.includes(store.regionNumber)
    );
  });

  constructor() {
    // Bölgeleri API'den yükle
    this.loadRegions();
  }

  // ==================== API CALLS ====================

  /**
   * Bölgeleri API'den yükle
   */
  async loadRegions(): Promise<Region[]> {
    if (this.isRegionsLoading()) return this.regions();
    
    this.isRegionsLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/regions`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const regions: Region[] = result.resultData.map(region => ({
          id: parseInt(region.territoryId) || 0,
          name: region.territoryName
        }));

        // İsme göre sırala
        regions.sort((a, b) => (a.name || '').localeCompare(b.name || '', 'tr'));

        this.regions.set(regions);
        console.log(`[RegionStoreService] Loaded ${regions.length} regions`);
        return regions;
      }

      return [];
    } catch (error) {
      console.error('[RegionStoreService] Error loading regions:', error);
      return [];
    } finally {
      this.isRegionsLoading.set(false);
    }
  }

  /**
   * Mağazaları API'den yükle
   */
  async loadStores(): Promise<Store[]> {
    if (this.isStoresLoading()) return this.stores();
    
    this.isStoresLoading.set(true);

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const result = await firstValueFrom(
        this.http.get<{ isSucceed: boolean; resultData: any[] }>(`${this.apiUrl}/api/v1/common/stores`, { headers })
      );

      if (result.isSucceed && result.resultData) {
        const stores: Store[] = result.resultData.map(store => ({
          id: store.storeNumber,
          name: store.storeName,
          code: store.storeNumber,
          isActive: store.isActive === '1' || store.isActive === 1,
          regionNumber: store.regionNumber
        }));

        // Aktif mağazaları önce, pasif mağazaları sona koy
        stores.sort((a, b) => {
          if (a.isActive === b.isActive) {
            return (a.name || '').localeCompare(b.name || '', 'tr');
          }
          return (b.isActive ? 1 : 0) - (a.isActive ? 1 : 0);
        });

        this.stores.set(stores);
        console.log(`[RegionStoreService] Loaded ${stores.length} stores (Active: ${stores.filter(s => s.isActive).length})`);
        return stores;
      }

      return [];
    } catch (error) {
      console.error('[RegionStoreService] Error loading stores:', error);
      return [];
    } finally {
      this.isStoresLoading.set(false);
    }
  }

  /**
   * Tüm verileri yükle
   */
  async loadAll(): Promise<void> {
    await Promise.all([
      this.loadRegions(),
      this.loadStores()
    ]);
    console.log('[RegionStoreService] All data loaded');
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Bölge seç
   */
  selectRegion(region: Region): boolean {
    if (this.selectedRegions().some(r => r.id === region.id)) {
      console.log('[RegionStoreService] Region already selected:', region.name);
      return false;
    }

    this.selectedRegions.update(regions => [...regions, region]);
    console.log('[RegionStoreService] Region selected:', region.name);
    return true;
  }

  /**
   * Bölge seçimini kaldır (index ile)
   */
  deselectRegionByIndex(index: number): Region | null {
    const regions = this.selectedRegions();
    if (index < 0 || index >= regions.length) {
      return null;
    }

    const removed = regions[index];
    this.selectedRegions.update(r => r.filter((_, i) => i !== index));
    console.log('[RegionStoreService] Region deselected:', removed.name);
    return removed;
  }

  /**
   * Mağaza seç
   */
  selectStore(store: Store): boolean {
    if (this.selectedStores().some(s => s.id === store.id)) {
      console.log('[RegionStoreService] Store already selected:', store.name);
      return false;
    }

    this.selectedStores.update(stores => [...stores, store]);
    console.log('[RegionStoreService] Store selected:', store.name);
    return true;
  }

  /**
   * Mağaza seçimini kaldır (index ile)
   */
  deselectStoreByIndex(index: number): Store | null {
    const stores = this.selectedStores();
    if (index < 0 || index >= stores.length) {
      return null;
    }

    const removed = stores[index];
    this.selectedStores.update(s => s.filter((_, i) => i !== index));
    console.log('[RegionStoreService] Store deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm bölge seçimlerini temizle
   */
  clearRegionSelections(): void {
    const count = this.selectedRegions().length;
    this.selectedRegions.set([]);
    console.log(`[RegionStoreService] Cleared ${count} region selections`);
  }

  /**
   * Tüm mağaza seçimlerini temizle
   */
  clearStoreSelections(): void {
    const count = this.selectedStores().length;
    this.selectedStores.set([]);
    console.log(`[RegionStoreService] Cleared ${count} store selections`);
  }

  /**
   * Tüm seçimleri temizle
   */
  clearAllSelections(): void {
    this.clearRegionSelections();
    this.clearStoreSelections();
  }

  // ==================== SELECT2 HELPERS ====================

  getRegionPlaceholder(): string {
    return 'Bölge seçin...';
  }

  getStorePlaceholder(): string {
    const count = this.selectedRegions().length;
    if (count > 0) {
      return `${count} bölgeden store seçin...`;
    }
    return 'Store seçin...';
  }

  getStoreTitle(): string {
    const count = this.selectedRegions().length;
    if (count > 0) {
      return `<i class="fas fa-store"></i> Store Seçin (${count} bölge filtreli)`;
    }
    return '<i class="fas fa-store"></i> Store Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Bölge ara
   */
  searchRegions(term: string): Region[] {
    if (!term.trim()) {
      return this.regions();
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.regions().filter(r =>
      (r.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Mağaza ara (bölge filtresini de uygular)
   */
  searchStores(term: string): Store[] {
    let data = this.filteredStores();

    if (!term.trim()) {
      return data;
    }

    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return data.filter(s =>
      (s.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm) ||
      (s.code?.toString() || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }

  /**
   * Sayfalı mağaza ara
   */
  getPagedStores(term: string, page: number, pageSize: number = 50): { items: Store[]; hasMore: boolean } {
    const filtered = this.searchStores(term);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = filtered.slice(startIndex, endIndex);
    const hasMore = endIndex < filtered.length;

    return { items, hasMore };
  }
}
