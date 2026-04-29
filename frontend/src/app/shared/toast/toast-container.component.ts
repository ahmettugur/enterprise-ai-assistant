import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../core/services/toast.service';

/**
 * Toast Container Componenti
 * Tüm toast notification'ları gösterir
 */
@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      @for (toast of toasts(); track toast.id) {
        <div 
          class="toast"
          [class]="'toast-' + toast.type"
          (click)="dismiss(toast.id)"
        >
          <div class="toast-icon">
            @switch (toast.type) {
              @case ('success') {
                <i class="fas fa-check-circle"></i>
              }
              @case ('error') {
                <i class="fas fa-exclamation-circle"></i>
              }
              @case ('warning') {
                <i class="fas fa-exclamation-triangle"></i>
              }
              @default {
                <i class="fas fa-info-circle"></i>
              }
            }
          </div>
          <div class="toast-message">{{ toast.message }}</div>
          <button class="toast-close" (click)="dismiss(toast.id); $event.stopPropagation()">
            <i class="fas fa-times"></i>
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 10px;
      max-width: 400px;
      pointer-events: none;
    }
    
    .toast {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 14px 16px;
      border-radius: 10px;
      background: var(--bg-primary, #ffffff);
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15), 0 2px 10px rgba(0, 0, 0, 0.1);
      animation: slideIn 0.3s ease-out;
      pointer-events: auto;
      cursor: pointer;
      transition: transform 0.2s, opacity 0.2s;
    }
    
    .toast:hover {
      transform: translateX(-4px);
    }
    
    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }
    
    .toast-icon {
      font-size: 1.25rem;
      flex-shrink: 0;
    }
    
    .toast-message {
      flex: 1;
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-primary, #1f2937);
      line-height: 1.4;
    }
    
    .toast-close {
      background: none;
      border: none;
      padding: 4px;
      cursor: pointer;
      color: var(--text-secondary, #9ca3af);
      border-radius: 4px;
      transition: all 0.2s;
      flex-shrink: 0;
    }
    
    .toast-close:hover {
      background: rgba(0, 0, 0, 0.05);
      color: var(--text-primary, #1f2937);
    }
    
    /* Success */
    .toast-success {
      border-left: 4px solid #10b981;
    }
    
    .toast-success .toast-icon {
      color: #10b981;
    }
    
    /* Error */
    .toast-error {
      border-left: 4px solid #ef4444;
    }
    
    .toast-error .toast-icon {
      color: #ef4444;
    }
    
    /* Warning */
    .toast-warning {
      border-left: 4px solid #f59e0b;
    }
    
    .toast-warning .toast-icon {
      color: #f59e0b;
    }
    
    /* Info */
    .toast-info {
      border-left: 4px solid #3b82f6;
    }
    
    .toast-info .toast-icon {
      color: #3b82f6;
    }
    
    /* Mobile responsive */
    @media (max-width: 480px) {
      .toast-container {
        left: 10px;
        right: 10px;
        max-width: none;
      }
    }
  `]
})
export class ToastContainerComponent {
  private readonly toastService = inject(ToastService);
  
  readonly toasts = this.toastService.toasts;

  dismiss(id: number): void {
    this.toastService.remove(id);
  }
}
