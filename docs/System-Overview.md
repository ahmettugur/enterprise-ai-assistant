# 🔍 Enterprise AI Assistant - Sistem Analizi

## 📊 Genel Bakış

Bu sistem, **Kurumsal İş Zekası AI Asistanı** olarak tasarlanmış, çok modlu bir konuşma yönetim platformudur. Kullanıcılar chat, döküman arama ve veritabanı raporlama işlemlerini tek bir arayüzden yapabilir.

### Teknoloji Stack

| Katman | Teknoloji |
|--------|-----------|
| **Frontend** | Angular 21, SignalR Client, RxJS, Angular Signals |
| **Backend** | .NET 10, ASP.NET Core, Semantic Kernel |
| **Veritabanı** | PostgreSQL, SQL Server |
| **Vector DB** | Qdrant |
| **Graph DB** | Neo4j (Schema Catalog — dinamik SQL prompt üretimi) |
| **Analitik DB** | DuckDB (Excel/CSV dosya analizi) |
| **Cache** | Redis + In-Memory |
| **AI** | OpenAI GPT-5.2, Azure OpenAI |
| **Real-time** | SignalR (WebSocket) |
| **Scheduler** | Hangfire |
| **Observability** | OpenTelemetry, Serilog |

---

## 🎯 ANA İŞLEV AKIŞI

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                           KULLANICI İSTEĞİ                                       │
└─────────────────────────────────┬────────────────────────────────────────────────┘
                                  │
                                  ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                        CONVERSATION ROUTER                                       │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  LLM ile istek analizi:                                                    │  │
│  │  • Prompt'u analiz et                                                      │  │
│  │  • Dosya var mı kontrol et                                                 │  │
│  │  • Geçmiş context'i değerlendir                                            │  │
│  │  • Action belirle: chat | document | report | ask_*                        │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────┬────────────────────────────────────────────────┘
                                  │
          ┌───────────────────────┼───────────────────────┐
          │                       │                       │
          ▼                       ▼                       ▼
    ┌───────────┐           ┌───────────┐           ┌───────────┐
    │   CHAT    │           │ DOCUMENT  │           │  REPORT   │
    │  (LLM)    │           │  (RAG)    │           │   (SQL)   │
    └─────┬─────┘           └─────┬─────┘           └─────┬─────┘
          │                       │                       │
          └───────────────────────┼───────────────────────┘
                                  │
                                  ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                         SIGNALR HUB → FRONTEND                                   │
│  Events: ReceiveMessage | ReceiveStreamingMessage | ReceiveLoadingMessage |      │
│          ReceiveReActStep                                                        │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## 📁 PROJE YAPISI

