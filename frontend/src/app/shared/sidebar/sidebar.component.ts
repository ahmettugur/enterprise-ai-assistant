import { Component, Input, Output, EventEmitter, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConversationListItem, HistoryService } from '../../core/services/history.service';

// Re-export for backward compatibility
export type Conversation = ConversationListItem;

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
  private historyService = inject(HistoryService);
  
  @Input() isOpen = false;
  @Input() conversations: Conversation[] = [];
  
  @Output() toggle = new EventEmitter<void>();
  @Output() newChat = new EventEmitter<void>();
  @Output() selectConversation = new EventEmitter<Conversation>();
  @Output() deleteConversation = new EventEmitter<string>();
  @Output() updateTitle = new EventEmitter<{ conversationId: string; newTitle: string }>();

  searchQuery = signal('');
  
  // Edit title modal state
  isEditModalOpen = signal(false);
  editingConversationId = signal<string | null>(null);
  editTitle = signal('');
  editTitleError = signal<string | null>(null);
  isUpdatingTitle = signal(false);

  // Delete confirmation modal state
  isDeleteModalOpen = signal(false);
  deletingConversationId = signal<string | null>(null);
  deletingConversationTitle = signal<string>('');
  isDeleting = signal(false);
  
  // Animation state for removal
  removingConversationId = signal<string | null>(null);
  
  get filteredConversations(): Conversation[] {
    const query = this.searchQuery().toLowerCase();
    if (!query) return this.conversations;
    return this.conversations.filter(c => 
      c.title.toLowerCase().includes(query) || 
      c.lastMessage.toLowerCase().includes(query)
    );
  }

  onToggle(): void {
    this.toggle.emit();
  }

  onNewChat(): void {
    this.newChat.emit();
  }

  onSelectConversation(conversation: Conversation): void {
    this.selectConversation.emit(conversation);
  }

  // ========== DELETE CONFIRMATION ==========

  /**
   * Open delete confirmation modal
   */
  openDeleteConfirmModal(event: Event, conversation: Conversation): void {
    event.stopPropagation();
    this.deletingConversationId.set(conversation.id);
    this.deletingConversationTitle.set(conversation.title || 'Bu sohbet');
    this.isDeleteModalOpen.set(true);
  }

  /**
   * Close delete confirmation modal
   */
  closeDeleteConfirmModal(): void {
    this.isDeleteModalOpen.set(false);
    this.deletingConversationId.set(null);
    this.deletingConversationTitle.set('');
  }

  /**
   * Confirm and execute delete with animation
   */
  async confirmDelete(): Promise<void> {
    const conversationId = this.deletingConversationId();
    if (!conversationId) return;

    this.isDeleting.set(true);

    try {
      const success = await this.historyService.deleteConversation(conversationId);
      
      if (success) {
        // Close modal first
        this.closeDeleteConfirmModal();
        
        // Start removal animation
        this.removingConversationId.set(conversationId);
        
        // Wait for animation to complete (300ms), then remove from list
        setTimeout(() => {
          this.historyService.removeConversationFromList(conversationId);
          this.deleteConversation.emit(conversationId);
          this.removingConversationId.set(null);
        }, 300);
      } else {
        this.closeDeleteConfirmModal();
      }
    } catch (error) {
      console.error('Error deleting conversation:', error);
      this.closeDeleteConfirmModal();
    } finally {
      this.isDeleting.set(false);
    }
  }

  // Legacy method for backward compatibility
  onDeleteConversation(event: Event, conversationId: string): void {
    event.stopPropagation();
    const conversation = this.conversations.find(c => c.id === conversationId);
    if (conversation) {
      this.openDeleteConfirmModal(event, conversation);
    }
  }

  formatDate(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    
    if (days === 0) {
      return 'Bugün';
    } else if (days === 1) {
      return 'Dün';
    } else if (days < 7) {
      return `${days} gün önce`;
    } else {
      return date.toLocaleDateString('tr-TR');
    }
  }

  // ========== EDIT TITLE MODAL ==========

  /**
   * Open edit title modal
   */
  openEditTitleModal(event: Event, conversation: Conversation): void {
    event.stopPropagation();
    this.editingConversationId.set(conversation.id);
    this.editTitle.set(conversation.title || '');
    this.editTitleError.set(null);
    this.isEditModalOpen.set(true);
  }

  /**
   * Close edit title modal
   */
  closeEditTitleModal(): void {
    this.isEditModalOpen.set(false);
    this.editingConversationId.set(null);
    this.editTitle.set('');
    this.editTitleError.set(null);
  }

  /**
   * Save updated title
   */
  async saveTitle(): Promise<void> {
    const conversationId = this.editingConversationId();
    const newTitle = this.editTitle().trim();

    if (!conversationId) {
      this.editTitleError.set('Geçersiz konuşma.');
      return;
    }

    if (!newTitle) {
      this.editTitleError.set('Başlık boş olamaz.');
      return;
    }

    if (newTitle.length > 200) {
      this.editTitleError.set('Başlık 200 karakterden uzun olamaz.');
      return;
    }

    this.isUpdatingTitle.set(true);
    this.editTitleError.set(null);

    try {
      const success = await this.historyService.updateConversationTitle(conversationId, newTitle);
      
      if (success) {
        // Emit event for parent component
        this.updateTitle.emit({ conversationId, newTitle });
        this.closeEditTitleModal();
      } else {
        this.editTitleError.set('Başlık güncellenemedi.');
      }
    } catch (error: any) {
      console.error('Error updating title:', error);
      this.editTitleError.set(error.message || 'Bir hata oluştu.');
    } finally {
      this.isUpdatingTitle.set(false);
    }
  }

  /**
   * Handle Enter key in title input
   */
  onTitleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.saveTitle();
    } else if (event.key === 'Escape') {
      this.closeEditTitleModal();
    }
  }
}
