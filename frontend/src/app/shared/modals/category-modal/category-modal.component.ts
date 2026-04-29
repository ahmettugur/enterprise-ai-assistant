import { Component, inject, signal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentCategoryService } from '../../../core/services/document-category.service';
import { ToastService } from '../../../core/services/toast.service';
import { CreateDocumentCategoryRequest } from '../../../core/models/document.models';

/**
 * Kategori Oluşturma Modal Componenti
 * document-manager.js'deki showCategoryModal fonksiyonunun Angular karşılığı
 */
@Component({
  selector: 'app-category-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-overlay" [class.show]="isOpen()" (click)="onOverlayClick($event)">
      <div class="modal-container" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>
            <i class="fas fa-folder-plus"></i>
            Yeni Kategori Ekle
          </h3>
          <button class="modal-close" (click)="close()">
            <i class="fas fa-times"></i>
          </button>
        </div>
        
        <div class="modal-body">
          <div class="form-group">
            <label for="categoryId">Kategori ID <span class="required">*</span></label>
            <input 
              type="text" 
              id="categoryId" 
              [(ngModel)]="categoryId" 
              placeholder="ornek-kategori"
              class="form-input"
              [class.invalid]="idError()"
            >
            <small class="form-hint">
              Benzersiz bir ID girin (küçük harf, rakam ve tire kullanabilirsiniz)
            </small>
            @if (idError()) {
              <small class="form-error">{{ idError() }}</small>
            }
          </div>
          
          <div class="form-group">
            <label for="categoryDisplayName">Görünen Ad <span class="required">*</span></label>
            <input 
              type="text" 
              id="categoryDisplayName" 
              [(ngModel)]="displayName" 
              placeholder="Örnek Kategori"
              class="form-input"
            >
          </div>
          
          <div class="form-group">
            <label for="categoryDescription">Açıklama</label>
            <textarea 
              id="categoryDescription" 
              [(ngModel)]="description" 
              placeholder="Kategori hakkında kısa açıklama..."
              class="form-textarea"
              rows="3"
            ></textarea>
          </div>
        </div>
        
        <div class="modal-footer">
          <button class="btn-secondary" (click)="close()">İptal</button>
          <button 
            class="btn-primary" 
            (click)="createCategory()"
            [disabled]="isSubmitting()"
          >
            @if (isSubmitting()) {
              <i class="fas fa-spinner fa-spin"></i>
              Oluşturuluyor...
            } @else {
              <i class="fas fa-plus"></i>
              Kategori Oluştur
            }
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      opacity: 0;
      visibility: hidden;
      transition: all 0.3s ease;
    }
    
    .modal-overlay.show {
      opacity: 1;
      visibility: visible;
    }
    
    .modal-container {
      background: var(--bg-primary, #ffffff);
      border-radius: 12px;
      width: 100%;
      max-width: 480px;
      max-height: 90vh;
      overflow: hidden;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
      transform: scale(0.9);
      transition: transform 0.3s ease;
    }
    
    .modal-overlay.show .modal-container {
      transform: scale(1);
    }
    
    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px 20px;
      border-bottom: 1px solid var(--border-color, #e5e7eb);
    }
    
    .modal-header h3 {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--text-primary, #1f2937);
      display: flex;
      align-items: center;
      gap: 10px;
    }
    
    .modal-header h3 i {
      color: var(--primary-color, #3b82f6);
    }
    
    .modal-close {
      background: none;
      border: none;
      padding: 8px;
      cursor: pointer;
      color: var(--text-secondary, #6b7280);
      border-radius: 6px;
      transition: all 0.2s;
    }
    
    .modal-close:hover {
      background: var(--bg-secondary, #f3f4f6);
      color: var(--text-primary, #1f2937);
    }
    
    .modal-body {
      padding: 20px;
      overflow-y: auto;
      max-height: calc(90vh - 140px);
    }
    
    .form-group {
      margin-bottom: 16px;
    }
    
    .form-group label {
      display: block;
      margin-bottom: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--text-primary, #1f2937);
    }
    
    .required {
      color: #ef4444;
    }
    
    .form-input,
    .form-textarea {
      width: 100%;
      padding: 10px 12px;
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      font-size: 0.9rem;
      background: var(--bg-primary, #ffffff);
      color: var(--text-primary, #1f2937);
      transition: all 0.2s;
      box-sizing: border-box;
    }
    
    .form-input:focus,
    .form-textarea:focus {
      outline: none;
      border-color: var(--primary-color, #3b82f6);
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }
    
    .form-input.invalid {
      border-color: #ef4444;
    }
    
    .form-textarea {
      resize: vertical;
      min-height: 80px;
    }
    
    .form-hint {
      display: block;
      margin-top: 4px;
      font-size: 0.75rem;
      color: var(--text-secondary, #6b7280);
    }
    
    .form-error {
      display: block;
      margin-top: 4px;
      font-size: 0.75rem;
      color: #ef4444;
    }
    
    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      padding: 16px 20px;
      border-top: 1px solid var(--border-color, #e5e7eb);
      background: var(--bg-secondary, #f9fafb);
    }
    
    .btn-secondary,
    .btn-primary {
      padding: 10px 18px;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 8px;
      transition: all 0.2s;
    }
    
    .btn-secondary {
      background: var(--bg-primary, #ffffff);
      border: 1px solid var(--border-color, #e5e7eb);
      color: var(--text-primary, #1f2937);
    }
    
    .btn-secondary:hover {
      background: var(--bg-secondary, #f3f4f6);
    }
    
    .btn-primary {
      background: var(--primary-color, #3b82f6);
      border: none;
      color: white;
    }
    
    .btn-primary:hover:not(:disabled) {
      background: var(--primary-hover, #2563eb);
    }
    
    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
  `]
})
export class CategoryModalComponent {
  private readonly categoryService = inject(DocumentCategoryService);
  private readonly toastService = inject(ToastService);

  // Modal state
  readonly isOpen = signal(false);
  readonly isSubmitting = signal(false);
  readonly idError = signal<string | null>(null);

  // Form fields
  categoryId = '';
  displayName = '';
  description = '';

  // Output events
  readonly created = output<void>();
  readonly closed = output<void>();

  /**
   * Modal'ı aç
   */
  open(): void {
    this.clearForm();
    this.isOpen.set(true);
  }

  /**
   * Modal'ı kapat
   */
  close(): void {
    this.isOpen.set(false);
    this.closed.emit();
  }

  /**
   * Overlay'e tıklanınca kapat
   */
  onOverlayClick(event: MouseEvent): void {
    // Overlay'e tıklanınca kapatma - sadece butonlarla kapatılır
    // this.close();
  }

  /**
   * Formu temizle
   */
  clearForm(): void {
    this.categoryId = '';
    this.displayName = '';
    this.description = '';
    this.idError.set(null);
  }

  /**
   * Kategori oluştur
   */
  async createCategory(): Promise<void> {
    // Validasyon
    this.idError.set(null);

    if (!this.categoryId.trim() || !this.displayName.trim()) {
      this.idError.set('Kategori ID ve Görünen Ad zorunludur.');
      return;
    }

    // ID format kontrolü
    if (!this.categoryService.validateCategoryId(this.categoryId.trim())) {
      this.idError.set('Kategori ID sadece küçük harf, rakam ve tire içerebilir.');
      return;
    }

    this.isSubmitting.set(true);

    const request: CreateDocumentCategoryRequest = {
      id: this.categoryId.trim(),
      displayName: this.displayName.trim(),
      description: this.description.trim() || undefined
    };

    this.categoryService.create(request).subscribe({
      next: (result) => {
        this.isSubmitting.set(false);
        if (result.isSucceed) {
          this.toastService.success(result.userMessage || 'Kategori başarıyla oluşturuldu!');
          this.created.emit();
          this.close();
        } else {
          this.toastService.error(result.userMessage || 'Kategori oluşturulamadı.');
          this.idError.set(result.userMessage || 'Kategori oluşturulamadı.');
        }
      },
      error: (error) => {
        console.error('[CategoryModal] Error creating category:', error);
        this.isSubmitting.set(false);
        this.toastService.error('Bir hata oluştu.');
        this.idError.set('Bir hata oluştu.');
      }
    });
  }
}