```
AIApplications/
├── AI.Api/                          # Primary Adapter (Composition Root)
│   ├── Program.cs                   # Entry point, DI configuration
│   ├── Endpoints/                   # Feature-based Minimal API endpoints
│   │   ├── Auth/
│   │   ├── Dashboard/
│   │   ├── Documents/
│   │   ├── Feedback/
│   │   ├── History/
│   │   ├── Reports/
│   │   └── Search/
│   ├── Hubs/                        # SignalR Hub (AIHub.cs)
│   ├── Common/                      # Shared utilities
│   └── Extensions/                  # DI Extensions
│
├── AI.Application/                  # Hexagonal Core (Ports Layer)
│   ├── Ports/
│   │   ├── Primary/UseCases/        # 19 Primary Port interfaces (I*UseCase)
│   │   └── Secondary/               # Driven Side ports
│   │       ├── Repositories/        # Repository interfaces
│   │       ├── Services/            # Service interfaces (categorized)
│   │       │   ├── AIChat/
│   │       │   ├── Auth/
│   │       │   ├── Cache/
│   │       │   ├── Common/
│   │       │   ├── Database/
│   │       │   ├── Document/
│   │       │   ├── Report/
│   │       │   └── Vector/
│   │       └── AgentCore/            # Agent Registry interfaces
│   │       ├── Notifications/
│   │       └── Scheduling/
│   ├── UseCases/                    # 19 Use Case implementations
│   │   └── ActionAgents/            # Agent Registry implementations
│   ├── DTOs/                        # Data Transfer Objects
│   ├── Configuration/               # Settings classes
│   ├── Results/                     # Result pattern implementations
│   └── Common/
│       └── Resources/
│           ├── Prompts/             # LLM Prompt templates
│           └── Templates/           # HTML templates
│
├── AI.Domain/                       # Core Layer (Aggregate-per-Folder)
│   ├── Common/                      # DDD Building Blocks (Entity, AggregateRoot, ValueObject)
│   ├── Conversations/               # Conversation, Message, IConversationRepository
│   ├── Identity/                    # User, Role, RefreshToken, UserRole
│   ├── Feedback/                    # MessageFeedback, FeedbackAnalysisReport, PromptImprovement
│   ├── Memory/                      # UserMemory, IUserMemoryRepository
│   ├── Documents/                   # DocumentCategory, DocumentDisplayInfo, DocumentMetadata, DocumentChunk
│   ├── Scheduling/                  # ScheduledReport, ScheduledReportLog
│   ├── Enums/                       # Domain enumerations
│   ├── Events/                      # Domain events (record types)
│   ├── Exceptions/                  # Domain exceptions
│   └── ValueObjects/                # Email, Password, Confidence, DateRange, FileInfo
│
├── AI.Infrastructure/               # Secondary Adapters (Infrastructure)
│   ├── Adapters/
│   │   ├── AI/                      # AI Service Adapters
│   │   │   ├── Agents/SqlAgents/
│   │   │   ├── DocumentServices/
│   │   │   ├── ExcelServices/
│   │   │   ├── Neo4j/
│   │   │   ├── Reranking/
│   │   │   ├── SelfQuery/
│   │   │   └── VectorServices/
│   │   ├── External/                # External Service Adapters
│   │   │   ├── Auth/                # TokenService, CurrentUserService
│   │   │   ├── Caching/
│   │   │   ├── DatabaseServices/
│   │   │   ├── Notifications/
│   │   │   └── Scheduling/
│   │   └── Persistence/             # Database Adapters
│   │       ├── Configurations/
│   │       ├── Migrations/
│   │       ├── Repositories/
│   │       └── ChatDbContext.cs
│   ├── Configuration/               # Infrastructure settings
│   └── Extensions/                  # DI Extensions
│
├── AI.Scheduler/                    # Background Job Service
│   ├── Jobs/                        # Hangfire job classes
│   ├── Services/                    # Notification services
│   └── Program.cs                   # Scheduler entry point
│
└── frontend/                        # Angular 21 SPA
    └── src/app/
        ├── core/services/           # Auth, SignalR, Chat services
        ├── pages/                   # Chat, Login pages
        └── shared/                  # Components, Pipes
```

---

## 1️⃣ CONVERSATION ROUTER (İstek Yönlendirme)

**Dosya:** `AI.Application/UseCases/RouteConversationUseCase.cs`

### Ne Yapar?

Kullanıcının her mesajını LLM ile analiz ederek **hangi moda yönlendirileceğine** karar verir. **Agent Registry** pattern kullanarak belirlenen action'ı ilgili agent'a (`ChatActionAgent`, `DocumentActionAgent`, `ReportActionAgent`, `AskActionAgent`) dispatch eder.

### Karar Mekanizması

```json
{
   "action": "chat|document|report|ask_*",
   "reportName": "adventureworks",
   "documentName": "Anayasa|Arge Merkezi|...",
   "templateName": "welcome|ask_document|ask_report_type_adventureworks|...",
   "message": "Kullanıcıya gösterilecek mesaj"
}
```

### Action Türleri

| Action | Açıklama | Yönlendirme |
|--------|----------|-------------|
| `welcome` | Ana menü göster | Template: welcome.html |
| `chat` | Genel sohbet | → AIChatUseCase |
| `document` | Döküman arama | → RagSearchUseCase |
| `report` | Veritabanı raporu | → ReportService (keyed) |
| `ask_document` | Döküman kategorisi sor | Template göster |
| `ask_report` | Veritabanı seç | Template göster |
| `ask_mode` | Mod belirsiz, seçim iste | Template göster |

### Dinamik Template Sistemi

```csharp
// Döküman kategorileri dinamik olarak DB'den geliyor
if (templateName == "ask_document")
    return await _documentMetadataService.GenerateDynamicCategorySelectionTemplateAsync();

// Rapor türleri dinamik olarak registry'den geliyor
if (templateName.StartsWith("ask_report_type_"))
    return _reportMetadataService.GenerateDynamicReportTypeTemplate(databaseId);
```

