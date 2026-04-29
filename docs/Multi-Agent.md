# Multi-Agent Entegrasyon Analiz Raporu

## ✅ Mevcut Implementasyonlar

> **Önemli:** Aşağıdaki agentic pattern'ler ve multi-agent özellikleri sistemde aktif olarak kullanılmaktadır.

---

### 1. SqlAgentPipeline ✅ TAMAMLANDI

**Dosya:** `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlAgentPipeline.cs` (559 satır)

| Agent | Dosya | Görevi |
|-------|-------|--------|
| **SqlValidationAgent** | `SqlValidationAgent.cs` | SQL syntax kontrolü, güvenlik taraması, otomatik düzeltme önerileri |
| **SqlOptimizationAgent** | `SqlOptimizationAgent.cs` | Query optimizasyonu, index önerileri, performans iyileştirmeleri |
| **SqlAgentPipeline** | `SqlAgentPipeline.cs` | Validation ve Optimization agent'larını koordine eder, retry mekanizması |

#### Yapılandırma (appsettings.json)

```json
{
  "MultiAgent": {
    "Enabled": false,
    "SqlAgents": {
      "Enabled": false,
      "EnableValidation": false,
      "EnableOptimization": false,
      "EnableSecurityCheck": false,
      "EnableAutoCorrection": false,
      "MaxRetries": 2
    }
  }
}
```

#### Agent Akışı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────┐
│  LLM SQL        │────▶│  Validation     │────▶│  Optimization   │────▶│  Execute    │
│  Generation     │     │  Agent          │     │  Agent          │     │             │
└─────────────────┘     └────────┬────────┘     └─────────────────┘     └──────┬──────┘
                                 │                                             │
                                 │ Hata varsa                                  │ Hata varsa
                                 ▼                                             ▼
                        ┌─────────────────┐                           ┌─────────────────┐
                        │  Return Error   │                           │  Auto-Correction│
                        └─────────────────┘                           │  (MaxRetries)   │
                                                                      └─────────────────┘
