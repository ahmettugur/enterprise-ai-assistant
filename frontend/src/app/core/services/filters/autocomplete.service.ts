import { Injectable, signal, computed, inject } from '@angular/core';
import { RegionStoreService, Region, Store } from './region-store.service';
import { ProductService, Product } from './product.service';
import { CategoryService, Category } from './category.service';
import { PromotionService, Promotion } from './promotion.service';
import { SalesPersonService, SalesPerson } from './salesperson.service';
import { CustomerTypeService, CustomerType } from './customer-type.service';
import { OrderStatusService, OrderStatus } from './order-status.service';
import { ShipMethodService, ShipMethod } from './ship-method.service';
import { CurrencyService, Currency } from './currency.service';
import { SalesReasonService, SalesReason } from './sales-reason.service';
import { DateFilterService } from './date-filter.service';

// ==================== TYPES ====================

export type FilterType =
  | 'date'
  | 'region'
  | 'store'
  | 'product'
  | 'category'
  | 'promotion'
  | 'salesperson'
  | 'customertype'
  | 'orderstatus'
  | 'shipmethod'
  | 'currency'
  | 'salesreason';

export interface FilterCategory {
  id: FilterType;
  icon: string;
  label: string;
  color: string;
}

export interface SelectOption {
  id: number | string;
  name: string;
  code?: string;
}

export interface FilterTag {
  type: FilterType;
  index: number;
  icon: string;
  label: string;
  value: string;
  color: string;
}

/**
 * AutocompleteService - Filtre koordinasyon servisi
 * AdventureWorks için filtre yönetimi
 */
@Injectable({
  providedIn: 'root'
})
export class AutocompleteService {
  // Inject all filter services
  private regionStoreService = inject(RegionStoreService);
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private promotionService = inject(PromotionService);
  private salesPersonService = inject(SalesPersonService);
  private customerTypeService = inject(CustomerTypeService);
  private orderStatusService = inject(OrderStatusService);
  private shipMethodService = inject(ShipMethodService);
  private currencyService = inject(CurrencyService);
  private salesReasonService = inject(SalesReasonService);
  
  // Date filter service (public for template access)
  dateFilterService = inject(DateFilterService);

  // ==================== UI STATE ====================

  isMenuOpen = signal(false);
  isModalOpen = signal(false);
  currentCategory = signal<FilterType | null>(null);
  triggerPosition = signal(0);
  activeMenuIndex = signal(0);

  // Menü pozisyonu (cursor pozisyonuna göre)
  menuPosition = signal<{ left: number; bottom: number }>({ left: 0, bottom: 0 });

  // ==================== FILTER CATEGORIES ====================

  readonly filterCategories: FilterCategory[] = [
    { id: 'date', icon: 'fa-calendar-alt', label: 'Tarih', color: 'emerald' },
    { id: 'region', icon: 'fa-map-marker-alt', label: 'Bölge', color: 'blue' },
    { id: 'store', icon: 'fa-store', label: 'Store', color: 'green' },
    { id: 'product', icon: 'fa-box', label: 'Ürün', color: 'orange' },
    { id: 'category', icon: 'fa-layer-group', label: 'Kategori', color: 'purple' },
    { id: 'salesperson', icon: 'fa-user-tie', label: 'Satış Temsilcisi', color: 'indigo' },
    { id: 'customertype', icon: 'fa-users', label: 'Müşteri Tipi', color: 'cyan' },
    { id: 'orderstatus', icon: 'fa-clipboard-check', label: 'Sipariş Durumu', color: 'amber' },
    { id: 'shipmethod', icon: 'fa-truck', label: 'Teslimat Yöntemi', color: 'teal' },
    { id: 'currency', icon: 'fa-dollar-sign', label: 'Para Birimi', color: 'yellow' },
    { id: 'salesreason', icon: 'fa-lightbulb', label: 'Satış Nedeni', color: 'pink' },
    { id: 'promotion', icon: 'fa-bullhorn', label: 'Kampanya', color: 'red' }
  ];

  // ==================== COMPUTED: ALL FILTERS ====================