---

## 2️⃣ AI CHAT SERVİSİ (Genel Sohbet + Dosya Analizi)

**Dosya:** `AI.Application/UseCases/AIChatUseCase.cs`

### Temel Özellikler

#### A. Streaming Chat

```csharp
await foreach (var content in _chatCompletionService.GetStreamingChatMessageContentsAsync(...))
{
    // Her chunk'ı SignalR ile gönder
    await _hubContext.Clients.Client(connectionId)
        .SendAsync("ReceiveStreamingMessage", streamingResponse);
}
```

#### B. Dosya Analizi (Base64)

Desteklenen formatlar:

- **Excel/CSV** → DuckDB ile SQL sorguları
- **PDF/Word/TXT** → Metin çıkarma ve analiz

```csharp
// Excel/CSV için özel işleme
if (_excelAnalysisService.IsSupported(request.FileName))
{
    return await ProcessExcelQueryAsync(request);
}

// Diğer dosyalar için metin çıkarma
var extractedText = DocumentExtractionHelper.ExtractTextFromDocument(fileName, stream);
```

#### C. Vector Store Arama (RAG)

```csharp
public async Task<Result<LLmResponseModel>> SearchVectorStoreAsync(ChatRequest request, string documentName)
{
    var searchRequest = new SearchRequestDto { Query = request.Prompt, DocumentName = documentName };
    var response = await _ragSearchService.SearchAsync(searchRequest);
    // Sonuçları SignalR ile gönder
}
```

---

## 3️⃣ EXCEL/CSV ANALİZ SERVİSİ (DuckDB)

**Dosya:** `AI.Infrastructure/Adapters/AI/ExcelServices/DuckDbExcelService.cs`

### Ne Yapar?

Kullanıcının yüklediği Excel/CSV dosyalarını **in-memory DuckDB** ile analiz eder.

### İş Akışı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Excel Upload   │────►│  Şema Çıkarma   │────►│  SQL Üretme     │
│  (Base64)       │     │  (DuckDB)       │     │  (LLM)          │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                        ┌─────────────────┐              │
                        │  SQL Çalıştır   │◄─────────────┘
                        │  (DuckDB)       │
                        └────────┬────────┘
                                 │
                        ┌────────▼────────┐
                        │  Sonuç Döndür   │
                        │  (HTML Tablo)   │
                        └─────────────────┘
```

### Örnek Kullanım

```
Kullanıcı: "Bu Excel'deki satış toplamını bul" + [satis.xlsx]

Sistem:
1. Şema çıkar: tablename, columns[], sampleRows[]
2. LLM'e gönder → SQL üret: SELECT SUM(satis_tutari) FROM satis_data
3. DuckDB'de çalıştır
4. Sonucu HTML olarak döndür
```

---

## 4️⃣ RAG ARAMA SERVİSİ (Döküman Arama)

**Dosya:** `AI.Application/UseCases/RagSearchUseCase.cs`

### Özellikler

| Özellik | Açıklama |
|---------|----------|
| **HyDE** | Hypothetical Document Embeddings - Daha iyi semantik arama |
| **Hybrid Search** | Dense + Sparse vectors birlikte kullanılır |
| **Spelling Correction** | Yazım hatalarını LLM ile düzeltme |
| **Turkish Stemming** | Lucene.NET ile Türkçe kök bulma |
| **Highlighting** | Sonuçlarda arama terimlerini vurgulama |

### Arama Akışı

```csharp
public async Task<SearchResponse> SearchAsync(SearchRequestDto request)
{
    // 1. Query'yi LLM ile işle (Spelling + HyDE)
    var processingResult = await ProcessQueryWithLLM(request.Query, ...);
    
    // 2. Weighted embeddings ile arama
    var searchResults = await SearchWithWeightedEmbeddings(processingResult, ...);
    
    // 3. Sonuçlara highlighting uygula
    foreach (var result in searchResults)
    {
        result.Content = BuildSnippetAndHighlight(result.Content, query);
    }
    
    return new SearchResponse { Results = results };
}
```

---

## 5️⃣ DÖKÜMAN İŞLEME SERVİSİ

**Dosya:** `AI.Application/UseCases/DocumentProcessingUseCase.cs`

### Desteklenen Formatlar

- PDF
- TXT
- DOCX/DOC
- JSON (Soru-Cevap formatı)

### İşlem Akışı

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Upload      │───►│  Parse       │───►│  Chunk       │───►│  Embed       │
│  (Dosya)     │    │  (Metin)     │    │  (Parçala)   │    │  (OpenAI)    │
└──────────────┘    └──────────────┘    └──────────────┘    └──────┬───────┘
                                                                   │
                                        ┌──────────────┐           │
                                        │  Qdrant      │◄──────────┘
                                        │  (Kaydet)    │
                                        └──────────────┘
```

