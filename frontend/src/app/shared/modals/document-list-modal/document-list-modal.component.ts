import { Component, inject, signal, output, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../../../core/services/document.service';
import { DocumentCategoryService } from '../../../core/services/document-category.service';
import { ToastService } from '../../../core/services/toast.service';
import { DocumentDisplayInfoList, DocumentType, DocumentCategorySelect } from '../../../core/models/document.models';

/**
 * Döküman Listesi Modal Componenti
 * document-manager.js'deki showDocumentListModal fonksiyonunun Angular karşılığı
 */
@Component({
  selector: 'app-document-list-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <div class="modal-overlay" [class.show]="isOpen()" (click)="onOverlayClick($event)">
      <div class="modal-container modal-xl" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>
            <i class="fas fa-list"></i>
            Dökümanlarım
          </h3>
          <button class="modal-close" (click)="close()">
            <i class="fas fa-times"></i>
          </button>
        </div>
        
        <div class="modal-body">
          <!-- Toolbar -->
          <div class="doc-list-toolbar">
            <div class="doc-search">
              <i class="fas fa-search"></i>
              <input 
                type="text" 
                placeholder="Döküman ara..."
                [(ngModel)]="searchTerm"
                (ngModelChange)="onSearchChange($event)"
              >
            </div>
            <div class="doc-filter">
              <select 
                [(ngModel)]="categoryFilter"
                (ngModelChange)="onCategoryFilterChange($event)"
              >
                <option value="">Tüm Kategoriler</option>
                @for (cat of categories(); track cat.id) {
                  <option [value]="cat.id">{{ cat.text }}</option>
                }
              </select>
            </div>
            <button class="btn-primary-sm" (click)="openUploadModal.emit()">
              <i class="fas fa-plus"></i>
              Yeni Yükle
            </button>
          </div>
          
          <!-- Document List -->
          <div class="doc-list-container">
            @if (isLoading()) {
              <div class="doc-list-loading">
                <div class="loading-spinner"></div>
                <span>Yükleniyor...</span>
              </div>
            } @else if (filteredDocuments().length === 0) {
              <div class="doc-list-empty">
                @if (searchTerm || categoryFilter) {
                  <i class="fas fa-search"></i>
                  <p>Döküman bulunamadı</p>
                } @else {
                  <i class="fas fa-file-alt"></i>
                  <p>Henüz döküman yüklenmemiş</p>
                }
              </div>
            } @else {
              <div class="doc-list-table">
                <div class="doc-list-header">
                  <div class="doc-col doc-col-name">Döküman Adı</div>
                  <div class="doc-col doc-col-type">Tip</div>
                  <div class="doc-col doc-col-category">Kategori</div>
                  <div class="doc-col doc-col-status">Durum</div>
                  <div class="doc-col doc-col-date">Tarih</div>
                  <div class="doc-col doc-col-actions">İşlemler</div>
                </div>
                <div class="doc-list-body">
                  @for (doc of filteredDocuments(); track doc.id) {
                    <div class="doc-list-row">
                      <div class="doc-col doc-col-name">
                        <i class="fas fa-file-alt"></i>
                        <div class="doc-name-info">
                          <span class="doc-display-name">{{ doc.displayName }}</span>
                          <small class="doc-file-name">{{ doc.fileName }}</small>
                        </div>
                      </div>
                      <div class="doc-col doc-col-type">
                        <span 
                          class="doc-type-badge"
                          [class.type-document]="doc.documentType === DocumentType.Document"
                          [class.type-qa]="doc.documentType === DocumentType.QuestionAnswer"
                        >
                          {{ getTypeLabel(doc.documentType) }}
                        </span>
                      </div>
                      <div class="doc-col doc-col-category">
                        {{ doc.categoryName || '-' }}
                      </div>
                      <div class="doc-col doc-col-status">
                        <span 
                          class="doc-status-badge"
                          [class.status-active]="doc.hasEmbeddings"
                          [class.status-pending]="!doc.hasEmbeddings"
                        >
                          {{ getStatusLabel(doc) }}
                        </span>
                      </div>
                      <div class="doc-col doc-col-date">
                        {{ doc.createdAt | date:'dd.MM.yyyy' }}
                      </div>
                      <div class="doc-col doc-col-actions">
                        <button 
                          class="doc-action-btn delete"
                          (click)="deleteDocument(doc)"
                          title="Sil"
                          [disabled]="deletingId() === doc.id"
                        >
                          @if (deletingId() === doc.id) {
                            <i class="fas fa-spinner fa-spin"></i>
                          } @else {
                            <i class="fas fa-trash"></i>
                          }
                        </button>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        </div>
        
        <div class="modal-footer">
          <div class="doc-count">
            {{ filteredDocuments().length }} döküman
          </div>
          <button class="btn-secondary" (click)="close()">Kapat</button>
        </div>
      </div>
    </div>
    
    <!-- Delete Confirmation -->
    @if (showDeleteConfirm()) {
      <div class="confirm-overlay" (click)="cancelDelete()">
        <div class="confirm-dialog" (click)="$event.stopPropagation()">
          <div class="confirm-icon">
            <i class="fas fa-exclamation-triangle"></i>
          </div>
          <h4>Dökümanı Sil</h4>
          <p>
            <strong>{{ documentToDelete()?.displayName }}</strong> dökümanını silmek istediğinizden emin misiniz?
          </p>
          <p class="confirm-warning">Bu işlem geri alınamaz!</p>
          <div class="confirm-actions">
            <button class="btn-secondary" (click)="cancelDelete()">İptal</button>
            <button class="btn-danger" (click)="confirmDelete()">
              <i class="fas fa-trash"></i>
              Sil
            </button>
          </div>
        </div>
      </div>
    }
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
    
    .modal-container.modal-xl {
      max-width: 900px;
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
    
    /* Toolbar */
    .doc-list-toolbar {
      display: flex;
      gap: 12px;
      margin-bottom: 16px;
      flex-wrap: wrap;
    }
    
    .doc-search {
      flex: 1;
      min-width: 200px;
      position: relative;
    }
    
    .doc-search i {
      position: absolute;
      left: 12px;
      top: 50%;
      transform: translateY(-50%);
      color: var(--text-secondary, #9ca3af);
    }
    
    .doc-search input {
      width: 100%;
      padding: 10px 12px 10px 36px;
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      font-size: 0.875rem;
      background: var(--bg-primary, #ffffff);
      color: var(--text-primary, #1f2937);
      box-sizing: border-box;
    }
    
    .doc-search input:focus {
      outline: none;
      border-color: var(--primary-color, #3b82f6);
    }
    
    .doc-filter select {
      padding: 10px 12px;
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      font-size: 0.875rem;
      background: var(--bg-primary, #ffffff);
      color: var(--text-primary, #1f2937);
      cursor: pointer;
      min-width: 160px;
    }
    
    .doc-filter select:focus {
      outline: none;
      border-color: var(--primary-color, #3b82f6);
    }
    
    .btn-primary-sm {
      padding: 10px 16px;
      background: var(--primary-color, #3b82f6);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 6px;
      transition: all 0.2s;
      white-space: nowrap;
    }
    
    .btn-primary-sm:hover {
      background: var(--primary-hover, #2563eb);
    }
    
    /* Document List */
    .doc-list-container {
      min-height: 300px;
    }
    
    .doc-list-loading,
    .doc-list-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 60px 20px;
      color: var(--text-secondary, #9ca3af);
    }
    
    .doc-list-loading i,
    .doc-list-empty i {
      font-size: 2.5rem;
      margin-bottom: 12px;
    }
    
    .loading-spinner {
      width: 40px;
      height: 40px;
      border: 3px solid var(--border-color, #e5e7eb);
      border-top-color: var(--primary-color, #3b82f6);
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 12px;
    }
    
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
    
    .doc-list-table {
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      overflow: hidden;
    }
    
    .doc-list-header {
      display: flex;
      background: var(--bg-secondary, #f9fafb);
      border-bottom: 1px solid var(--border-color, #e5e7eb);
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      color: var(--text-secondary, #6b7280);
    }
    
    .doc-list-body {
      max-height: 400px;
      overflow-y: auto;
    }
    
    .doc-list-row {
      display: flex;
      border-bottom: 1px solid var(--border-color, #e5e7eb);
      transition: background 0.2s;
    }
    
    .doc-list-row:last-child {
      border-bottom: none;
    }
    
    .doc-list-row:hover {
      background: var(--bg-secondary, #f9fafb);
    }
    
    .doc-col {
      padding: 12px;
      display: flex;
      align-items: center;
    }
    
    .doc-col-name {
      flex: 2;
      gap: 10px;
      min-width: 200px;
    }
    
    .doc-col-name > i {
      color: var(--text-secondary, #9ca3af);
      font-size: 1.1rem;
    }
    
    .doc-name-info {
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    
    .doc-display-name {
      font-weight: 500;
      color: var(--text-primary, #1f2937);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    
    .doc-file-name {
      font-size: 0.75rem;
      color: var(--text-secondary, #9ca3af);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    
    .doc-col-type {
      flex: 0 0 100px;
    }
    
    .doc-col-category {
      flex: 1;
      min-width: 100px;
      color: var(--text-secondary, #6b7280);
    }
    
    .doc-col-status {
      flex: 0 0 130px;
    }
    
    .doc-col-date {
      flex: 0 0 90px;
      color: var(--text-secondary, #6b7280);
      font-size: 0.875rem;
    }
    
    .doc-col-actions {
      flex: 0 0 60px;
      justify-content: center;
    }
    
    /* Badges */
    .doc-type-badge {
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 500;
    }
    
    .doc-type-badge.type-document {
      background: rgba(59, 130, 246, 0.1);
      color: #3b82f6;
    }
    
    .doc-type-badge.type-qa {
      background: rgba(139, 92, 246, 0.1);
      color: #8b5cf6;
    }
    
    .doc-status-badge {
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 500;
    }
    
    .doc-status-badge.status-active {
      background: rgba(16, 185, 129, 0.1);
      color: #10b981;
    }
    
    .doc-status-badge.status-pending {
      background: rgba(245, 158, 11, 0.1);
      color: #f59e0b;
    }
    
    /* Action buttons */
    .doc-action-btn {
      background: none;
      border: none;
      padding: 8px;
      cursor: pointer;
      border-radius: 6px;
      transition: all 0.2s;
      color: var(--text-secondary, #6b7280);
    }
    
    .doc-action-btn:hover {
      background: var(--bg-secondary, #f3f4f6);
    }
    
    .doc-action-btn.delete:hover {
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
    }
    
    .doc-action-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    
    .modal-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 20px;
      border-top: 1px solid var(--border-color, #e5e7eb);
      background: var(--bg-secondary, #f9fafb);
    }
    
    .doc-count {
      font-size: 0.875rem;
      color: var(--text-secondary, #6b7280);
    }
    
    .btn-secondary {
      padding: 10px 18px;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      background: var(--bg-primary, #ffffff);
      border: 1px solid var(--border-color, #e5e7eb);
      color: var(--text-primary, #1f2937);
      transition: all 0.2s;
    }
    
    .btn-secondary:hover {
      background: var(--bg-secondary, #f3f4f6);
    }
    
    /* Confirm Dialog */
    .confirm-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1100;
    }
    
    .confirm-dialog {
      background: var(--bg-primary, #ffffff);
      border-radius: 12px;
      padding: 24px;
      max-width: 400px;
      text-align: center;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    }
    
    .confirm-icon {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      background: rgba(239, 68, 68, 0.1);
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 16px;
    }
    
    .confirm-icon i {
      font-size: 1.5rem;
      color: #ef4444;
    }
    
    .confirm-dialog h4 {
      margin: 0 0 8px;
      font-size: 1.1rem;
      color: var(--text-primary, #1f2937);
    }
    
    .confirm-dialog p {
      margin: 0 0 8px;
      color: var(--text-secondary, #6b7280);
      font-size: 0.9rem;
    }
    
    .confirm-warning {
      color: #ef4444 !important;
      font-size: 0.8rem !important;
    }
    
    .confirm-actions {
      display: flex;
      gap: 10px;
      justify-content: center;
      margin-top: 20px;
    }
    
    .btn-danger {
      padding: 10px 18px;
      background: #ef4444;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      display: inline-flex;
      align-items: center;
      gap: 6px;
      transition: all 0.2s;
    }
    
    .btn-danger:hover {
      background: #dc2626;
    }
    
    @media (max-width: 768px) {
      .doc-list-header {
        display: none;
      }
      
      .doc-list-row {
        flex-wrap: wrap;
        padding: 12px;
      }
      
      .doc-col {
        padding: 4px 8px;
      }
      
      .doc-col-name {
        flex: 1 1 100%;
        margin-bottom: 8px;
      }
      
      .doc-col-type,
      .doc-col-status {
        flex: 1;
      }
      
      .doc-col-category,
      .doc-col-date {
        display: none;
      }
    }
  `]
})
export class DocumentListModalComponent {
  private readonly documentService = inject(DocumentService);
  private readonly categoryService = inject(DocumentCategoryService);
  private readonly toastService = inject(ToastService);

  // Expose enum to template
  readonly DocumentType = DocumentType;

  // Modal state
  readonly isOpen = signal(false);
  readonly isLoading = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly showDeleteConfirm = signal(false);
  readonly documentToDelete = signal<DocumentDisplayInfoList | null>(null);

  // Filters
  searchTerm = '';
  categoryFilter = '';

  // Categories for dropdown
  readonly categories = signal<DocumentCategorySelect[]>([]);

  // Computed filtered documents
  readonly filteredDocuments = computed(() => this.documentService.filteredDocuments());

  // Output events
  readonly openUploadModal = output<void>();
  readonly closed = output<void>();

  /**
   * Modal'ı aç
   */
  open(): void {
    this.isOpen.set(true);
    this.loadData();
  }

  /**
   * Modal'ı kapat
   */
  close(): void {
    this.isOpen.set(false);
    this.closed.emit();
  }

  /**
   * Overlay'e tıklanınca
   */
  onOverlayClick(event: MouseEvent): void {
    // Overlay'e tıklanınca kapatma - sadece butonlarla kapatılır
  }

  /**
   * Verileri yükle
   */
  private loadData(): void {
    this.isLoading.set(true);
    console.log('[DocumentListModal] Loading data...');
    
    // Kategorileri yükle
    this.categoryService.getAllForSelect().subscribe(categories => {
      console.log('[DocumentListModal] Categories loaded:', categories);
      this.categories.set(categories);
    });
    
    // Dökümanları yükle
    this.documentService.getAll().subscribe(documents => {
      console.log('[DocumentListModal] Documents loaded:', documents);
      console.log('[DocumentListModal] Documents signal:', this.documentService.documents());
      console.log('[DocumentListModal] Filtered documents:', this.filteredDocuments());
      this.isLoading.set(false);
    });
  }

  /**
   * Arama değiştiğinde
   */
  onSearchChange(term: string): void {
    this.documentService.setSearchTerm(term);
  }

  /**
   * Kategori filtresi değiştiğinde
   */
  onCategoryFilterChange(categoryId: string): void {
    this.documentService.setCategoryFilter(categoryId);
  }

  /**
   * Döküman tipi label'ı
   */
  getTypeLabel(type: DocumentType): string {
    return this.documentService.getDocumentTypeLabel(type);
  }

  /**
   * Durum label'ı
   */
  getStatusLabel(doc: DocumentDisplayInfoList): string {
    return this.documentService.getStatusLabel(doc);
  }

  /**
   * Döküman sil (onay iste)
   */
  deleteDocument(doc: DocumentDisplayInfoList): void {
    this.documentToDelete.set(doc);
    this.showDeleteConfirm.set(true);
  }

  /**
   * Silme iptal
   */
  cancelDelete(): void {
    this.showDeleteConfirm.set(false);
    this.documentToDelete.set(null);
  }

  /**
   * Silmeyi onayla
   */
  confirmDelete(): void {
    const doc = this.documentToDelete();
    if (!doc) return;
    
    this.showDeleteConfirm.set(false);
    this.deletingId.set(doc.id);
    
    this.documentService.delete(doc.id).subscribe({
      next: (result) => {
        this.deletingId.set(null);
        this.documentToDelete.set(null);
        
        if (result.isSucceed) {
          this.toastService.success(result.userMessage || 'Döküman başarıyla silindi.');
        } else {
          this.toastService.error(result.userMessage || 'Döküman silinemedi.');
          console.error('[DocumentListModal] Delete failed:', result.userMessage);
        }
      },
      error: (error) => {
        console.error('[DocumentListModal] Error deleting document:', error);
        this.toastService.error('Bir hata oluştu.');
        this.deletingId.set(null);
        this.documentToDelete.set(null);
      }
    });
  }
}
