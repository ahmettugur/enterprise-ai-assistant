import { Injectable, signal, computed } from '@angular/core';

// ==================== INTERFACES ====================

export type DateSelectionMode = 'single' | 'range' | 'month' | 'preset';

export interface DatePreset {
  id: string;
  label: string;
  icon: string;
  getValue: () => DateSelection;
}

export interface DateSelection {
  mode: DateSelectionMode;
  // Tek tarih
  singleDate?: Date;
  // Tarih aralığı
  startDate?: Date;
  endDate?: Date;
  // Ay seçimi
  year?: number;
  month?: number; // 0-11
  // Preset id
  presetId?: string;
  // Görüntüleme metni
  displayText: string;
}

/**
 * DateFilterService - Tarih filtresi yönetimi
 * Tek tarih, tarih aralığı ve ay seçimi destekler
 */
@Injectable({
  providedIn: 'root'
})
export class DateFilterService {
  
  // ==================== STATE ====================
  
  /** Seçili tarih bilgisi */
  selectedDate = signal<DateSelection | null>(null);
  
  /** Modal açık mı? */
  isModalOpen = signal(false);
  
  /** Aktif mod (single, range, month) */
  activeMode = signal<DateSelectionMode>('preset');
  
  /** Geçici değerler (modal içi) */
  tempSingleDate = signal<string>('');
  tempStartDate = signal<string>('');
  tempEndDate = signal<string>('');
  tempYear = signal<number>(new Date().getFullYear());
  tempMonth = signal<number>(new Date().getMonth());
  
  // ==================== PRESETS ====================
  
