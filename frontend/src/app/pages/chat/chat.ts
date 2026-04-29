import { Component, OnInit, OnDestroy, AfterViewInit, signal, computed, ViewChild, ElementRef, inject, effect, ViewEncapsulation, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DomSanitizer, SafeHtml, SafeResourceUrl } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { SignalRService, ReceivedMessage, StreamingMessage } from '../../core/services/signalr.service';
import { ChatService } from '../../core/services/chat.service';
import { HistoryService, ConversationListItem } from '../../core/services/history.service';
import { MarkdownRendererService } from '../../core/services/markdown-renderer.service';
import { AutocompleteService, FilterType, SelectOption, FilterTag } from '../../core/services/filters';
import { FeedbackService, FeedbackType } from '../../core/services/feedback.service';
import { SidebarComponent, Conversation } from '../../shared/sidebar/sidebar.component';
import { HeaderComponent } from '../../shared/header/header.component';
import { SafeHtmlPipe } from '../../shared/pipes/safe-html.pipe';

// jQuery global declaration
declare const $: any;

interface Message {
  id: string;
  type: 'user' | 'ai' | 'system' | 'loading' | 'error';
  content: string;
  safeContent?: SafeHtml;  // Sanitized HTML content
  timestamp: Date;
  isStreaming?: boolean;
  suggestions?: string[];
  showFeedback?: boolean;  // Whether to show feedback buttons (false for template menus)
}

interface ChatOption {
  type: 'chat' | 'document' | 'report';
  icon: string;
  title: string;
  description: string;
}

