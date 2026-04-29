# 🔍 Chat Sistemi Detaylı Analizi

## 📋 İçindekiler

- [Mevcut Özellikler](#mevcut-özellikler)
- [Implementasyon Detayları](#implementasyon-detayları)
- [Sistem Güçlü Yönleri](#sistem-güçlü-yönleri)

---

## ✅ Mevcut Özellikler (Aktif Çalışan)

### Chat & Mesajlaşma

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **Streaming Yanıtlar** | `AI.Application/UseCases/AIChatUseCase.cs` | SignalR ile gerçek zamanlı streaming |
| **Conversation Router** | `AI.Application/UseCases/RouteConversationUseCase.cs` | Chat/Document/Report Agent Registry dispatch ile otomatik yönlendirme |
| **Konuşma Geçmişi** | `frontend/src/app/core/services/history.service.ts` | History Sidebar ile geçmiş yönetimi |
| **Konuşma Başlığı Düzenleme** | `frontend/src/app/shared/sidebar/sidebar.component.ts` | Inline title editing |
| **Konuşma Silme** | `frontend/src/app/shared/sidebar/sidebar.component.ts` | Animasyonlu silme |
| **Öneri Butonları** | `frontend/src/app/pages/chat/chat.html` | Mesaj altında suggestions |
| **Message Reactions (👍👎)** | `frontend/src/app/pages/chat/chat.ts`, `FeedbackEndpoints.cs` | ✅ Kullanıcı feedback sistemi |
| **Context Summarization** | `AI.Application/UseCases/ContextSummarizationUseCase.cs` | ✅ Uzun konuşmaları özetleme |

### Feedback & Analytics

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **Message Feedback** | `AI.Domain/Feedback/MessageFeedback.cs` | 👍👎 feedback kaydetme |
| **Feedback Analysis Job** | `AI.Scheduler/Jobs/FeedbackAnalysisJob.cs` | Hangfire ile günlük AI analizi (02:00) |
| **Analytics Dashboard** | `frontend/src/app/pages/dashboard/dashboard.ts` | İstatistik, trend grafikleri, kategori dağılımı |
| **Prompt Improvements** | `AI.Domain/Feedback/PromptImprovement.cs` | AI önerilerini yönetme (Approve/Reject) |

### RAG (Retrieval Augmented Generation)

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **Hybrid Search** | `AI.Application/UseCases/RagSearchUseCase.cs` | Dense + Sparse vectors |
| **HyDE** | `AI.Application/UseCases/RagSearchUseCase.cs` | Hypothetical Document Embeddings |
| **Turkish Stemming** | `AI.Application/UseCases/RagSearchUseCase.cs` | Lucene.NET ile Türkçe kök bulma |
| **Spelling Correction** | `AI.Application/UseCases/RagSearchUseCase.cs` | LLM tabanlı yazım düzeltme |
| **Highlighting** | `AI.Application/UseCases/RagSearchUseCase.cs` | Arama sonuçlarında vurgulama |

### Dosya İşleme

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **Excel/CSV Analizi** | `AI.Infrastructure/Adapters/AI/ExcelServices/DuckDbExcelService.cs` | DuckDB ile in-memory SQL |
| **PDF/Word/PowerPoint/TXT** | `AI.Application/UseCases/DocumentProcessingUseCase.cs` | Metin çıkarma ve analiz |
| **Dosya Yükleme (Base64)** | `frontend/src/app/core/services/chat.service.ts` | Drag & drop dosya yükleme |

### Long-Term Memory

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **User Memory Service** | `AI.Application/UseCases/UserMemoryUseCase.cs` | PostgreSQL + Qdrant |
| **L0/L1/L2 Context** | `AI.Application/UseCases/UserMemoryUseCase.cs` | Katmanlı context stratejisi |
| **Otomatik Extraction** | `AI.Application/UseCases/UserMemoryUseCase.cs` | Konuşmadan bilgi çıkarma |

### Raporlama

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **Rapor Servisi** | `AI.Infrastructure/Adapters/AI/Reports/SqlServer/` | SQL Server |
| **LLM → SQL Generation** | Rapor servisleri | Doğal dil → SQL dönüşümü |
| **HTML Dashboard** | Rapor servisleri | Otomatik görselleştirme |
| **Zamanlanmış Raporlar** | `AI.Scheduler/Jobs/ScheduledReportJob.cs` | Hangfire cron jobs |
| **Email Bildirimi** | `AI.Scheduler/Jobs/ScheduledReportJob.cs` | SMTP entegrasyonu |
| **Teams Entegrasyonu** | `AI.Scheduler/Jobs/ScheduledReportJob.cs` | Webhook bildirimleri |
| **Cron Builder UI** | `frontend/src/app/pages/chat/chat.ts` | Görsel cron oluşturucu |
| **Report Export (PDF/Excel)** | `frontend/src/app/pages/reports/*/` | ✅ Rapor sayfalarında mevcut |

### Multi-Agent Pipeline

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **SQL Agent Pipeline** | `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlAgentPipeline.cs` | ✅ 560 satır |
| **SQL Validation Agent** | `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlValidationAgent.cs` | Syntax ve güvenlik kontrolü |
| **SQL Optimization Agent** | `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlOptimizationAgent.cs` | Query optimizasyonu |
| **SQL Fixer (Pipeline)** | `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlAgentPipeline.cs` | Hata düzeltme (retry mekanizması pipeline içinde) |

### ReAct Pattern (Reasoning + Acting)

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **IReActUseCase** | `AI.Application/Ports/Primary/UseCases/IReActUseCase.cs` | ReAct port interface |
| **ReActUseCase** | `AI.Application/UseCases/ReActUseCase.cs` | THOUGHT/OBSERVATION/ACTION adımları |
| **ReAct Prompt** | `AI.Application/Common/Resources/Prompts/react-thought.md` | Düşünce üretim prompt'u |
| **SignalR Event** | `ReceiveReActStep` | Frontend'e adım bildirimi |

### Filtreleme

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **# Autocomplete** | `frontend/src/app/pages/chat/chat.ts`, `filters/` | Hashtag tabanlı filtreleme |
| **Tarih Filtresi** | `frontend/src/app/pages/chat/chat.html` | Preset, tek tarih, aralık, ay |
| **Filter Tags Display** | `frontend/src/app/pages/chat/chat.html` | Aktif filtre gösterimi |

### Kimlik Doğrulama & Altyapı

| Özellik | Dosya/Konum | Açıklama |
|---------|-------------|----------|
| **JWT + Refresh Token** | `frontend/src/app/core/services/auth.service.ts` | Token tabanlı auth |
| **AD Entegrasyonu** | `AI.Api/Endpoints/Auth/AuthEndpoints.cs` | Active Directory |
| **L1 + L2 Cache** | `AI.Infrastructure/Adapters/External/Caching/` | Memory + Redis |
| **OpenTelemetry** | `AI.Application/Common/Telemetry/` | Distributed tracing |
| **Rate Limiting** | `AI.Api/Extensions/RateLimitingExtensions.cs` | 7 farklı policy |
| **Health Checks** | `AI.Api/Extensions/HealthChecksExtensions.cs` | 10+ health check |

---

## Implementasyon Detayları

### 1. Message Reactions (Feedback)

**Backend Entity (DDD AggregateRoot):**

```csharp
// AI.Domain/Feedback/MessageFeedback.cs
public sealed class MessageFeedback : AggregateRoot<Guid>
{
    public Guid MessageId { get; private set; }
    public Guid ConversationId { get; private set; }
    public string UserId { get; private set; } = null!;
    public FeedbackType Type { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsAnalyzed { get; private set; }
    public DateTime? AnalyzedAt { get; private set; }

    public static MessageFeedback Create(
        Guid messageId, Guid conversationId,
        string userId, FeedbackType type, string? comment = null)
    {
        var feedback = new MessageFeedback { ... };
        feedback.AddDomainEvent(new FeedbackSubmittedEvent(...));
        return feedback;
    }
}
```

**API Endpoint:**

```csharp
// AI.Api/Endpoints/Feedback/FeedbackEndpoints.cs
var group = endpoints.MapGroup("/api/v1/feedback")
    .WithTags("Feedback")
    .RequireAuthorization()
    .RequireRateLimiting(RateLimitingExtensions.FixedWindowPolicy);

group.MapPost("/messages/{messageId:guid}", AddFeedback);
group.MapGet("/messages/{messageId:guid}", GetFeedbackForMessage);
group.MapGet("/conversations/{conversationId:guid}", GetFeedbacksForConversation);
group.MapGet("/statistics", GetStatistics);
group.MapDelete("/messages/{messageId:guid}", DeleteFeedback);
group.MapGet("/dashboard", GetDashboardStatistics);
group.MapGet("/analysis/reports", GetAnalysisReports);
group.MapGet("/analysis/reports/{reportId:guid}/improvements", GetImprovements);
group.MapPut("/analysis/improvements/{improvementId:guid}/status", UpdateImprovementStatus);
```

---

## �️ Konuşma Geçmişi Yönetimi (Backend)

**Dosya:** `AI.Application/UseCases/ConversationUseCase.cs` (833 satır)

Konuşma geçmişi CRUD işlemleri, cache koordinasyonu ve retry mekanizması:

| Özellik | Detay |
|---------|-------|
| **Cache** | L1 (Memory) + L2 (Redis) — `IChatCacheService` |
| **Retry** | Polly — 3 deneme, exponential backoff (200ms, 400ms, 800ms) |
| **Cache Keys** | `CacheKeys` sınıfı ile tutarlı key üretimi |
| **Kullanıcı** | `ICurrentUserService` ile otomatik userId çözümleme |

### Ana Metotlar

```csharp
// ConversationUseCase.cs
GetChatHistoryAsync(request, includeDbResponses)  // Cache-first okuma
SaveMessageAsync(conversationId, message)          // Cache + DB yazma
CreateConversationAsync(userId, title)             // Yeni konuşma
DeleteConversationAsync(conversationId)            // Konuşma silme
UpdateTitleAsync(conversationId, title)            // Başlık güncelleme
GetConversationListAsync(userId)                   // Geçmiş listesi
```

### Cache-First Okuma Akışı

```
GetChatHistoryAsync:
    ├─ Cache hit (includeDbResponses=true) → return cached
    ├─ Cache miss → DB'den oku
    │   ├─ IConversationQueryService.GetMessagesAsync()
    │   └─ Cache'e yaz (CacheSettings TTL)
    └─ return chatHistory
```

---

## 📊 Dashboard Query Servisi (Backend)

**Dosya:** `AI.Application/UseCases/DashboardQueryUseCase.cs` (333 satır)

Feedback analytics dashboard'u için veri sağlayan UseCase:

| Metot | Açıklama |
|-------|----------|
| `GetOverviewAsync()` | Son 30 gün istatistikleri + trend karşılaştırması |
| `GetFeedbackTrendsAsync(days)` | Günlük pozitif/negatif feedback trend grafikleri |
| `GetCategoryBreakdownAsync()` | Son 5 rapordan kategori dağılımı (aggregation) |
| `GetImprovementsAsync(status, priority)` | Prompt improvement listesi (filtrelenebilir) |
| `UpdateImprovementStatusAsync(id, status)` | Improvement durumu güncelle (Applied/Rejected/UnderReview) |
| `GetAnalysisReportsAsync(limit)` | Feedback analiz raporları listesi |
| `GetAnalysisReportDetailAsync(id)` | Rapor detayı + kategoriler + öneriler + improvements |

### Dashboard HTML İşleme

**Dosya:** `AI.Application/UseCases/DashboardUseCase.cs` (145 satır)

SQL rapor sonuçlarından interaktif HTML dashboard oluşturma:

```
LLM Response → IDashboardParser.ParseResponse()
    ├─ HTML, CSS, JS dosyalarını çıkar
    ├─ Insight HTML ekle (AI Veri Analizi)
    └─ IFileSaver.SaveDashboardFiles() → dosya sistemi
        └─ OutputApiUrl + ProjectPath döndür
```

---

## �📈 Sistem Güçlü Yönleri

1. ✅ **Çok Modlu Mimari:** Chat, Document, Report otomatik yönlendirme
2. ✅ **Streaming Yanıtlar:** SignalR ile gerçek zamanlı
3. ✅ **Gelişmiş RAG:** HyDE, Hybrid Search, Turkish Stemming
4. ✅ **Long-Term Memory:** Kişiselleştirilmiş yanıtlar
5. ✅ **Zamanlanmış Raporlar:** Cron builder, Email, Teams
6. ✅ **Güçlü Filtreleme:** # ile autocomplete, tarih seçici
7. ✅ **Veritabanı:** SQL Server + PostgreSQL
8. ✅ **Observability:** OpenTelemetry tracing
9. ✅ **Güvenlik:** Rate limiting, JWT, AD entegrasyonu
10. ✅ **Multi-Agent Pipeline:** SQL Validation, Optimization, Auto-Fixer
11. ✅ **Context Summarization:** Uzun konuşmalarda token optimizasyonu
12. ✅ **Message Feedback System:** 👍👎 feedback + AI analiz + Dashboard
13. ✅ **Usage Analytics Dashboard:** Feedback metrikleri ve raporlar

---

## 📚 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) | Agentic AI pattern'leri |
| [Multi-Agent.md](Multi-Agent.md) | SQL Agent pipeline |
| [Message-Feedback.md](Message-Feedback.md) | Feedback sistemi |
| [System-Overview.md](System-Overview.md) | Genel sistem analizi |
| [Authentication-Authorization.md](Authentication-Authorization.md) | JWT Auth + Active Directory SSO |
| [Scheduled-Reports.md](Scheduled-Reports.md) | Zamanlanmış rapor sistemi (Hangfire) |
| [Infrastructure-Cross-Cutting.md](Infrastructure-Cross-Cutting.md) | Cache, Rate Limiting, Health Checks, Error Handling |
| [Application-Layer.md](Application-Layer.md) | Application Layer detayları |

---