  readonly presets: DatePreset[] = [
    {
      id: 'today',
      label: 'Bugün',
      icon: 'fa-calendar-day',
      getValue: () => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        return {
          mode: 'preset',
          singleDate: today,
          presetId: 'today',
          displayText: 'Bugün'
        };
      }
    },
    {
      id: 'yesterday',
      label: 'Dün',
      icon: 'fa-calendar-minus',
      getValue: () => {
        const yesterday = new Date();
        yesterday.setDate(yesterday.getDate() - 1);
        yesterday.setHours(0, 0, 0, 0);
        return {
          mode: 'preset',
          singleDate: yesterday,
          presetId: 'yesterday',
          displayText: 'Dün'
        };
      }
    },
    {
      id: 'thisWeek',
      label: 'Bu Hafta',
      icon: 'fa-calendar-week',
      getValue: () => {
        const today = new Date();
        const dayOfWeek = today.getDay();
        const monday = new Date(today);
        monday.setDate(today.getDate() - (dayOfWeek === 0 ? 6 : dayOfWeek - 1));
        monday.setHours(0, 0, 0, 0);
        
        const sunday = new Date(monday);
        sunday.setDate(monday.getDate() + 6);
        sunday.setHours(23, 59, 59, 999);
        
        return {
          mode: 'preset',
          startDate: monday,
          endDate: sunday,
          presetId: 'thisWeek',
          displayText: 'Bu Hafta'
        };
      }
    },
    {
      id: 'lastWeek',
      label: 'Geçen Hafta',
      icon: 'fa-calendar-week',
      getValue: () => {
        const today = new Date();
        const dayOfWeek = today.getDay();
        const monday = new Date(today);
        monday.setDate(today.getDate() - (dayOfWeek === 0 ? 6 : dayOfWeek - 1) - 7);
        monday.setHours(0, 0, 0, 0);
        
        const sunday = new Date(monday);
        sunday.setDate(monday.getDate() + 6);
        sunday.setHours(23, 59, 59, 999);
        
        return {
          mode: 'preset',
          startDate: monday,
          endDate: sunday,
          presetId: 'lastWeek',
          displayText: 'Geçen Hafta'
        };
      }
    },
    {
      id: 'thisMonth',
      label: 'Bu Ay',
      icon: 'fa-calendar-alt',
      getValue: () => {
        const today = new Date();
        const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
        const lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0);
        lastDay.setHours(23, 59, 59, 999);
        
        return {
          mode: 'preset',
          startDate: firstDay,
          endDate: lastDay,
          year: today.getFullYear(),
          month: today.getMonth(),
          presetId: 'thisMonth',
          displayText: 'Bu Ay'
        };
      }
    },
    {
      id: 'lastMonth',
      label: 'Geçen Ay',
      icon: 'fa-calendar-alt',
      getValue: () => {
        const today = new Date();
        const firstDay = new Date(today.getFullYear(), today.getMonth() - 1, 1);
        const lastDay = new Date(today.getFullYear(), today.getMonth(), 0);
        lastDay.setHours(23, 59, 59, 999);
        
        return {
          mode: 'preset',
          startDate: firstDay,
          endDate: lastDay,
          year: firstDay.getFullYear(),
          month: firstDay.getMonth(),
          presetId: 'lastMonth',
          displayText: 'Geçen Ay'
        };
      }
    },
    {
      id: 'last7days',
      label: 'Son 7 Gün',
      icon: 'fa-history',
      getValue: () => {
        const today = new Date();
        today.setHours(23, 59, 59, 999);
        const startDate = new Date(today);
        startDate.setDate(today.getDate() - 6);
        startDate.setHours(0, 0, 0, 0);
        
        return {
          mode: 'preset',
          startDate,
          endDate: today,
          presetId: 'last7days',
          displayText: 'Son 7 Gün'
        };
      }
    },
    {
      id: 'last30days',
      label: 'Son 30 Gün',
      icon: 'fa-history',
      getValue: () => {
        const today = new Date();
        today.setHours(23, 59, 59, 999);
        const startDate = new Date(today);
        startDate.setDate(today.getDate() - 29);
        startDate.setHours(0, 0, 0, 0);
        
        return {
          mode: 'preset',
          startDate,
          endDate: today,
          presetId: 'last30days',
          displayText: 'Son 30 Gün'
        };
      }
    }
  ];
  
  // ==================== COMPUTED ====================
  
  /** Tarih seçili mi? */
  hasSelection = computed(() => this.selectedDate() !== null);
  
  /** Görüntüleme metni */
  displayText = computed(() => this.selectedDate()?.displayText || '');
  
  // ==================== AY İSİMLERİ ====================
  
  readonly monthNames = [
    'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
    'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'
  ];
  
  /** Yıl seçenekleri (son 5 yıl + bu yıl) */
  readonly yearOptions = computed(() => {
    const currentYear = new Date().getFullYear();
    const years: number[] = [];
    for (let i = currentYear; i >= currentYear - 5; i--) {
      years.push(i);
    }
    return years;
  });
  
  // ==================== MODAL CONTROL ====================
  
  openModal(): void {
    this.isModalOpen.set(true);
    this.activeMode.set('preset');
    
    // Mevcut seçimi temp'e yükle
    const current = this.selectedDate();
    if (current) {
      if (current.singleDate) {
        this.tempSingleDate.set(this.formatDateForInput(current.singleDate));
      }
      if (current.startDate) {
        this.tempStartDate.set(this.formatDateForInput(current.startDate));
      }
      if (current.endDate) {
        this.tempEndDate.set(this.formatDateForInput(current.endDate));
      }
      if (current.year !== undefined) {
        this.tempYear.set(current.year);
      }
      if (current.month !== undefined) {
        this.tempMonth.set(current.month);
      }
    } else {
      // Varsayılan değerler
      this.tempSingleDate.set(this.formatDateForInput(new Date()));
      this.tempStartDate.set('');
      this.tempEndDate.set('');
      this.tempYear.set(new Date().getFullYear());
      this.tempMonth.set(new Date().getMonth());
    }
    
    console.log('[DateFilterService] Modal opened');
  }
  
  closeModal(): void {
    this.isModalOpen.set(false);
    console.log('[DateFilterService] Modal closed');
  }
  
  setMode(mode: DateSelectionMode): void {
    this.activeMode.set(mode);
  }
  
  // ==================== SELECTION METHODS ====================
  
  /**
   * Preset seç
   */
  selectPreset(presetId: string): void {
    const preset = this.presets.find(p => p.id === presetId);
    if (preset) {
      const selection = preset.getValue();
      this.selectedDate.set(selection);
      this.closeModal();
      console.log('[DateFilterService] Preset selected:', presetId, selection);
    }
  }
  
  /**
   * Tek tarih seç
   */
  selectSingleDate(dateStr: string): void {
    if (!dateStr) return;
    
    const date = new Date(dateStr);
    date.setHours(0, 0, 0, 0);
    
    const selection: DateSelection = {
      mode: 'single',
      singleDate: date,
      displayText: this.formatDisplayDate(date)
    };
    
    this.selectedDate.set(selection);
    this.closeModal();
    console.log('[DateFilterService] Single date selected:', selection);
  }
  
  /**
   * Tarih aralığı seç
   */
  selectDateRange(startStr: string, endStr: string): void {
    if (!startStr || !endStr) return;
    
    const startDate = new Date(startStr);
    startDate.setHours(0, 0, 0, 0);
    
    const endDate = new Date(endStr);
    endDate.setHours(23, 59, 59, 999);
    
    // Başlangıç bitiş'ten sonra olamaz
    if (startDate > endDate) {
      console.warn('[DateFilterService] Start date cannot be after end date');
      return;
    }
    
    const selection: DateSelection = {
      mode: 'range',
      startDate,
      endDate,
      displayText: `${this.formatDisplayDate(startDate)} - ${this.formatDisplayDate(endDate)}`
    };
    
    this.selectedDate.set(selection);
    this.closeModal();
    console.log('[DateFilterService] Date range selected:', selection);
  }
  
  /**
   * Ay seç
   */
  selectMonth(year: number, month: number): void {
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    lastDay.setHours(23, 59, 59, 999);
    
    const selection: DateSelection = {
      mode: 'month',
      startDate: firstDay,
      endDate: lastDay,
      year,
      month,
      displayText: `${this.monthNames[month]} ${year}`
    };
    
    this.selectedDate.set(selection);
    this.closeModal();
    console.log('[DateFilterService] Month selected:', selection);
  }
  
  /**
   * Seçimi temizle
   */
  clearSelection(): void {
    this.selectedDate.set(null);
    console.log('[DateFilterService] Selection cleared');
  }
  
  // ==================== FORMAT HELPERS ====================
  
  /**
   * Input için tarih formatı (YYYY-MM-DD)
   */
  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
  
  /**
   * Görüntüleme için tarih formatı (DD.MM.YYYY)
   */
  formatDisplayDate(date: Date): string {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
  }
  
  /**
   * Prompt'a eklemek için tarih metni (sadece değer, prefix yok)
   */
  getDateTextForPrompt(): string {
    const selection = this.selectedDate();
    if (!selection) return '';
    
    switch (selection.mode) {
      case 'single':
        return selection.displayText;
      case 'range':
        return selection.displayText;
      case 'month':
        return selection.displayText;
      case 'preset':
        // Preset'ler için tarih aralığı veya tek tarih
        if (selection.startDate && selection.endDate) {
          return `${this.formatDisplayDate(selection.startDate)} - ${this.formatDisplayDate(selection.endDate)}`;
        }
        if (selection.singleDate) {
          return this.formatDisplayDate(selection.singleDate);
        }
        return selection.displayText;
      default:
        return selection.displayText;
    }
  }
}