  /**
   * Tüm seçili filtreleri birleştir (tag olarak göstermek için)
   */
  allFilters = computed<FilterTag[]>(() => {
    const tags: FilterTag[] = [];

    // Regions
    this.regionStoreService.selectedRegions().forEach((r, i) => {
      const cat = this.filterCategories.find(c => c.id === 'region')!;
      tags.push({
        type: 'region',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: r.name,
        color: cat.color
      });
    });

    // Stores
    this.regionStoreService.selectedStores().forEach((s, i) => {
      const cat = this.filterCategories.find(c => c.id === 'store')!;
      tags.push({
        type: 'store',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: s.name,
        color: cat.color
      });
    });

    // Products
    this.productService.selectedProducts().forEach((p, i) => {
      const cat = this.filterCategories.find(c => c.id === 'product')!;
      tags.push({
        type: 'product',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: p.name,
        color: cat.color
      });
    });

    // Categories
    this.categoryService.selectedCategories().forEach((c, i) => {
      const cat = this.filterCategories.find(fc => fc.id === 'category')!;
      tags.push({
        type: 'category',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: c.name,
        color: cat.color
      });
    });

    // Sales Persons
    this.salesPersonService.selectedSalesPersons().forEach((sp, i) => {
      const cat = this.filterCategories.find(c => c.id === 'salesperson')!;
      tags.push({
        type: 'salesperson',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: sp.name,
        color: cat.color
      });
    });

    // Customer Types
    this.customerTypeService.selectedCustomerTypes().forEach((ct, i) => {
      const cat = this.filterCategories.find(c => c.id === 'customertype')!;
      tags.push({
        type: 'customertype',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: ct.name,
        color: cat.color
      });
    });

    // Order Statuses
    this.orderStatusService.selectedOrderStatuses().forEach((os, i) => {
      const cat = this.filterCategories.find(c => c.id === 'orderstatus')!;
      tags.push({
        type: 'orderstatus',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: os.name,
        color: cat.color
      });
    });

    // Ship Methods
    this.shipMethodService.selectedShipMethods().forEach((sm, i) => {
      const cat = this.filterCategories.find(c => c.id === 'shipmethod')!;
      tags.push({
        type: 'shipmethod',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: sm.name,
        color: cat.color
      });
    });

    // Currencies
    this.currencyService.selectedCurrencies().forEach((c, i) => {
      const cat = this.filterCategories.find(fc => fc.id === 'currency')!;
      tags.push({
        type: 'currency',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: c.name,
        color: cat.color
      });
    });

    // Sales Reasons
    this.salesReasonService.selectedSalesReasons().forEach((sr, i) => {
      const cat = this.filterCategories.find(c => c.id === 'salesreason')!;
      tags.push({
        type: 'salesreason',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: sr.name,
        color: cat.color
      });
    });

    // Promotions
    this.promotionService.selectedPromotions().forEach((p, i) => {
      const cat = this.filterCategories.find(c => c.id === 'promotion')!;
      tags.push({
        type: 'promotion',
        index: i,
        icon: cat.icon,
        label: cat.label,
        value: p.name,
        color: cat.color
      });
    });

    // Date
    const dateSelection = this.dateFilterService.selectedDate();
    if (dateSelection) {
      const cat = this.filterCategories.find(c => c.id === 'date')!;
      tags.push({
        type: 'date',
        index: 0,
        icon: cat.icon,
        label: cat.label,
        value: dateSelection.displayText,
        color: cat.color
      });
    }

    return tags;
  });

  /**
   * Aktif filtre var mı?
   */
  hasActiveFilters = computed(() => this.allFilters().length > 0);

  // ==================== MENU CONTROL ====================

  openMenu(): void {
    this.isMenuOpen.set(true);
    this.activeMenuIndex.set(0);
    console.log('[AutocompleteService] Menu opened');
  }

  closeMenu(): void {
    this.isMenuOpen.set(false);
    console.log('[AutocompleteService] Menu closed');
  }

