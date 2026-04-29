import { Component, inject, signal, output, computed, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../../../core/services/document.service';
import { DocumentCategoryService } from '../../../core/services/document-category.service';
import { ToastService } from '../../../core/services/toast.service';
import { DocumentType, DocumentUploadData, DocumentCategorySelect } from '../../../core/models/document.models';

/**
 * Döküman Yükleme Modal Componenti
 * document-manager.js'deki showUploadModal fonksiyonunun Angular karşılığı
 * Drag & Drop + dosya seçimi destekler
 */
@Component({
  selector: 'app-upload-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-overlay" [class.show]="isOpen()" (click)="onOverlayClick($event)">
      <div class="modal-container modal-lg" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>
            <i class="fas fa-file-upload"></i>
            Döküman Yükle
          </h3>
          <button class="modal-close" (click)="close()">
            <i class="fas fa-times"></i>
          </button>
        </div>
        
        <div class="modal-body">
          <!-- File Drop Zone -->
          <div 
            class="file-drop-zone"
            [class.drag-over]="isDragOver()"
            [class.has-file]="selectedFile()"
            (dragover)="onDragOver($event)"
            (dragleave)="onDragLeave($event)"
            (drop)="onDrop($event)"
            (click)="triggerFileInput()"
          >
            <input 
              type="file" 
              #fileInput
              (change)="onFileSelected($event)"
              accept=".pdf,.txt,.docx,.doc,.json"
              hidden
            >
            
            @if (!selectedFile()) {
              <div class="drop-zone-content">
                <i class="fas fa-cloud-upload-alt"></i>
                <p>Dosyayı sürükleyip bırakın veya <span class="browse-link">dosya seçin</span></p>
                <small>PDF, TXT, DOCX, DOC, JSON (Maks. 50MB)</small>
              </div>
            } @else {
              <div class="selected-file-info">
                <i class="fas fa-file"></i>
                <span>{{ selectedFile()?.name }}</span>
                <button class="clear-file-btn" (click)="clearSelectedFile($event)">
                  <i class="fas fa-times"></i>
                </button>
              </div>
            }
          </div>
          
          @if (fileError()) {
            <div class="form-error-box">
              <i class="fas fa-exclamation-circle"></i>
              {{ fileError() }}
            </div>
          }
          
          <div class="form-row">
            <div class="form-group flex-1">
              <label for="docDisplayName">Görünen Ad <span class="required">*</span></label>
              <input 
                type="text" 
                id="docDisplayName" 
                [(ngModel)]="displayName"
                placeholder="Döküman adı..."
                class="form-input"
              >
            </div>
            <div class="form-group flex-1">
              <label for="docCategory">Kategori</label>
              <select 
                id="docCategory" 
                [(ngModel)]="categoryId"
                class="form-input"
              >
                <option value="">Kategori seçin...</option>
                @for (cat of categories(); track cat.id) {
                  <option [value]="cat.id">{{ cat.text }}</option>
                }
              </select>
            </div>
          </div>
          
          <div class="form-group">
            <label>Döküman Tipi <span class="required">*</span></label>
            <div class="doc-type-selector">
              <label 
                class="doc-type-option"
                [class.selected]="documentType === DocumentType.Document"
              >
                <input 
                  type="radio" 
                  name="docType" 
                  [value]="DocumentType.Document"
                  [(ngModel)]="documentType"
                >
                <i class="fas fa-file-alt"></i>
                <span>Döküman</span>
              </label>
              <label 
                class="doc-type-option"
                [class.selected]="documentType === DocumentType.QuestionAnswer"
              >
                <input 
                  type="radio" 
                  name="docType" 
                  [value]="DocumentType.QuestionAnswer"
                  [(ngModel)]="documentType"
                >
                <i class="fas fa-question-circle"></i>
                <span>Soru-Cevap</span>
              </label>
            </div>
          </div>
          
          <div class="form-group">
            <label for="docDescription">Açıklama</label>
            <textarea 
              id="docDescription" 
              [(ngModel)]="description"
              placeholder="Döküman hakkında açıklama..."
              class="form-textarea"
              rows="2"
            ></textarea>
          </div>
          
          <div class="form-group">
            <label for="docKeywords">Anahtar Kelimeler</label>
            <input 
              type="text" 
              id="docKeywords" 
              [(ngModel)]="keywords"
              placeholder="kelime1, kelime2, kelime3"
              class="form-input"
            >
            <small class="form-hint">Virgülle ayırarak yazın</small>
          </div>
          
          <!-- Upload Progress -->
          @if (showProgress()) {
            <div class="upload-progress">
              <div class="progress-header">
                <span class="progress-title">{{ uploadStatus().message }}</span>
                <span class="progress-percent">{{ uploadStatus().percent }}%</span>
              </div>
              <div class="progress-bar">
                <div 
                  class="progress-fill"
                  [style.width.%]="uploadStatus().percent"
                  [class.completed]="uploadStatus().status === 'completed'"
                  [class.error]="uploadStatus().status === 'error'"
                ></div>
              </div>
            </div>
          }
        </div>
        
        <div class="modal-footer">
          <button class="btn-secondary" (click)="close()">İptal</button>
          <button 
            class="btn-primary" 
            (click)="uploadDocument()"
            [disabled]="isUploading() || !canUpload()"
          >
            @if (isUploading()) {
              <i class="fas fa-spinner fa-spin"></i>
              Yükleniyor...
            } @else {
              <i class="fas fa-upload"></i>
              Yükle ve İşle
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
    
    .modal-container.modal-lg {
      max-width: 600px;
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
    
    /* File Drop Zone */
    .file-drop-zone {
      border: 2px dashed var(--border-color, #e5e7eb);
      border-radius: 12px;
      padding: 30px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;
      margin-bottom: 16px;
    }
    
    .file-drop-zone:hover,
    .file-drop-zone.drag-over {
      border-color: var(--primary-color, #3b82f6);
      background: rgba(59, 130, 246, 0.05);
    }
    
    .file-drop-zone.has-file {
      border-style: solid;
      border-color: var(--success-color, #10b981);
      background: rgba(16, 185, 129, 0.05);
    }
    
    .drop-zone-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
    }
    
    .drop-zone-content i {
      font-size: 2.5rem;
      color: var(--text-secondary, #9ca3af);
    }
    
    .drop-zone-content p {
      margin: 0;
      color: var(--text-primary, #1f2937);
    }
    
    .drop-zone-content small {
      color: var(--text-secondary, #9ca3af);
    }
    
    .browse-link {
      color: var(--primary-color, #3b82f6);
      font-weight: 500;
    }
    
    .selected-file-info {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 10px;
    }
    
    .selected-file-info i {
      font-size: 1.5rem;
      color: var(--success-color, #10b981);
    }
    
    .selected-file-info span {
      font-weight: 500;
      color: var(--text-primary, #1f2937);
    }
    
    .clear-file-btn {
      background: none;
      border: none;
      padding: 4px 8px;
      cursor: pointer;
      color: var(--text-secondary, #6b7280);
      border-radius: 4px;
    }
    
    .clear-file-btn:hover {
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
    }
    
    .form-error-box {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 14px;
      background: rgba(239, 68, 68, 0.1);
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 8px;
      color: #ef4444;
      font-size: 0.875rem;
      margin-bottom: 16px;
    }
    
    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }
    
    .flex-1 {
      flex: 1;
    }
    
    .form-group {
      margin-bottom: 16px;
    }
    
    .form-row .form-group {
      margin-bottom: 0;
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
    
    .form-textarea {
      resize: vertical;
      min-height: 60px;
    }
    
    .form-hint {
      display: block;
      margin-top: 4px;
      font-size: 0.75rem;
      color: var(--text-secondary, #6b7280);
    }
    
    /* Document Type Selector */
    .doc-type-selector {
      display: flex;
      gap: 12px;
    }
    
    .doc-type-option {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 12px;
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
    }
    
    .doc-type-option input {
      display: none;
    }
    
    .doc-type-option:hover {
      border-color: var(--primary-color, #3b82f6);
    }
    
    .doc-type-option.selected {
      border-color: var(--primary-color, #3b82f6);
      background: rgba(59, 130, 246, 0.05);
      color: var(--primary-color, #3b82f6);
    }
    
    .doc-type-option i {
      font-size: 1.1rem;
    }
    
    /* Upload Progress */
    .upload-progress {
      margin-top: 16px;
      padding: 16px;
      background: var(--bg-secondary, #f9fafb);
      border-radius: 8px;
    }
    
    .progress-header {
      display: flex;
      justify-content: space-between;
      margin-bottom: 8px;
      font-size: 0.875rem;
    }
    
    .progress-title {
      color: var(--text-primary, #1f2937);
    }
    
    .progress-percent {
      color: var(--primary-color, #3b82f6);
      font-weight: 600;
    }
    
    .progress-bar {
      height: 8px;
      background: var(--border-color, #e5e7eb);
      border-radius: 4px;
      overflow: hidden;
    }
    
    .progress-fill {
      height: 100%;
      background: var(--primary-color, #3b82f6);
      border-radius: 4px;
      transition: width 0.3s ease;
    }
    
    .progress-fill.completed {
      background: var(--success-color, #10b981);
    }
    
    .progress-fill.error {
      background: #ef4444;
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
    
    @media (max-width: 600px) {
      .form-row {
        flex-direction: column;
      }
      
      .doc-type-selector {
        flex-direction: column;
      }
    }
  `]
})
export class UploadModalComponent {
  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;
  
  private readonly documentService = inject(DocumentService);
  private readonly categoryService = inject(DocumentCategoryService);
  private readonly toastService = inject(ToastService);

  // Expose enum to template
  readonly DocumentType = DocumentType;

  // Modal state
  readonly isOpen = signal(false);
  readonly isDragOver = signal(false);
  readonly selectedFile = signal<File | null>(null);
  readonly fileError = signal<string | null>(null);
  readonly isUploading = signal(false);
  readonly showProgress = signal(false);

  // Form fields
  displayName = '';
  categoryId = '';
  description = '';
  keywords = '';
  documentType = DocumentType.Document;

  // Categories for dropdown
  readonly categories = signal<DocumentCategorySelect[]>([]);

  // Upload status
  readonly uploadStatus = computed(() => this.documentService.uploadProgress());

  // Can upload check
  readonly canUpload = computed(() => {
    return this.selectedFile() !== null && this.displayName.trim() !== '';
  });

  /**
   * Modal'ı aç
   */
  open(): void {
    this.clearForm();
    this.loadCategories();
    this.isOpen.set(true);
  }

  /**
   * Modal'ı kapat
   */
  close(): void {
    if (this.isUploading()) {
      return; // Yükleme sırasında kapatma
    }
    this.isOpen.set(false);
    this.documentService.resetUploadProgress();
  }

  /**
   * Overlay'e tıklanınca
   */
  onOverlayClick(event: MouseEvent): void {
    // Overlay'e tıklanınca kapatma - sadece butonlarla kapatılır
  }

  /**
   * Kategorileri yükle
   */
  private loadCategories(): void {
    this.categoryService.getAllForSelect().subscribe(categories => {
      this.categories.set(categories);
    });
  }

  /**
   * Formu temizle
   */
  clearForm(): void {
    this.selectedFile.set(null);
    this.fileError.set(null);
    this.displayName = '';
    this.categoryId = '';
    this.description = '';
    this.keywords = '';
    this.documentType = DocumentType.Document;
    this.showProgress.set(false);
    this.isUploading.set(false);
    
    if (this.fileInputRef?.nativeElement) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  /**
   * Dosya input'unu tetikle
   */
  triggerFileInput(): void {
    if (!this.selectedFile()) {
      this.fileInputRef?.nativeElement?.click();
    }
  }

  /**
   * Dosya seçildiğinde
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  /**
   * Drag over
   */
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  /**
   * Drag leave
   */
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  /**
   * Drop
   */
  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
    
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.handleFile(event.dataTransfer.files[0]);
    }
  }

  /**
   * Dosyayı işle
   */
  private handleFile(file: File): void {
    this.fileError.set(null);
    
    const validation = this.documentService.validateFile(file);
    if (!validation.valid) {
      this.fileError.set(validation.error || 'Geçersiz dosya.');
      return;
    }
    
    this.selectedFile.set(file);
    
    // Display name'i otomatik doldur
    if (!this.displayName) {
      this.displayName = file.name.replace(/\.[^/.]+$/, '');
    }
  }

  /**
   * Seçili dosyayı temizle
   */
  clearSelectedFile(event: Event): void {
    event.stopPropagation();
    this.selectedFile.set(null);
    this.fileError.set(null);
    
    if (this.fileInputRef?.nativeElement) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  /**
   * Dökümanı yükle
   */
  uploadDocument(): void {
    const file = this.selectedFile();
    if (!file) {
      this.fileError.set('Lütfen bir dosya seçin.');
      return;
    }
    
    if (!this.displayName.trim()) {
      this.fileError.set('Görünen ad zorunludur.');
      return;
    }
    
    this.isUploading.set(true);
    this.showProgress.set(true);
    
    const uploadData: DocumentUploadData = {
      file: file,
      displayName: this.displayName.trim(),
      documentType: this.documentType,
      categoryId: this.categoryId || undefined,
      description: this.description.trim() || undefined,
      keywords: this.keywords.trim() || undefined
    };
    
    this.documentService.upload(uploadData).subscribe({
      next: (result) => {
        if (result.isSucceed) {
          this.toastService.success(result.userMessage || 'Döküman başarıyla yüklendi ve işlendi!');
          // 1.5 saniye sonra kapat
          setTimeout(() => {
            this.isUploading.set(false);
            this.close();
          }, 1500);
        } else {
          this.isUploading.set(false);
          this.toastService.error(result.userMessage || 'Döküman yüklenemedi.');
          this.fileError.set(result.userMessage || 'Döküman yüklenemedi.');
        }
      },
      error: (error) => {
        console.error('[UploadModal] Error uploading document:', error);
        this.isUploading.set(false);
        this.toastService.error('Bir hata oluştu.');
        this.fileError.set('Bir hata oluştu.');
      }
    });
  }
}
