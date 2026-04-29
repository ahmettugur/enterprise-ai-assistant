# 📝 Message Feedback (Reaction) Sistemi

<div align="center">

![Feedback System](https://img.shields.io/badge/Feature-Feedback%20System-success)
![AI Analysis](https://img.shields.io/badge/AI-Feedback%20Analysis-purple)
![Dashboard](https://img.shields.io/badge/UI-Analytics%20Dashboard-blue)

**AI Yanıtları için Kullanıcı Geri Bildirimi ve Otomatik Analiz Sistemi**

</div>

---

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Özellikler](#özellikler)
- [Mimari](#mimari)
- [Veritabanı Şeması](#veritabanı-şeması)
- [Backend API](#backend-api)
- [Frontend Bileşenleri](#frontend-bileşenleri)
- [AI Analiz Sistemi](#ai-analiz-sistemi)
- [Dashboard](#dashboard)
- [Konfigürasyon](#konfigürasyon)

---

## 🎯 Genel Bakış

Message Feedback sistemi, kullanıcıların AI yanıtlarını değerlendirmesini sağlar ve bu geri bildirimleri AI ile analiz ederek prompt iyileştirme önerileri oluşturur.

### Akış Diyagramı

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Kullanıcı │────▶│  👍/👎      │────▶│  Veritabanı │────▶│  Hangfire   │
│   AI Yanıtı │     │  Feedback   │     │  Kayıt      │     │  Job        │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
                           │                                       │
                           ▼                                       ▼
                    ┌─────────────┐                         ┌─────────────┐
                    │  Olumsuz?   │                         │  AI Analiz  │
                    │  Yorum Al   │                         │  Rapor      │
                    └─────────────┘                         └─────────────┘
                                                                   │
                                                                   ▼
                                                            ┌─────────────┐
                                                            │  İyileştirme│
                                                            │  Önerileri  │
                                                            └─────────────┘
```

---

## ✨ Özellikler

### 1. Kullanıcı Geri Bildirimi
- **👍 Pozitif Feedback**: Tek tıkla olumlu değerlendirme
- **👎 Negatif Feedback**: Detaylı yorum modalı ile olumsuz değerlendirme
- **Gerçek Zamanlı UI**: Anında görsel geri bildirim
- **Toggle Desteği**: Aynı butona tekrar basarak geri alma

### 2. AI Analiz
- **Günlük Otomatik Analiz**: Hangfire job ile her gece 02:00'da çalışır
- **Semantic Kernel Entegrasyonu**: GPT-5.2 ile akıllı analiz
- **Kategori Bazlı Gruplandırma**: Sorunları kategorize etme
- **Öncelik Belirleme**: High/Medium/Low öncelik seviyeleri

### 3. Dashboard
- **İstatistik Kartları**: Toplam feedback, memnuniyet oranı, trend
- **Trend Grafikleri**: Chart.js ile günlük değişim grafiği
- **Kategori Dağılımı**: Doughnut chart ile kategori analizi
- **İyileştirme Yönetimi**: Önerileri onaylama/reddetme arayüzü

---

## 🏗️ Mimari

### Katmanlı Yapı

```
┌──────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                         │
├──────────────────────────────────────────────────────────────────┤
│  Angular 21 Frontend                                              │
│  ├── FeedbackService (feedback.service.ts)                       │
│  ├── DashboardService (dashboard.service.ts)                     │
│  ├── Chat Component (feedback buttons)                           │
│  └── Dashboard Component (analytics UI)                          │
├──────────────────────────────────────────────────────────────────┤
│  ASP.NET Core API                                                 │
│  ├── FeedbackEndpoints (/api/v1/feedback)                        │
│  └── DashboardEndpoints (/api/v1/dashboard)                      │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                        APPLICATION LAYER                         │
├──────────────────────────────────────────────────────────────────┤
│  ├── IMessageFeedbackRepository                                  │
│  └── IFeedbackAnalysisReportRepository (+ PromptImprovement ops) │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                       INFRASTRUCTURE LAYER                       │
├──────────────────────────────────────────────────────────────────┤
│  ├── MessageFeedbackRepository                                   │
│  ├── PromptImprovementRepository                                 │
│  ├── FeedbackAnalysisReportRepository                            │
│  └── ChatDbContext (EF Core)                                     │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                          DOMAIN LAYER                            │
├──────────────────────────────────────────────────────────────────┤
│  Entities:                                                       │
│  ├── MessageFeedback                                             │
│  ├── FeedbackAnalysisReport                                      │
│  └── PromptImprovement                                           │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│                        SCHEDULER LAYER                           │
├──────────────────────────────────────────────────────────────────┤
│  AI.Scheduler (Hangfire)                                         │
│  └── FeedbackAnalysisJob (Daily 02:00 Turkey Time)               │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Veritabanı Şeması

### Schema: `history`

#### message_feedbacks

| Kolon | Tip | Açıklama |
|-------|-----|----------|
| `id` | UUID | Primary Key |
| `message_id` | UUID | FK → messages.id |
| `conversation_id` | UUID | Konuşma ID |
| `user_id` | VARCHAR(100) | Kullanıcı ID |
| `type` | VARCHAR(20) | 'positive' veya 'negative' |
| `comment` | TEXT | Olumsuz feedback yorumu (nullable) |
| `created_at` | TIMESTAMPTZ | Oluşturulma tarihi |
| `updated_at` | TIMESTAMPTZ | Güncellenme tarihi |

```sql
CREATE TABLE history.message_feedbacks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    message_id UUID NOT NULL REFERENCES history.messages(id),
    conversation_id UUID NOT NULL,
    user_id VARCHAR(100) NOT NULL,
    type VARCHAR(20) NOT NULL CHECK (type IN ('positive', 'negative')),
    comment TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX ix_message_feedbacks_message_user 
    ON history.message_feedbacks(message_id, user_id);
CREATE INDEX ix_message_feedbacks_user_id 
    ON history.message_feedbacks(user_id);
CREATE INDEX ix_message_feedbacks_created_at 
    ON history.message_feedbacks(created_at);
```

#### feedback_analysis_reports

| Kolon | Tip | Açıklama |
|-------|-----|----------|
| `id` | UUID | Primary Key |
| `analysis_date` | TIMESTAMPTZ | Analiz tarihi |
| `total_feedbacks_analyzed` | INT | Analiz edilen feedback sayısı |
| `positive_feedbacks` | INT | Pozitif sayısı |
| `negative_feedbacks` | INT | Negatif sayısı |
| `satisfaction_score` | DECIMAL | Memnuniyet yüzdesi |
| `summary` | TEXT | AI özeti |
| `key_insights` | JSONB | Önemli bulgular array |
| `raw_analysis` | TEXT | Ham AI yanıtı |
| `created_at` | TIMESTAMPTZ | Oluşturulma tarihi |

```sql
CREATE TABLE history.feedback_analysis_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    analysis_date TIMESTAMPTZ NOT NULL,
    total_feedbacks_analyzed INT NOT NULL,
    positive_feedbacks INT NOT NULL,
    negative_feedbacks INT NOT NULL,
    satisfaction_score DECIMAL(5,2) NOT NULL,
    summary TEXT NOT NULL,
    key_insights JSONB NOT NULL DEFAULT '[]',
    raw_analysis TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_feedback_analysis_reports_date 
    ON history.feedback_analysis_reports(analysis_date DESC);
```

#### prompt_improvements

| Kolon | Tip | Açıklama |
|-------|-----|----------|
| `id` | UUID | Primary Key |
| `analysis_report_id` | UUID | FK → feedback_analysis_reports.id |
| `category` | VARCHAR(100) | İyileştirme kategorisi |
| `issue` | TEXT | Tespit edilen sorun |
| `suggestion` | TEXT | İyileştirme önerisi |
| `priority` | VARCHAR(20) | 'High', 'Medium', 'Low' |
| `status` | VARCHAR(20) | Durum enum |
| `review_notes` | TEXT | İnceleme notları |
| `reviewed_by` | VARCHAR(100) | İnceleyen kişi |
| `reviewed_at` | TIMESTAMPTZ | İnceleme tarihi |
| `created_at` | TIMESTAMPTZ | Oluşturulma tarihi |

**Status Enum:**
- `Pending`: Beklemede
- `UnderReview`: İnceleniyor
- `Applied`: Uygulandı
- `Rejected`: Reddedildi

```sql
CREATE TABLE history.prompt_improvements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    analysis_report_id UUID NOT NULL REFERENCES history.feedback_analysis_reports(id),
    category VARCHAR(100) NOT NULL,
    issue TEXT NOT NULL,
    suggestion TEXT NOT NULL,
    priority VARCHAR(20) NOT NULL CHECK (priority IN ('High', 'Medium', 'Low')),
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    review_notes TEXT,
    reviewed_by VARCHAR(100),
    reviewed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_prompt_improvements_status 
    ON history.prompt_improvements(status);
CREATE INDEX ix_prompt_improvements_priority 
    ON history.prompt_improvements(priority);
```

---

## 🔌 Backend API

### Feedback Endpoints

**Base URL:** `/api/v1/feedback`

#### POST /messages/{messageId}
Mesaja feedback ekle veya güncelle.

**Request:**
```json
{
  "type": "negative",
  "comment": "Yanıt çok uzundu ve konudan saptı"
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "messageId": "123e4567-e89b-12d3-a456-426614174000",
  "conversationId": "789e0123-e45b-67d8-a901-234567890abc",
  "userId": "user@example.com",
  "type": "negative",
  "comment": "Yanıt çok uzundu ve konudan saptı",
  "createdAt": "2025-12-30T10:30:00Z"
}
```

#### DELETE /messages/{messageId}
Mesajdan feedback kaldır.

#### GET /messages/{messageId}
Mesajın feedback'ini getir.

#### GET /conversations/{conversationId}
Konuşmadaki tüm feedback'leri getir.

#### GET /statistics
İstatistikleri getir.

**Query Parameters:**
- `startDate`: Başlangıç tarihi (ISO 8601)
- `endDate`: Bitiş tarihi (ISO 8601)

**Response:**
```json
{
  "totalPositive": 150,
  "totalNegative": 30,
  "totalCount": 180,
  "satisfactionRate": 83.33
}
```

### Dashboard Endpoints

**Base URL:** `/api/v1/dashboard`

#### GET /overview
Dashboard genel istatistikleri.

**Query Parameters:**
- `days`: Gün sayısı (default: 30)

**Response:**
```json
{
  "totalFeedbacks": 500,
  "positiveFeedbacks": 420,
  "negativeFeedbacks": 80,
  "satisfactionRate": 84.0,
  "totalAnalysisReports": 15,
  "pendingImprovements": 12,
  "appliedImprovements": 25,
  "trendChange": 2.5,
  "trendDirection": "up"
}
```

#### GET /trends
Günlük trend verileri.

**Response:**
```json
{
  "startDate": "2025-12-01",
  "endDate": "2025-12-30",
  "dailyData": [
    {
      "date": "2025-12-01",
      "positiveCount": 15,
      "negativeCount": 3,
      "satisfactionRate": 83.3
    }
  ],
  "averageSatisfactionRate": 85.2
}
```

#### GET /categories
Kategori bazlı dağılım.

**Response:**
```json
{
  "categories": [
    { "category": "Yanıt Kalitesi", "count": 25, "percentage": 31.25 },
    { "category": "Hız", "count": 20, "percentage": 25.0 }
  ],
  "totalFeedbacks": 80
}
```

#### GET /improvements
İyileştirme önerileri listesi.

**Query Parameters:**
- `status`: Durum filtresi (Pending, UnderReview, Applied, Rejected)
- `priority`: Öncelik filtresi (High, Medium, Low)
- `page`: Sayfa numarası
- `pageSize`: Sayfa boyutu

**Response:**
```json
{
  "improvements": [
    {
      "id": "...",
      "category": "Yanıt Formatı",
      "issue": "Uzun paragraflar okunabilirliği azaltıyor",
      "suggestion": "Yanıtları madde işaretleriyle formatla",
      "priority": "High",
      "status": "Pending",
      "createdAt": "2025-12-30T02:00:00Z"
    }
  ],
  "statistics": {
    "total": 50,
    "pending": 12,
    "underReview": 5,
    "applied": 28,
    "rejected": 5
  },
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 20
}
```

#### PATCH /improvements/{id}/status
İyileştirme durumunu güncelle.

**Request:**
```json
{
  "status": "Applied",
  "reviewNotes": "Prompt'a eklendi",
  "reviewedBy": "admin@example.com"
}
```

#### GET /reports
Analiz raporları listesi.

#### GET /reports/{id}
Rapor detayı.

---

## 🎨 Frontend Bileşenleri

### FeedbackService

**Dosya:** `frontend/src/app/core/services/feedback.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class FeedbackService {
  // Feedback durumunu takip eden signal
  private messageFeedbacks = signal<Map<string, MessageFeedbackState>>(new Map());
  
  // Feedback ekle/güncelle
  addFeedback(messageId: string, request: AddFeedbackRequest): Observable<FeedbackResponse>
  
  // Feedback kaldır
  removeFeedback(messageId: string): Observable<void>
  
  // Feedback durumunu kontrol et
  getFeedbackType(messageId: string): FeedbackType | null
  
  // Loading durumu
  isSubmitting(messageId: string): boolean
}
```

### DashboardService

**Dosya:** `frontend/src/app/core/services/dashboard.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class DashboardService {
  // Dashboard overview
  getOverview(days: number): Observable<DashboardOverview>
  
  // Trend verileri
  getTrends(days: number): Observable<FeedbackTrends>
  
  // Kategori dağılımı
  getCategories(days: number): Observable<CategoryBreakdown>
  
  // İyileştirmeler
  getImprovements(status?, priority?, page?, pageSize?): Observable<PromptImprovementsResponse>
  
  // Durum güncelle
  updateImprovementStatus(id: string, request: UpdateImprovementStatusRequest): Observable<PromptImprovementItem>
  
  // Raporlar
  getReports(page?, pageSize?): Observable<ReportsResponse>
  getReportDetail(id: string): Observable<AnalysisReportDetail>
}
```

### Chat Component - Feedback Buttons

```html
<!-- AI mesajı altında feedback butonları -->
<div class="feedback-buttons">
  <button 
    class="feedback-btn positive"
    [class.active]="feedbackType() === 'positive'"
    [disabled]="isSubmitting()"
    (click)="onPositiveFeedback()">
    <i class="pi pi-thumbs-up"></i>
  </button>
  <button 
    class="feedback-btn negative"
    [class.active]="feedbackType() === 'negative'"
    [disabled]="isSubmitting()"
    (click)="onNegativeFeedback()">
    <i class="pi pi-thumbs-down"></i>
  </button>
</div>

<!-- Negatif feedback modal -->
<p-dialog header="Geri Bildiriminiz" [(visible)]="showCommentModal">
  <textarea [(ngModel)]="feedbackComment" placeholder="Yanıt neden yardımcı olmadı?"></textarea>
  <p-button label="Gönder" (click)="submitNegativeFeedback()"></p-button>
</p-dialog>
```

### Dashboard Component

**Dosya:** `frontend/src/app/pages/dashboard/dashboard.ts`

**Özellikler:**
- 4 istatistik kartı (KPI)
- Trend line chart (Chart.js)
- Kategori doughnut chart
- İyileştirmeler tablosu (filtreleme, sayfalama)
- Raporlar tablosu
- Detay ve onay modalları

---

## 🤖 AI Analiz Sistemi

### FeedbackAnalysisJob

**Dosya:** `AI.Scheduler/Jobs/FeedbackAnalysisJob.cs`

```csharp
[Queue("default")]
public class FeedbackAnalysisJob
{
    // Her gece 02:00'da çalışır (Türkiye saati)
    public async Task ExecuteAsync(PerformContext context)
    {
        // 1. Son 24 saatteki feedback'leri topla
        var feedbacks = await _feedbackRepository.GetRecentFeedbacksAsync(24);
        
        // 2. AI ile analiz et
        var analysisPrompt = BuildAnalysisPrompt(feedbacks);
        var result = await _kernel.InvokePromptAsync(analysisPrompt);
        
        // 3. JSON parse et
        var analysis = ParseAnalysisResult(result);
        
        // 4. Rapor kaydet
        var report = new FeedbackAnalysisReport { ... };
        await _reportRepository.AddAsync(report);
        
        // 5. İyileştirme önerilerini kaydet
        foreach (var improvement in analysis.Improvements)
        {
            var entity = new PromptImprovement { ... };
            await _improvementRepository.AddAsync(entity);
        }
    }
}
```

### AI Prompt Şablonu

```
Sen bir AI asistan kalite analisti olarak görev yapıyorsun.

Aşağıdaki kullanıcı geri bildirimlerini analiz et:

{feedbackList}

Lütfen şu formatta JSON yanıt ver:
{
  "summary": "Genel değerlendirme özeti",
  "keyInsights": ["Bulgu 1", "Bulgu 2"],
  "improvements": [
    {
      "category": "Kategori adı",
      "issue": "Tespit edilen sorun",
      "suggestion": "İyileştirme önerisi",
      "priority": "High|Medium|Low"
    }
  ]
}
```

### Hangfire Konfigürasyonu

```csharp
// AI.Scheduler/Program.cs
services.AddHangfire(config => 
    config.UsePostgreSqlStorage(connectionString));

// Job kaydı
RecurringJob.AddOrUpdate<FeedbackAnalysisJob>(
    "feedback-analysis",
    job => job.ExecuteAsync(null),
    "0 2 * * *",  // Her gün 02:00
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")
    });
```

---

## 📊 Dashboard

### Erişim

**URL:** `/dashboard`

**Menü:** Kullanıcı dropdown → Dashboard

### Ekran Görüntüsü Yapısı

```
┌─────────────────────────────────────────────────────────────────┐
│  ← Geri   Geri Bildirim Dashboard              [Son 30 gün ▼]   │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │ Toplam   │  │ Memn.    │  │ Bekleyen │  │ Uygulanan│         │
│  │ Feedback │  │ Oranı    │  │ İyileşt. │  │ İyileşt. │         │
│  │   500    │  │  84% ↑   │  │    12    │  │    25    │         │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘         │
├─────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────┐  ┌────────────────────────┐ │
│  │     📈 Geri Bildirim Trendi    │  │  🍩 Kategori Dağılımı  │ │
│  │   [Line Chart]                 │  │  [Doughnut Chart]      │ │
│  └────────────────────────────────┘  └────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│  [Prompt İyileştirmeleri] [Analiz Raporları]                    │
│  ┌──────────────────────────────────────────────────────────────┤
│  │ Kategori | Sorun | Öneri | Öncelik | Durum | İşlemler        │
│  │ Yanıt F. | ...   | ...   | High    | Bekl. | 👁 ✓ ✗          │
│  │ Hız      | ...   | ...   | Medium  | Uyg.  |                 │
│  └──────────────────────────────────────────────────────────────┤
└─────────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Konfigürasyon

### appsettings.json

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=ChatDb;Username=postgres;Password=...;Minimum Pool Size=10;Maximum Pool Size=100;Connection Lifetime=300;Connection Idle Lifetime=30",
    "AdventureWorks2022": "Server=localhost;Database=AdventureWorks2022;User=sa;Password=...;TrustServerCertificate=true;MultipleActiveResultSets=true",
    "Redis": "localhost:6379,abortConnect=false,connectTimeout=5000,syncTimeout=5000"
  },
  "LLMProvider": {
    "Type": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-...",
      "ChatModel": "gpt-5.2",
      "Endpoint": "https://api.openai.com/v1",
      "TimeoutMinutes": 20
    },
    "Azure": {
      "ApiKey": "...",
      "Endpoint": "https://your-endpoint.openai.azure.com",
      "ChatDeployment": "gpt-5.2",
      "EmbeddingDeployment": "text-embedding-3-large",
      "TimeoutMinutes": 20
    }
  },
  "Hangfire": {
    "SchemaName": "hangfire",
    "DashboardPath": "/hangfire",
    "DashboardTitle": "AI Scheduler - Zamanlanmış Raporlar",
    "WorkerCount": 5,
    "Queues": [ "critical", "default", "reports" ],
    "ServerName": "AI-Scheduler-01",
    "HeartbeatInterval": "00:00:30",
    "ServerCheckInterval": "00:05:00",
    "SchedulePollingInterval": "00:00:15",
    "StatsPollingInterval": "00:00:05",
    "JobExpirationCheckInterval": "01:00:00",
    "CountersAggregateInterval": "00:05:00",
    "DeletedListSize": 10000,
    "SucceededListSize": 10000
  },
  
  "ScheduledReports": {
    "MaxConcurrentReports": 3,
    "DefaultTimeoutMinutes": 30,
    "RetryCount": 3,
    "RetryDelayMinutes": 5,
    "OutputDirectory": "wwwroot/reports",
    "BaseUrl": "https://localhost:7041"
  }
}
```

### Environment Variables

```bash
# OpenAI
OPENAI_API_KEY=sk-...

# Database
CHAT_HISTORY_CONNECTION=Host=...

# Hangfire
HANGFIRE_DASHBOARD_USER=admin
HANGFIRE_DASHBOARD_PASSWORD=...
```

---

## 🔧 Migration Komutları

```bash
# Migration oluştur
dotnet ef migrations add AddMessageFeedback \
  --project AI.Infrastructure \
  --startup-project AI.Api \
  --context ChatDbContext

# Veritabanına uygula
dotnet ef database update \
  --project AI.Infrastructure \
  --startup-project AI.Api \
  --context ChatDbContext
```

---

##  İlgili Dökümanlar

- [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) - Agentic AI pattern'leri
- [System-Overview.md](System-Overview.md) - Genel sistem analizi
- [Chat-System.md](Chat-System.md) - Chat sistemi özellikleri
- [Report-System.md](Report-System.md) - Raporlama sistemi
- [Infrastructure-Cross-Cutting.md](Infrastructure-Cross-Cutting.md) - Cache, Rate Limiting, Health Checks
- [Authentication-Authorization.md](Authentication-Authorization.md) - JWT Auth + AD SSO

---

<div align="center">




</div>