```

#### Özellikler

- ✅ SQL syntax kontrolü (LLM tabanlı)
- ✅ Güvenlik taraması (SQL injection pattern tespiti, DDL/DML kontrolü)
- ✅ Otomatik düzeltme önerileri (EnableAutoCorrection)
- ✅ Query optimizasyonu (index önerileri, performans iyileştirmeleri)
- ✅ Retry mekanizması (MaxRetries ile düzeltme denemeleri)
- ✅ Fail-open yaklaşımı (hata durumunda orijinal SQL kullanılır)

---

### 2. Chunk-based Insight Analysis ✅ TAMAMLANDI

**Dosya:** `AI.Infrastructure/Adapters/AI/Reports/SqlServer/SqlServerReportServiceBase.cs`

Büyük veri setleri için paralel chunk analizi ve insight üretimi.

#### Yapılandırma (appsettings.json)

```json
{
  "InsightAnalysis": {
    "TokenLimits": {
      "ModelContextLimit": 1000000,
      "SystemPromptTokens": 3000,
      "OutputReserveTokens": 32000,
      "SafetyMarginPercent": 10,
      "TargetTokensPerChunk": 80000,
      "SinglePassThreshold": 700000,
      "TokensPerCharacter": 0.3
    },
    "Chunking": {
      "MaxChunks": 10,
      "MinRowsPerChunk": 200,
      "MaxRowsPerChunk": 10000,
      "EnableSampling": true,
      "SamplingStrategy": "Stratified",
      "MinSamplingRate": 0.1
    },
    "Processing": {
      "EnableParallelChunkAnalysis": true,
      "MaxParallelism": 5,
      "MinDelayBetweenCallsMs": 100,
      "ChunkAnalysisTimeoutSeconds": 120,
      "RetryCount": 3,
      "RetryDelayMs": 1000,
      "EnableProgressNotifications": true
    }
  }
}
```

#### Akış Diyagramı

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          INSIGHT ANALYSIS PIPELINE                              │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌──────────────┐     ┌──────────────┐     ┌─────────────────────────────────┐  │
│  │ Report Data  │────▶│ Chunking     │────▶│ Parallel Chunk Analysis (LLM)   │  │
│  │ (JSON)       │     │ Decision     │     │ - MaxParallelism: 5             │  │
│  └──────────────┘     └──────────────┘     │ - Semaphore based               │  │
│                              │             └───────────────┬─────────────────┘  │
│                              │                             │                    │
│                              ▼                             ▼                    │
│                    ┌──────────────────┐         ┌──────────────────────────────┐│
│                    │ Small Data       │         │ Aggregate Chunk Results      ││
│                    │ (<700K tokens)   │         │ - Theme merging              ││
│                    │ → Direct LLM     │         │ - Statistics aggregation     ││
│                    └──────────────────┘         │ - Critical cases collection  ││
│                              │                  └──────────────┬───────────────┘│
│                              │                                 │                │
│                              ▼                                 ▼                │
│                    ┌───────────────────────────────────────────────────────────┐│
│                    │           Final LLM Analysis → Insight HTML               ││
│                    │           (insight_analysis_prompt.md)                    ││
│                    └───────────────────────────────────────────────────────────┘│
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

#### Özellikler

| Özellik | Açıklama |
|---------|----------|
| **Token-aware Chunking** | Model context limit'ine göre otomatik chunk boyutu hesaplama |
| **Stratified Sampling** | Büyük veri setlerinde temsili örnekleme |
| **Parallel Processing** | SemaphoreSlim ile kontrollü paralel LLM çağrıları |
| **Progress Notifications** | SignalR ile gerçek zamanlı ilerleme bildirimi |
| **Theme Aggregation** | Chunk'lardan gelen temaları birleştirme |
| **Critical Case Detection** | Kritik vakaları tespit ve önceliklendirme |
| **Anomaly Detection** | Veri anomalilerini tespit etme |

#### İlgili Dosyalar

| Dosya | Açıklama |
|-------|----------|
| `InsightAnalysisSettings.cs` | Konfigürasyon sınıfı |
| `InsightAnalysisDtos.cs` | DTO'lar (550 satır) - DataChunk, ChunkSummary, AggregatedInsightData |
| `chunk_analysis_prompt.md` | Chunk analizi için LLM prompt |
| `insight_analysis_prompt.md` | Final insight üretimi için LLM prompt |

---

### 3. Advanced RAG Agents ✅ TAMAMLANDI

**Dosya:** `AI.Application/UseCases/RagSearchUseCase.cs` ve `AdvancedRag/` klasörü

#### 3.1 LLM Reranker

**Dosya:** `AI.Infrastructure/Adapters/AI/Reranking/LLMReranker.cs` (247 satır)

Arama sonuçlarını LLM ile değerlendirerek yeniden sıralar.

```csharp
public interface IReranker
{
    bool IsEnabled { get; }
    Task<List<SearchResult>> RerankAsync(
        string query,
        List<SearchResult> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
```

**Özellikler:**

- ✅ LLM tabanlı relevance scoring (0-10 arası)
- ✅ Batch processing ile verimli çağrı
- ✅ Configurable topK ve candidate count
- ✅ Fail-safe (hata durumunda orijinal sonuçlar döner)

#### 3.2 Self-Query Extractor

**Dosya:** `AI.Infrastructure/Adapters/AI/SelfQuery/SelfQueryExtractor.cs` (268 satır)

Kullanıcı sorgusundan otomatik metadata filtreleri çıkarır.

```csharp
public interface ISelfQueryExtractor
{
    bool IsEnabled { get; }
    Task<SelfQueryResult> ExtractAsync(
        string userQuery,
        List<MetadataFieldInfo>? availableMetadataFields = null,
        CancellationToken cancellationToken = default);
}
```

**Desteklenen Metadata Alanları:**

- `category` - Doküman kategorisi
- `year` - Doküman yılı
- `fileName` - Dosya adı
- `documentType` - Doküman tipi
- `department` - İlgili departman
- `language` - Doküman dili

**Örnek:**

```
Sorgu: "2024 yılına ait finans departmanı politikaları"
→ Semantic Query: "finans departmanı politikaları"
→ Filters: { year: 2024, department: "finans" }
```

#### RAG Akışı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  User Query     │────▶│  Self-Query     │────▶│  Spelling       │
│                 │     │  Extractor      │     │  Correction     │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Answer         │◀────│  LLM Reranker   │◀────│  Hybrid Search  │
│  Synthesis      │     │  (Relevance)    │     │  (Dense+Sparse) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

---

### 4. Context Summarization ✅ TAMAMLANDI

**Dosya:** `AI.Application/UseCases/ContextSummarizationUseCase.cs`

Uzun konuşma geçmişlerini özetleyerek token tasarrufu sağlar.

#### Yapılandırma (appsettings.json)

```json
{
  "ContextSummarization": {
    "Enabled": true,
    "MaxTokenThreshold": 8000,
    "SlidingWindowSize": 10,
    "SummaryMaxTokens": 500,
    "SummaryCacheTtlMinutes": 30
  }
}
```

#### Interface

```csharp
public interface IContextSummarizationService
{
    Task<ChatHistory> GetSummarizedChatHistoryAsync(
        Guid conversationId,
        ChatHistory fullHistory,
        CancellationToken cancellationToken = default);

    bool RequiresSummarization(ChatHistory history);

    Task<string> SummarizeMessagesAsync(
        IEnumerable<ChatMessageContent> messages,
        CancellationToken cancellationToken = default);
}
```

#### Akış

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Full Chat      │────▶│  Token Count    │────▶│  > 8000 tokens? │
│  History        │     │  Check          │     │                 │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                                              ┌──────────┴──────────┐
                                              │                     │
                                              ▼                     ▼
                                     ┌─────────────────┐   ┌─────────────────┐
                                     │  Summarize      │   │  Return Full    │
                                     │  Old Messages   │   │  History        │
                                     └────────┬────────┘   └─────────────────┘
                                              │
                                              ▼
                                     ┌─────────────────┐
                                     │  Summary +      │
                                     │  Sliding Window │
                                     │  (Last 10 msgs) │
                                     └─────────────────┘
```

---

### 5. User Memory Service (Long-term Memory) ✅ TAMAMLANDI

**Dosya:** `AI.Application/UseCases/UserMemoryUseCase.cs`

Kullanıcı tercihlerini ve bağlamını uzun vadeli olarak saklar.

#### Memory Layers

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        USER MEMORY LAYERS                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  L0: Tarih Bilgisi (BEDAVA)                                                 │
│  ├── Bugünün tarihi, gün, saat                                              │
│  └── "Son 3 ay", "geçen hafta" gibi ifadeleri çözümleme                     │
│                                                                             │
│  L1: CurrentUserService (BEDAVA - JWT'den)                                  │
│  ├── Kullanıcı adı                                                          │
│  ├── Roller (Admin, User, etc.)                                             │
│  └── Departman bilgisi                                                      │
│                                                                             │
│  L2: Semantic Search (Qdrant + LLM)                                         │
│  ├── Kullanıcı tercihleri                                                   │
│  ├── Geçmiş konuşma bağlamları                                              │
│  └── Extracted memories from conversations                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Interface

```csharp
public interface IUserMemoryService
{
    Task<string> BuildMemoryContextAsync(string query, CancellationToken cancellationToken = default);
    
    Task ExtractAndStoreMemoriesAsync(
        string userMessage, 
        string assistantResponse, 
        string userId,
        CancellationToken cancellationToken = default);
}
```

#### Özellikler

- ✅ L0/L1/L2 katmanlı memory mimarisi
- ✅ LLM ile otomatik memory extraction
- ✅ Qdrant'ta vektör olarak saklama
- ✅ Semantic search ile ilgili memory'leri getirme
- ✅ MAX_MEMORIES_PER_USER = 100 limit

---

### 6. Dashboard Generation ✅ TAMAMLANDI

**Dosya:** `AI.Infrastructure/Adapters/AI/Reports/SqlServer/SqlServerReportServiceBase.cs`

İki farklı dashboard generation modu:

| Mod | Açıklama | Kullanım |
|-----|----------|----------|
| **Fast Dashboard** | Template-based, şema + örnek veri | `Dashboard.UseFastDashboard = true` |
| **Full Dashboard** | LLM ile tam HTML üretimi | `Dashboard.UseFastDashboard = false` |

#### Yapılandırma

```json
{
  "Dashboard": {
    "UseFastDashboard": false,
    "ConfigPromptFileName": "dashboard_config_generator_prompt.md",
    "FullPromptFileName": "dashboard_generator_prompt_adventureworks.md",
    "OutputFolder": "output-folder"
  }
}
```

---

## 📊 Mevcut Mimari Özeti

| Bileşen | Mevcut Durum | Karmaşıklık | Agentic Features |
|---------|--------------|-------------|------------------|
| `RouteConversationUseCase` | LLM ile mode seçimi + **Agent Registry dispatch** | Düşük | Mode detection, Agent dispatch |
| `SqlServerReportServiceBase` | LLM → SQL → **SqlAgentPipeline** → Execute → Dashboard + Insight | Yüksek | SQL Agents, Chunk Analysis |
| `AdventureWorksReportService` | SQL Server için rapor + Agent Pipeline | Yüksek | SQL Agents, Chunk Analysis |
| `AIChatUseCase` | 5 retry + Context Summarization + User Memory | Yüksek | Memory, Summarization |
| `ExcelAnalysisUseCase` | Excel multi-query analiz + DuckDB + Retry | Yüksek | Excel Analysis |
| `RagSearchUseCase` | Self-Query + HyDE + Spelling + Reranking | Çok Yüksek | Reranker, Self-Query |
| `ConversationUseCase` | Cache + Retry + Orchestration | Orta | - |

---

## Mevcut Multi-Agent Mimarisi

```
                    ┌─────────────────────────────────────┐
                    │         ORCHESTRATOR                │
                    │      (RouteConversationUseCase)     │
                    │      + Agent Registry Dispatch      │
                    └─────────────┬───────────────────────┘
                                  │
    ┌─────────────────────────────┼─────────────────────────────┐
    │                             │                             │
    ▼                             ▼                             ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  REPORT AGENTS  │    │   RAG AGENTS    │    │  CHAT AGENTS    │
│  (✅ Aktif)     │    │  (✅ Aktif)     │    │  (✅ Aktif)     │
├─────────────────┤    ├─────────────────┤    ├─────────────────┤
│ • SQL Generator │    │ • Self-Query    │    │ • Context       │
│ • SQL Validator │    │   Extractor     │    │   Summarization │
│ • SQL Optimizer │    │ • LLM Reranker  │    │ • User Memory   │
│ • Chunk Analyzer│    │ • HyDE          │    │   Service       │
│ • Insight Agent │    │ • Spell Check   │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                      │                      │
         └──────────────────────┼──────────────────────┘
                                ▼
                    ┌─────────────────────────────────────┐
                    │          RESPONSE SYNTHESIS         │
                    │    (Dashboard + Insight HTML)       │
                    └─────────────────────────────────────┘
```

---

## 🛠️ Implementasyon Durumu

### Tamamlanan Faz'lar

| Faz | Özellik | Durum |
|-----|---------|-------|
| 1 | SQL Validation & Optimization Agent | ✅ TAMAMLANDI |
| 2 | Chunk-based Insight Analysis | ✅ TAMAMLANDI |
| 3 | LLM Reranker (RAG) | ✅ TAMAMLANDI |
| 4 | Self-Query Extractor (RAG) | ✅ TAMAMLANDI |
| 5 | Context Summarization | ✅ TAMAMLANDI |
| 6 | User Memory Service | ✅ TAMAMLANDI |
| 7 | ReAct Pattern (THOUGHT/OBSERVATION) | ✅ TAMAMLANDI |

---

## 📁 İlgili Dosyalar

### Agents

| Dosya | Açıklama |
|-------|----------|
| `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlValidationAgent.cs` | SQL validation agent |
| `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlOptimizationAgent.cs` | SQL optimization agent |
| `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlAgentPipeline.cs` | Agent pipeline koordinatörü |

### Advanced RAG

| Dosya | Açıklama |
|-------|----------|
| `AI.Infrastructure/Adapters/AI/Reranking/LLMReranker.cs` | LLM reranking |
| `AI.Infrastructure/Adapters/AI/SelfQuery/SelfQueryExtractor.cs` | Metadata filter extraction |

### Services

| Dosya | Açıklama |
|-------|----------|
| `AI.Infrastructure/Adapters/AI/Reports/SqlServer/SqlServerReportServiceBase.cs` | Rapor üretimi + Insight |
| `AI.Application/UseCases/ContextSummarizationUseCase.cs` | Context özetleme |
| `AI.Application/UseCases/UserMemoryUseCase.cs` | Uzun vadeli hafıza |
| `AI.Application/UseCases/RagSearchUseCase.cs` | RAG arama servisi |

### DTOs ve Konfigürasyon

| Dosya | Açıklama |
|-------|----------|
| `AI.Application/DTOs/InsightAnalysisDtos.cs` | Chunk analysis DTO'ları |
| `AI.Application/Configuration/InsightAnalysisSettings.cs` | Insight konfigürasyonu |

### Prompts

| Dosya | Açıklama |
|-------|----------|
| `chunk_analysis_prompt.md` | Chunk analizi prompt |
| `insight_analysis_prompt.md` | Final insight prompt |
| `dashboard_generator_prompt_adventureworks.md` | Dashboard üretimi prompt |

---

## 🔗 Teknik Notlar

### Mevcut Teknolojiler

- .NET 10 / ASP.NET Core 10
- Semantic Kernel (AI/LLM entegrasyonu)
- PostgreSQL (public, history, reports, hangfire schemas)
- Qdrant (Vector store - RAG + User Memory)
- Hangfire 1.8.17 (Background job processing)
- DuckDB (Excel/CSV analysis)
- SignalR (Real-time progress notifications)

### Agentic Patterns

| Pattern | Kullanım Yeri | Açıklama |
|---------|--------------|----------|
| **Pipeline** | SqlAgentPipeline | Sıralı agent işleme |
| **Parallel Fan-out** | Chunk Analysis | Paralel LLM çağrıları |
| **Memory** | UserMemoryUseCase | Long-term user context |
| **Self-Query** | SelfQueryExtractor | Otomatik filter extraction |
| **Reranking** | LLMReranker | Sonuç yeniden sıralama |
| **Summarization** | ContextSummarizationUseCase | Token optimizasyonu |
| **ReAct** | RouteConversationUseCase | Şeffaf düşünce süreci (THOUGHT/OBSERVATION) |
| **Agent Registry** | RouteConversationUseCase → ActionAgents | Strategy pattern ile action dispatch (Chat, Document, Report, Ask) |

---

## 📚 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) | Tüm Agentic AI pattern'leri |
| [System-Overview.md](System-Overview.md) | Genel sistem analizi |
| [Conversation-Router.md](Conversation-Router.md) | İstek yönlendirme (Agent Registry) |
| [Qdrant-Vector-Search.md](Qdrant-Vector-Search.md) | RAG ve vektör arama |
| [Long-Term-Memory.md](Long-Term-Memory.md) | User Memory detayları |
| [Report-System.md](Report-System.md) | Rapor sistemi detayları |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
| [Neo4j-Schema-Catalog.md](Neo4j-Schema-Catalog.md) | Neo4j şema kataloğu |

---

*Bu doküman Multi-Agent entegrasyonu için referans olarak kullanılacaktır.*