### Chunk Metadata

```csharp
var payload = new Dictionary<string, Value>
{
    ["document_id"] = chunk.DocumentId.ToString(),
    ["chunk_id"] = chunk.Id.ToString(),
    ["content"] = chunk.Content,
    ["content_length"] = chunk.ContentLength,
    ["start_position"] = chunk.StartPosition,
    ["end_position"] = chunk.EndPosition,
};
```

---

## 6️⃣ RAPOR SERVİSLERİ (SQL Tabanlı)

**Dosyalar:**

- `AI.Infrastructure/Adapters/AI/Reports/SqlServer/SqlServerReportServiceBase.cs`
- `AI.Infrastructure/Adapters/AI/Reports/SqlServer/AdventureWorksReportService.cs`

### Kayıtlı Rapor Servisleri

| Service Key | Veritabanı | Açıklama |
|-------------|------------|----------|
| `adventureworks` | SQL Server | AdventureWorks demo DB |

### Rapor Üretim Akışı

```
┌───────────────┐    ┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  User Prompt  │───►│  System       │───►│  LLM          │───►│  SQL Query    │
│               │    │  Prompt       │    │  (GPT-5.2)    │    │  Generation   │
└───────────────┘    └───────────────┘    └───────────────┘    └───────┬───────┘
                                                                       │
┌───────────────┐    ┌───────────────┐    ┌───────────────┐            │
│  SignalR      │◄───│  HTML         │◄───│  Execute      │◄───────────┘
│  Response     │    │  Generation   │    │  SQL          │
└───────────────┘    └───────────────┘    └───────────────┘
```

### SQL Agent Pipeline (Multi-Agent)

```csharp
if (SqlAgentPipeline != null)
{
    // SQL'i doğrula
    var validationResult = await SqlAgentPipeline.ValidateAsync(sql);
    
    // Optimize et
    var optimizedSql = await SqlAgentPipeline.OptimizeAsync(sql);
}
```

---

## 7️⃣ CHAT GEÇMİŞİ YÖNETİMİ

**Dosya:** `AI.Application/UseCases/ConversationUseCase.cs`

### Özellikler

| Özellik | Açıklama |
|---------|----------|
| **L1 + L2 Cache** | Memory + Redis katmanlı cache |
| **Polly Retry** | 3 deneme, exponential backoff |
| **Conversation Tracking** | ConversationId ile takip |
| **Message Types** | System, User, Assistant, Temporary, Action |

### Mesaj Tipleri

```csharp
public enum MessageType
{
    System,      // System prompt
    User,        // Kullanıcı mesajı
    Assistant,   // AI yanıtı
    Temporary,   // Geçici mesajlar (silinecek)
    Action       // Eylem logları
}
```

### Cache Stratejisi

```csharp
// L1: Memory Cache (hızlı)
if (_memoryCache.TryGetValue(cacheKey, out ChatHistory? cached))
    return cached;

// L2: Redis Cache (distributed)
var cachedData = await _distributedCache.GetAsync(cacheKey);
```

---

## 8️⃣ ZAMANLANMIŞ RAPORLAR

**Dosyalar:**

- `AI.Scheduler/Jobs/ScheduledReportJob.cs`
- Frontend: `frontend/src/app/pages/chat/chat.ts` - `openScheduleModal`

### Özellikler

| Özellik | Açıklama |
|---------|----------|
| **Cron Expression** | Esnek zamanlama |
| **Email Bildirimi** | Rapor e-posta ile gönderilir |
| **Teams Entegrasyonu** | Microsoft Teams webhook |
| **Retry Mekanizması** | 3 deneme, artan bekleme |
| **Log Takibi** | Her çalışma loglanır |

### Cron Builder UI