  navigateMenu(direction: 'up' | 'down'): void {
    const total = this.filterCategories.length;
    const current = this.activeMenuIndex();

    if (direction === 'down') {
      this.activeMenuIndex.set((current + 1) % total);
    } else {
      this.activeMenuIndex.set((current - 1 + total) % total);
    }
  }

  selectCurrentMenuItem(): FilterType {
    const category = this.filterCategories[this.activeMenuIndex()];
    this.closeMenu();
    return category.id;
  }

  setTriggerPosition(pos: number): void {
    this.triggerPosition.set(pos);
  }

  // ==================== MODAL CONTROL ====================

  openModal(category: FilterType): void {
    this.currentCategory.set(category);
    this.isModalOpen.set(true);
    console.log('[AutocompleteService] Modal opened for:', category);
  }

  closeModal(): void {
    this.isModalOpen.set(false);
    this.currentCategory.set(null);
    console.log('[AutocompleteService] Modal closed');
  }

  // ==================== DATA LOADING ====================

  /**
   * Tüm verileri yükle
   */
  async loadAllData(): Promise<void> {
    console.log('[AutocompleteService] Loading all data...');

    await Promise.all([
      this.regionStoreService.loadAll(),
      this.productService.loadProducts(),
      this.categoryService.loadCategories(),
      this.promotionService.loadPromotions(),
      this.salesPersonService.loadSalesPersons(),
      this.orderStatusService.loadOrderStatuses(),
      this.shipMethodService.loadShipMethods(),
      this.currencyService.loadCurrencies(),
      this.salesReasonService.loadSalesReasons()
    ]);

    console.log('[AutocompleteService] All data loaded');
  }

  /**
   * Kategoriye göre veriyi yükle
   */
  async ensureDataLoaded(category: FilterType): Promise<void> {
    switch (category) {
      case 'region':
        if (this.regionStoreService.regions().length === 0) {
          await this.regionStoreService.loadRegions();
        }
        break;
      case 'store':
        if (this.regionStoreService.stores().length === 0) {
          await this.regionStoreService.loadStores();
        }
        break;
      case 'product':
        if (this.productService.products().length === 0) {
          await this.productService.loadProducts();
        }
        break;
      case 'category':
        if (this.categoryService.categories().length === 0) {
          await this.categoryService.loadCategories();
        }
        break;
      case 'promotion':
        if (this.promotionService.promotions().length === 0) {
          await this.promotionService.loadPromotions();
        }
        break;
      case 'salesperson':
        if (this.salesPersonService.salesPersons().length === 0) {
          await this.salesPersonService.loadSalesPersons();
        }
        break;
      case 'orderstatus':
        if (this.orderStatusService.orderStatuses().length === 0) {
          await this.orderStatusService.loadOrderStatuses();
        }
        break;
      case 'shipmethod':
        if (this.shipMethodService.shipMethods().length === 0) {
          await this.shipMethodService.loadShipMethods();
        }
        break;
      case 'currency':
        if (this.currencyService.currencies().length === 0) {
          await this.currencyService.loadCurrencies();
        }
        break;
      case 'salesreason':
        if (this.salesReasonService.salesReasons().length === 0) {
          await this.salesReasonService.loadSalesReasons();
        }
        break;
      // Statik veriler için yükleme gerekmez (customertype, date)
      default:
        break;
    }
  }

  // ==================== CATEGORY HELPERS ====================

  getCategoryTitle(category: FilterType): string {
    const found = this.filterCategories.find(c => c.id === category);
    return found?.label || 'Seçim Yapın';
  }

  getCategoryIcon(category: FilterType): string {
    const found = this.filterCategories.find(c => c.id === category);
    return found?.icon || 'fa-filter';
  }

  getCategoryColor(category: FilterType): string {
    const found = this.filterCategories.find(c => c.id === category);
    return found?.color || 'gray';
  }

  // ==================== GET DATA FOR CATEGORY ====================

