import { Injectable, signal, computed } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
  duration: number;
}

/**
 * Toast Notification Servisi
 * document-manager.js'deki showToast fonksiyonunun Angular karşılığı
 */
@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastId = 0;
  private readonly toastsSignal = signal<Toast[]>([]);

  readonly toasts = computed(() => this.toastsSignal());

  /**
   * Toast göster
   */
  show(message: string, type: ToastType = 'info', duration = 3000): number {
    const id = ++this.toastId;
    
    const toast: Toast = {
      id,
      message,
      type,
      duration
    };
    
    this.toastsSignal.update(toasts => [...toasts, toast]);
    
    // Auto-remove after duration
    if (duration > 0) {
      setTimeout(() => {
        this.remove(id);
      }, duration);
    }
    
    return id;
  }

  /**
   * Başarı toast'ı
   */
  success(message: string, duration = 3000): number {
    return this.show(message, 'success', duration);
  }

  /**
   * Hata toast'ı
   */
  error(message: string, duration = 4000): number {
    return this.show(message, 'error', duration);
  }

  /**
   * Bilgi toast'ı
   */
  info(message: string, duration = 3000): number {
    return this.show(message, 'info', duration);
  }

  /**
   * Uyarı toast'ı
   */
  warning(message: string, duration = 3500): number {
    return this.show(message, 'warning', duration);
  }

  /**
   * Toast'ı kaldır
   */
  remove(id: number): void {
    this.toastsSignal.update(toasts => toasts.filter(t => t.id !== id));
  }

  /**
   * Tüm toast'ları temizle
   */
  clear(): void {
    this.toastsSignal.set([]);
  }
}