```typescript
// Preset seçenekler
<button data-cron="0 9 * * 1-5">Hafta içi 09:00</button>
<button data-cron="0 9 * * *">Her gün 09:00</button>
<button data-cron="0 9 * * 1">Haftalık (Pzt)</button>
<button data-cron="0 9 1 * *">Aylık (1.)</button>
```

---

## 9️⃣ FRONTEND İŞLEVLERİ

**Dosya:** `frontend/src/app/pages/chat/chat.ts`

### Ana Özellikler

#### A. SignalR Event Handling

```typescript
// ReceiveMessage - Tam yanıt
this.signalRService.onMessageReceived.subscribe((message) => {
    this.handleReceiveMessage(message);
});

// ReceiveStreamingMessage - Streaming chunks
this.signalRService.onStreamingReceived.subscribe((streamingMessage) => {
    this.handleReceiveStreamingMessage(streamingMessage);
});
```

#### B. Event Delegation (Dinamik HTML)

```typescript
container.addEventListener('click', (e: Event) => {
    const optionButton = target.closest('.option-button');
    if (optionButton) {
        const dataOption = optionButton.getAttribute('data-option');
        this.handleOptionClick(dataOption);
    }
});
```

#### C. Global Report Manager

```typescript
(window as any).reportManager = {
    selectOption: (optionValue) => this.handleOptionClick(optionValue),
    openScheduleModal: (messageId, reportName) => this.openScheduleModal(messageId, reportName),
    // ...
};
```

---

## 🔄 VERİ AKIŞ DİYAGRAMI

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                                  FRONTEND                                      │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐         │
│  │   Chat UI   │   │  Sidebar    │   │   Header    │   │  Modals     │         │
│  │             │   │  (History)  │   │  (User)     │   │  (Schedule) │         │
│  └──────┬──────┘   └──────┬──────┘   └─────────────┘   └──────┬──────┘         │
│         │                 │                                    │               │
│         └─────────────────┼────────────────────────────────────┘               │
│                           │                                                    │
│                    ┌──────▼──────┐                                             │
│                    │  SignalR    │                                             │
│                    │  Service    │                                             │
│                    └──────┬──────┘                                             │
└───────────────────────────┼────────────────────────────────────────────────────┘
                            │ WebSocket
                            ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                                 AI.API                                        │
│                                                                               │
│  ┌────────────┐    ┌────────────────────────────────────────────────────────┐ │
│  │  AIHub     │◄───│                 APPLICATION LAYER                      │ │
│  │  (SignalR) │    │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │ │
│  └────────────┘    │  │ Conversation │  │  AI Chat     │  │   Report     │  │ │
│                    │  │   Router     │  │   Service    │  │   Services   │  │ │
│                    │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │ │
│                    │         │                 │                 │          │ │
│                    │         └─────────────────┼─────────────────┘          │ │
│                    │                           │                            │ │
│                    │  ┌──────────────┐  ┌──────▼───────┐  ┌──────────────┐  │ │
│                    │  │  RAG Search  │  │   History    │  │   Document   │  │ │
│                    │  │   Service    │  │   Service    │  │   Service    │  │ │
│                    │  └──────┬───────┘  └──────┬───────┘  └──────────────┘  │ │
│                    └─────────┼─────────────────┼────────────────────────────┘ │
│                              │                 │                              │
└──────────────────────────────┼─────────────────┼──────────────────────────────┘
                               │                 │
              ┌────────────────┴─────┐   ┌───────┴───────┐
              ▼                      ▼   ▼               ▼
        ┌──────────┐          ┌──────────┐        ┌──────────┐
        │  Qdrant  │          │ Postgres │        │  Redis   │
        │ (Vector) │          │  (Chat)  │        │ (Cache)  │
        └──────────┘          └──────────┘        └──────────┘
              │
              │
        ┌─────▼─────┐
        │  OpenAI   │
        │(Embedding)│
        └───────────┘