  /**
   * Kategoriye göre veri getir
   */
  getCategoryItems(category: FilterType): SelectOption[] {
    switch (category) {
      case 'region':
        return this.regionStoreService.regions().map(r => ({ id: r.id, name: r.name }));
      case 'store':
        return this.regionStoreService.filteredStores().map(s => ({ id: s.id, name: s.name, code: s.code }));
      case 'product':
        return this.productService.products().map(p => ({ id: p.id, name: p.name, code: p.code }));
      case 'category':
        return this.categoryService.categories().map(c => ({ id: c.id, name: c.name }));
      case 'salesperson':
        return this.salesPersonService.salesPersons().map(sp => ({ id: sp.id, name: sp.name }));
      case 'customertype':
        return this.customerTypeService.customerTypes().map(ct => ({ id: ct.id, name: ct.name }));
      case 'orderstatus':
        return this.orderStatusService.orderStatuses().map(os => ({ id: os.id, name: os.name }));
      case 'shipmethod':
        return this.shipMethodService.shipMethods().map(sm => ({ id: sm.id, name: sm.name }));
      case 'currency':
        return this.currencyService.currencies().map(c => ({ id: c.id, name: c.name }));
      case 'salesreason':
        return this.salesReasonService.salesReasons().map(sr => ({ id: sr.id, name: sr.name }));
      case 'promotion':
        return this.promotionService.promotions().map(p => ({ id: p.id, name: p.name }));
      default:
        return [];
    }
  }

  /**
   * Kategoriye göre sayfalı veri getir
   */
  getPagedItems(category: FilterType, term: string, page: number, pageSize: number = 50): { items: SelectOption[]; hasMore: boolean } {
    switch (category) {
      case 'region': {
        const filtered = this.regionStoreService.searchRegions(term);
        return this.paginate(filtered.map(r => ({ id: r.id, name: r.name })), page, pageSize);
      }
      case 'store': {
        const { items, hasMore } = this.regionStoreService.getPagedStores(term, page, pageSize);
        return { items: items.map(s => ({ id: s.id, name: s.name, code: s.code })), hasMore };
      }
      case 'product': {
        const { items, hasMore } = this.productService.getPagedProducts(term, page, pageSize);
        return { items: items.map(p => ({ id: p.id, name: p.name, code: p.code })), hasMore };
      }
      case 'category': {
        const { items, hasMore } = this.categoryService.getPagedCategories(term, page, pageSize);
        return { items: items.map(c => ({ id: c.id, name: c.name })), hasMore };
      }
      case 'salesperson': {
        const { items, hasMore } = this.salesPersonService.getPagedSalesPersons(term, page, pageSize);
        return { items: items.map(sp => ({ id: sp.id, name: sp.name })), hasMore };
      }
      case 'customertype': {
        const filtered = this.customerTypeService.searchCustomerTypes(term);
        return this.paginate(filtered.map(ct => ({ id: ct.id, name: ct.name })), page, pageSize);
      }
      case 'orderstatus': {
        const { items, hasMore } = this.orderStatusService.getPagedOrderStatuses(term, page, pageSize);
        return { items: items.map(os => ({ id: os.id, name: os.name })), hasMore };
      }
      case 'shipmethod': {
        const { items, hasMore } = this.shipMethodService.getPagedShipMethods(term, page, pageSize);
        return { items: items.map(sm => ({ id: sm.id, name: sm.name })), hasMore };
      }
      case 'currency': {
        const { items, hasMore } = this.currencyService.getPagedCurrencies(term, page, pageSize);
        return { items: items.map(c => ({ id: c.id, name: c.name })), hasMore };
      }
      case 'salesreason': {
        const { items, hasMore } = this.salesReasonService.getPagedSalesReasons(term, page, pageSize);
        return { items: items.map(sr => ({ id: sr.id, name: sr.name })), hasMore };
      }
      case 'promotion': {
        const { items, hasMore } = this.promotionService.getPagedPromotions(term, page, pageSize);
        return { items: items.map(p => ({ id: p.id, name: p.name })), hasMore };
      }
      default:
        return { items: [], hasMore: false };
    }
  }