// Minimized report interface for multiple reports
interface OpenReport {
  id: string;
  url: string;
  safeUrl?: SafeResourceUrl;
  title: string;
  loading: boolean;
  minimized: boolean;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent, HeaderComponent, SafeHtmlPipe],
  templateUrl: './chat.html',
  styleUrl: './chat.css',
  encapsulation: ViewEncapsulation.None
})
export class Chat implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;
  @ViewChild('messageInput') messageInput!: ElementRef;
  @ViewChild('fileInputRef') fileInputRef!: ElementRef<HTMLInputElement>;

  private authService = inject(AuthService);
  private signalRService = inject(SignalRService);
  private chatService = inject(ChatService);
  private historyService = inject(HistoryService);
  private markdownRenderer = inject(MarkdownRendererService);
  autocompleteService = inject(AutocompleteService);  // Public for template access (renamed from autocompleteService)
  feedbackService = inject(FeedbackService);  // Public for template access
  private router = inject(Router);
  private ngZone = inject(NgZone);
  private sanitizer = inject(DomSanitizer);

  // Subscriptions
  private subscriptions: Subscription[] = [];

  // State signals
  messages = signal<Message[]>([]);
  inputMessage = '';
  isLoading = signal(false);
  isSidebarOpen = signal(false);
  selectedFile = signal<File | null>(null);
  conversationId = signal<string | null>(null);
  loadingText = signal('Düşünüyor...');
  errorMessage = signal<string | null>(null);

  // Report iframe state - All open reports (both active and minimized)
  openReports = signal<OpenReport[]>([]);

  // Active report ID (the one that is maximized, null if all minimized)
  activeReportId = signal<string | null>(null);

  // More reports dropdown state
  showMoreReportsDropdown = signal(false);

  // Max visible minimized reports
  private readonly MAX_VISIBLE_REPORTS = 3;

  // Computed: Get active report
  activeReport = computed(() => {
    const id = this.activeReportId();
    if (!id) return null;
    return this.openReports().find(r => r.id === id) || null;
  });

  // Computed: Get minimized reports
  minimizedReports = computed(() => {
    const activeId = this.activeReportId();
    return this.openReports().filter(r => r.id !== activeId);
  });

  // Computed: Get visible minimized reports (first 3)
  visibleMinimizedReports = computed(() => {
    return this.minimizedReports().slice(0, this.MAX_VISIBLE_REPORTS);
  });

  // Computed: Get hidden minimized reports (after first 3)
  hiddenMinimizedReports = computed(() => {
    return this.minimizedReports().slice(this.MAX_VISIBLE_REPORTS);
  });

  // Legacy signals for backward compatibility
  reportIframeUrl = computed(() => this.activeReport()?.url || null);
  reportIframeTitle = computed(() => this.activeReport()?.title || 'Rapor');
  reportIframeLoading = computed(() => this.activeReport()?.loading ?? true);

  // Safe iframe URL (computed)
  safeReportIframeUrl = computed((): SafeResourceUrl | null => {
    const report = this.activeReport();
    if (!report) return null;
    return report.safeUrl || null;
  });

  // Streaming için birikimli mesaj (report.js'deki fullStreamingMessage gibi)
  private fullStreamingMessage = '';

  // API URL (dosya linkleri ve scheduled reports için)
  private readonly uploadUrl = 'https://localhost:7041';
  private readonly apiUrl = 'https://localhost:7041';

  // Filters - eski uyumluluk için
  activeFilters = signal<string[]>([]);

  // Filter Modal state
  filterSearchTerm = signal('');
  filteredOptions = signal<SelectOption[]>([]);
  selectedFilterOption = signal<SelectOption | null>(null);

  // Current user
  currentUser = this.authService.currentUser;
  isConnected = this.signalRService.isConnected;
  connectionId = this.signalRService.connectionId;

  // ReAct Pattern - AI düşünme süreci görünürlüğü
  reactSteps = computed(() => this.signalRService.reactSteps());
  showReActPanel = signal(false);

  toggleReActPanel(): void {
    this.showReActPanel.update(v => !v);
  }

  // Welcome options
  chatOptions: ChatOption[] = [
    { type: 'chat', icon: 'fas fa-comments', title: 'Chat', description: 'Genel sorularınız ve sohbet için' },
    { type: 'document', icon: 'fas fa-file-alt', title: 'Döküman İşlemleri', description: 'Sisteme yüklenen belgelerde arama işlemleri' },
    { type: 'report', icon: 'fas fa-chart-bar', title: 'Rapor İşlemleri', description: 'Veri analizi ve rapor oluşturma' }
  ];

  // Conversations from history
  conversations = this.historyService.conversations;

  constructor() {
    // Global window.reportManager handler - Backend template'lerindeki onclick için
    this.setupGlobalReportManager();

    // Effect ile signal'ları dinle (geriye uyumluluk için)
    effect(() => {
      const message = this.signalRService.lastMessage();
      if (message) {
        // Artık subscription'lardan geliyor, burayı yorum satırına alalım
        // this.handleIncomingMessage(message);
      }
    });

    effect(() => {
      const streamingContent = this.signalRService.streamingMessage();
      if (streamingContent) {
        // Artık subscription'lardan geliyor, burayı yorum satırına alalım  
        // this.handleStreamingMessage(streamingContent);
      }
    });
  }

  ngOnInit(): void {
    this.signalRService.connect();
    this.historyService.startAutoRefresh(); // Load and start auto-refresh
    this.showWelcomeMessage();
    this.setupSubscriptions();

    // Filter verilerini yükle
    this.autocompleteService.loadAllData();
  }

  ngAfterViewInit(): void {
    // Event Delegation ile dinamik içerikleri handle et
    this.setupEventDelegation();
  }

  ngOnDestroy(): void {
    // Tüm subscription'ları temizle
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.subscriptions = [];
    this.signalRService.disconnect();
    this.historyService.stopAutoRefresh();

    // Global handler'ı temizle
    this.cleanupGlobalReportManager();
  }

  // ========== EVENT DELEGATION (Dinamik HTML için best practice) ==========

  /**
   * Event Delegation ile tüm click'leri container üzerinden yakala
   * Bu yöntem MutationObserver'dan daha güvenilir çalışır
   */
  private setupEventDelegation(): void {
    const container = this.messagesContainer?.nativeElement;
    console.log('setupEventDelegation - container:', container);

    if (!container) {
      console.warn('Messages container not found, retrying...');
      setTimeout(() => this.setupEventDelegation(), 100);
      return;
    }

    // Container üzerinde tek bir click listener
    container.addEventListener('click', (e: Event) => {
      const target = e.target as HTMLElement;

      // .option-button'a veya içindeki elemente tıklandı mı?
      const optionButton = target.closest('.option-button') as HTMLElement;

      if (optionButton) {
        console.log('Option button outerHTML:', optionButton.outerHTML);
        e.preventDefault();
        e.stopPropagation();

        console.log('Option button clicked via delegation:', optionButton);

        // data-option attribute
        const dataOption = optionButton.getAttribute('data-option');
        console.log('data-option attribute:', dataOption);

        if (dataOption) {
          console.log('Found data-option:', dataOption);
          this.ngZone.run(() => this.handleOptionClick(dataOption));
          return;
        }

        // data-url attribute (ready reports için)
        const dataUrl = optionButton.getAttribute('data-url');
        if (dataUrl) {
          console.log('Found data-url:', dataUrl);
          const reportTitle = optionButton.textContent?.trim() || 'Rapor';
          this.ngZone.run(() => this.openReportInIframe(dataUrl, reportTitle));
          return;
        }

        console.log('No data-option or data-url found on button');
      }

      // .suggestion-button'a tıklandı mı?
      const suggestionButton = target.closest('.suggestion-button') as HTMLElement;
      if (suggestionButton) {
        e.preventDefault();
        e.stopPropagation();

        // data-suggestion attribute (report.js ile aynı)
        const dataSuggestion = suggestionButton.getAttribute('data-suggestion');
        if (dataSuggestion) {
          this.ngZone.run(() => this.handleSuggestionClick(dataSuggestion));
          return;
        }

        const buttonText = suggestionButton.textContent?.trim();
        if (buttonText) {
          this.ngZone.run(() => this.handleSuggestionClick(buttonText));
        }
        return;
      }

      // .schedule-report-button'a tıklandı mı? (Zamanla butonu)
      const scheduleButton = target.closest('.schedule-report-button') as HTMLElement;
      if (scheduleButton) {
        e.preventDefault();
        e.stopPropagation();

        const messageId = scheduleButton.getAttribute('data-message-id') || '';
        const reportName = scheduleButton.getAttribute('data-report-name') || 'Rapor';

        console.log('Schedule button clicked:', messageId, reportName);
        this.ngZone.run(() => this.openScheduleModal(messageId, reportName));
        return;
      }
    });

    console.log('Event delegation setup complete');
  }

  /**
   * Zamanla modalını aç - report.js openScheduleModal ile aynı
   */
  private openScheduleModal(messageId: string, reportName: string): void {
    console.log('openScheduleModal çağrıldı:', messageId, reportName);

    // Modal zaten varsa kaldır
    const existingModal = document.getElementById('scheduleReportModal');
    if (existingModal) {
      existingModal.remove();
    }

    // Gün seçenekleri oluştur
    const dayOfMonthOptions = Array.from({ length: 28 }, (_, i) =>
      `<option value="${i + 1}">${i + 1}</option>`
    ).join('');

    // Saat seçenekleri oluştur
    const hourOptions = Array.from({ length: 24 }, (_, i) =>
      `<option value="${i}" ${i === 9 ? 'selected' : ''}>${i.toString().padStart(2, '0')}:00</option>`
    ).join('');

    const modalHtml = `
      <div class="schedule-modal-overlay" id="scheduleReportModal">
        <div class="schedule-modal">
          <div class="schedule-modal-header">
            <h3><i class="fas fa-clock"></i> Raporu Zamanla</h3>
            <button class="schedule-modal-close" onclick="reportManager.closeScheduleModal()">
              <i class="fas fa-times"></i>
            </button>
          </div>
          <div class="schedule-modal-body">
            <input type="hidden" id="scheduleMessageId" value="${messageId || ''}">
            
            <div class="schedule-form-group">
              <label for="scheduleReportName">
                <i class="fas fa-tag"></i> Rapor Adı
              </label>
              <input type="text" id="scheduleReportName" 
                     placeholder="Örn: Günlük Satış Raporu" 
                     value="${this.escapeHtml(reportName || '')}" 
                     maxlength="200">
            </div>

            <div class="schedule-form-group">
              <label for="scheduleDescription">
                <i class="fas fa-align-left"></i> Açıklama (Opsiyonel)
              </label>
              <textarea id="scheduleDescription" 
                        placeholder="Bu rapor hakkında kısa bir açıklama..." 
                        rows="2" 
                        maxlength="500"></textarea>
            </div>

            <div class="schedule-form-group">
              <label>
                <i class="fas fa-magic"></i> Hızlı Seçim
              </label>
              <div class="schedule-preset-buttons">
                <button type="button" class="schedule-preset-btn" data-cron="0 9 * * 1-5" title="Hafta içi her gün 09:00">
                  <i class="fas fa-briefcase"></i> Hafta içi 09:00
                </button>
                <button type="button" class="schedule-preset-btn" data-cron="0 9 * * *" title="Her gün 09:00">
                  <i class="fas fa-sun"></i> Her gün 09:00
                </button>
                <button type="button" class="schedule-preset-btn" data-cron="0 9 * * 1" title="Her Pazartesi 09:00">
                  <i class="fas fa-calendar-week"></i> Haftalık (Pzt)
                </button>
                <button type="button" class="schedule-preset-btn" data-cron="0 9 1 * *" title="Her ayın 1'i 09:00">
                  <i class="fas fa-calendar"></i> Aylık (1.)
                </button>
              </div>
            </div>

            <div class="schedule-form-group">
              <label>
                <i class="fas fa-sliders-h"></i> Özel Zamanlama
              </label>
              <div class="cron-builder">
                <div class="cron-builder-row">
                  <div class="cron-field">
                    <label>Sıklık</label>
                    <select id="cronFrequency" onchange="reportManager.updateCronFromBuilder()">
                      <option value="daily">Her gün</option>
                      <option value="weekdays" selected>Hafta içi</option>
                      <option value="weekly">Haftada bir</option>
                      <option value="monthly">Ayda bir</option>
                    </select>
                  </div>
                  <div class="cron-field" id="cronDayOfWeekField" style="display:none;">
                    <label>Gün</label>
                    <select id="cronDayOfWeek" onchange="reportManager.updateCronFromBuilder()">
                      <option value="1">Pazartesi</option>
                      <option value="2">Salı</option>
                      <option value="3">Çarşamba</option>
                      <option value="4">Perşembe</option>
                      <option value="5">Cuma</option>
                      <option value="6">Cumartesi</option>
                      <option value="0">Pazar</option>
                    </select>
                  </div>
                  <div class="cron-field" id="cronDayOfMonthField" style="display:none;">
                    <label>Ayın Günü</label>
                    <select id="cronDayOfMonth" onchange="reportManager.updateCronFromBuilder()">
                      ${dayOfMonthOptions}
                    </select>
                  </div>
                  <div class="cron-field">
                    <label>Saat</label>
                    <select id="cronHour" onchange="reportManager.updateCronFromBuilder()">
                      ${hourOptions}
                    </select>
                  </div>
                  <div class="cron-field">
                    <label>Dakika</label>
                    <select id="cronMinute" onchange="reportManager.updateCronFromBuilder()">
                      <option value="0" selected>00</option>
                      <option value="15">15</option>
                      <option value="30">30</option>
                      <option value="45">45</option>
                    </select>
                  </div>
                </div>
                <div class="cron-preview">
                  <i class="fas fa-info-circle"></i>
                  <span id="cronPreviewText">Hafta içi her gün saat 09:00'da çalışacak</span>
                </div>
              </div>
            </div>

            <div class="schedule-form-group">
              <label for="scheduleCronExpression">
                <i class="fas fa-code"></i> Cron İfadesi
                <button type="button" class="cron-toggle-btn" onclick="reportManager.toggleAdvancedCron()" title="Gelişmiş düzenleme">
                  <i class="fas fa-edit"></i>
                </button>
              </label>
              <input type="text" id="scheduleCronExpression" 
                     placeholder="0 9 * * 1-5" 
                     value="0 9 * * 1-5"
                     readonly>
              <small class="schedule-cron-help" id="cronAdvancedHelp" style="display:none;">
                Format: dakika saat gün ay haftanın_günü (örn: 0 9 * * 1-5 = Hafta içi 09:00)
              </small>
            </div>

            <div class="schedule-form-row">
              <div class="schedule-form-group">
                <label for="scheduleRecipientEmails">
                  <i class="fas fa-envelope"></i> E-posta Adresleri (Opsiyonel)
                </label>
                <input type="text" id="scheduleRecipientEmails" 
                       placeholder="ornek@firma.com, diger@firma.com">
                <small>Virgülle ayırarak birden fazla adres girebilirsiniz</small>
              </div>
            </div>

            <div class="schedule-form-group">
              <label class="schedule-checkbox-label">
                <input type="checkbox" id="scheduleSendToTeams">
                <span><i class="fab fa-microsoft"></i> Microsoft Teams'e Gönder</span>
              </label>
            </div>
          </div>
          <div class="schedule-modal-footer">
            <button class="schedule-btn-cancel" onclick="reportManager.closeScheduleModal()">
              <i class="fas fa-times"></i> İptal
            </button>
            <button class="schedule-btn-save" onclick="reportManager.saveScheduledReport()">
              <i class="fas fa-save"></i> Kaydet
            </button>
          </div>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    // Preset butonlarına event listener ekle
    this.setupSchedulePresetButtons();

    // ESC tuşu ile kapatma
    document.addEventListener('keydown', this.handleScheduleModalEscape);
  }

  /**
   * ESC tuşu handler'ı - report.js ile aynı
   */
  private handleScheduleModalEscape = (e: KeyboardEvent): void => {
    if (e.key === 'Escape') {
      this.closeScheduleModal();
    }
  };

  /**
   * Preset butonlarına event listener ekler - report.js ile aynı
   */
  private setupSchedulePresetButtons(): void {
    const presetButtons = document.querySelectorAll('.schedule-preset-btn');
    const cronInput = document.getElementById('scheduleCronExpression') as HTMLInputElement;

    presetButtons.forEach(btn => {
      btn.addEventListener('click', () => {
        // Tüm butonlardan active class'ını kaldır
        presetButtons.forEach(b => b.classList.remove('active'));
        // Tıklanan butona active ekle
        btn.classList.add('active');
        // Cron değerini input'a yaz
        if (cronInput) {
          cronInput.value = (btn as HTMLElement).dataset['cron'] || '';
          // Builder'ı da güncelle
          this.updateBuilderFromCron(cronInput.value);
          // Preview'ı güncelle
          this.updateCronPreview();
        }
      });
    });
  }

  /**
   * Cron builder'dan cron ifadesi oluşturur - report.js ile aynı
   */
  private updateCronFromBuilder(): void {
    const frequency = (document.getElementById('cronFrequency') as HTMLSelectElement)?.value || 'weekdays';
    const dayOfWeek = (document.getElementById('cronDayOfWeek') as HTMLSelectElement)?.value || '1';
    const dayOfMonth = (document.getElementById('cronDayOfMonth') as HTMLSelectElement)?.value || '1';
    const hour = (document.getElementById('cronHour') as HTMLSelectElement)?.value || '9';
    const minute = (document.getElementById('cronMinute') as HTMLSelectElement)?.value || '0';

    // Alanları göster/gizle
    const dayOfWeekField = document.getElementById('cronDayOfWeekField');
    const dayOfMonthField = document.getElementById('cronDayOfMonthField');

    if (dayOfWeekField) dayOfWeekField.style.display = frequency === 'weekly' ? 'block' : 'none';
    if (dayOfMonthField) dayOfMonthField.style.display = frequency === 'monthly' ? 'block' : 'none';

    // Cron oluştur
    let cron = '';
    switch (frequency) {
      case 'daily':
        cron = `${minute} ${hour} * * *`;
        break;
      case 'weekdays':
        cron = `${minute} ${hour} * * 1-5`;
        break;
      case 'weekly':
        cron = `${minute} ${hour} * * ${dayOfWeek}`;
        break;
      case 'monthly':
        cron = `${minute} ${hour} ${dayOfMonth} * *`;
        break;
      default:
        cron = `${minute} ${hour} * * 1-5`;
    }

    const cronInput = document.getElementById('scheduleCronExpression') as HTMLInputElement;
    if (cronInput) {
      cronInput.value = cron;
    }

    // Preset butonlarından active'i kaldır
    document.querySelectorAll('.schedule-preset-btn').forEach(b => b.classList.remove('active'));

    // Preview'ı güncelle
    this.updateCronPreview();
  }

  /**
   * Cron ifadesinden builder'ı günceller - report.js ile aynı
   */
  private updateBuilderFromCron(cron: string): void {
    const parts = cron.split(' ');
    if (parts.length !== 5) return;

    const [minute, hour, dayOfMonth, , dayOfWeek] = parts;

    // Dakika
    const minuteSelect = document.getElementById('cronMinute') as HTMLSelectElement;
    if (minuteSelect) minuteSelect.value = minute;

    // Saat
    const hourSelect = document.getElementById('cronHour') as HTMLSelectElement;
    if (hourSelect) hourSelect.value = hour;

    // Sıklık ve ilgili alanları belirle
    const frequencySelect = document.getElementById('cronFrequency') as HTMLSelectElement;
    const dayOfWeekField = document.getElementById('cronDayOfWeekField');
    const dayOfMonthField = document.getElementById('cronDayOfMonthField');

    if (dayOfWeek === '1-5') {
      if (frequencySelect) frequencySelect.value = 'weekdays';
      if (dayOfWeekField) dayOfWeekField.style.display = 'none';
      if (dayOfMonthField) dayOfMonthField.style.display = 'none';
    } else if (dayOfWeek === '*' && dayOfMonth === '*') {
      if (frequencySelect) frequencySelect.value = 'daily';
      if (dayOfWeekField) dayOfWeekField.style.display = 'none';
      if (dayOfMonthField) dayOfMonthField.style.display = 'none';
    } else if (dayOfMonth !== '*') {
      if (frequencySelect) frequencySelect.value = 'monthly';
      if (dayOfWeekField) dayOfWeekField.style.display = 'none';
      if (dayOfMonthField) dayOfMonthField.style.display = 'block';
      const dayOfMonthSelect = document.getElementById('cronDayOfMonth') as HTMLSelectElement;
      if (dayOfMonthSelect) dayOfMonthSelect.value = dayOfMonth;
    } else {
      if (frequencySelect) frequencySelect.value = 'weekly';
      if (dayOfWeekField) dayOfWeekField.style.display = 'block';
      if (dayOfMonthField) dayOfMonthField.style.display = 'none';
      const dayOfWeekSelect = document.getElementById('cronDayOfWeek') as HTMLSelectElement;
      if (dayOfWeekSelect) dayOfWeekSelect.value = dayOfWeek;
    }
  }

  /**
   * Cron preview metnini günceller - report.js ile aynı
   */
  private updateCronPreview(): void {
    const frequency = (document.getElementById('cronFrequency') as HTMLSelectElement)?.value || 'weekdays';
    const dayOfWeek = (document.getElementById('cronDayOfWeek') as HTMLSelectElement)?.value || '1';
    const dayOfMonth = (document.getElementById('cronDayOfMonth') as HTMLSelectElement)?.value || '1';
    const hour = (document.getElementById('cronHour') as HTMLSelectElement)?.value || '9';
    const minute = (document.getElementById('cronMinute') as HTMLSelectElement)?.value || '0';

    const dayNames: Record<string, string> = {
      '0': 'Pazar', '1': 'Pazartesi', '2': 'Salı', '3': 'Çarşamba',
      '4': 'Perşembe', '5': 'Cuma', '6': 'Cumartesi'
    };

    let previewText = '';
    const timeStr = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;

    switch (frequency) {
      case 'daily':
        previewText = `Her gün saat ${timeStr}'de çalışacak`;
        break;
      case 'weekdays':
        previewText = `Hafta içi her gün saat ${timeStr}'de çalışacak`;
        break;
      case 'weekly':
        previewText = `Her ${dayNames[dayOfWeek]} saat ${timeStr}'de çalışacak`;
        break;
      case 'monthly':
        previewText = `Her ayın ${dayOfMonth}. günü saat ${timeStr}'de çalışacak`;
        break;
    }

    const previewElement = document.getElementById('cronPreviewText');
    if (previewElement) {
      previewElement.textContent = previewText;
    }
  }

  /**
   * Gelişmiş cron düzenleme modunu açar/kapar - report.js ile aynı
   */
  private toggleAdvancedCron(): void {
    const cronInput = document.getElementById('scheduleCronExpression') as HTMLInputElement;
    const helpText = document.getElementById('cronAdvancedHelp');

    if (cronInput) {
      const isReadonly = cronInput.hasAttribute('readonly');
      if (isReadonly) {
        cronInput.removeAttribute('readonly');
        cronInput.classList.add('editable');
        if (helpText) helpText.style.display = 'block';
      } else {
        cronInput.setAttribute('readonly', 'readonly');
        cronInput.classList.remove('editable');
        if (helpText) helpText.style.display = 'none';
        // Builder'ı güncelle
        this.updateBuilderFromCron(cronInput.value);
        this.updateCronPreview();
      }
    }
  }

  /**
   * Zamanlama modalını kapatır - report.js ile aynı
   */
  private closeScheduleModal(): void {
    const modal = document.getElementById('scheduleReportModal');
    if (modal) {
      modal.classList.add('closing');
      setTimeout(() => {
        modal.remove();
      }, 200);
    }
    document.removeEventListener('keydown', this.handleScheduleModalEscape);
  }

  /**
   * Zamanlanmış raporu kaydet - report.js ile aynı
   */
  private async saveScheduledReport(): Promise<void> {
    const messageId = (document.getElementById('scheduleMessageId') as HTMLInputElement)?.value;
    const reportName = (document.getElementById('scheduleReportName') as HTMLInputElement)?.value?.trim();
    const description = (document.getElementById('scheduleDescription') as HTMLTextAreaElement)?.value?.trim();
    const cronExpression = (document.getElementById('scheduleCronExpression') as HTMLInputElement)?.value?.trim();
    const recipientEmailsRaw = (document.getElementById('scheduleRecipientEmails') as HTMLInputElement)?.value?.trim();
    const sendToTeams = (document.getElementById('scheduleSendToTeams') as HTMLInputElement)?.checked || false;

    // Validasyon
    if (!reportName) {
      this.showScheduleError('Rapor adı zorunludur.');
      return;
    }

    if (!cronExpression) {
      this.showScheduleError('Cron ifadesi zorunludur.');
      return;
    }

    if (!messageId) {
      this.showScheduleError('Mesaj ID bulunamadı. Lütfen tekrar deneyin.');
      return;
    }

    // E-posta adreslerini diziye çevir
    const recipientEmails = recipientEmailsRaw
      ? recipientEmailsRaw.split(',').map(e => e.trim()).filter(e => e.length > 0)
      : [];

    // API request body
    const requestBody = {
      name: reportName,
      description: description || null,
      cronExpression: cronExpression,
      messageId: messageId,
      recipientEmails: recipientEmails,
      sendToTeams: sendToTeams,
      isActive: true
    };

    // Kaydet butonunu disable et ve loading göster
    const saveBtn = document.querySelector('.schedule-btn-save') as HTMLButtonElement;
    if (saveBtn) {
      saveBtn.disabled = true;
      saveBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Kaydediliyor...';
    }

    try {
      const token = this.authService.getToken();
      const headers: Record<string, string> = {
        'Content-Type': 'application/json'
      };

      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const response = await fetch(`${this.apiUrl}/api/v1/scheduled-reports/from-message`, {
        method: 'POST',
        headers: headers,
        body: JSON.stringify(requestBody)
      });

      const result = await response.json();

      if (response.ok && result.isSucceed) {
        this.showScheduleSuccess('Zamanlanmış rapor başarıyla oluşturuldu!');
        setTimeout(() => {
          this.closeScheduleModal();
        }, 1500);
      } else {
        const errorMessage = result.message || result.errors?.join(', ') || 'Bir hata oluştu';
        this.showScheduleError(errorMessage);
      }
    } catch (error) {
      console.error('Schedule report error:', error);
      this.showScheduleError('Bağlantı hatası. Lütfen tekrar deneyin.');
    } finally {
      // Butonu tekrar aktif et
      if (saveBtn) {
        saveBtn.disabled = false;
        saveBtn.innerHTML = '<i class="fas fa-save"></i> Kaydet';
      }
    }
  }

  /**
   * Modal içinde hata mesajı göster - report.js ile aynı
   */
  private showScheduleError(message: string): void {
    this.showScheduleMessage(message, 'error');
  }

  /**
   * Modal içinde başarı mesajı göster - report.js ile aynı
   */
  private showScheduleSuccess(message: string): void {
    this.showScheduleMessage(message, 'success');
  }

  /**
   * Modal içinde mesaj göster - report.js ile aynı
   */
  private showScheduleMessage(message: string, type: 'error' | 'success'): void {
    // Mevcut mesajı kaldır
    const existingMsg = document.querySelector('.schedule-message');
    if (existingMsg) {
      existingMsg.remove();
    }

    const msgHtml = `
      <div class="schedule-message schedule-message-${type}">
        <i class="fas fa-${type === 'error' ? 'exclamation-circle' : 'check-circle'}"></i>
        ${this.escapeHtml(message)}
      </div>
    `;

    const modalBody = document.querySelector('.schedule-modal-body');
    if (modalBody) {
      modalBody.insertAdjacentHTML('afterbegin', msgHtml);
    }

    // 5 saniye sonra mesajı kaldır
    setTimeout(() => {
      const msg = document.querySelector('.schedule-message');
      if (msg) {
        msg.remove();
      }
    }, 5000);
  }

  // ========== GLOBAL REPORT MANAGER (Backend template onclick için) ==========

  /**
   * Backend template'lerindeki onclick="reportManager.selectOption('...')" için
   * Global window.reportManager handler'ı oluştur
   */
  private setupGlobalReportManager(): void {
    (window as any).reportManager = {
      selectOption: (optionValue: string) => this.handleOptionClick(optionValue),
      handleSuggestionClick: (suggestion: string) => this.handleSuggestionClick(suggestion),
      openScheduleModal: (messageId: string, reportName: string) => this.openScheduleModal(messageId, reportName),
      closeScheduleModal: () => this.closeScheduleModal(),
      saveScheduledReport: () => this.saveScheduledReport(),
      updateCronFromBuilder: () => this.updateCronFromBuilder(),
      toggleAdvancedCron: () => this.toggleAdvancedCron()
    };
    console.log('Global reportManager handler registered');
  }

  /**
   * Global handler'ı temizle
   */
  private cleanupGlobalReportManager(): void {
    if ((window as any).reportManager) {
      delete (window as any).reportManager;
      console.log('Global reportManager handler removed');
    }
  }

  /**
   * Option butonuna tıklandığında çalışır
   * Backend template'lerindeki selectOption çağrılarını handle eder
   */
  handleOptionClick(optionValue: string): void {
    console.log('handleOptionClick called with:', optionValue);

    // Option value'ya göre mesaj belirle
    let message = '';

    switch (optionValue) {
      case 'chat':
        message = 'Chat seçeneğini seçtim, genel sohbet yapmak istiyorum.';
        break;
      case 'document':
        message = 'Döküman işlemleri seçeneğini seçtim, belge işlemleri yapmak istiyorum.';
        break;
      case 'report':
        message = 'Rapor işlemleri seçeneğini seçtim, veri analizi ve rapor oluşturmak istiyorum.';
        break;
      default:
        // Kategori ID veya diğer değerler için direkt gönder
        message = optionValue;
        break;
    }

    if (message) {
      this.inputMessage = message;
      this.sendMessage();
    }
  }

  /**
   * Suggestion butonuna tıklandığında çalışır
   */
  handleSuggestionClick(suggestion: string): void {
    console.log('Suggestion clicked:', suggestion);
    this.inputMessage = suggestion;
    this.sendMessage();
  }

  // SignalR event'lerine subscribe ol
  private setupSubscriptions(): void {
    // Message alındığında
    this.subscriptions.push(
      this.signalRService.onMessageReceived.subscribe((message: ReceivedMessage) => {
        console.log('Chat: Message received', message);
        this.handleReceiveMessage(message);
      })
    );

    // Streaming mesaj alındığında
    this.subscriptions.push(
      this.signalRService.onStreamingReceived.subscribe((streamingMessage: StreamingMessage) => {
        console.log('Chat: Streaming received');
        this.handleReceiveStreamingMessage(streamingMessage);
      })
    );

    // Loading mesaj alındığında
    this.subscriptions.push(
      this.signalRService.onLoadingReceived.subscribe((loadingMessage: string) => {
        console.log('Chat: Loading message received', loadingMessage);
        this.handleLoadingMessage(loadingMessage);
      })
    );

    // Hata mesajı alındığında
    this.subscriptions.push(
      this.signalRService.onErrorReceived.subscribe((error: string) => {
        console.log('Chat: Error received', error);
        this.showErrorMessage(error);
      })
    );
  }

  // Toggle sidebar
  toggleSidebar(): void {
    this.isSidebarOpen.update(v => !v);
  }

  // Show welcome message
  showWelcomeMessage(): void {
    const user = this.currentUser();
    const displayName = user?.displayName || user?.email?.split('@')[0] || '';
    const greeting = displayName ? `Merhaba, <strong>${displayName}</strong>!` : '<strong>Merhaba!</strong>';

    const welcomeMessage: Message = {
      id: 'welcome',
      type: 'ai',
      content: `<p>${greeting} Size nasıl yardımcı olabilirim? Aşağıdaki seçeneklerden birini seçerek asistan ile sohbete başlayabilirsiniz:</p>`,
      timestamp: new Date()
    };
    this.messages.set([welcomeMessage]);
  }

  // Select chat option
  selectOption(option: ChatOption): void {
    let message = '';
    switch (option.type) {
      case 'chat':
        message = 'Chat seçeneğini seçtim, genel sohbet yapmak istiyorum.';
        break;
      case 'document':
        message = 'Döküman işlemleri seçeneğini seçtim, belge işlemleri yapmak istiyorum.';
        break;
      case 'report':
        message = 'Rapor işlemleri seçeneğini seçtim, veri analizi ve rapor oluşturmak istiyorum.';
        break;
    }
    this.inputMessage = message;
    this.sendMessage();
  }

  // Send message (report.js sendReportPrompt'tan taşındı)
  async sendMessage(): Promise<void> {
    const message = this.inputMessage.trim();
    if (!message && !this.selectedFile()) return;

    // SignalR bağlantı kontrolü
    if (!this.signalRService.isConnected()) {
      this.showErrorMessage('SignalR bağlantısı aktif değil. Lütfen bağlantının kurulmasını bekleyin.');
      return;
    }

    // Connection ID kontrolü
    if (!this.signalRService.connectionId()) {
      this.showErrorMessage('Connection ID bulunamadı. Lütfen sayfayı yenileyin.');
      return;
    }

    // Streaming state'i temizle
    this.signalRService.clearStreamingState();
    this.signalRService.clearReActSteps(); // ReAct adımlarını temizle
    this.resetStreamingState(); // fullStreamingMessage'ı sıfırla

    // Add user message to chat
    const userMessage: Message = {
      id: this.generateId(),
      type: 'user',
      content: message,
      timestamp: new Date()
    };
    this.messages.update(msgs => [...msgs, userMessage]);

    // Clear input
    this.inputMessage = '';

    // Add loading indicator
    this.addLoadingMessage();

    // Scroll to bottom
    this.scrollToBottom();

    try {
      this.isLoading.set(true);

      // ConversationId'yi ChatService ile senkronize et (history'den seçilmiş conversation'a devam etmek için)
      if (this.conversationId()) {
        this.chatService.conversationId.set(this.conversationId());
      }

      // ChatService üzerinden API'ye gönder
      const response = await this.chatService.sendChatbotMessage(message);

      // ConversationId'yi güncelle (hem component hem service'te)
      if (response.isSucceed && response.resultData?.conversationId) {
        this.conversationId.set(response.resultData.conversationId);
        this.chatService.conversationId.set(response.resultData.conversationId);
      }

      // NOT: Dosya kullanıcı tarafından manuel olarak kaldırılmalı
      // Otomatik kaldırma yapılmıyor - kullanıcı kaldır butonunu kullanmalı

      // Grafik scriptlerini çalıştır - report.js ile aynı (300ms bekle)
      this.executeChartsAfterResponse();

      // Fetch başarılı oldu, SignalR'dan mesaj gelecek
      // report.js ile aynı: Typing indicator'ı kaldır
      this.removeLoadingMessage();
      console.log('Message sent successfully, waiting for SignalR response...');

      // report.js ile aynı: AutocompleteManager seçimlerini temizle ve UI'ı güncelle
      this.autocompleteService.clearAllSelections();

    } catch (error: any) {
      console.error('Error sending message:', error);
      this.removeLoadingMessage();
      this.showErrorMessage(error?.message || 'Mesaj gönderilemedi');
    } finally {
      this.isLoading.set(false);
    }
  }

  // Handle incoming message from SignalR
  private handleIncomingMessage(message: any): void {
    this.removeLoadingMessage();

    // Use messageId from backend if available, otherwise generate one
    const messageId = message.messageId || this.generateId();

    const aiMessage: Message = {
      id: messageId,
      type: 'ai',
      content: message.htmlMessage || message.content || '',
      timestamp: new Date(),
      suggestions: message.suggestions
    };

    this.messages.update(msgs => [...msgs, aiMessage]);
    this.scrollToBottom();
  }

  // ========== YENİ HANDLER'LAR (report.js'den taşındı) ==========

  /**
   * ReceiveMessage handler - Tam mesaj alındığında
   * report.js handleReceiveMessage fonksiyonu ile birebir aynı
   */
  private handleReceiveMessage(response: ReceivedMessage): void {
    console.log('ReceiveMessage called:', response);

    // report.js ile aynı: Uzun işlem bittiğini işaretle (ping'i yeniden başlat)
    // this.isLongRunningOperation = false; // SignalR service'de yapılıyor
    console.log('Long running operation completed - ping resumed');

    // report.js ile aynı: Önce typing indicator'ı kaldır
    this.removeLoadingMessage();
    this.isLoading.set(false);

    // Streaming tamamlandı - mevcut streaming mesajların flag'ini false yap
    this.resetStreamingState();

    // systemMessage'a göre işlem yap - report.js ile aynı: split('_')[0]
    // report.js'deki gibi: this.handleAskAction = "ask", this.handleWelcomeAction = "welcome" vs.
    const systemMessageAction = response.systemMessage?.split('_')[0] || '';

    console.log('systemMessageAction:', systemMessageAction);
    console.log('resultData:', response.resultData);

    // report.js ile aynı karşılaştırma (case-sensitive)
    if (systemMessageAction === 'ask') {
      console.log('Ask response:', response.resultData);
      this.handleAskAction(response);
      this.scrollToBottom();
      return;
    }

    if (systemMessageAction === 'welcome') {
      console.log('Welcome response:', response.resultData);
      this.handleWelcomeAction(response);
      this.scrollToBottom();
      return;
    }

    // document action - data array içeren sonuçlar için özel handler
    if (systemMessageAction === 'document') {
      console.log('Document response:', response.resultData);
      this.handleDocumentAction(response);
      this.scrollToBottom();
      return;
    }

    // chat action
    if (systemMessageAction === 'chat') {
      console.log('Chat response:', response.resultData);
      this.handleAskAction(response);
      this.scrollToBottom();
      return;
    }

    // report action - URL veya summary içeriyorsa da report olarak işle
    if (systemMessageAction === 'report' ||
      (response.resultData?.htmlMessage?.includes('/output-folder/') || response.resultData?.summary)) {
      console.log('Report response:', response.resultData);
      this.handleReportAction(response);
      this.scrollToBottom();
      console.log('Message handled successfully');
      return;
    }

    // Varsayılan işlem
    console.log('Default response:', response.resultData);
    this.handleDefaultAction(response);
    this.scrollToBottom();
  }

  private handleAskAction(response: ReceivedMessage): void {
    const resultData = response.resultData;

    // Use messageId from backend if available, otherwise generate one
    const messageId = resultData?.messageId || this.generateId();

    // Template menus have isSuccess=false, real AI responses have isSuccess=true
    const showFeedback = resultData?.isSuccess !== false;

    // Check if there's an existing streaming message to update
    // OR find the last AI message (streaming may have been reset already)
    const existingStreamingIndex = this.messages().findIndex(m => m.isStreaming);

    if (existingStreamingIndex >= 0) {
      // Update existing streaming message with final content and messageId
      this.messages.update(msgs => msgs.map((m, i) => {
        if (i === existingStreamingIndex) {
          return {
            ...m,
            id: messageId,  // Update with real messageId from backend
            content: resultData?.htmlMessage || m.content,
            suggestions: resultData?.suggestions,
            isStreaming: false,
            showFeedback
          };
        }
        return m;
      }));
    } else {
      // No streaming message found - check if last message is an AI message with temp ID
      // This happens when resetStreamingState() was called before this handler
      const messages = this.messages();
      const lastMessage = messages[messages.length - 1];

      if (lastMessage && lastMessage.type === 'ai' && !lastMessage.id.includes('-')) {
        // Last message has a generated ID (UUID format), update it with real messageId
        this.messages.update(msgs => msgs.map((m, i) => {
          if (i === messages.length - 1) {
            return {
              ...m,
              id: messageId,
              suggestions: resultData?.suggestions,
              isStreaming: false,
              showFeedback
            };
          }
          return m;
        }));
      } else if (lastMessage && lastMessage.type === 'ai') {
        // Update last AI message with messageId (keep existing content from streaming)
        this.messages.update(msgs => msgs.map((m, i) => {
          if (i === messages.length - 1 && m.type === 'ai') {
            return {
              ...m,
              id: messageId,
              suggestions: resultData?.suggestions,
              isStreaming: false,
              showFeedback
            };
          }
          return m;
        }));
      } else {
        // No AI message found, add new one (non-streaming response)
        // History'den yüklenirken content ham markdown olabilir, parse et
        const rawContent = resultData?.htmlMessage || '';
        const parsedContent = this.parseMarkdownContent(rawContent);

        const aiMessage: Message = {
          id: messageId,
          type: 'ai',
          content: parsedContent,
          timestamp: new Date(),
          suggestions: resultData?.suggestions,
          showFeedback
        };

        this.messages.update(msgs => [...msgs, aiMessage]);
      }
    }

    // Conversation ID'yi güncelle
    if (resultData?.conversationId) {
      this.conversationId.set(resultData.conversationId);
    }
  }

  private handleWelcomeAction(response: ReceivedMessage): void {
    // Mevcut mesajları temizle ve welcome mesajını göster
    const resultData = response.resultData;

    const welcomeMessage: Message = {
      id: 'welcome',
      type: 'ai',
      content: resultData?.htmlMessage || '<p><strong>Merhaba!</strong> Size nasıl yardımcı olabilirim?</p>',
      timestamp: new Date(),
      suggestions: resultData?.suggestions
    };

    this.messages.set([welcomeMessage]);
  }

  /**
   * Report action handler - report.js ile aynı
   * Rapor linki ve zamanla butonu oluşturur
   */
  private handleReportAction(response: ReceivedMessage): void {
    const resultData = response.resultData;

    // report.js'deki gibi rapor UI'ını oluştur
    const summary = resultData?.summary || 'Rapor';
    const reportUrl = resultData?.htmlMessage || '#';
    const messageId = resultData?.messageId || this.generateId();

    // report.js ile aynı: Rapor adını escape et (özel karakterler için)
    // NOT: URL escape EDİLMEZ (report.js ile aynı)
    const escapedSummary = this.escapeHtml(summary);
    const escapedReportName = summary.replace(/'/g, "\\'");

    // HTML içeriği oluştur - report.js ile aynı yapı
    const content = `
      <div class="report-link-container">
        <div class="report-title">
          <i class="fas fa-chart-bar"></i>
          <span>Rapor Hazır: ${escapedSummary}</span>
        </div>
        <div class="report-buttons-container">
          <a href="${reportUrl}" target="_blank" class="modern-report-link" data-message-id="${messageId}">
            <i class="fas fa-external-link-alt"></i> Raporu Yeni Sekmede Aç
          </a>
          <button type="button" class="schedule-report-button" 
                  data-message-id="${messageId}" 
                  data-report-name="${escapedReportName}"
                  data-action="schedule">
            <i class="fas fa-clock"></i> Zamanla
          </button>
        </div>
      </div>
    `;

    const aiMessage: Message = {
      id: messageId,
      type: 'ai',
      content,
      timestamp: new Date(),
      suggestions: resultData?.suggestions,
      showFeedback: true
    };

    this.messages.update(msgs => [...msgs, aiMessage]);

    // Conversation ID'yi güncelle
    if (resultData?.conversationId) {
      this.conversationId.set(resultData.conversationId);
    }
  }

  /**
   * Document action handler - Vector store arama sonuçları için
   * Data array içeren sonuçları işler
   */
  private handleDocumentAction(response: ReceivedMessage): void {
    const resultData = response.resultData;
    const messageId = resultData?.messageId || this.generateId();

    // Remove loading message
    this.removeLoadingMessage();
    this.isLoading.set(false);

    let content = '';

    // Check if data array exists (document search results)
    if (Array.isArray(resultData?.data) && resultData.data.length > 0) {
      // Build content from data array - similar to handleReceiveStreamingMessage
      for (const item of resultData.data) {
        const itemContent = item.content || '';
        content += this.parseMarkdownContent(itemContent);

        // Add file link if available
        try {
          const filePath = item.metadata?.filePath || null;
          if (filePath && typeof filePath === 'string' && filePath.trim().length > 0) {
            const safeFilePath = filePath.trim();
            const base = this.uploadUrl || '';
            const sep = base.endsWith('/') ? '' : '/';
            const fileUrl = `${base}${sep}${safeFilePath}`;
            const fileName = item.metadata?.fileName || item.documentTitle || 'Dosya';
            content += `<div class="file-link-container"><a href="${fileUrl}" target="_blank" rel="noopener noreferrer" class="doc-file-link"><i class="fas fa-file-alt"></i> ${fileName}</a></div>`;
          }
        } catch (e) {
          console.warn('Dosya linki oluşturulamadı:', e);
        }
      }
    } else if (resultData?.htmlMessage) {
      // History'den yüklenirken content ham markdown olabilir, parse et
      content = this.parseMarkdownContent(resultData.htmlMessage);
    } else {
      content = '<p>Arama sonucu bulunamadı.</p>';
    }

    const aiMessage: Message = {
      id: messageId,
      type: 'ai',
      content,
      timestamp: new Date(),
      suggestions: resultData?.suggestions,
      showFeedback: true
    };

    this.messages.update(msgs => [...msgs, aiMessage]);

    // Conversation ID'yi güncelle
    if (resultData?.conversationId) {
      this.conversationId.set(resultData.conversationId);
    }
  }

  /**
   * HTML escape helper - report.js escapeHtml ile aynı
   */
  private escapeHtml(text: string): string {
    const map: Record<string, string> = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#039;'
    };
    return text?.replace(/[&<>"']/g, m => map[m]) || '';
  }

  private handleDefaultAction(response: ReceivedMessage): void {
    const resultData = response.resultData;

    // Use messageId from backend if available, otherwise generate one
    const messageId = resultData?.messageId || this.generateId();

    // Check if last message is an AI message that needs updating (from streaming)
    const messages = this.messages();
    const lastMessage = messages[messages.length - 1];

    if (lastMessage && lastMessage.type === 'ai') {
      // Update last AI message with messageId (keep existing content from streaming)
      this.messages.update(msgs => msgs.map((m, i) => {
        if (i === messages.length - 1 && m.type === 'ai') {
          return {
            ...m,
            id: messageId,
            suggestions: resultData?.suggestions,
            isStreaming: false,
            showFeedback: true
          };
        }
        return m;
      }));
    } else {
      // No AI message found, add new one
      // History'den yüklenirken content ham markdown olabilir, parse et
      const rawContent = resultData?.htmlMessage || JSON.stringify(resultData) || '';
      const parsedContent = this.parseMarkdownContent(rawContent);

      const aiMessage: Message = {
        id: messageId,
        type: 'ai',
        content: parsedContent,
        timestamp: new Date(),
        suggestions: resultData?.suggestions,
        showFeedback: true
      };

      this.messages.update(msgs => [...msgs, aiMessage]);
    }
  }

  /**
   * ReceiveStreamingMessage handler - Streaming mesaj parçaları
   * report.js handleReceiveStreamingMessage fonksiyonu ile aynı işlevi görür
   */
  private handleReceiveStreamingMessage(streamingMessage: StreamingMessage): void {
    // İlk streaming mesajında loading'i kaldır ve isLoading'i false yap
    // report.js ile aynı: removeTypingIndicator('streaming message received')
    if (this.isLoading()) {
      this.removeLoadingMessage();
      this.isLoading.set(false);
    }

    let content = '';

    if (streamingMessage.resultData) {
      if (Array.isArray(streamingMessage.resultData.data)) {
        // Data array varsa - her item için döngü yap (report.js ile aynı)
        for (const item of streamingMessage.resultData.data) {
          // İçerik metnini ekle
          const itemContent = item.content || '';
          content += this.parseMarkdownContent(itemContent);

          // Dosya linki ekle (varsa) - HER ITEM'DAN SONRA (report.js ile aynı)
          try {
            const filePath = item.metadata?.filePath || null;
            if (filePath && typeof filePath === 'string' && filePath.trim().length > 0) {
              const safeFilePath = filePath.trim();
              const base = this.uploadUrl || '';
              const sep = base.endsWith('/') ? '' : '/';
              const fileUrl = `${base}${sep}${safeFilePath}`;
              const fileName = item.metadata?.fileName || 'Dosya';
              // Her item'dan sonra linki hemen ekle
              content += `<div class="file-link-container"><a href="${fileUrl}" target="_blank" rel="noopener noreferrer" class="doc-file-link"><i class="fas fa-file-alt"></i> ${fileName}</a></div>`;
            }
          } catch (e) {
            console.warn('Dosya linki oluşturulamadı:', e);
          }
        }
      } else if (typeof streamingMessage.resultData === 'object') {
        // Obje ise htmlMessage özelliğini kullan - report.js ile aynı
        this.fullStreamingMessage += streamingMessage.resultData.htmlMessage || '';
        content = this.parseMarkdownContent(this.fullStreamingMessage);
      }
    } else if (streamingMessage.userMessage) {
      // userMessage varsa biriktir ve parse et - report.js ile aynı
      this.fullStreamingMessage += streamingMessage.userMessage || '';
      content = this.parseMarkdownContent(this.fullStreamingMessage);
    }

    if (!content) return;

    // Mevcut streaming mesajı bul veya yeni oluştur
    this.messages.update(msgs => {
      // Önce loading mesajlarını kaldır (her durumda)
      const withoutLoading = msgs.filter(m => m.type !== 'loading');

      const existingStreamingIndex = withoutLoading.findIndex(m => m.isStreaming);

      if (existingStreamingIndex >= 0) {
        // Mevcut streaming mesajını güncelle
        // NOT: fullStreamingMessage biriktiği için content'i replace ediyoruz (append değil)
        return withoutLoading.map((m, i) => {
          if (i === existingStreamingIndex) {
            // Eğer data array'den geliyorsa append, değilse replace
            const newContent = streamingMessage.resultData?.data
              ? m.content + content
              : content;
            return { ...m, content: newContent };
          }
          return m;
        });
      } else {
        // Yeni streaming mesaj ekle
        return [...withoutLoading, {
          id: this.generateId(),
          type: 'ai' as const,
          content,
          timestamp: new Date(),
          isStreaming: true
        }];
      }
    });

    this.scrollToBottom();
  }

  /**
   * Markdown içeriği parse eder - report.js parseMarkdownContent ile aynı
   * MarkdownRendererService kullanır
   */
  private parseMarkdownContent(content: string): string {
    // MarkdownRendererService'i kullan
    return this.markdownRenderer.renderAnalysis(content);
  }

  /**
   * Script'leri çalıştır (chart rendering için)
   */
  private executeScripts(container: HTMLElement): void {
    this.markdownRenderer.executeScripts(container);
  }

  /**
   * API yanıtı sonrası grafik script'lerini çalıştır
   * report.js ile aynı mantık - 300ms bekleyerek DOM'un güncellenmesini bekle
   */
  private executeChartsAfterResponse(): void {
    setTimeout(() => {
      const container = this.messagesContainer?.nativeElement;
      if (!container) return;

      // En son streaming-chat-container veya ai mesajını bul
      const lastStreamingMessage = container.querySelector('.streaming-chat-container:last-child .streaming-message') ||
        container.querySelector('.ai-message:last-child');

      if (lastStreamingMessage) {
        console.log('Executing scripts on last streaming message');
        this.executeScripts(lastStreamingMessage as HTMLElement);
      } else {
        // Fallback: tüm container'da script ara
        const allGraphicContainers = container.querySelectorAll('.graphic-html-container');
        allGraphicContainers.forEach((graphicContainer: Element) => {
          console.log('Executing scripts on graphic container');
          this.executeScripts(graphicContainer as HTMLElement);
        });
      }
    }, 300); // report.js ile aynı - DOM güncellenmesi için bekle
  }

  /**
   * Streaming mesaj tamamlandığında çağrılır
   * fullStreamingMessage'ı sıfırlar ve mevcut streaming mesajların flag'ini false yapar
   */
  resetStreamingState(): void {
    this.fullStreamingMessage = '';

    // Mevcut streaming mesajların isStreaming flag'ini false yap
    // Bu sayede yeni soru sorulduğunda eski mesaj güncellenmez
    this.messages.update(msgs =>
      msgs.map(m => m.isStreaming ? { ...m, isStreaming: false } : m)
    );
  }

  /**
   * LoadingMessage handler - Yükleme durumu
   * report.js handleLoadingMessage fonksiyonu ile birebir aynı
   */
  private handleLoadingMessage(loadingMessage: string): void {
    console.log('Loading message received:', loadingMessage);

    // report.js ile aynı: loadingText signal'i güncelle
    this.loadingText.set(loadingMessage);

    // Loading mesajını güncelle (Angular reactive approach)
    this.messages.update(msgs => {
      return msgs.map(m => {
        if (m.type === 'loading') {
          return { ...m, content: loadingMessage };
        }
        return m;
      });
    });

    // report.js ile aynı: DOM'daki typing-message elementini de güncelle (fallback)
    setTimeout(() => {
      const typingMessage = document.getElementById('typing-message');
      if (typingMessage) {
        const pElement = typingMessage.querySelector('p');
        if (pElement) {
          pElement.textContent = loadingMessage;
        } else {
          typingMessage.textContent = loadingMessage;
        }
      }
    }, 100);
  }

  /**
   * Error handler - Hata mesajı göster
   * report.js showErrorMessage fonksiyonu
   */
  showErrorMessage(error: string): void {
    this.removeLoadingMessage();
    this.errorMessage.set(error);

    const errorMsg: Message = {
      id: this.generateId(),
      type: 'error',
      content: error,
      timestamp: new Date()
    };

    this.messages.update(msgs => [...msgs, errorMsg]);
    this.scrollToBottom();

    // 5 saniye sonra error signal'ı temizle
    setTimeout(() => {
      this.errorMessage.set(null);
    }, 5000);
  }

  // Data array'den tablo oluştur
  private createDataTable(data: any[]): string {
    if (!data || data.length === 0) return '';

    const headers = Object.keys(data[0]);
    let table = '<div class="table-responsive"><table class="table table-striped">';

    // Header
    table += '<thead><tr>';
    headers.forEach(h => table += `<th>${h}</th>`);
    table += '</tr></thead>';

    // Body
    table += '<tbody>';
    data.forEach(row => {
      table += '<tr>';
      headers.forEach(h => table += `<td>${row[h] ?? ''}</td>`);
      table += '</tr>';
    });
    table += '</tbody></table></div>';

    return table;
  }

  // Handle streaming message
  private handleStreamingMessage(content: string): void {
    this.messages.update(msgs => {
      const lastMsg = msgs[msgs.length - 1];

      if (lastMsg?.isStreaming) {
        // Update existing streaming message
        return msgs.map((m, i) =>
          i === msgs.length - 1 ? { ...m, content: m.content + content } : m
        );
      } else {
        // Remove loading and add new streaming message
        const filtered = msgs.filter(m => m.type !== 'loading');
        return [...filtered, {
          id: this.generateId(),
          type: 'ai' as const,
          content,
          timestamp: new Date(),
          isStreaming: true
        }];
      }
    });

    this.scrollToBottom();
  }

  // Add loading message
  private addLoadingMessage(): void {
    const loadingMessage: Message = {
      id: 'loading',
      type: 'loading',
      content: 'Düşünüyor...',
      timestamp: new Date()
    };
    this.messages.update(msgs => [...msgs, loadingMessage]);
  }

  // Remove loading message
  private removeLoadingMessage(): void {
    this.messages.update(msgs => msgs.filter(m => m.type !== 'loading'));
  }

  // Add error message
  private addErrorMessage(error: string): void {
    const errorMessage: Message = {
      id: this.generateId(),
      type: 'error',
      content: error,
      timestamp: new Date()
    };
    this.messages.update(msgs => [...msgs, errorMessage]);
  }

  // Handle keyboard shortcuts - eski uyumluluk için
  onKeydown(event: KeyboardEvent): void {
    // Filter menü/modal kontrolünü yap
    this.onInputKeydown(event);

    // Eğer event işlendiyse (preventDefault çağrıldıysa) veya menü/modal açıksa çık
    if (event.defaultPrevented || this.autocompleteService.isMenuOpen() || this.autocompleteService.isModalOpen()) {
      return;
    }

    // Normal Enter ile mesaj gönder
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  // Handle file input
  triggerFileInput(): void {
    this.fileInputRef?.nativeElement?.click();
  }

  // Handle file selection (report.js handleFileSelect ile aynı)
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];

      // Dosya validasyonu (report.js validateFile ile aynı)
      const validation = this.chatService.validateFile(file);
      if (!validation.valid) {
        this.showErrorMessage(validation.error || 'Dosya doğrulama hatası');
        input.value = ''; // Input'u temizle
        return;
      }

      // Hem local signal'a hem ChatService'e aktar
      this.selectedFile.set(file);
      this.chatService.selectFile(file);

      console.log(`Dosya seçildi: ${file.name} (${(file.size / 1024 / 1024).toFixed(2)}MB)`);
    }
  }

  // Clear selected file
  clearSelectedFile(): void {
    this.selectedFile.set(null);
    this.chatService.clearSelectedFile(); // ChatService'i de temizle
    if (this.fileInputRef?.nativeElement) {
      this.fileInputRef.nativeElement.value = '';
    }
  }

  // New chat
  clearChat(): void {
    this.messages.set([]);
    this.conversationId.set(null);
    this.chatService.conversationId.set(null); // ChatService'teki conversationId'yi de temizle
    this.activeFilters.set([]);
    this.feedbackService.clearFeedbackStates();
    this.showWelcomeMessage();
  }

  // Load conversation from cache (API call yok!)
  // history.js loadConversationIntoReport ile aynı mantık
  async loadConversation(conversation: Conversation): Promise<void> {
    console.log('Loading conversation into report:', conversation.id);

    // 1. Mesajları temizle (history.js: reportBox.innerHTML = "")
    this.messages.set([]);

    // 2. Conversation ID'yi set et (history.js: window.reportManager.conversationId = conversationId)
    // Hem component hem de ChatService'teki conversationId'yi senkronize et
    this.conversationId.set(conversation.id);
    this.chatService.conversationId.set(conversation.id);

    // 3. Clear previous feedback states and load new ones
    this.feedbackService.clearFeedbackStates();
    this.feedbackService.loadConversationFeedbacks(conversation.id);

    // 4. SignalR'ı reconnect et (history.js: window.reportManager.reconnectSignalR())
    this.signalRService.reconnect();

    // 4. HistoryService cache'den al (API call yapmaz)
    const fullConversation = await this.historyService.getConversationById(conversation.id);

    if (!fullConversation) {
      console.error('Conversation not found in cache:', conversation.id);
      return;
    }

    // 5. Streaming state'i temizle (history.js: fullStreamingMessage = '')
    this.fullStreamingMessage = '';

    // 6. Mesajları filtrele (history.js: getFilteredMessages - Temporary ve Action hariç)
    const filteredMessages = this.historyService.getFilteredMessages(fullConversation.messages);

    // 7. Her mesajı işle - history.js ile aynı mantık
    for (const message of filteredMessages) {
      if (message.messageType === 'User') {
        // User mesajı - Action ile başlayanları atla
        if (!message.content.startsWith('Action: ')) {
          // User mesajını ekle (history.js: createReportMessage)
          const userMessage: Message = {
            id: message.id,
            type: 'user',
            content: message.content,
            timestamp: new Date(message.createdAt)
          };
          this.messages.update(msgs => [...msgs, userMessage]);
        }
      } else if (message.messageType === 'Assistant') {
        // Assistant mesajı - metadata'ya göre işle
        // history.js: processAssistantMessage
        this.processHistoryAssistantMessage(message);
      }
    }

    // 8. Loading indicator'ı kaldır (history.js: removeTypingIndicator)
    this.removeLoadingMessage();

    // 9. Scroll to bottom
    this.scrollToBottom();

    // 10. Tüm mesajlar işlendikten sonra grafik scriptlerini çalıştır
    // history.js: MarkdownRenderer.executeScripts
    this.executeChartsAfterHistoryLoad();

    // Close sidebar on mobile
    if (window.innerWidth < 768) {
      this.isSidebarOpen.set(false);
    }
  }

  /**
   * History'den yüklenen mesajlardaki grafik scriptlerini çalıştır
   * history.js'deki executeScripts mantığı ile aynı
   */
  private executeChartsAfterHistoryLoad(): void {
    setTimeout(() => {
      if (this.messagesContainer) {
        // Tüm streaming-message container'larındaki scriptleri çalıştır
        const streamingMessages = this.messagesContainer.nativeElement.querySelectorAll('.streaming-message');
        streamingMessages.forEach((container: HTMLElement) => {
          console.log('Executing scripts in history streaming message');
          this.markdownRenderer.executeScripts(container);
        });

        // Ayrıca .ai-message-content içindeki scriptleri de çalıştır
        const aiMessages = this.messagesContainer.nativeElement.querySelectorAll('.ai-message-content');
        aiMessages.forEach((container: HTMLElement) => {
          console.log('Executing scripts in AI message content');
          this.markdownRenderer.executeScripts(container);
        });
      }
    }, 300);
  }

  /**
   * History'den yüklenen Assistant mesajını işle
   * history.js processAssistantMessage ile aynı mantık
   */
  private processHistoryAssistantMessage(message: { metadataJson?: string; content: string; id: string; createdAt: string }): void {
    if (!message.metadataJson) {
      // Metadata yoksa düz mesaj olarak göster
      // Ham markdown olabilir, parse et
      console.debug('No metadata for assistant message, showing as plain text');
      const parsedContent = this.parseMarkdownContent(message.content);
      const aiMessage: Message = {
        id: message.id,
        type: 'ai',
        content: parsedContent,
        timestamp: new Date(message.createdAt)
      };
      this.messages.update(msgs => [...msgs, aiMessage]);
      return;
    }

    let metadata: { SignalRJsFunction?: string; signalRJsFunction?: string };
    try {
      metadata = JSON.parse(message.metadataJson);
    } catch (error) {
      console.error('Failed to parse message metadata:', error);
      return;
    }

    const signalRFunction = metadata.SignalRJsFunction || metadata.signalRJsFunction;

    // None ise atla
    if (signalRFunction === 'None') {
      return;
    }

    let jsonContent: any;
    try {
      jsonContent = JSON.parse(message.content);
    } catch {
      // JSON değilse düz metin olarak göster - bu beklenen bir durum
      // Ham markdown olabilir, parse et
      const parsedContent = this.parseMarkdownContent(message.content);
      const aiMessage: Message = {
        id: message.id,
        type: 'ai',
        content: parsedContent,
        timestamp: new Date(message.createdAt)
      };
      this.messages.update(msgs => [...msgs, aiMessage]);
      return;
    }

    console.log('Original JSON Content:', jsonContent);

    // JSON anahtarlarını normalize et (PascalCase -> camelCase)
    const normalizedContent = this.historyService.normalizeJsonKeys(jsonContent);
    console.log('Normalized JSON Content:', normalizedContent);

    // Döküman arama sonuçları kontrolü - array ise ve DocumentTitle/Content içeriyorsa
    if (Array.isArray(normalizedContent) && normalizedContent.length > 0 &&
      (normalizedContent[0].documentTitle || normalizedContent[0].content)) {
      console.log('Processing document search results from history');
      this.handleDocumentResultsFromHistory(normalizedContent, message.id, message.createdAt);
      return;
    }

    // İlgili handler'ı çağır
    if (signalRFunction === 'ReceiveMessage') {
      console.log('Processing ReceiveMessage from history');
      this.handleReceiveMessage(normalizedContent as ReceivedMessage);
    } else if (signalRFunction === 'ReceiveStreamingMessage') {
      console.log('Processing ReceiveStreamingMessage from history');
      this.handleReceiveStreamingMessage(normalizedContent as StreamingMessage);
    }
  }

  /**
   * History'den yüklenen döküman arama sonuçlarını işle
   */
  private handleDocumentResultsFromHistory(
    documents: Array<{ documentTitle?: string; content?: string; score?: number; metadata?: any }>,
    messageId: string,
    createdAt: string
  ): void {
    let content = '';

    for (const doc of documents) {
      const docTitle = doc.documentTitle || 'Döküman';
      const docContent = doc.content || '';

      // Başlık ekle
      content += `<div class="document-result-item">`;
      content += `<h4 class="document-title"><i class="fas fa-file-alt"></i> ${this.escapeHtml(docTitle)}</h4>`;

      // İçeriği parse et
      content += `<div class="document-content">${this.parseMarkdownContent(docContent)}</div>`;

      // Dosya linki ekle
      try {
        const filePath = doc.metadata?.filePath || null;
        if (filePath && typeof filePath === 'string' && filePath.trim().length > 0) {
          const safeFilePath = filePath.trim();
          const base = this.uploadUrl || '';
          const sep = base.endsWith('/') ? '' : '/';
          const fileUrl = `${base}${sep}${safeFilePath}`;
          const fileName = doc.metadata?.fileName || docTitle || 'Dosya';
          content += `<div class="file-link-container"><a href="${fileUrl}" target="_blank" rel="noopener noreferrer" class="doc-file-link"><i class="fas fa-file-pdf"></i> ${this.escapeHtml(fileName)}</a></div>`;
        }
      } catch (e) {
        console.warn('Dosya linki oluşturulamadı:', e);
      }

      content += `</div>`;
    }

    const aiMessage: Message = {
      id: messageId,
      type: 'ai',
      content,
      timestamp: new Date(createdAt),
      showFeedback: true
    };

    this.messages.update(msgs => [...msgs, aiMessage]);
  }

  // Delete conversation - called after animation completes in sidebar
  deleteConversation(conversationId: string): void {
    // If the deleted conversation is currently active, clear the chat
    if (this.conversationId() === conversationId) {
      this.clearChat();
    }
  }

  // Clear all filters
  clearFilters(): void {
    this.activeFilters.set([]);
    this.autocompleteService.clearAllSelections();
  }

  // ==================== FILTER MENU & MODAL ====================

  /**
   * Input değişikliğinde # karakteri kontrolü - autocomplete-manager.js'deki gibi
   */
  onInputChange(event: Event): void {
    const input = event.target as HTMLTextAreaElement;
    const value = input.value;
    const cursorPosition = input.selectionStart || 0;
    const textBeforeCursor = value.substring(0, cursorPosition);
    const lastHashIndex = textBeforeCursor.lastIndexOf('#');

    if (lastHashIndex !== -1) {
      const textAfterHash = textBeforeCursor.substring(lastHashIndex + 1);
      if (textAfterHash === '') {
        // # karakteri yazıldı, menüyü aç
        this.autocompleteService.setTriggerPosition(lastHashIndex);

        // Cursor pozisyonunu hesapla ve menüyü o pozisyonda aç
        const menuPosition = this.calculateMenuPosition(input, lastHashIndex);
        this.autocompleteService.menuPosition.set(menuPosition);

        this.autocompleteService.openMenu();
      } else {
        // # karakterinden sonra yazı yazıldıysa menüyü kapat
        if (this.autocompleteService.isMenuOpen()) {
          this.autocompleteService.closeMenu();
        }
      }
    } else {
      // # karakteri yoksa menüyü kapat
      if (this.autocompleteService.isMenuOpen()) {
        this.autocompleteService.closeMenu();
      }
    }
  }

  /**
   * Cursor pixel pozisyonunu hesapla - autocomplete-manager.js _getCursorPixelPosition gibi
   */
  private calculateMenuPosition(input: HTMLTextAreaElement, triggerPos: number): { left: number; bottom: number } {
    const inputRect = input.getBoundingClientRect();
    const cursorPixelPos = this.getCursorPixelPosition(input, triggerPos);
    const menuWidth = 220;

    let leftPosition = inputRect.left + cursorPixelPos;

    // Ekran dışına taşmayı önle
    if (leftPosition + menuWidth > window.innerWidth - 20) {
      leftPosition = window.innerWidth - menuWidth - 20;
    }
    if (leftPosition < 20) {
      leftPosition = 20;
    }

    // Bottom: input'un üstünde görünsün
    const bottomPosition = window.innerHeight - inputRect.top + 8;

    return { left: leftPosition, bottom: bottomPosition };
  }

  /**
   * Cursor'un piksel pozisyonunu hesapla
   */
  private getCursorPixelPosition(input: HTMLTextAreaElement, triggerPos: number): number {
    const span = document.createElement('span');
    const computedStyle = window.getComputedStyle(input);

    span.style.font = computedStyle.font;
    span.style.whiteSpace = 'pre';
    span.style.position = 'absolute';
    span.style.visibility = 'hidden';
    span.textContent = input.value.substring(0, triggerPos);

    document.body.appendChild(span);
    const width = span.offsetWidth;
    document.body.removeChild(span);

    const paddingLeft = parseFloat(computedStyle.paddingLeft) || 0;
    return Math.min(width + paddingLeft, input.offsetWidth - 20);
  }

  /**
   * Input'ta keydown olayı
   */
  onInputKeydown(event: KeyboardEvent): void {
    // Menü açıkken klavye navigasyonu
    if (this.autocompleteService.isMenuOpen()) {
      switch (event.key) {
        case 'ArrowDown':
          event.preventDefault();
          this.autocompleteService.navigateMenu('down');
          break;
        case 'ArrowUp':
          event.preventDefault();
          this.autocompleteService.navigateMenu('up');
          break;
        case 'Enter':
          event.preventDefault();
          // Seçilen kategoriyi al ve modal'ı aç
          const selectedCategory = this.autocompleteService.selectCurrentMenuItem();
          this.removeHashFromInput();
          this.openFilterModal(selectedCategory);
          break;
        case 'Escape':
          event.preventDefault();
          this.autocompleteService.closeMenu();
          break;
      }
      return;
    }

    // Modal açıkken ESC ile kapat
    if (this.autocompleteService.isModalOpen() && event.key === 'Escape') {
      event.preventDefault();
      this.closeFilterModal();
    }
  }

  /**
   * Filtre kategorisine tıklandığında modal aç
   */
  onFilterCategoryClick(categoryId: FilterType): void {
    this.autocompleteService.closeMenu();
    this.removeHashFromInput();

    // Tarih için özel modal aç
    if (categoryId === 'date') {
      this.autocompleteService.dateFilterService.openModal();
      return;
    }

    this.openFilterModal(categoryId);
  }

  /**
   * Filtre modal'ını aç ve Select2'yi başlat
   */
  async openFilterModal(category: FilterType): Promise<void> {
    this.autocompleteService.openModal(category);
    this.filterSearchTerm.set('');
    this.selectedFilterOption.set(null);

    // Veri yüklenmemişse bekle
    await this.ensureDataLoaded(category);

    // Select2'yi başlat (DOM render olduktan sonra)
    setTimeout(() => {
      this.initializeSelect2ForCategory(category);
    }, 100);
  }

  /**
   * Kategoriye göre verilerin yüklendiğinden emin ol
   */
  async ensureDataLoaded(category: FilterType): Promise<void> {
    await this.autocompleteService.ensureDataLoaded(category);
  }

  /**
   * Kategori için Select2 başlat (eski JS'deki gibi)
   */
  private initializeSelect2ForCategory(category: FilterType): void {
    const $select = ($('#autocomplete-select2') as any);

    // Önceki Select2'yi temizle
    $select.off('select2:select');
    if ($select.hasClass('select2-hidden-accessible')) {
      $select.select2('destroy');
    }
    $select.empty().append('<option value=""></option>');

    const config = this.getSelect2Config(category);
    $select.select2(config);

    // Seçim event'i
    $select.on('select2:select', (e: any) => {
      const data = e.params.data;
      this.ngZone.run(() => {
        this.onSelect2Selection(data);
      });
    });

    // Focus
    $select.select2('open');
  }

  /**
   * Kategori için Select2 config oluştur
   */
  private getSelect2Config(category: FilterType): any {
    const self = this;
    const PAGE_SIZE = 50;

    const baseConfig = {
      placeholder: this.autocompleteService.getCategoryTitle(category),
      allowClear: false,
      width: '100%',
      dropdownParent: $('#filter-modal'),
      minimumInputLength: 0,
      language: {
        noResults: () => "Sonuç bulunamadı",
        searching: () => "Aranıyor...",
        inputTooShort: () => this.autocompleteService.getCategoryTitle(category)
      }
    };

    // Statik data kategorileri (artık yok, tüm filtreler API'den geliyor)
    // Eğer gelecekte statik kategori eklenirse buraya eklenebilir

    // Ajax/lazy loading kategorileri (store, product, category, promotion, salesperson, orderstatus, shipmethod, currency, salesreason)
    return {
      ...baseConfig,
      ajax: {
        transport: (params: any, success: any, failure: any) => {
          const { term = '', page = 1 } = params.data;

          // Veri kontrolü - yüklenmediyse bekle
          const checkAndFetch = async () => {
            try {
              await self.ensureDataLoaded(category);
              const { items, hasMore } = self.autocompleteService.getPagedItems(category, term, page, PAGE_SIZE);

              const results = items.map((item: any) => ({
                id: item.id,
                text: item.name,
                name: item.name,
                code: item.code || '',
                isActive: item.isActive // Store için aktif/pasif durumu
              }));

              success({ results, pagination: { more: hasMore } });
            } catch (error) {
              console.error('[Chat] Error fetching data for Select2:', error);
              failure(error);
            }
          };

          checkAndFetch();
        },
        delay: 250
      },
      templateResult: this.createTemplateResult(category),
      templateSelection: (item: any) => item.text,
      escapeMarkup: (markup: string) => markup
    };
  }

  /**
   * Kategori için template result oluştur
   */
  private createTemplateResult(category: FilterType): (item: any) => any {
    switch (category) {
      case 'store':
        return this.createStoreTemplateResult();
      case 'product':
        return this.createProductTemplateResult();
      case 'promotion':
        return this.createPromotionTemplateResult();
      case 'category':
        return this.createCategoryTemplateResult();
      case 'salesperson':
        return this.createSalesPersonTemplateResult();
      case 'orderstatus':
        return this.createOrderStatusTemplateResult();
      case 'shipmethod':
        return this.createShipMethodTemplateResult();
      case 'currency':
        return this.createCurrencyTemplateResult();
      case 'salesreason':
        return this.createSalesReasonTemplateResult();
      default:
        return this.createDefaultTemplateResult(category);
    }
  }

  /**
   * Mağaza template result - aktif/pasif durum göstergesi ile
   */
  private createStoreTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;

      const isActive = item.isActive !== false;
      const statusIcon = isActive
        ? '<i class="fas fa-check-circle" style="color: #10b981; margin-right: 6px;"></i>'
        : '<i class="fas fa-times-circle" style="color: #ef4444; margin-right: 6px;"></i>';
      const statusStyle = isActive ? '' : 'opacity: 0.6;';

      return $(`
        <div class="store-option" style="${statusStyle} display: flex; align-items: center; padding: 4px 0;">
          ${statusIcon}
          <span class="store-name">${item.text}</span>
          <span class="store-code" style="margin-left: 8px; color: #9ca3af; font-size: 12px;">(${item.code || item.id})</span>
        </div>
      `);
    };
  }

  /**
   * Kategori template result - mor ikon ile
   */
  private createCategoryTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;
      return $(`
        <span style="padding: 4px 0; display: flex; align-items: center;">
          <i class="fas fa-layer-group" style="margin-right: 8px; color: #8b5cf6;"></i>
          <span>${item.text}</span>
        </span>
      `);
    };
  }

  /**
   * Satış Temsilcisi template result - indigo ikon ile
   */
  private createSalesPersonTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;
      return $(`
        <span style="padding: 4px 0; display: flex; align-items: center;">
          <i class="fas fa-user-tie" style="margin-right: 8px; color: #6366f1;"></i>
          <span>${item.text}</span>
        </span>
      `);
    };
  }

  /**
   * Sipariş Durumu template result - amber ikon ile
   */
  private createOrderStatusTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;
      return $(`
        <span style="padding: 4px 0; display: flex; align-items: center;">
          <i class="fas fa-clipboard-check" style="margin-right: 8px; color: #f59e0b;"></i>
          <span>${item.text}</span>
        </span>
      `);
    };
  }

  /**
   * Teslimat Yöntemi template result - teal ikon ile
   */
  private createShipMethodTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;
      return $(`
        <span style="padding: 4px 0; display: flex; align-items: center;">
          <i class="fas fa-truck" style="margin-right: 8px; color: #14b8a6;"></i>
          <span>${item.text}</span>
        </span>
      `);
    };
  }

  /**
   * Para Birimi template result - yellow ikon ile
   */
  private createCurrencyTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;
      return $(`
        <span style="padding: 4px 0; display: flex; align-items: center;">
          <i class="fas fa-dollar-sign" style="margin-right: 8px; color: #eab308;"></i>
          <span>${item.text}</span>
        </span>
      `);
    };
  }

  /**
   * Satış Nedeni template result - pink ikon ile
   */
  private createSalesReasonTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;
      return $(`
        <span style="padding: 4px 0; display: flex; align-items: center;">
          <i class="fas fa-lightbulb" style="margin-right: 8px; color: #ec4899;"></i>
          <span>${item.text}</span>
        </span>
      `);
    };
  }

  /**
   * Ürün template result - kod göstergesi ile
   */
  private createProductTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;

      return $(`
        <div class="product-option" style="
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 8px 12px;
          border-radius: 6px;
          margin: 2px 0;
          background: #f0f9ff;
          border: 1px solid #bae6fd;
          transition: all 0.2s ease;
        ">
          <div style="display: flex; align-items: center; gap: 10px;">
            <i class="fas fa-box" style="color: #0284c7; font-size: 14px;"></i>
            <span style="font-weight: 500; color: #1f2937;">${item.text}</span>
          </div>
          <span style="
            font-size: 11px;
            color: #6b7280;
            background: rgba(0,0,0,0.05);
            padding: 2px 8px;
            border-radius: 4px;
          ">${item.code || ''}</span>
        </div>
      `);
    };
  }

  /**
   * Kampanya template result - özel stil ile
   */
  private createPromotionTemplateResult(): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;

      return $(`
        <div class="promotion-option" style="
          display: flex;
          align-items: center;
          justify-content: space-between;
          padding: 8px 12px;
          border-radius: 6px;
          margin: 2px 0;
          background: #fffbeb;
          border: 1px solid #fde68a;
          transition: all 0.2s ease;
        ">
          <div style="display: flex; align-items: center; gap: 10px;">
            <i class="fas fa-bullhorn" style="color: #f59e0b; font-size: 14px;"></i>
            <span style="font-weight: 500; color: #1f2937;">${item.text}</span>
          </div>
          <span style="
            font-size: 11px;
            color: #6b7280;
            background: rgba(0,0,0,0.05);
            padding: 2px 8px;
            border-radius: 4px;
          ">${item.id || ''}</span>
        </div>
      `);
    };
  }

  /**
   * Varsayılan template result
   */
  private createDefaultTemplateResult(category: FilterType): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;

      const icon = this.autocompleteService.getCategoryIcon(category);
      const iconColor = this.getCategoryIconColor(category);
      let html = `<span style="padding: 4px 0; display: block;"><i class="fas ${icon}" style="margin-right: 8px; color: ${iconColor};"></i>${item.text}`;

      if (item.code) {
        html += `<span style="margin-left: 8px; color: #9ca3af; font-size: 12px;">(${item.code})</span>`;
      }

      html += '</span>';
      return $(html);
    };
  }

  /**
   * Genel ikon template result
   */
  private createIconTemplateResult(iconClass: string, color: string): (item: any) => any {
    return (item: any) => {
      if (!item.id) return item.text;
      return $(`<span style="padding: 4px 0; display: block;"><i class="fas ${iconClass}" style="margin-right: 8px; color: ${color};"></i>${item.text}</span>`);
    };
  }


  /**
   * Kategori için ikon rengi getir
   */
  private getCategoryIconColor(category: FilterType): string {
    const colorMap: Record<FilterType, string> = {
      date: '#10b981',        // Emerald
      region: '#3b82f6',      // Blue
      store: '#10b981',       // Green
      product: '#f97316',     // Orange
      category: '#8b5cf6',    // Purple
      salesperson: '#6366f1', // Indigo
      customertype: '#06b6d4', // Cyan
      orderstatus: '#f59e0b',  // Amber
      shipmethod: '#14b8a6',   // Teal
      currency: '#eab308',    // Yellow
      salesreason: '#ec4899', // Pink
      promotion: '#ef4444'     // Red
    };
    return colorMap[category] || '#6b7280';
  }

  /**
   * Select2 seçimi yapıldığında
   */
  private onSelect2Selection(data: any): void {
    const category = this.autocompleteService.currentCategory();
    if (!category || !data.id) return;

    const selected: SelectOption = {
      id: data.id,
      name: data.text || data.name,
      code: data.code
    };

    // Seçimi kaydet
    this.autocompleteService.selectItem(category, selected);

    // Input'a ekle
    this.insertSelectionToInput(selected.name);

    // Modal'ı kapat
    this.closeFilterModal();
  }

  /**
   * Filtre modal'ını kapat ve Select2'yi temizle
   */
  closeFilterModal(): void {
    // Select2'yi temizle
    const $select = ($('#autocomplete-select2') as any);
    if ($select.hasClass('select2-hidden-accessible')) {
      $select.select2('close');
    }

    this.autocompleteService.closeModal();
    this.filterSearchTerm.set('');
    this.selectedFilterOption.set(null);
    this.filteredOptions.set([]);

    // Focus'u input'a geri ver
    setTimeout(() => {
      if (this.messageInput?.nativeElement) {
        this.messageInput.nativeElement.focus();
      }
    }, 100);
  }

  /**
   * Filtre arama
   */
  onFilterSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.filterSearchTerm.set(input.value);
    this.updateFilteredOptions();
  }

  /**
   * Filtrelenmiş seçenekleri güncelle
   */
  private updateFilteredOptions(): void {
    const category = this.autocompleteService.currentCategory();
    if (!category) {
      this.filteredOptions.set([]);
      return;
    }

    const { items } = this.autocompleteService.getPagedItems(category, this.filterSearchTerm(), 1, 100);
    this.filteredOptions.set(items);
  }

  /**
   * Filtre seçeneğine tıklandığında
   */
  onFilterOptionClick(option: SelectOption): void {
    this.selectedFilterOption.set(option);
  }

  /**
   * Filtre seçeneğini çift tıkla seç ve modal'ı kapat
   */
  onFilterOptionDoubleClick(option: SelectOption): void {
    this.confirmFilterSelection(option);
  }

  /**
   * Seçimi onayla ve input'a ekle
   */
  confirmFilterSelection(option?: SelectOption): void {
    const selected = option || this.selectedFilterOption();
    const category = this.autocompleteService.currentCategory();

    if (!selected || !category) {
      return;
    }

    // Seçimi kaydet
    this.autocompleteService.selectItem(category, selected);

    // Input'a ekle
    this.insertSelectionToInput(selected.name);

    // Modal'ı kapat
    this.closeFilterModal();
  }

  /**
   * Seçimi input'a ekle
   */
  private insertSelectionToInput(value: string): void {
    const triggerPos = this.autocompleteService.triggerPosition();
    const currentValue = this.inputMessage;
    const beforeHash = currentValue.substring(0, triggerPos);
    const afterCursor = currentValue.substring(triggerPos);

    const insertion = `"${value}" `;
    this.inputMessage = beforeHash + insertion + afterCursor;

    // Focus'u input'a geri ver
    setTimeout(() => {
      if (this.messageInput?.nativeElement) {
        this.messageInput.nativeElement.focus();
        const newPos = beforeHash.length + insertion.length;
        this.messageInput.nativeElement.setSelectionRange(newPos, newPos);
      }
    }, 100);
  }

  /**
   * # karakterini input'tan kaldır
   */
  private removeHashFromInput(): void {
    const triggerPos = this.autocompleteService.triggerPosition();
    if (triggerPos >= 0 && this.inputMessage[triggerPos] === '#') {
      const beforeHash = this.inputMessage.substring(0, triggerPos);
      const afterHash = this.inputMessage.substring(triggerPos + 1);
      this.inputMessage = beforeHash + afterHash;
    }
  }

  /**
   * Filtre tag'ını kaldır
   */
  removeFilterTag(filter: FilterTag): void {
    // Service'den kaldır
    this.autocompleteService.deselectItem(filter.type, filter.index);

    // Input'tan da kaldır
    const valueToRemove = `"${filter.value}"`;
    this.inputMessage = this.inputMessage.replace(valueToRemove, '').replace(/\s+/g, ' ').trim();
  }

  /**
   * Tüm filtre tag'larını temizle
   */
  clearAllFilterTags(): void {
    this.autocompleteService.clearAllSelections();

    // Input'tan tüm tırnak içindeki değerleri kaldır
    this.inputMessage = this.inputMessage.replace(/"[^"]*"/g, '').replace(/\s+/g, ' ').trim();
  }

  // ========== DATE FILTER HELPERS ==========

  /**
   * Preset seçimi
   */
  selectDatePreset(presetId: string): void {
    this.autocompleteService.dateFilterService.selectPreset(presetId);
    this.insertDateToInput();
  }

  /**
   * Tek tarih değiştiğinde
   */
  onSingleDateChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.autocompleteService.dateFilterService.tempSingleDate.set(input.value);
  }

  /**
   * Başlangıç tarihi değiştiğinde
   */
  onStartDateChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.autocompleteService.dateFilterService.tempStartDate.set(input.value);
  }

  /**
   * Bitiş tarihi değiştiğinde
   */
  onEndDateChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.autocompleteService.dateFilterService.tempEndDate.set(input.value);
  }

  /**
   * Yıl değiştiğinde
   */
  onYearChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.autocompleteService.dateFilterService.tempYear.set(parseInt(select.value, 10));
  }

  /**
   * Ay değiştiğinde
   */
  onMonthChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.autocompleteService.dateFilterService.tempMonth.set(parseInt(select.value, 10));
  }

  /**
   * Tek tarih onayı
   */
  confirmSingleDate(): void {
    const dateStr = this.autocompleteService.dateFilterService.tempSingleDate();
    if (dateStr) {
      this.autocompleteService.dateFilterService.selectSingleDate(dateStr);
      this.insertDateToInput();
    }
  }

  /**
   * Tarih aralığı onayı
   */
  confirmDateRange(): void {
    const startStr = this.autocompleteService.dateFilterService.tempStartDate();
    const endStr = this.autocompleteService.dateFilterService.tempEndDate();
    if (startStr && endStr) {
      this.autocompleteService.dateFilterService.selectDateRange(startStr, endStr);
      this.insertDateToInput();
    }
  }

  /**
   * Ay onayı
   */
  confirmMonth(): void {
    const year = this.autocompleteService.dateFilterService.tempYear();
    const month = this.autocompleteService.dateFilterService.tempMonth();
    this.autocompleteService.dateFilterService.selectMonth(year, month);
    this.insertDateToInput();
  }

  /**
   * Tarih seçimini input'a ekle
   */
  private insertDateToInput(): void {
    const dateText = this.autocompleteService.dateFilterService.getDateTextForPrompt();
    if (dateText) {
      // Mevcut tarih filtresi varsa kaldır (tarih formatı: DD.MM.YYYY veya DD.MM.YYYY - DD.MM.YYYY)
      this.inputMessage = this.inputMessage
        .replace(/"\d{2}\.\d{2}\.\d{4}(\s*-\s*\d{2}\.\d{2}\.\d{4})?"/g, '')
        .replace(/"[A-Za-zıİğĞüÜşŞöÖçÇ]+\s+\d{4}"/g, '') // Ay yılı formatı (Ocak 2025)
        .replace(/\s+/g, ' ')
        .trim();

      // Yeni tarih ekle (tırnak içinde)
      this.inputMessage = `${this.inputMessage} "${dateText}"`.trim();
    }

    // Focus'u input'a geri ver
    setTimeout(() => {
      if (this.messageInput?.nativeElement) {
        this.messageInput.nativeElement.focus();
      }
    }, 100);
  }

  // Scroll to bottom
  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messagesContainer) {
        const container = this.messagesContainer.nativeElement;
        container.scrollTop = container.scrollHeight;
      }
    }, 100);
  }

  // Generate unique ID
  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }

  // Logout
  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // Click on suggestion
  onSuggestionClick(suggestion: string): void {
    this.inputMessage = suggestion;
    this.sendMessage();
  }

  // Open report in iframe
  openReportInIframe(url: string, title: string): void {
    console.log('Opening report in iframe:', url, title);

    // Prepare the URL
    let finalUrl = url;
    if (url.startsWith('/reports/')) {
      const separator = url.includes('?') ? '&' : '?';
      finalUrl = url + separator + 'embedded=true';
    }

    // Check if this report is already open
    const existingReport = this.openReports().find(r => r.url === finalUrl);
    if (existingReport) {
      // Just activate the existing report
      this.activeReportId.set(existingReport.id);
      return;
    }

    // Create new report
    const reportId = 'report-' + Date.now();
    const safeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(finalUrl);

    this.openReports.update(reports => [
      ...reports,
      { id: reportId, url: finalUrl, safeUrl, title, loading: true, minimized: false }
    ]);

    // Set as active
    this.activeReportId.set(reportId);
  }

  // Close report iframe (active report)
  closeReportIframe(): void {
    const activeId = this.activeReportId();
    if (activeId) {
      this.openReports.update(reports => reports.filter(r => r.id !== activeId));
      this.activeReportId.set(null);
    }
  }

  // Toggle minimize/maximize report iframe
  toggleReportIframeMinimize(): void {
    // Just set activeReportId to null - the report stays in openReports
    this.activeReportId.set(null);
  }

  // Restore minimized report to active
  restoreMinimizedReport(report: OpenReport): void {
    // Just set this report as active - no reload needed
    this.activeReportId.set(report.id);
    // Close dropdown if open
    this.showMoreReportsDropdown.set(false);
  }

  // Close minimized report
  closeMinimizedReport(report: OpenReport, event: Event): void {
    event.stopPropagation();
    this.openReports.update(reports => reports.filter(r => r.id !== report.id));
  }

  // Close all minimized reports
  closeAllMinimizedReports(event: Event): void {
    event.stopPropagation();
    const activeId = this.activeReportId();
    // Keep only the active report
    this.openReports.update(reports => reports.filter(r => r.id === activeId));
    this.showMoreReportsDropdown.set(false);
  }

  // Toggle more reports dropdown
  toggleMoreReportsDropdown(): void {
    this.showMoreReportsDropdown.update(v => !v);
  }

  // Called when iframe loads
  onReportIframeLoad(reportId?: string): void {
    const id = reportId || this.activeReportId();
    if (id) {
      this.openReports.update(reports =>
        reports.map(r => r.id === id ? { ...r, loading: false } : r)
      );
    }
  }

  // Open report in new tab
  openReportInNewTab(): void {
    const report = this.activeReport();
    if (report) {
      // Remove embedded param for new tab
      const cleanUrl = report.url.replace(/[?&]embedded=true/, '');
      window.open(cleanUrl, '_blank');
    }
  }

  // ============================================
  // FEEDBACK METHODS
  // ============================================

  /**
   * Handle feedback button click
   * @param messageId The message ID to provide feedback for
   * @param type 'positive' (👍) or 'negative' (👎)
   */
  async onFeedbackClick(messageId: string, type: FeedbackType): Promise<void> {
    // Don't process welcome message or loading messages
    if (messageId === 'welcome' || !messageId) {
      return;
    }

    // Check if already submitting
    if (this.feedbackService.isSubmitting(messageId)) {
      return;
    }

    // For negative feedback, show comment modal first
    if (type === 'negative') {
      // If already has negative feedback, toggle it off
      if (this.feedbackService.getFeedbackType(messageId) === 'negative') {
        await this.feedbackService.toggleFeedback(messageId, type);
        return;
      }
      // Show modal for new negative feedback
      this.pendingNegativeFeedbackMessageId = messageId;
      this.showFeedbackCommentModal = true;
      return;
    }

    try {
      const success = await this.feedbackService.toggleFeedback(messageId, type);

      if (success) {
        const currentType = this.feedbackService.getFeedbackType(messageId);
        const actionText = currentType
          ? (currentType === 'positive' ? 'Olumlu geri bildirim gönderildi' : 'Olumsuz geri bildirim gönderildi')
          : 'Geri bildirim kaldırıldı';

        console.log(`Feedback: ${actionText} for message ${messageId}`);
      }
    } catch (error) {
      console.error('Failed to submit feedback:', error);
    }
  }

  // Negative feedback comment modal state
  showFeedbackCommentModal = false;
  pendingNegativeFeedbackMessageId: string | null = null;
  feedbackComment = '';

  /**
   * Submit negative feedback with comment
   */
  async submitNegativeFeedbackWithComment(): Promise<void> {
    if (!this.pendingNegativeFeedbackMessageId) return;

    try {
      const success = await this.feedbackService.submitFeedback(
        this.pendingNegativeFeedbackMessageId,
        'negative',
        this.feedbackComment.trim() || undefined
      );

      if (success) {
        console.log('Negative feedback submitted with comment');
      }
    } catch (error) {
      console.error('Failed to submit negative feedback:', error);
    } finally {
      this.closeFeedbackCommentModal();
    }
  }

  /**
   * Skip comment and submit negative feedback without comment
   */
  async skipCommentAndSubmit(): Promise<void> {
    if (!this.pendingNegativeFeedbackMessageId) return;

    try {
      await this.feedbackService.submitFeedback(this.pendingNegativeFeedbackMessageId, 'negative');
    } catch (error) {
      console.error('Failed to submit negative feedback:', error);
    } finally {
      this.closeFeedbackCommentModal();
    }
  }

  /**
   * Close feedback comment modal
   */
  closeFeedbackCommentModal(): void {
    this.showFeedbackCommentModal = false;
    this.pendingNegativeFeedbackMessageId = null;
    this.feedbackComment = '';
  }

  /**
   * Check if a message has positive feedback
   */
  hasPositiveFeedback(messageId: string): boolean {
    return this.feedbackService.getFeedbackType(messageId) === 'positive';
  }

  /**
   * Check if a message has negative feedback
   */
  hasNegativeFeedback(messageId: string): boolean {
    return this.feedbackService.getFeedbackType(messageId) === 'negative';
  }

  /**
   * Check if feedback is being submitted for a message
   */
  isFeedbackSubmitting(messageId: string): boolean {
    return this.feedbackService.isSubmitting(messageId);
  }
}
