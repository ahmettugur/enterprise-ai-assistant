import { Component, Input, Output, EventEmitter, signal, HostListener, ElementRef, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CategoryModalComponent } from '../modals/category-modal/category-modal.component';
import { UploadModalComponent } from '../modals/upload-modal/upload-modal.component';
import { DocumentListModalComponent } from '../modals/document-list-modal/document-list-modal.component';

export interface User {
  id?: string;
  email?: string;
  displayName?: string;
  roles?: string[];
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule,
    CategoryModalComponent,
    UploadModalComponent,
    DocumentListModalComponent
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent {
  private elementRef = inject(ElementRef);
  
  // Modal references
  @ViewChild('categoryModal') categoryModal!: CategoryModalComponent;
  @ViewChild('uploadModal') uploadModal!: UploadModalComponent;
  @ViewChild('documentListModal') documentListModal!: DocumentListModalComponent;
  
  @Input() user: User | null = null;
  @Input() isConnected = false;
  @Input() connectionId: string | null = null;
  
  @Output() menuClick = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  isDropdownOpen = signal(false);

  // Dışarı tıklandığında dropdown'ı kapat
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.closeDropdown();
    }
  }

  // Escape tuşuna basıldığında kapat
  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.closeDropdown();
  }

  toggleDropdown(event?: MouseEvent): void {
    if (event) {
      event.stopPropagation();
    }
    console.log('toggleDropdown called, current state:', this.isDropdownOpen());
    this.isDropdownOpen.update(v => !v);
    console.log('toggleDropdown after update:', this.isDropdownOpen());
  }

  closeDropdown(): void {
    this.isDropdownOpen.set(false);
  }

  onMenuClick(): void {
    this.menuClick.emit();
  }

  onLogout(): void {
    this.logout.emit();
    this.closeDropdown();
  }

  getConnectionStatusClass(): string {
    return this.isConnected ? 'connected' : 'disconnected';
  }

  getConnectionStatusText(): string {
    if (this.isConnected) {
      return this.connectionId ? `Bağlı - ${this.connectionId.substring(0, 8)}...` : 'Bağlı';
    }
    return 'Bağlantı kesildi';
  }

  getUserInitials(): string {
    if (this.user?.displayName) {
      return this.user.displayName
        .split(' ')
        .map(n => n[0])
        .join('')
        .toUpperCase()
        .substring(0, 2);
    }
    return 'U';
  }

  // ==================== MODAL METHODS ====================

  /**
   * Kategori modal'ını aç
   */
  openCategoryModal(): void {
    this.closeDropdown();
    this.categoryModal?.open();
  }

  /**
   * Yükleme modal'ını aç
   */
  openUploadModal(): void {
    this.closeDropdown();
    this.uploadModal?.open();
  }

  /**
   * Döküman listesi modal'ını aç
   */
  openDocumentListModal(): void {
    this.closeDropdown();
    this.documentListModal?.open();
  }

  /**
   * Kategori oluşturulduğunda
   */
  onCategoryCreated(): void {
    console.log('[Header] Category created successfully');
  }

  /**
   * DocumentListModal'dan yükleme modal'ını aç
   */
  onOpenUploadFromList(): void {
    this.documentListModal?.close();
    setTimeout(() => {
      this.uploadModal?.open();
    }, 300);
  }
}
