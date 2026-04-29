# ⏰ Scheduled Reports (Zamanlanmış Rapor) Sistemi

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Mimari Yapı](#mimari-yapı)
- [Dosya Yapısı](#dosya-yapısı)
- [CRUD Operasyonları](#crud-operasyonları)
- [Hangfire Job Sistemi](#hangfire-job-sistemi)
- [Mesajdan Rapor Oluşturma](#mesajdan-rapor-oluşturma)
- [Cron Expression Yönetimi](#cron-expression-yönetimi)
- [API Endpoints](#api-endpoints)
- [Domain Entity](#domain-entity)

---

## Genel Bakış

Kullanıcıların oluşturduğu SQL raporlarını **Cron schedule** ile otomatik çalıştıran sistem.

| Özellik | Değer |
|---------|-------|
| **UseCase** | `ScheduledReportUseCase.cs` (667 satır) |
| **Scheduler** | Hangfire (recurring jobs) |
| **Cron Kütüphanesi** | Cronos |
| **Job Queue** | `reports` (normal), `critical` (sync job) |
| **Retry** | 3 deneme — 60s, 300s, 900s (exponential) |
| **Yetkilendirme** | Kullanıcı bazlı erişim kontrolü |
| **Result Pattern** | `Result<T>` — Success/Error |
| **Bildirim** | Email + Teams webhook (placeholder) |

---

## Mimari Yapı

```
Frontend (Angular)
       │
       ▼
┌────────────────────────────────────────────────────────────────┐
│                         AI.Api                                 │
│  ScheduledReportEndpoints.cs (247 satır)                       │
│  /api/v1/scheduled-reports/*                                   │
│  ├─ GET    /              → GetMyReportsAsync                  │
│  ├─ GET    /{id}          → GetByIdAsync                       │
│  ├─ GET    /{id}/details  → GetByIdWithLogsAsync               │
│  ├─ POST   /              → CreateAsync                        │
│  ├─ POST   /from-message  → CreateFromMessageAsync             │
│  ├─ PUT    /{id}          → UpdateAsync                        │
│  ├─ DELETE /{id}          → DeleteAsync                        │
│  ├─ POST   /{id}/pause    → PauseAsync                         │
│  ├─ POST   /{id}/resume   → ResumeAsync                        │
│  ├─ POST   /{id}/run-now  → RunNowAsync                        │
│  ├─ GET    /{id}/logs     → GetLogsAsync                       │
│  └─ GET    /cron-presets  → Hazır cron şablonları              │
└───────────────────┬────────────────────────────────────────────┘
                    │ IScheduledReportUseCase
┌───────────────────▼────────────────────────────────────────────┐
│                    AI.Application                              │
│  ScheduledReportUseCase.cs (667 satır)                         │
│  ├─ CRUD: Create, GetById, GetMyReports, Update, Delete        │
│  ├─ Status: Pause, Resume, SetActive                           │
│  ├─ Execution: RunNow, GetLogs                                 │
│  ├─ CreateFromMessageAsync() — mesajdan SQL çıkarma            │
│  ├─ Cron validasyonu (Cronos)                                  │
│  └─ Yetki kontrolü (HasAccessToReport)                         │
└───────────────────┬────────────────────────────────────────────┘
                    │ IScheduledReportRepository
┌───────────────────▼────────────────────────────────────────────┐
│                    AI.Domain                                   │
│  ScheduledReport.cs (Aggregate Root)                           │
│  ├─ Create() — factory method                                  │
│  ├─ UpdateName(), UpdateSchedule(), UpdateSqlQuery()           │
│  ├─ SetActive(), SetNextRunAt()                                │
│  ├─ UpdateNotificationSettings()                               │
│  └─ AddLog() → ScheduledReportLog                              │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                    AI.Scheduler (Hangfire)                     │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ReportSchedulerJob.cs (221 satır) — her 5 dk çalışır    │   │
│  │ ├─ SyncScheduledReportsAsync() — aktif raporları sync   │   │
│  │ ├─ RegisterReport() — recurring job ekle                │   │
│  │ ├─ UnregisterReport() — recurring job kaldır            │   │
│  │ └─ TriggerReport() — anında çalıştır                    │   │
│  └─────────────────────────┬───────────────────────────────┘   │
│                            │                                   │
│  ┌─────────────────────────▼───────────────────────────────┐   │
│  │ ScheduledReportJob.cs (79 satır)                        │   │
│  │ ├─ ExecuteReportAsync() — SQL çalıştır                  │   │
│  │ └─ [AutomaticRetry(3)] [Queue("reports")]               │   │
│  └─────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────┘
```

---

## Dosya Yapısı

```
AI.Application/
├── UseCases/
│   └── ScheduledReportUseCase.cs          # Ana iş mantığı (667 satır)
├── Ports/Primary/UseCases/
│   └── IScheduledReportUseCase.cs         # Interface
├── DTOs/
│   ├── ScheduledReportDto.cs              # Rapor DTO
│   ├── ScheduledReportDetailDto.cs        # Detay + loglar
│   ├── ScheduledReportLogDto.cs           # Çalışma logu
│   ├── CreateScheduledReportDto.cs        # Oluşturma isteği
│   ├── CreateScheduledReportFromMessageDto.cs # Mesajdan oluşturma
│   └── UpdateScheduledReportDto.cs        # Güncelleme isteği

AI.Domain/Scheduling/
├── ScheduledReport.cs                     # Aggregate Root
├── ScheduledReportLog.cs                  # Entity — çalışma logları
└── IScheduledReportRepository.cs          # Repository interface

AI.Scheduler/
├── Jobs/
│   ├── ReportSchedulerJob.cs              # Sync job (221 satır) — her 5 dk
│   └── ScheduledReportJob.cs              # Execution job (79 satır)
├── Configuration/
│   ├── HangfireSettings.cs                # Hangfire bağlantı ayarları
│   └── ScheduledReportSettings.cs         # Rapor konfigürasyonu
├── Extensions/
│   ├── HangfireExtensions.cs              # DI + dashboard config
│   └── LLMExtensions.cs                   # LLM servis DI
└── Program.cs                             # Scheduler host

AI.Api/Endpoints/Reports/
└── ScheduledReportEndpoints.cs            # 12 REST endpoint (247 satır)
```

---

## CRUD Operasyonları

### Rapor Oluşturma

```csharp
// AI.Application/UseCases/ScheduledReportUseCase.cs
public async Task<Result<ScheduledReportDto>> CreateAsync(CreateScheduledReportDto request, ...)
{
    // 1. Kullanıcı kimliği kontrolü (ICurrentUserService)
    // 2. Cron expression validasyonu (Cronos)
    // 3. İsim benzersizlik kontrolü
    // 4. ScheduledReport.Create() — domain factory method
    // 5. İlk NextRunAt hesaplanır
    // 6. DB'ye kaydet
    // → Result<ScheduledReportDto>.Success(...)
}
```

### Yetki Kontrolü

```csharp
private bool HasAccessToReport(ScheduledReport report)
{
    var currentUserId = _currentUserService.UserId;
    return report.UserId == currentUserId;
    // TODO: Admin rolü kontrolü eklenebilir
}
```

Her CRUD operasyonunda (Get, Update, Delete, Pause, Resume, RunNow) `HasAccessToReport()` çağrılır.

---

## Hangfire Job Sistemi

### ReportSchedulerJob — Senkronizasyon (her 5 dk)

```
Her 5 dakikada çalışır:
    │
    ├─ GetAllActiveAsync() — aktif raporları getir
    ├─ Her rapor için:
    │   ├─ Cron validasyon
    │   ├─ Job zaten kayıtlı mı? (_registeredJobs HashSet)
    │   └─ Değilse → RecurringJobManager.AddOrUpdate()
    │
    └─ Artık aktif olmayanları kaldır:
        └─ RecurringJobManager.RemoveIfExists()
```

### ScheduledReportJob — Rapor Çalıştırma

```csharp
// AI.Scheduler/Jobs/ScheduledReportJob.cs
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
[Queue("reports")]
public async Task ExecuteReportAsync(Guid reportId, CancellationToken cancellationToken)
{
    // 1. SQL sorgusunu çalıştır
    // 2. Başarılı → UpdateScheduledReportLastRunAsync()
    // 3. Başarısız → Hangfire retry (3 deneme)
}
```

### Job ID Formatı

```
scheduled-report-{safeName}-{reportId}
Örnek: scheduled-report-haftalik-satis-raporu-a1b2c3d4-...
```

- Türkçe karakterler ASCII'ye çevrilir (ğ→g, ü→u, ş→s, vb.)
- Maksimum 50 karakter
- URL-safe

---

## Mesajdan Rapor Oluşturma

Kullanıcı chat'te bir rapor mesajını seçip "Zamanla" dediğinde:

```
POST /api/v1/scheduled-reports/from-message
{
    "messageId": "abc-123",
    "name": "Haftalık Satış Raporu",
    "cronExpression": "0 9 * * 1",
    "recipientEmails": ["user@company.com"],
    "isActive": true,
    "sendToTeams": false
}
```

### Akış

```
1. MessageId ile mesajı getir
2. Content'ten SQL sorgusunu çıkar:
   ├─ SELECT ile başlıyor → SQL sorgusu
   └─ WITH ile başlıyor → CTE SQL sorgusu
3. MetadataJson'dan ek bilgileri çıkar:
   ├─ ReportServiceType (AdventureWorks, SocialMedia, vb.)
   ├─ ReportDatabaseType
   └─ ReportDatabaseServiceType
4. SQL bulunamadı → Hata: "Sadece rapor mesajları zamanlanabilir"
5. Başarılı → ScheduledReport.Create() + DB kayıt
```

---

## Cron Expression Yönetimi

### Hazır Şablonlar (GET /cron-presets)

| Cron | Açıklama |
|------|----------|
| `0 * * * *` | Her saat başı |
| `0 0 * * *` | Her gün gece yarısı |
| `0 9 * * *` | Her gün saat 09:00 |
| `0 9 * * 1` | Her pazartesi 09:00 |
| `0 9 * * 1-5` | Hafta içi 09:00 |
| `0 0 1 * *` | Her ayın 1'inde gece yarısı |
| `0 9 1 * *` | Her ayın 1'inde 09:00 |
| `0 0 * * 0` | Her pazar gece yarısı |

### Validasyon

```csharp
// Cronos kütüphanesi ile
private static bool IsValidCronExpression(string cronExpression)
{
    try { CronExpression.Parse(cronExpression); return true; }
    catch { return false; }
}
```

---

## API Endpoints

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| `GET` | `/api/v1/scheduled-reports` | Kullanıcının raporları |
| `GET` | `/api/v1/scheduled-reports/{id}` | Rapor detayı |
| `GET` | `/api/v1/scheduled-reports/{id}/details` | Rapor + loglar |
| `POST` | `/api/v1/scheduled-reports` | Yeni rapor oluştur |
| `POST` | `/api/v1/scheduled-reports/from-message` | Mesajdan rapor oluştur |
| `PUT` | `/api/v1/scheduled-reports/{id}` | Rapor güncelle |
| `DELETE` | `/api/v1/scheduled-reports/{id}` | Rapor sil |
| `POST` | `/api/v1/scheduled-reports/{id}/pause` | Duraklat |
| `POST` | `/api/v1/scheduled-reports/{id}/resume` | Devam ettir |
| `POST` | `/api/v1/scheduled-reports/{id}/run-now` | Hemen çalıştır |
| `GET` | `/api/v1/scheduled-reports/{id}/logs` | Çalışma logları |
| `GET` | `/api/v1/scheduled-reports/cron-presets` | Cron şablonları |

Tüm endpoint'ler `RequireAuthorization()` + `RequireRateLimiting("fixed")` ile korunur.

---

## Domain Entity

### ScheduledReport (Aggregate Root)

```csharp
// AI.Domain/Scheduling/ScheduledReport.cs
public class ScheduledReport : AggregateRoot<Guid>
{
    public string UserId { get; }
    public string Name { get; }
    public string OriginalPrompt { get; }
    public string SqlQuery { get; }
    public string CronExpression { get; }
    public string ReportServiceType { get; }
    public string ReportDatabaseType { get; }
    public string ReportDatabaseServiceType { get; }
    public Guid? OriginalMessageId { get; }
    public Guid? OriginalConversationId { get; }
    public bool IsActive { get; }
    public DateTime? LastRunAt { get; }
    public DateTime? NextRunAt { get; }
    public int RunCount { get; }
    public bool? LastRunSuccess { get; }
    public string? LastErrorMessage { get; }
    public string? NotificationEmail { get; }
    public string? TeamsWebhookUrl { get; }

    // Factory Method
    public static ScheduledReport Create(...);

    // Domain Methods
    public void UpdateName(string name);
    public void UpdateSchedule(string cronExpression);
    public void UpdateSqlQuery(string sqlQuery);
    public void UpdateNotificationSettings(email, teamsUrl);
    public void SetActive(bool isActive);
    public void SetNextRunAt(DateTime? nextRunAt);
    public ScheduledReportLog AddLog();  // DDD — child entity oluşturma
}
```

### ScheduledReportLog (Entity)

```csharp
// AI.Domain/Scheduling/ScheduledReportLog.cs
public class ScheduledReportLog : Entity<Guid>
{
    public Guid ScheduledReportId { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
    public long? DurationMs { get; }
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? OutputFilePath { get; }
    public string? OutputUrl { get; }
    public int? RecordCount { get; }
    public bool EmailSent { get; }
    public bool TeamsSent { get; }
}
```

---

## İlgili Dosyalar

| Dosya | Satır | Açıklama |
|-------|-------|----------|
| `ScheduledReportUseCase.cs` | 667 | CRUD + Status + Execution |
| `ScheduledReportEndpoints.cs` | 247 | 12 REST endpoint |
| `ReportSchedulerJob.cs` | 221 | Hangfire sync job (her 5 dk) |
| `ScheduledReportJob.cs` | 79 | Rapor çalıştırma job |
| `ScheduledReport.cs` | — | Aggregate Root |
| `ScheduledReportLog.cs` | — | Çalışma logu entity |

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Report-System.md](Report-System.md) | SQL rapor sistemi detayları |
| [Multi-Agent.md](Multi-Agent.md) | SqlAgentPipeline |
| [Authentication-Authorization.md](Authentication-Authorization.md) | Yetkilendirme |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı |
