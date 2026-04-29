# 🤖 Enterprise AI Assistant

<div align="center">

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Angular 21](https://img.shields.io/badge/Angular-21-DD0031?logo=angular)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--5.2-412991?logo=openai)
![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-AI%20Orchestration-512BD4?logo=microsoft)
![SignalR](https://img.shields.io/badge/SignalR-Real--Time-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791?logo=postgresql)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver)
![Neo4j](https://img.shields.io/badge/Neo4j-5.x-008CC1?logo=neo4j)
![Qdrant](https://img.shields.io/badge/Qdrant-Vector%20DB-DC244C?logo=qdrant)
![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?logo=redis)
![DuckDB](https://img.shields.io/badge/DuckDB-Excel%20Analysis-FFF000?logo=duckdb&logoColor=black)
![Hangfire](https://img.shields.io/badge/Hangfire-Job%20Scheduler-5B4638?logo=clockify)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)

**Kurumsal İş Zekası ve Veri Analizi için Yapay Zeka Destekli Akıllı Asistan Platformu**

[Özellikler](#-özellikler) • [Ekranlar](#-ekranlar-ve-sayfalar) • [Mimari](#-sistem-mimarisi) • [Kurulum](#-kurulum)

</div>

---

## 🎬 Demo

https://github.com/user-attachments/assets/6f3e76d8-fbdd-4b9c-9cc3-ce54629114f6

> **Not:** Video GitHub'da görüntülenemiyorsa, dosyayı `docs/media/1767601282558.mp4` konumunda bulabilirsiniz.

---

## 📋 İçindekiler

- [Genel Bakış](#-genel-bakış)
- [Özellikler](#-özellikler)
- [Ekranlar ve Sayfalar](#-ekranlar-ve-sayfalar)
- [Sistem Mimarisi](#-sistem-mimarisi)
- [Teknoloji Stack](#-teknoloji-stack)
- [Proje Yapısı](#-proje-yapısı)
- [Kurulum](#-kurulum)
- [Yapılandırma](#-yapılandırma)
- [API Referansı](#-api-referansı)
- [Veritabanı Şeması](#-veritabanı-şeması)

---

## 🎯 Genel Bakış

**Enterprise AI Assistant**, iş analistlerinin, yöneticilerinin ve operasyonel ekiplerin iş süreçlerini hızlandırmak için tasarlanmış, yapay zeka destekli çok modlu bir platformdur.

### Ne Yapar?

| Kullanıcı İhtiyacı | Sistem Çözümü |
|-------------------|---------------|
| "Son 7 günün satış istatistiklerini göster" | AI soruyu anlar → SQL üretir → Veritabanından çeker → Tablo/grafik olarak sunar |
| "İade politikamız ne?" | Kurumsal dökümanlardan arar → İlgili paragrafları bulur → Özetleyerek sunar |
| "Bu Excel'deki toplam satışı hesapla" | Dosyayı DuckDB'ye yükler → SQL üretir → Analiz sonucunu gösterir |
| "Müşteri memnuniyeti analizi raporu" | Veritabanından çeker → Grafiklerle zenginleştirir → İnteraktif rapor sunar |

### Hedef Kullanıcılar

| Rol | Kullanım Senaryosu |
|-----|-------------------|
| **İş Analisti** | Veri analizi, ad-hoc raporlar, SQL bilmeden sorgu oluşturma |
| **Takım Lideri** | Günlük/haftalık performans raporları |
| **Yönetici** | Trend analizi, dashboard görüntüleme |
| **Operasyonel Ekip** | Hızlı veri sorgulama, döküman arama |

---

## ✨ Özellikler

### 1. 🧠 Akıllı İstek Yönlendirme (RouteConversationUseCase)

Kullanıcının doğal dildeki isteğini analiz ederek **Agent Registry** üzerinden otomatik olarak doğru agent'a yönlendirir:

```
Kullanıcı İsteği → AI Analiz → Mod Belirleme → Agent Registry Dispatch
```

| Mod | Tetikleyici Örnek | Agent |
|-----|------------------|----------|
| **Chat** | "Merhaba, nasılsın?" | `ChatActionAgent` |
| **Document** | "İade politikası nedir?" | `DocumentActionAgent` |
| **Report** | "Son 7 günün satış sayısı" | `ReportActionAgent` |
| **Ask** | Belirsiz sorular | `AskActionAgent` |

**Kaynak Kod:** `AI.Application/UseCases/RouteConversationUseCase.cs`

---

### 2. 📊 Veritabanı Raporları

Doğal dilde soru sorarak SQL bilmeden rapor oluşturma:

#### Desteklenen Veritabanları ve Raporlar

| Veritabanı | Rapor Tipi | Servis Dosyası |
|------------|------------|----------------|
| **SQL Server** | AdventureWorks (Demo) | `AdventureWorksReportService.cs` |

#### Rapor Akışı

```
1. Kullanıcı: "Bugünkü satış istatistiklerini göster"
2. RouteConversationUseCase → action: "report", reportName: "adventureworks"
3. AdventureWorksReportService → LLM'e veritabanı şemasını + kullanıcı sorusunu gönderir
4. LLM → SQL üretir
5. SQL Server'dan veri çekilir
6. HTML tablo + grafik olarak frontend'e streaming ile gönderilir
```

---

### 3. 📚 Döküman Arama (RAG - Retrieval Augmented Generation)

Kurumsal dökümanları yükleyip doğal dilde arama:

#### Desteklenen Formatlar

| Format | Parser |
|--------|--------|
| PDF | `PdfDocumentParser.cs` |
| DOCX/DOC | Word parser |
| TXT | `TextDocumentParser.cs` |

#### Arama Özellikleri

| Özellik | Açıklama | Yapılandırma |
|---------|----------|--------------|
| **HyDE (Hypothetical Document Embeddings)** | Daha iyi arama sonuçları | `Qdrant.HyDEMaxTokens` |
| **Hybrid Search** | Dense + Sparse vektörler | `Qdrant.DenseWeight`, `Qdrant.SparseWeight` |
| **Türkçe Stemming** | "çalışıyorum" → "çalış" | Lucene.NET TurkishStemmer |
| **Yazım Düzeltme** | Otomatik düzeltme | - |

**Kaynak Kod:** `AI.Application/UseCases/RagSearchUseCase.cs`

---

### 4. 📁 Dosya Analizi (DuckDB)

Excel ve CSV dosyalarını yükleyip AI ile analiz:

| Özellik | Değer |
|---------|-------|
| **Desteklenen Formatlar** | .xlsx, .xls, .csv |
| **Maksimum Dosya Boyutu** | 50 MB |
| **İşlem Motoru** | DuckDB (in-memory SQL) |
| **Satır Limiti** | 100.000+ satır desteklenir |

#### Akış

```
1. Kullanıcı Excel yükler
2. DuckDbExcelService → Şema çıkarır (sütunlar, tipler, satır sayısı)
3. Şema + soru → LLM → SQL üretir
4. DuckDB'de SQL çalıştırılır
5. Sonuç kullanıcıya gösterilir
```

**Kaynak Kod:** `AI.Infrastructure/Adapters/AI/ExcelServices/DuckDbExcelService.cs`

---

### 5. 🧠 Uzun Vadeli Hafıza (Long-Term Memory)

Kullanıcı tercihlerini öğrenir ve kişiselleştirilmiş yanıtlar üretir:

| Kategori | Örnek | Kullanım |
|----------|-------|----------|
| **Preference** | "Excel formatını tercih ediyor" | Rapor formatı |
| **WorkContext** | "Finans departmanı" | Varsayılan filtreler |
| **Interaction** | "Grafikli raporlar istiyor" | Görselleştirme |

**Kaynak Kod:** `AI.Application/UseCases/UserMemoryUseCase.cs`

---

### 6. 🤖 Multi-Agent SQL Pipeline

SQL sorgularını doğrulayan, optimize eden ve hataları düzelten agent zinciri:

```
Kullanıcı Sorusu → SQL Generator → Validation Agent → Optimization Agent → Fixer Agent → Sonuç
```

| Agent | Görevi | Prompt Dosyası |
|-------|--------|----------------|
| **SqlValidationAgent** | Syntax ve güvenlik kontrolü | `sql_validation_agent_prompt.md` |
| **SqlOptimizationAgent** | Query optimizasyonu | `sql_optimization_agent_prompt.md` |
| **SqlAgentPipeline** | Hata düzeltme (max 2 retry) | - |

#### Yapılandırma (appsettings.json)

```json
"MultiAgent": {
  "Enabled": true,
  "SqlAgents": {
    "Enabled": true,
    "EnableValidation": true,      // SQL syntax kontrolü
    "EnableOptimization": true,    // Query optimizasyonu
    "EnableSecurityCheck": true,   // SQL injection koruması
    "EnableAutoCorrection": true,  // Hatalı SQL düzeltme
    "MaxRetries": 2                // Düzeltme deneme sayısı
  }
}
```

| Ayar | Varsayılan | Açıklama |
|------|------------|----------|
| `Enabled` | false | Multi-agent sistemi aktif/pasif |
| `EnableValidation` | false | Validation agent'ı çalıştır |
| `EnableOptimization` | false | Optimization agent'ı çalıştır |
| `EnableSecurityCheck` | false | Güvenlik kontrolü yap |
| `EnableAutoCorrection` | false | Hata düzeltme agent'ı çalıştır |
| `MaxRetries` | 2 | Düzeltme başarısız olursa kaç kez dene |

**Kaynak Kod:** `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/`

---

### 7. 💬 Context Summarization

Uzun konuşmalarda token tasarrufu için otomatik özetleme:

| Ayar | Değer | Açıklama |
|------|-------|----------|
| `MaxTokenThreshold` | 8000 | Bu eşik aşılırsa özet oluşturulur |
| `SlidingWindowSize` | 10 | Son N mesaj özete dahil edilmez |
| `SummaryMaxTokens` | 500 | Özet maksimum token |

**Kaynak Kod:** `AI.Application/UseCases/ContextSummarizationUseCase.cs`

---

### 8. 👍👎 Mesaj Geri Bildirimi ve Analytics

AI yanıtlarını değerlendirme ve analiz sistemi:

| Özellik | Açıklama |
|---------|----------|
| **Feedback Butonları** | 👍👎 ile hızlı değerlendirme |
| **Detaylı Yorum** | Olumsuz feedbacklerde modal ile açıklama |
| **Günlük AI Analizi** | Hangfire job ile 02:00'da otomatik analiz |
| **Dashboard** | İstatistikler, trendler, kategori dağılımı |
| **İyileştirme Önerileri** | AI'ın ürettiği prompt iyileştirmeleri |

**Kaynak Kod:** 
- Backend: `AI.Scheduler/Jobs/FeedbackAnalysisJob.cs`
- Frontend: `frontend/src/app/pages/dashboard/`

---

### 9. ⏰ Zamanlanmış Raporlar

Raporları otomatik çalıştırma ve e-posta/Teams ile gönderme:

| Zamanlama | Cron | Örnek |
|-----------|------|-------|
| Hafta içi 09:00 | `0 9 * * 1-5` | Günlük satış raporu |
| Her Pazartesi | `0 9 * * 1` | Haftalık özet |
| Her ayın 1'i | `0 9 1 * *` | Aylık performans |

**Kaynak Kod:** `AI.Scheduler/Jobs/ScheduledReportJob.cs`

---

### 10. 🧠 ReAct Pattern (Reasoning + Acting)

Sistem, kullanıcı isteklerini işlerken ReAct (Reasoning and Acting) pattern'ini kullanarak düşünce sürecini şeffaf hale getirir:

| Adım | Açıklama | SignalR Event |
|------|----------|---------------|
| **THOUGHT** | İstek analizi ve plan oluşturma | `ReceiveReActStep` |
| **ACTION** | Seçilen aksiyonun uygulanması | `ReceiveReActStep` |
| **OBSERVATION** | Sonuç gözlemi ve değerlendirme | `ReceiveReActStep` |

**Kaynak Kod:** `AI.Application/UseCases/ReActUseCase.cs`

---

## 📱 Ekranlar ve Sayfalar

### Frontend Sayfa Yapısı

```
frontend/src/app/pages/
├── login/              → Giriş sayfası
├── chat/               → Ana sohbet arayüzü (tüm modlar)
├── dashboard/          → Feedback Analytics Dashboard
└── reports/
    └── adventureworks/  → AdventureWorks Raporları (standalone)
        ├── employee-department-distribution/
        ├── low-stock-alert/
        ├── monthly-sales-trend/
        ├── product-category-profitability/
        ├── top-customers/
        └── top-products/
```

### 1. Chat Sayfası (`/chat`)

Ana çalışma ekranı - tüm AI etkileşimleri burada gerçekleşir:

| Bileşen | Açıklama |
|---------|----------|
| **Sol Panel (Sidebar)** | Konuşma geçmişi listesi |
| **Merkez Alan** | Mesajlaşma alanı |
| **Alt Input** | Mesaj yazma, dosya ekleme |
| **Sağ Panel** | Açılan raporlar (iframe) |

**Önemli Özellikler:**
- Hoşgeldin menüsü (Chat / Döküman / Rapor seçimi)
- Streaming mesaj gösterimi (kelime kelime)
- Markdown render (tablo, liste, kod)
- Dosya yükleme (Excel, PDF, Word)
- Rapor iframe yönetimi (minimize, maximize, çoklu)
- Filter sistemi (#hashtag autocomplete, tarih seçici)
- Feedback butonları (👍👎)

**Kaynak:** `frontend/src/app/pages/chat/chat.ts` (2940 satır)

---

### 2. Dashboard Sayfası (`/dashboard`)

Feedback analytics ve prompt iyileştirme önerileri:

| Bölüm | İçerik |
|-------|--------|
| **Overview Kartları** | Toplam feedback, pozitif/negatif oran, trend |
| **Trend Grafikleri** | Günlük/haftalık feedback değişimi |
| **Kategori Dağılımı** | Hangi konularda feedback alındı |
| **İyileştirme Önerileri** | AI'ın ürettiği prompt önerileri |
| **Analiz Raporları** | Günlük AI analiz sonuçları |

**Kaynak:** `frontend/src/app/pages/dashboard/dashboard.ts`

---

### 3. AdventureWorks Report Sayfaları (`/reports/adventureworks`)

AdventureWorks veritabanı için standalone rapor sayfaları:

| Rapor | Açıklama |
|-------|----------|
| **Employee Department Distribution** | Departman bazlı çalışan dağılımı |
| **Low Stock Alert** | Düşük stok uyarı raporu |
| **Monthly Sales Trend** | Aylık satış trend analizi |
| **Product Category Profitability** | Ürün kategori karlılık analizi |
| **Top Customers** | En iyi müşteriler raporu |
| **Top Products** | En çok satan ürünler raporu |

**Kaynak:** `frontend/src/app/pages/reports/adventureworks/`

---

## 🏗 Sistem Mimarisi

### Üst Düzey Akış

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND (Angular 21)                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐                  │
│  │   Chat Page     │  │   Dashboard     │  │  Report Pages   │                  │
│  │   (chat.ts)     │  │ (dashboard.ts)  │                  │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘                  │
│           │                    │                    │                           │
│           └────────────────────┼────────────────────┘                           │
│                                │                                                │
│                    ┌───────────▼───────────┐                                    │
│                    │   SignalR Service     │                                    │
│                    │  (signalr.service.ts) │                                    │
│                    └───────────┬───────────┘                                    │
└────────────────────────────────┼────────────────────────────────────────────────┘
                                 │ WebSocket (WSS)
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              BACKEND (ASP.NET Core 10)                          │
│                                                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐   │
│  │                            API KATMANI (AI.Api)                          │   │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐         │   │
│  │  │   AIHub    │  │  Auth API  │  │ History    │  │ Dashboard  │         │   │
│  │  │ (SignalR)  │  │            │  │ API        │  │ API        │         │   │
│  │  └─────┬──────┘  └────────────┘  └────────────┘  └────────────┘         │   │
│  └────────┼─────────────────────────────────────────────────────────────────┘   │
│           │                                                                     │
│  ┌────────▼─────────────────────────────────────────────────────────────────┐   │
│  │                      İŞ MANTIĞI (AI.Application)                         │   │
│  │                                                                          │   │
│  │  ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐   │   │
│  │  │ Conversation    │      │   AIChatUseCase │      │  ReportServices │   │   │
│  │  │ Router          │─────►│                 │      │  (SQL Server)   │   │   │
│  │  │                 │      │  Chat + Dosya   │      │                 │   │   │
│  │  └────────┬────────┘      └────────┬────────┘      └────────┬────────┘   │   │
│  │           │                        │                        │            │   │
│  │           ▼                        ▼                        ▼            │   │
│  │  ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐   │   │
│  │  │ RagSearchUseCase│      │   DuckDB Excel  │      │ UserMemory      │   │   │
│  │  │ (Qdrant Vector) │      │   Service       │      │ Service         │   │   │
│  │  └─────────────────┘      └─────────────────┘      └─────────────────┘   │   │
│  │                                                                          │   │
│  │  ┌───────────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Multi-Agent SQL Pipeline                       │   │   │
│  │  │  SqlValidationAgent → SqlOptimizationAgent → Retry (Pipeline)    │   │   │
│  │  └───────────────────────────────────────────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
└───────────────────────────────────────┬─────────────────────────────────────────┘
                                        │
         ┌──────────────────────────────┼──────────────────────────────┐
         │                              │                              │
         ▼                              ▼                              ▼
┌─────────────────────┐   ┌─────────────────────┐   ┌─────────────────────┐
│     VERİTABANLARI   │   │    VEKTÖR & CACHE   │   │    DIŞ SERVİSLER    │
│                     │   │                     │   │                     │
│  • PostgreSQL       │   │  • Qdrant           │   │  • OpenAI API       │
│    (Chat geçmişi,   │   │    (Döküman         │   │  • Azure OpenAI     │
│     hafıza, users)  │   │     vektörleri)     │   │  • Hangfire         │
│                     │   │                     │   │    (Job scheduler)  │
│  • SQL Server       │   │  • Redis            │   │                     │
│    (AdventureWorks) │   │    (L2 Cache)       │   │                     │
│  • Neo4j            │   │                     │   │                     │
│    (Schema Catalog) │   │                     │   │                     │
│                     │   │                     │   │                     │
└─────────────────────┘   └─────────────────────┘   └─────────────────────┘
```

---

## 💻 Teknoloji Stack

### Backend (.NET 10)

| Teknoloji | Kullanım |
|-----------|----------|
| **ASP.NET Core 10** | Web API, Minimal APIs |
| **SignalR** | Real-time, streaming mesajlar |
| **Semantic Kernel** | AI orchestration, prompt yönetimi |
| **Entity Framework Core** | PostgreSQL ORM |
| **Hangfire** | Zamanlanmış görevler |
| **DuckDB.NET** | Excel/CSV analizi |
| **Neo4j.Driver** | Graph-based Schema Catalog |
| **Polly** | Retry, circuit breaker |
| **Serilog** | Structured logging |
| **OpenTelemetry** | Distributed tracing |

### Frontend (Angular 21)

| Teknoloji | Kullanım |
|-----------|----------|
| **Angular 21** | SPA framework |
| **Angular Signals** | Reaktif state yönetimi |
| **PrimeNG v21** | UI bileşenleri |
| **SignalR Client** | WebSocket iletişim |
| **Chart.js / ApexCharts** | Grafikler |
| **jsPDF + xlsx** | PDF/Excel export |

### Veritabanları

| Veritabanı | Kullanım | Connection String Key |
|------------|----------|----------------------|
| **PostgreSQL** | Chat geçmişi, kullanıcılar, hafıza | `PostgreSQL` |
| **SQL Server** | Demo verileri (AdventureWorks) | `AdventureWorks2022` |
| **Qdrant** | Vektör araması | `Qdrant.Host`, `Qdrant.Port` |
| **Neo4j** | Graph DB — Schema Catalog (tablo ilişkileri, join path) | `Neo4j.Uri` |
| **Redis** | Distributed cache | `Redis` |

### AI Servisleri

| Servis | Model | Kullanım |
|--------|-------|----------|
| **OpenAI / Azure OpenAI** | GPT-5.2 | Chat, SQL üretimi, analiz |
| **OpenAI / Azure OpenAI** | text-embedding-3-large | Döküman embedding (3072 boyut) |

---

## 📁 Proje Yapısı (Hexagonal Architecture)

```
AIApplications/
│
├── 📁 AI.Api/                          # Composition Root (Sunum Katmanı)
│   ├── Program.cs                      # Uygulama giriş noktası, DI
│   ├── appsettings.json                # Ana yapılandırma
│   ├── Endpoints/                      # Minimal API'ler (Feature-based)
│   │   ├── Auth/                       # /api/auth/*
│   │   ├── Documents/                  # /api/documents/*
│   │   ├── History/                    # /api/history/*
│   │   ├── Dashboard/                  # /api/dashboard/*
│   │   └── ...
│   ├── Hubs/
│   │   └── AIHub.cs                    # SignalR Hub
│   ├── Common/
│   │   └── SignalRHubContextWrapper.cs
│   └── Extensions/                     # DI, middleware
│
├── 📁 AI.Application/                  # Ports Katmanı (Hexagonal)
│   ├── Ports/
│   │   ├── Primary/                    # Driving Side (Inbound)
│   │   │   └── UseCases/               # 19 Primary Port Interface
│   │   │       ├── IAuthUseCase.cs
│   │   │       ├── IRouteConversationUseCase.cs
│   │   │       ├── IConversationUseCase.cs
│   │   │       ├── IRagSearchUseCase.cs
│   │   │       ├── IReActUseCase.cs
│   │   │       └── ...
│   │   └── Secondary/                  # Driven Side (Outbound)
│   │       ├── Repositories/           # 11 Repository Interface
│   │       ├── Services/               # 34 Service Interface (8 kategori)
│   │       │   ├── AIChat/             # IAIChatUseCase, IUserMemoryService...
│   │       │   ├── Auth/               # ICurrentUserService, ITokenService
│   │       │   ├── Cache/              # ICacheService
│   │       │   ├── Common/             # ISignalRHubContext...
│   │       │   ├── Database/           # IDatabaseService, IReportService...
│   │       │   ├── Document/           # IEmbeddingService, IVectorSearchService...
│   │       │   ├── Report/             # IExcelAnalysisService...
│   │       │   └── Vector/             # IQdrantService, ISparseVectorService...
│   │       ├── Notifications/          # IEmailService, ITeamsNotificationService
│   │       └── Scheduling/             # ISchedulingService
│   │
│   ├── UseCases/                       # Use Case Implementations
│   │   ├── AuthUseCase.cs
│   │   ├── RouteConversationUseCase.cs # İstek yönlendirici (Agent Registry)
│   │   ├── ConversationUseCase.cs
│   │   ├── RagSearchUseCase.cs
│   │   ├── ActionAgents/              # Agent Registry Implementations
│   │   │   ├── ActionAgentRegistry.cs # Agent discovery & dispatch
│   │   │   ├── ChatActionAgent.cs
│   │   │   ├── DocumentActionAgent.cs
│   │   │   ├── ReportActionAgent.cs
│   │   │   └── AskActionAgent.cs
│   │   └── ...
│   │
│   ├── Services/                       # Business Logic Services
│   │   └── AIServices/AdvancedRag/
│   ├── Common/Resources/Prompts/       # AI system prompt'ları
│   └── DTOs/
│
├── 📁 AI.Domain/                       # Core Layer (Aggregate-per-Folder)
│   ├── Common/                         # DDD Building Blocks (Entity, AggregateRoot, ValueObject)
│   ├── Conversations/                  # Conversation, Message
│   ├── Identity/                       # User, Role, RefreshToken
│   ├── Feedback/                       # MessageFeedback, FeedbackAnalysisReport
│   ├── Memory/                         # UserMemory
│   ├── Documents/                      # DocumentCategory, DocumentChunk
│   ├── Scheduling/                     # ScheduledReport, ScheduledReportLog
│   ├── Enums/                          # AuthenticationSource, FeedbackType...
│   ├── Events/                         # Domain Events (10 record types)
│   ├── Exceptions/                     # DomainException, UserLockedException...
│   └── ValueObjects/                   # Email, Password, Confidence, DateRange
│
├── 📁 AI.Infrastructure/               # Adapters Katmanı (Hexagonal)
│   ├── Adapters/
│   │   ├── AI/                         # 22 AI Service Adapter
│   │   │   ├── Agents/SqlAgents/       # Multi-Agent Pipeline
│   │   │   ├── DocumentServices/       # DocumentCategoryService...
│   │   │   ├── ExcelServices/          # DuckDbExcelService
│   │   │   ├── ReadyReports/           # AdventureWorks raporları
│   │   │   ├── Reports/SqlServer/      # SQL Server raporları
│   │   │   └── VectorServices/         # QdrantService, EmbeddingService
│   │   │
│   │   ├── Common/                     # Common Adapters
│   │   │   └── DashboardFileService.cs
│   │   │
│   │   ├── External/                   # 9 External Service Adapter
│   │   │   ├── Caching/                # RedisCacheService
│   │   │   ├── DatabaseServices/       # SqlServerDatabaseService
│   │   │   ├── AuthService.cs
│   │   │   ├── CurrentUserService.cs
│   │   │   └── TokenService.cs
│   │   │
│   │   └── Persistence/                # 44 Persistence Adapter
│   │       ├── Configurations/         # EF Core configurations
│   │       ├── Migrations/             # EF Core migrations
│   │       ├── Repositories/           # Repository implementations
│   │       └── ChatDbContext.cs        # EF Core DbContext
│   │
│   └── Extensions/                     # DI Extensions
│
├── 📁 AI.Scheduler/                    # Background Jobs
│   ├── Jobs/
│   │   ├── ScheduledReportJob.cs
│   │   └── FeedbackAnalysisJob.cs
│   └── Services/
│
├── 📁 frontend/                        # Angular SPA
│   └── src/app/
│       ├── core/services/              # Angular services
│       ├── pages/
│       │   ├── chat/                   # Ana chat
│       │   ├── dashboard/              # Analytics
│       │   └── login/
│       └── shared/
│
└── 📁 docs/                            # Dokümantasyon
    ├── README.md
    ├── Hexagonal-Architecture.md             # Hexagonal Architecture analizi
    └── ...
```

---

## 🚀 Kurulum

### Ön Gereksinimler

| Gereksinim | Minimum Versiyon |
|------------|------------------|
| .NET SDK | 10.0 |
| Node.js | 20.x |
| PostgreSQL | 15.x |
| Redis | 7.x |
| Qdrant | 1.x |

### 1. Kaynak Kodu Klonlama

```bash
git clone https://github.com/your-org/AIApplications.git
cd AIApplications
```

### 2. Altyapı Servisleri (Docker)

```bash
docker-compose up -d postgres redis qdrant
```

### 3. Backend Başlatma

```bash
cd AI.Api
dotnet ef database update  # Migrations
dotnet run
```

### 4. Frontend Başlatma

```bash
cd frontend
npm install
npm start
```

### 5. Tarayıcıda Açın

```
https://localhost:4200
```

---

## ⚙ Yapılandırma

### appsettings.json Ana Bölümleri

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=ChatDb;...",
    "AdventureWorks2022": "Server=localhost;Database=AdventureWorks2022;...",
    "Redis": "localhost:6379"
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
  
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "EmbeddingModel": "text-embedding-3-large",
    "VectorSize": 3072,
    "DenseWeight": 0.7,
    "SparseWeight": 0.3
  },
  
  "ContextSummarization": {
    "Enabled": true,
    "MaxTokenThreshold": 8000,
    "SlidingWindowSize": 10,
    "SummaryMaxTokens": 500,
    "SummaryCacheTtlMinutes": 30
  },
  
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
  },
  
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
  },
  
  "Dashboard": {
    "UseFastDashboard": false,
    "ConfigPromptFileName": "dashboard_config_generator_prompt.md",
    "FullPromptFileName": "dashboard_generator_prompt_2.md",
    "OutputFolder": "output-folder"
  },
  
  "ReAct": {
    "Enabled": true,
    "DelayMs": 800
  }
}
```

---

## 📡 API Referansı

### SignalR Hub (`/ai-hub`)

| Event (Server → Client) | Açıklama |
|------------------------|----------|
| `ReceiveMessage` | Tam yanıt mesajı |
| `ReceiveStreamingMessage` | Streaming chunk (kelime kelime) |
| `ReceiveLoadingMessage` | Yükleniyor durumu |
| `ReceiveError` | Hata mesajı |
| `ReceiveReActStep` | ReAct düşünce/gözlem adımları |

### REST Endpoints

| Endpoint | Metot | Açıklama |
|----------|-------|----------|
| `/api/auth/login` | POST | Kullanıcı girişi |
| `/api/auth/refresh` | POST | Token yenileme |
| `/api/history/conversations` | GET | Konuşma listesi |
| `/api/history/conversations/{id}/messages` | GET | Mesaj geçmişi |
| `/api/feedback/{messageId}` | POST | Feedback gönder |
| `/api/dashboard/overview` | GET | Dashboard istatistikleri |
| `/api/dashboard/improvements` | GET | İyileştirme önerileri |

---

## 🗄 Veritabanı Şeması

### PostgreSQL (Ana Veritabanı)

```sql
-- Konuşmalar
CREATE TABLE history.conversations (
    id UUID PRIMARY KEY,
    user_id VARCHAR(36),
    title VARCHAR(500),
    created_at TIMESTAMP,
    deleted_at TIMESTAMP NULL
);

-- Mesajlar
CREATE TABLE history.messages (
    id UUID PRIMARY KEY,
    conversation_id UUID REFERENCES conversations(id),
    role VARCHAR(20),  -- 'user', 'assistant', 'system'
    content TEXT,
    message_type INT,
    token_count INT,
    created_at TIMESTAMP
);

-- Mesaj Geri Bildirimleri
CREATE TABLE history.message_feedbacks (
    id UUID PRIMARY KEY,
    message_id UUID,
    user_id VARCHAR(100),
    type VARCHAR(20),  -- 'positive', 'negative'
    comment TEXT NULL,
    created_at TIMESTAMP
);

-- Kullanıcı Hafızası
CREATE TABLE memory.user_memories (
    id UUID PRIMARY KEY,
    user_id VARCHAR(255),
    key VARCHAR(255),      -- "tercih_edilen_format"
    value TEXT,            -- "Excel"
    category VARCHAR(50),  -- Preference, WorkContext
    confidence FLOAT,
    usage_count INT
);
```

---

## 📚 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [System-Overview.md](docs/System-Overview.md) | Genel sistem analizi ve mimari bakış |
| [Hexagonal-Architecture.md](docs/Hexagonal-Architecture.md) | Hexagonal mimari (Ports & Adapters) analizi |
| [Application-Layer.md](docs/Application-Layer.md) | Kapsamlı uygulama katmanı analizi |
| [Infrastructure-Cross-Cutting.md](docs/Infrastructure-Cross-Cutting.md) | Altyapı ve cross-cutting concerns |
| [Project-Structure-Tree.md](docs/Project-Structure-Tree.md) | Detaylı proje yapısı ağacı |
| [Conversation-Router.md](docs/Conversation-Router.md) | İstek yönlendirme ve Agent Registry sistemi |
| [Agentic-AI-Patterns.md](docs/Agentic-AI-Patterns.md) | Agentic AI pattern'leri analizi |
| [Multi-Agent.md](docs/Multi-Agent.md) | Multi-Agent entegrasyon ve SQL Agent pipeline |
| [Chat-System.md](docs/Chat-System.md) | Chat sistemi özellikleri |
| [Message-Feedback.md](docs/Message-Feedback.md) | Mesaj geri bildirim ve değerlendirme sistemi |
| [Qdrant-Vector-Search.md](docs/Qdrant-Vector-Search.md) | Vektör arama ve embedding sistemi |
| [Advanced-RAG.md](docs/Advanced-RAG.md) | Advanced RAG (Reranking, Self-Query, HyDE) analizi |
| [Long-Term-Memory.md](docs/Long-Term-Memory.md) | Kullanıcı hafıza sistemi (L0/L1/L2) |
| [DuckDB-Excel.md](docs/DuckDB-Excel.md) | DuckDB Excel/CSV analiz sistemi detaylı analiz |
| [DuckDB-Usage.md](docs/DuckDB-Usage.md) | DuckDB kullanım kılavuzu ve akış dökümanı |
| [Report-System.md](docs/Report-System.md) | Rapor sistemi detaylı analizi |
| [Scheduled-Reports.md](docs/Scheduled-Reports.md) | Zamanlanmış rapor sistemi |
| [Neo4j-Schema-Catalog.md](docs/Neo4j-Schema-Catalog.md) | Neo4j şema kataloğu |
| [Authentication-Authorization.md](docs/Authentication-Authorization.md) | Kimlik doğrulama ve yetkilendirme sistemi |
| [User-Guide.md](docs/User-Guide.md) | Son kullanıcı kılavuzu |

---

<div align="center">

**Enterprise AI Assistant** - İş zekası ve veri analizi için akıllı asistanınız

**Enterprise AI Assistant**

</div>
