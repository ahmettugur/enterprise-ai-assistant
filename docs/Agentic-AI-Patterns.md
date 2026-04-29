# 🤖 Agentic AI Patternleri Analizi

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [1. Router Pattern](#1-router-pattern-intent-based-routing)
- [2. Pipeline Pattern](#2-pipeline-pattern-sequential-multi-agent)
- [3. RAG Pattern](#3-rag-pattern-retrieval-augmented-generation)
- [4. Memory Pattern](#4-memory-pattern-long-term-memory)
- [5. Summarization Pattern](#5-summarization-pattern-context-compression)
- [6. Feedback Loop Pattern](#6-feedback-loop-pattern-self-improvement)
- [7. Tool Use Pattern](#7-tool-use-pattern-function-calling)
- [8. ReAct Pattern](#8-react-pattern-reasoning--acting)
- [Pattern Özet Tablosu](#pattern-özet-tablosu)
- [Gelecek Patternler](#gelecek-patternler)

---

## 🎯 Genel Bakış

Bu dokümanda, Enterprise AI Assistant projesinde aktif olarak kullanılan **8 farklı Agentic AI pattern** detaylı şekilde açıklanmaktadır.

### Agentic AI Nedir?

Agentic AI, yapay zekanın otonom karar verme, planlama ve eyleme geçme yeteneklerini ifade eder. Geleneksel AI sistemlerinden farklı olarak, Agentic AI:

- **Otonom hareket eder**: İnsan müdahalesi olmadan görevleri tamamlar
- **Araçları kullanır**: Veritabanları, API'ler, dosya sistemleri ile etkileşir
- **Öğrenir ve adapte olur**: Geri bildirimlerden öğrenerek gelişir
- **Planlama yapar**: Karmaşık görevleri alt görevlere böler

---

## ✅ 1. Router Pattern (Intent-Based Routing)

### Tanım

Router pattern, kullanıcı isteklerini analiz ederek en uygun işlem moduna yönlendiren bir orchestration pattern'idir.

### Konum

**Dosya:** `AI.Application/UseCases/RouteConversationUseCase.cs` (456 satır)

### Akış Diyagramı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Kullanıcı      │────▶│  LLM Analiz     │────▶│  Mode Belirleme │
│  Mesajı         │     │  (Intent)       │     │                 │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
         ┌───────────────────┬───────────────────┬───────┴───────┐
         ▼                   ▼                   ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────┐
│  Chat Mode      │ │  Document Mode  │ │  Report Mode    │ │  Ask Mode   │
│  AIChatUseCase  │ │  RagSearchSvc   │ │  IReportService │ │  Clarify    │
└─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────┘
```

### Yönlendirme Tablosu

| Mode | Tetikleyici Örnek | Hedef Servis |
|------|-------------------|--------------|
| `chat` | "Merhaba, nasılsın?" | AIChatUseCase |
| `document` | "İade politikası nedir?" | RagSearchUseCase |
| `report` | "Son 7 günün satış sayısı" | AdventureWorksReportService |
| `ask` | Belirsiz sorular | AskActionAgent |

### Teknik Özellikler

| Özellik | Değer |
|---------|-------|
| LLM Çağrısı | Tek çağrı ile classification |
| Retry Sayısı | 3 deneme |
| Base Delay | 500ms |
| Metadata Extraction | DocumentName, ReportName, Suggestion |

### Kod Örneği (Agent Registry Pattern)

```csharp
public async Task<Result<dynamic>> OrchestrateAsync(ChatRequest request, CancellationToken ct)
{
    var llmSelectionResult = await SelectModeWithLlmAsync(request!);
    
    // Agent Registry ile yönlendirme (Strategy + Registry Pattern)
    var agent = _agentRegistry.FindAgent(llmSelectionResult.Action)
        ?? throw new InvalidOperationException($"Action agent not found: {llmSelectionResult.Action}");
    
    var context = new ActionContext(request, llmSelectionResult);
    var apiResult = await agent.HandleAsync(context, ct);
    return apiResult;
}
```

**Registered Agents:**

| Agent Sınıfı | Action | Hedef Servis |
|-------------|--------|--------------|
| `ChatActionAgent` | `chat` | `IAIChatUseCase.GetStreamingChatResponseAsync()` |
| `DocumentActionAgent` | `document` | `IAIChatUseCase.SearchVectorStoreAsync()` |
| `ReportActionAgent` | `report` | `IReportService` (Keyed Service) |
| `AskActionAgent` | `ask_*` | Dinamik template + SignalR |

---

## ✅ 2. Pipeline Pattern (Sequential Multi-Agent)

### Tanım

Pipeline pattern, birden fazla agent'ın sıralı olarak çalıştığı ve her agent'ın bir öncekinin çıktısını girdi olarak aldığı bir multi-agent orchestration pattern'idir.

### Konum

**Klasör:** `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/`

| Dosya | Satır | Açıklama |
|-------|-------|----------|
| `SqlAgentPipeline.cs` | 559 | Pipeline coordinator |
| `SqlValidationAgent.cs` | 223 | Syntax ve security validation |
| `SqlOptimizationAgent.cs` | 310 | Query optimization |
| `ISqlAgentPipeline.cs` | - | Interface tanımları (AI.Application/Ports içinde) |

### Akış Diyagramı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────┐
│  LLM SQL        │────▶│  Validation     │────▶│  Optimization   │────▶│  Execute    │
│  Generation     │     │  Agent          │     │  Agent          │     │             │
└─────────────────┘     └────────┬────────┘     └────────┬────────┘     └──────┬──────┘
                                 │                        │                     │
                                 │ Hata varsa             │ Re-validate         │ Hata varsa
                                 ▼                        ▼                     ▼
                        ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
                        │  Return Error   │     │  Rollback       │     │  Fixer Agent    │
                        │                 │     │  (Orijinal SQL) │     │  (MaxRetries)   │
                        └─────────────────┘     └─────────────────┘     └─────────────────┘
```

### Agent Görevleri

#### SqlValidationAgent

| Görev | Açıklama |
|-------|----------|
| **Syntax Validation** | SQL syntax kontrolü (LLM tabanlı) |
| **Security Check** | SQL injection pattern'leri tespit |
| **Semantic Validation** | Tablo/kolon isimleri kontrolü |
| **Auto-Correction** | Hatalı SQL düzeltme önerisi |

**Tehlikeli Pattern Örnekleri:**

```csharp
private static readonly string[] DangerousPatterns =
[
    @"\bDROP\s+TABLE\b",
    @"\bDROP\s+DATABASE\b",
    @"\bTRUNCATE\s+TABLE\b",
    @"\bDELETE\s+FROM\b(?!\s+.*\bWHERE\b)",  // WHERE olmadan DELETE
    @"\bUPDATE\b(?!\s+.*\bWHERE\b)",          // WHERE olmadan UPDATE
    @";\s*--",                                 // Comment injection
    @"'\s*OR\s+'1'\s*=\s*'1",                  // SQL injection pattern
];
```

#### SqlOptimizationAgent

| Görev | Açıklama |
|-------|----------|
| **Quick Optimizations** | Regex tabanlı hızlı düzeltmeler |
| **Deep Optimization** | LLM ile performans iyileştirme |
| **Index Suggestions** | Index kullanım önerileri |
| **Query Rewrite** | Daha verimli SQL yazımı |

### Konfigürasyon (appsettings.json)

```json
"MultiAgent": {
  "Enabled": true,
  "SqlAgents": {
    "Enabled": true,
    "EnableValidation": true,
    "EnableOptimization": true,
    "EnableSecurityCheck": true,
    "EnableAutoCorrection": true,
    "MaxRetries": 2
  }
}
```

### Fail-Safe Mekanizmaları

| Mekanizma | Açıklama |
|-----------|----------|
| **Fail-Open** | Agent hatası durumunda orijinal SQL ile devam |
| **Re-Validation** | Optimization sonrası tekrar validate |
| **Auto-Rollback** | Optimization SQL'i bozarsa geri al |
| **Stage Tracking** | Her stage için duration ve status takibi |

---

## ✅ 3. RAG Pattern (Retrieval-Augmented Generation)

### Tanım

RAG pattern, kullanıcı sorusunu vektör veritabanında arayarak ilgili dökümanları bulur ve bu bilgiyi LLM'e context olarak sağlayarak daha doğru yanıtlar üretir.

### Konum

**Dosya:** `AI.Application/UseCases/RagSearchUseCase.cs` (777 satır)

### Akış Diyagramı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  User Query     │────▶│  Spelling       │────▶│  HyDE           │
│                 │     │  Correction     │     │  Generation     │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                                                         ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  LLM Synthesis  │◀────│  Result Merge   │◀────│  Hybrid Search  │
│  (Final Answer) │     │  & Ranking      │     │  (Dense+Sparse) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

### Alt-Patternler

#### 3.1 HyDE (Hypothetical Document Embeddings)

Kullanıcı sorusundan varsayımsal bir döküman paragrafı üretilir ve bu paragrafın embedding'i ile arama yapılır. Bu, "query-document semantic gap" problemini çözer.

```
User Query: "İade politikası nedir?"
     │
     ▼
HyDE Generation: "Ürün iade işlemleri için müşteriler, satın alma 
tarihinden itibaren 14 gün içinde orijinal ambalajı ile birlikte 
ürünü iade edebilir. İade işlemi için fatura ve ürün etiketi..."
     │
     ▼
Embedding → Vector Search → Daha Doğru Sonuçlar
```

**Prompt Örneği:**

```
Sen Türkçe query processing asistanısın. İki görevin var:

1. SPELLING CORRECTION: Query'deki yazım hatalarını düzelt
2. HYPOTHETICAL DOCUMENT: Bu query'ye cevap verebilecek, 
   varsayımsal bir doküman paragrafı üret

HYPOTHETICAL DOCUMENT kuralları:
- 50-150 kelime uzunluğunda olmalı
- Formal, bilgilendirici ve uzman üslubu kullan
- Domain bağlamına uygun terimler ekle
```

#### 3.2 Hybrid Search (Dense + Sparse)

| Yöntem | Ağırlık | Açıklama |
|--------|---------|----------|
| **Dense Vectors** | 0.7 (70%) | Semantic similarity (text-embedding-3-large) |
| **Sparse Vectors** | 0.3 (30%) | Keyword matching (BM25-like) |

```csharp
await _qdrantService.HybridSearchAsync(
    collectionName,
    denseEmbedding,
    sparseIndices,
    sparseValues,
    limit,
    minScore,
    denseWeight: 0.7,
    sparseWeight: 0.3,
    cancellationToken);
```

#### 3.3 Weighted Dual Embedding

İki farklı embedding ile arama yapılır ve sonuçlar merge edilir:

| Embedding | Kaynak | Amaç |
|-----------|--------|------|
| **Corrected Query** | Yazım düzeltilmiş kullanıcı sorgusu | Exact match |
| **HyDE Document** | Varsayımsal döküman paragrafı | Semantic match |

#### 3.4 Turkish Stemming

Lucene.NET ile Türkçe morfolojik analiz:

```csharp
// "komisyonları" → "komisyon"
private static string RemoveTurkishSuffix(string word)
{
    var turkishSuffixes = new[]
    {
        "ları", "leri", "lar", "ler",     // Plural
        "ını", "ini", "unu", "ünü",       // Accusative
        "ının", "inin", "unun", "ünün",   // Genitive
        // ...
    };
}
```

### Konfigürasyon

```json
"Qdrant": {
  "Host": "localhost",
  "Port": 6333,
  "DefaultCollection": "documents",
  "VectorSize": 3072,
  "MinSimilarityScore": 0.3,
  "DenseWeight": 0.7,
  "SparseWeight": 0.3,
  "HyDEWeight": 0.7,
  "HyDEMaxTokens": 350
}
```

---

## ✅ 4. Memory Pattern (Long-Term Memory)

### Tanım

Memory pattern, kullanıcı tercihlerini ve bilgilerini kalıcı olarak saklayarak kişiselleştirilmiş yanıtlar üretilmesini sağlar.

### Konum

**Dosya:** `AI.Application/UseCases/UserMemoryUseCase.cs` (559 satır)

### Katmanlı Memory Stratejisi

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           MEMORY CONTEXT KATMANLARI                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ L0: Tarih Bilgisi                                                 BEDAVA│    │
│  │ "Tarih: 31 Aralık 2025, Salı (Saat: 14:30)"                             │    │
│  │ → LLM "son 3 ay", "geçen hafta" gibi ifadeleri çözümleyebilir           │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ L1: JWT Claims                                                    BEDAVA│    │
│  │ "Kullanıcı: Ahmet Yılmaz (Admin, Supervisor)"                           │    │
│  │ → Zaten authentication'da mevcut, ek maliyet yok                        │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ L2: Öğrenilen Tercihler                                     ~50-80 token│    │
│  │ "Context: tercih_edilen_format: Excel; departman: Finans"               │    │
│  │ → Qdrant semantic search ile en alakalı 5 memory getirilir              │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Veri Akışı

#### Memory Okuma (Her Chat İsteğinde)

```
Kullanıcı Mesajı
     │
     ▼
BuildMemoryContextAsync()
     │
     ├── L0: DateTime.Now (Türkçe format)
     ├── L1: CurrentUserService (JWT claims)
     └── L2: GetRelevantMemoriesAsync() → Qdrant semantic search
     │
     ▼
System Prompt'a Ekle
```

#### Memory Yazma (Fire-and-Forget)

```
Chat Yanıtı Tamamlandı
     │
     ▼
Task.Run (background)
     │
     ▼
ExtractAndStoreMemoriesAsync()
     │
     ▼
LLM Extraction Prompt
     │
     ▼
JSON Parse: [{"key": "...", "value": "..."}]
     │
     ├── PostgreSQL (UserMemory entity)
     └── Qdrant (Vector embedding)
```

### Memory Entity

```csharp
public class UserMemory : BaseEntity
{
    public string UserId { get; private set; }
    public string Key { get; private set; }           // "tercih_edilen_format"
    public string Value { get; private set; }         // "Excel"
    public MemoryCategory Category { get; private set; }
    public float Confidence { get; private set; }     // 0.0 - 1.0
    public int UsageCount { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }
}

public enum MemoryCategory
{
    Preference,    // Tercihler
    Interaction,   // Etkileşim kalıpları
    Feedback,      // Geri bildirimler
    WorkContext    // İş bağlamı
}
```

### Özellikler

| Özellik | Değer |
|---------|-------|
| Max Memories Per User | 100 |
| Min Extraction Confidence | 0.6 |
| Min Message Length | 10 karakter |
| Storage | PostgreSQL + Qdrant (dual) |
| GDPR | "Beni Unut" desteği |

---

## ✅ 5. Summarization Pattern (Context Compression)

### Tanım

Summarization pattern, uzun konuşma geçmişlerini özetleyerek token tasarrufu sağlar ve context window limitlerini aşmayı önler.

### Konum

**Dosya:** `AI.Application/UseCases/ContextSummarizationUseCase.cs` (311 satır)

### Akış Diyagramı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Full Chat      │────▶│  Token Count    │────▶│  Threshold      │
│  History        │     │  Check          │     │  Aşıldı mı?     │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                                   ┌─────────────────────┴─────────────────────┐
                                   │ Hayır                                Evet │
                                   ▼                                           ▼
                        ┌─────────────────┐                         ┌─────────────────┐
                        │  Direkt Kullan  │                         │  Summarize      │
                        │  (Full History) │                         │  Old Messages   │
                        └─────────────────┘                         └────────┬────────┘
                                                                             │
                                                                             ▼
                                                                   ┌─────────────────┐
                                                                   │  Summary +      │
                                                                   │  Sliding Window │
                                                                   └─────────────────┘
```

### Konfigürasyon

```json
"ContextSummarization": {
  "Enabled": true,
  "MaxTokenThreshold": 8000,
  "SlidingWindowSize": 10,
  "SummaryMaxTokens": 500,
  "SummaryCacheTtlMinutes": 30
}
```

### Özetleme Prompt'u

```
Aşağıdaki konuşma geçmişini kısa ve öz bir şekilde özetle.
Önemli noktaları, kararları ve bağlamı koru.

Özetlerken şunlara dikkat et:
- Kullanıcının ana soruları/istekleri
- Verilen önemli bilgiler (isimler, numaralar, tarihler)
- Alınan kararlar veya çözümler
- Devam eden konular
```

### Sonuç Yapısı

```
┌─────────────────────────────────────────────────────┐
│ ÖZET (eski mesajlar)                                │
│ "Kullanıcı son 7 günün satış raporunu istedi,       │
│  Excel formatında almak istediğini belirtti..."     │
├─────────────────────────────────────────────────────┤
│ SON 10 MESAJ (sliding window - detaylı)             │
│ User: "Bunu grafikle gösterebilir misin?"           │
│ AI: "Tabii, işte grafik..."                         │
│ ...                                                 │
└─────────────────────────────────────────────────────┘
```

---

## ✅ 6. Feedback Loop Pattern (Self-Improvement)

### Tanım

Feedback Loop pattern, kullanıcı geri bildirimlerini toplayarak AI yanıtlarını iyileştirme önerileri üretir. Sistem zamanla kendini geliştirir.

### Konum

| Dosya | Açıklama |
|-------|----------|
| `AI.Scheduler/Jobs/FeedbackAnalysisJob.cs` | Hangfire job (294 satır) |
| `AI.Domain/Feedback/MessageFeedback.cs` | Feedback entity |
| `AI.Domain/Feedback/PromptImprovement.cs` | İyileştirme önerileri |
| `AI.Domain/Feedback/FeedbackAnalysisReport.cs` | Analiz raporu |

### Akış Diyagramı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Kullanıcı      │────▶│  👍/👎          │────▶│  Veritabanı     │
│  AI Yanıtı      │     │  Feedback       │     │  (Kayıt)        │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                               ┌─────────────────────────┘
                               │ Her gece 02:00
                               ▼
                        ┌─────────────────┐
                        │  Hangfire Job   │
                        │  (Analiz)       │
                        └────────┬────────┘
                                 │
                                 ▼
                        ┌─────────────────┐
                        │  AI Analiz      │
                        │  (GPT-5.2)      │
                        └────────┬────────┘
                                 │
         ┌───────────────────────┼───────────────────────┐
         ▼                       ▼                       ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Kategori       │     │  İyileştirme    │     │  Öncelik        │
│  Gruplandırma   │     │  Önerileri      │     │  Belirleme      │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                                 │
                                 ▼
                        ┌─────────────────┐
                        │  Dashboard      │
                        │  (Approve/Reject)│
                        └─────────────────┘
```

### Analiz Prompt'u

```
Sen bir yapay zeka kalite analisti ve geliştirme uzmanısın. 
Kullanıcılardan gelen olumsuz geri bildirimleri analiz edip, 
yapay zeka yanıtlarını iyileştirmek için somut öneriler sunuyorsun.

Çıktı formatı:
- categories: Sorun kategorileri
- suggestions: İyileştirme önerileri
- priority: High/Medium/Low
```

### Dashboard Özellikleri

| Özellik | Açıklama |
|---------|----------|
| **İstatistik Kartları** | Toplam feedback, memnuniyet oranı, trend |
| **Trend Grafikleri** | Chart.js ile günlük değişim |
| **Kategori Dağılımı** | Doughnut chart |
| **İyileştirme Yönetimi** | Approve/Reject UI |

---

## ✅ 7. Tool Use Pattern (Function Calling)

### Tanım

Tool Use pattern, LLM'in dış araçları (veritabanları, API'ler, dosya sistemleri) kullanarak görevleri tamamlamasını sağlar.

### Kullanım Alanları

#### 7.1 Excel/CSV Analizi (DuckDB)

**Dosya:** `AI.Infrastructure/Adapters/AI/ExcelServices/DuckDbExcelService.cs`

```
User Query + Excel File
     │
     ▼
LLM → SQL Generation
     │
     ▼
DuckDB Execute (in-memory)
     │
     ▼
LLM → Result Interpretation
     │
     ▼
Streaming Response
```

#### 7.2 Database Raporları

**Dosyalar:** `AI.Infrastructure/Adapters/AI/Reports/SqlServer/`

```
User Query: "Son 7 günün çağrı istatistikleri"
     │
     ▼
LLM → SQL Generation (SQL Server)
     │
     ▼
SqlAgentPipeline (Validate + Optimize)
     │
     ▼
Database Execute
     │
     ▼
LLM → HTML Dashboard Generation
     │
     ▼
Streaming Response
```

### Desteklenen Araçlar

| Araç | Kullanım | Dosya |
|------|----------|-------|
| **DuckDB** | Excel/CSV analizi | DuckDbExcelService.cs |
| **SQL Server** | AdventureWorks raporları | SqlServerReportServiceBase.cs |
| **SQL Server** | AdventureWorks raporları | AdventureWorksReportService.cs |
| **Qdrant** | Vector search | QdrantService.cs |
| **PostgreSQL** | Chat history, memory | Repository'ler |

## ✅ 8. ReAct Pattern (Reasoning + Acting)

### Tanım

ReAct pattern, LLM'in her işlem öncesi düşünme (Thought) ve işlem sonrası gözlem (Observation) adımları üretmesini sağlayan bir reasoning pattern'idir. Kullanıcıya AI'ın karar sürecini şeffaf olarak gösterir.

### Konum

| Dosya | Açıklama |
|-------|----------|
| `AI.Application/Ports/Primary/UseCases/IReActUseCase.cs` | Primary Port interface |
| `AI.Application/UseCases/ReActUseCase.cs` | Merkezi implementasyon |
| `AI.Application/Common/Resources/Prompts/react-thought.md` | Thought generation prompt |
| `AI.Application/DTOs/ReAct/ReActStep.cs` | Frontend'e gönderilen model |
| `AI.Application/Configuration/ReActSettings.cs` | Feature toggle ayarları |

### Akış Diyagramı

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Kullanıcı      │────▶│  THOUGHT        │────▶│  ACTION         │
│  Mesajı         │     │  (LLM Düşünme)  │     │  (İşlem Yapma)  │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │  OBSERVATION    │
                                                │  (Sonuç Raporu) │
                                                └────────┬────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │  SignalR ile    │
                                                │  Frontend'e     │
                                                │  Gönder         │
                                                └─────────────────┘
```

### Entegrasyon Noktaları

| Servis | THOUGHT | OBSERVATION | Flow Context |
|--------|---------|-------------|--------------|
| `RouteConversationUseCase` | Routing öncesi | Yönlendirme sonucu | "Routing" |
| `AIChatUseCase` | Chat/Search öncesi | İşlem sonucu | "Chat", "Document Search" |
| `ExcelAnalysisUseCase` | Excel analiz öncesi | Analiz sonucu | "Excel Analysis" |
| `SqlServerReportServiceBase` | Rapor oluşturma öncesi | Dashboard/insight tamamlanma | "Report Generation" |

### Dinamik Prompt (react-thought.md)

```markdown
# ReAct Thought Prompt
{{FLOW_CONTEXT}} placeholder ile akış bazlı context injection:

Örnekler:
- Routing: "Kullanıcının isteğini analiz edip uygun servise yönlendireceğim"
- Report: "SQL sorgusu oluşturup dashboard render edeceğim"
- Document: "Vektör veritabanında arama yapacağım"
```

### Konfigürasyon (appsettings.json)

```json
"ReAct": {
  "Enabled": true,
  "VerboseLogging": false,
  "SendStepsToFrontend": true
}
```

### SignalR Event

```csharp
// Frontend'e gönderilen ReActStep modeli
public record ReActStep(
    int StepNumber,
    string StepType,     // "thought" | "observation"
    string Content,
    string Action,
    DateTime Timestamp
);

// SignalR event: "ReceiveReActStep"
await hubContext.Clients.Group(connectionId)
    .SendAsync("ReceiveReActStep", reActStep);
```

---

## 📊 Pattern Özet Tablosu

| # | Pattern | Kategori | Durum | Ana Dosya | Satır |
|---|---------|----------|-------|-----------|-------|
| 1 | **Router** | Orchestration | ✅ Aktif | RouteConversationUseCase.cs | 456 |
| 2 | **Pipeline** | Multi-Agent | ✅ Aktif | SqlAgentPipeline.cs | 559 |
| 3 | **RAG + HyDE** | Retrieval | ✅ Aktif | RagSearchUseCase.cs | 777 |
| 4 | **Long-Term Memory** | Memory | ✅ Aktif | UserMemoryUseCase.cs | 559 |
| 5 | **Context Summarization** | Compression | ✅ Aktif | ContextSummarizationUseCase.cs | 311 |
| 6 | **Feedback Loop** | Self-Improvement | ✅ Aktif | FeedbackAnalysisJob.cs | 294 |
| 7 | **Tool Use** | Execution | ✅ Aktif | DuckDbExcelService, ReportServices | - |
| 8 | **ReAct** | Reasoning | ✅ Aktif | ReActUseCase.cs | - |

---

## İlgili Dokümanlar

| Doküman | Açıklama |
|---------|----------|
| [Multi-Agent.md](Multi-Agent.md) | Multi-Agent fırsatları analizi |
| [Qdrant-Vector-Search.md](Qdrant-Vector-Search.md) | RAG ve vektör arama detayları |
| [Long-Term-Memory.md](Long-Term-Memory.md) | Memory sistemi implementasyonu |
| [Message-Feedback.md](Message-Feedback.md) | Feedback sistemi detayları |
| [System-Overview.md](System-Overview.md) | Genel sistem mimarisi |

---

<div align="center">

</div>