  private paginate<T>(items: T[], page: number, pageSize: number): { items: T[]; hasMore: boolean } {
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    return {
      items: items.slice(startIndex, endIndex),
      hasMore: endIndex < items.length
    };
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Seçim yap
   */
  selectItem(category: FilterType, item: SelectOption): void {
    switch (category) {
      case 'region':
        this.regionStoreService.selectRegion({ id: item.id as number, name: item.name });
        break;
      case 'store':
        this.regionStoreService.selectStore({ id: item.id, name: item.name, code: item.code, isActive: true, regionNumber: 0 });
        break;
      case 'product':
        this.productService.selectProduct({ id: item.id, name: item.name, code: item.code });
        break;
      case 'category':
        this.categoryService.selectCategory({ id: item.id as number, name: item.name });
        break;
      case 'salesperson':
        this.salesPersonService.selectSalesPerson({ id: item.id, name: item.name });
        break;
      case 'customertype':
        this.customerTypeService.selectCustomerType({ id: item.id as string, name: item.name });
        break;
      case 'orderstatus':
        this.orderStatusService.selectOrderStatus({ id: item.id, name: item.name });
        break;
      case 'shipmethod':
        this.shipMethodService.selectShipMethod({ id: item.id, name: item.name });
        break;
      case 'currency':
        this.currencyService.selectCurrency({ id: item.id as string, name: item.name });
        break;
      case 'salesreason':
        this.salesReasonService.selectSalesReason({ id: item.id, name: item.name });
        break;
      case 'promotion':
        this.promotionService.selectPromotion({ id: item.id as number, name: item.name });
        break;
    }
  }

  /**
   * Seçimi kaldır
   */
  deselectItem(category: FilterType, index: number): void {
    switch (category) {
      case 'region':
        this.regionStoreService.deselectRegionByIndex(index);
        break;
      case 'store':
        this.regionStoreService.deselectStoreByIndex(index);
        break;
      case 'product':
        this.productService.deselectProductByIndex(index);
        break;
      case 'category':
        this.categoryService.deselectCategoryByIndex(index);
        break;
      case 'salesperson':
        this.salesPersonService.deselectSalesPersonByIndex(index);
        break;
      case 'customertype':
        this.customerTypeService.deselectCustomerTypeByIndex(index);
        break;
      case 'orderstatus':
        this.orderStatusService.deselectOrderStatusByIndex(index);
        break;
      case 'shipmethod':
        this.shipMethodService.deselectShipMethodByIndex(index);
        break;
      case 'currency':
        this.currencyService.deselectCurrencyByIndex(index);
        break;
      case 'salesreason':
        this.salesReasonService.deselectSalesReasonByIndex(index);
        break;
      case 'promotion':
        this.promotionService.deselectPromotionByIndex(index);
        break;
      case 'date':
        this.dateFilterService.clearSelection();
        break;
    }
  }

  /**
   * Tüm seçimleri temizle
   */
  clearAllSelections(): void {
    this.regionStoreService.clearAllSelections();
    this.productService.clearSelections();
    this.categoryService.clearSelections();
    this.promotionService.clearSelections();
    this.salesPersonService.clearSelections();
    this.customerTypeService.clearSelections();
    this.orderStatusService.clearSelections();
    this.shipMethodService.clearSelections();
    this.currencyService.clearSelections();
    this.salesReasonService.clearSelections();
    this.dateFilterService.clearSelection();
    console.log('[AutocompleteService] All selections cleared');
  }

  // ==================== SUMMARY ====================

  getSummary(): Record<string, number> {
    return {
      regions: this.regionStoreService.selectedRegions().length,
      stores: this.regionStoreService.selectedStores().length,
      products: this.productService.selectedProducts().length,
      categories: this.categoryService.selectedCategories().length,
      salespersons: this.salesPersonService.selectedSalesPersons().length,
      customertypes: this.customerTypeService.selectedCustomerTypes().length,
      orderstatuses: this.orderStatusService.selectedOrderStatuses().length,
      shipmethods: this.shipMethodService.selectedShipMethods().length,
      currencies: this.currencyService.selectedCurrencies().length,
      salesreasons: this.salesReasonService.selectedSalesReasons().length,
      promotions: this.promotionService.selectedPromotions().length,
      total: this.allFilters().length
    };
  }
}
