import { Injectable, signal } from '@angular/core';

// ==================== INTERFACES ====================

export interface CustomerType {
  id: string;
  name: string;
}

/**
 * CustomerTypeService - Müşteri tipi verilerini yöneten servis
 * Statik veri içerir (Individual, Store)
 */
@Injectable({
  providedIn: 'root'
})
export class CustomerTypeService {
  // ==================== DATA STORES ====================

  customerTypes = signal<CustomerType[]>([]);

  // ==================== SELECTED VALUES ====================

  selectedCustomerTypes = signal<CustomerType[]>([]);

  constructor() {
    this.initializeData();
  }

  // ==================== DATA INITIALIZATION ====================

  /**
   * Statik verileri yükle
   */
  private initializeData(): void {
    const types: CustomerType[] = [
      { id: 'Individual', name: 'Individual' },
      { id: 'Store', name: 'Store' }
    ];

    this.customerTypes.set(types);
    console.log('[CustomerTypeService] Data initialized:', types.length);
  }

  // ==================== SELECTION MANAGEMENT ====================

  /**
   * Müşteri tipi seç
   */
  selectCustomerType(customerType: CustomerType): boolean {
    if (this.selectedCustomerTypes().some(ct => ct.id === customerType.id)) {
      return false;
    }
    this.selectedCustomerTypes.update(types => [...types, customerType]);
    console.log('[CustomerTypeService] Customer type selected:', customerType.name);
    return true;
  }

  /**
   * Müşteri tipi seçimini kaldır (index ile)
   */
  deselectCustomerTypeByIndex(index: number): CustomerType | null {
    const types = this.selectedCustomerTypes();
    if (index < 0 || index >= types.length) return null;
    const removed = types[index];
    this.selectedCustomerTypes.update(ct => ct.filter((_, i) => i !== index));
    console.log('[CustomerTypeService] Customer type deselected:', removed.name);
    return removed;
  }

  /**
   * Tüm seçimleri temizle
   */
  clearSelections(): void {
    this.selectedCustomerTypes.set([]);
  }

  // ==================== SELECT2 HELPERS ====================

  getPlaceholder(): string {
    return 'Müşteri tipi seçin...';
  }

  getTitle(): string {
    return '<i class="fas fa-users"></i> Müşteri Tipi Seçin';
  }

  // ==================== SEARCH ====================

  /**
   * Müşteri tipi ara
   */
  searchCustomerTypes(term: string): CustomerType[] {
    if (!term.trim()) {
      return this.customerTypes();
    }
    const searchTerm = term.toLocaleLowerCase('tr-TR');
    return this.customerTypes().filter(ct =>
      (ct.name || '').toLocaleLowerCase('tr-TR').includes(searchTerm)
    );
  }
}