```

---

## 📋 MEVCUT ÖZELLİK ÖZETİ

| Kategori | Özellik | Durum |
|----------|---------|-------|
| **Chat** | Streaming yanıt | ✅ |
| **Chat** | Dosya analizi (Excel/PDF) | ✅ |
| **Chat** | Konuşma geçmişi | ✅ |
| **RAG** | Hybrid search (Dense+Sparse) | ✅ |
| **RAG** | HyDE (Hypothetical embeddings) | ✅ |
| **RAG** | Turkish stemming | ✅ |
| **RAG** | Highlighting | ✅ |
| **Report** | SQL Server (AdventureWorks) | ✅ |
| **Report** | LLM → SQL generation | ✅ |
| **Report** | HTML dashboard output | ✅ |
| **Report** | Zamanlanmış raporlar | ✅ |
| **Auth** | JWT + Refresh token | ✅ |
| **Auth** | Active Directory | ✅ |
| **Cache** | L1 (Memory) + L2 (Redis) | ✅ |
| **Observability** | OpenTelemetry tracing | ✅ |
| **Real-time** | SignalR (WebSocket) | ✅ |

---

## 📚 REFERANS DOSYALAR

| Dosya | Açıklama |
|-------|----------|
| `AI.Api/Program.cs` | Uygulama entry point |
| `AI.Api/Extensions/DependencyInjectionExtensions.cs` | DI configuration |
| `AI.Application/UseCases/RouteConversationUseCase.cs` | İstek yönlendirme (Agent Registry dispatch) |
| `AI.Application/UseCases/ActionAgents/` | Agent Registry implementations (Chat, Document, Report, Ask) |
| `AI.Application/UseCases/AIChatUseCase.cs` | Chat servisi |
| `AI.Application/UseCases/RagSearchUseCase.cs` | RAG arama |
| `AI.Application/UseCases/ConversationUseCase.cs` | Geçmiş yönetimi |
| `AI.Infrastructure/Adapters/External/Caching/RedisCacheService.cs` | Cache implementasyonu |
| `AI.Scheduler/Jobs/ScheduledReportJob.cs` | Zamanlanmış raporlar |
| `AI.Scheduler/Jobs/FeedbackAnalysisJob.cs` | Feedback AI analizi (günlük 02:00) |
| `AI.Application/UseCases/ReActUseCase.cs` | ReAct pattern (THOUGHT/OBSERVATION) |
| `frontend/src/app/pages/chat/chat.ts` | Frontend chat component |
| `frontend/src/app/pages/dashboard/dashboard.ts` | Feedback Analytics Dashboard |
| `frontend/src/app/core/services/signalr.service.ts` | SignalR client |
| `frontend/src/app/core/services/feedback.service.ts` | Feedback API servisi |
| `frontend/src/app/core/services/dashboard.service.ts` | Dashboard API servisi |

---

## 📚 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) | Agentic AI pattern'leri |
| [Multi-Agent.md](Multi-Agent.md) | SQL Agent pipeline |
| [Qdrant-Vector-Search.md](Qdrant-Vector-Search.md) | RAG ve vektör arama |
| [Long-Term-Memory.md](Long-Term-Memory.md) | Kullanıcı hafıza sistemi |
| [Chat-System.md](Chat-System.md) | Chat sistemi özellikleri |
| [Authentication-Authorization.md](Authentication-Authorization.md) | JWT Auth + Active Directory SSO |
| [Scheduled-Reports.md](Scheduled-Reports.md) | Zamanlanmış rapor sistemi (Hangfire) |
| [Infrastructure-Cross-Cutting.md](Infrastructure-Cross-Cutting.md) | Cache, Rate Limiting, Health Checks, Error Handling |
| [Report-System.md](Report-System.md) | SQL rapor sistemi detayları |
| [Conversation-Router.md](Conversation-Router.md) | İstek yönlendirme (Agent Registry) |
| [Neo4j-Schema-Catalog.md](Neo4j-Schema-Catalog.md) | Neo4j Schema Catalog |
| [Advanced-RAG.md](Advanced-RAG.md) | Gelişmiş RAG stratejileri |
| [DuckDB-Excel.md](DuckDB-Excel.md) | DuckDB Excel analiz sistemi |
| [DuckDB-Usage.md](DuckDB-Usage.md) | DuckDB teknik kullanım detayları |
| [Message-Feedback.md](Message-Feedback.md) | Feedback analiz sistemi |
| [Hexagonal-Architecture.md](Hexagonal-Architecture.md) | Port/Adapter mimarisi |
| [Application-Layer.md](Application-Layer.md) | Application Layer detayları |
| [User-Guide.md](User-Guide.md) | Kullanım kılavuzu (son kullanıcı) |

---

> **Not:** Bu analiz, mevcut kod tabanının incelenmesiyle oluşturulmuştur. Sistem aktif olarak geliştirilmektedir.
>
