# Enterprise AI Assistant - Kapsamlı Uygulama Analizi

---

## 📋 İçindekiler

1. [Genel Bakış](#1-genel-bakış)
2. [Mimari Yapı](#2-mimari-yapı)
3. [Sistem Topolojisi](#3-sistem-topolojisi)
4. [Teknoloji Stack'i](#4-teknoloji-stacki)
5. [Proje Yapısı](#5-proje-yapısı)
6. [Ana Bileşenler](#6-ana-bileşenler)
7. [Veri Akışları](#7-veri-akışları)
8. [API Endpoint'leri](#8-api-endpointleri)
9. [Veritabanı Şeması](#9-veritabanı-şeması)
10. [AI/ML Yetenekleri](#10-aiml-yetenekleri)
11. [Güvenlik ve Kimlik Doğrulama](#11-güvenlik-ve-kimlik-doğrulama)
12. [İzleme ve Loglama](#12-izleme-ve-loglama)
13. [Konfigürasyon Yönetimi](#13-konfigürasyon-yönetimi)
14. [Frontend Mimarisi](#14-frontend-mimarisi)
15. [Dağıtım ve DevOps](#15-dağıtım-ve-devops)

---

## 1. Genel Bakış

### 1.1 Uygulama Amacı

**Enterprise AI Assistant**, kurumsal iş zekası ve veri analizi için tasarlanmış yapay zeka destekli bir chatbot platformudur. Sistem, doğal dil işleme (NLP) yetenekleri kullanarak:

- 📊 **Rapor Üretimi:** Veritabanlarından SQL sorguları ile dinamik raporlar oluşturma
- 📄 **Doküman Analizi:** PDF, Word, Excel dosyalarını semantik olarak arama ve analiz etme
- 💬 **Akıllı Sohbet:** Bağlamı anlayan, hafızalı konuşmalar yürütme
- 📈 **Dashboard Oluşturma:** Rapor verilerinden otomatik görselleştirmeler üretme
- 🔄 **Zamanlanmış Raporlama:** Cron tabanlı otomatik rapor çalıştırma ve bildirim

### 1.2 İş Değeri

| Özellik | İş Faydası |
|---------|-----------|
| Doğal Dil Sorguları | SQL bilmeden veritabanı raporları oluşturma |
| Semantik Arama | Büyük doküman arşivlerinde anlamlı arama |
| Kişiselleştirilmiş Yanıtlar | Kullanıcı tercihlerini öğrenen sistem |
| Otomatik Dashboard | Manuel grafik/tablo oluşturma süresini ortadan kaldırma |
| Zamanlanmış Raporlar | Tekrarlayan rapor taleplerini otomatikleştirme |

### 1.3 Hedef Kullanıcılar

1. **İş Analistleri:** Veri analizi, ad-hoc raporlar, SQL bilmeden sorgu oluşturma
2. **Yöneticiler:** Performans metrikleri ve trend analizleri
3. **Operasyonel Ekipler:** Hızlı veri sorgulama, döküman arama
4. **IT Ekipleri:** Sistem yönetimi ve entegrasyon

---

## 2. Mimari Yapı

### 2.1 Hexagonal Architecture (Ports & Adapters)

Uygulama, **Hexagonal Architecture** (Altıgen Mimari) prensiplerine uygun olarak tasarlanmıştır:

```
┌────────────────────────────────────────────────────────────────┐
│                  AI.Api (Composition Root)                     │
│    ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐   │
│    │  Endpoints  │  │    Hubs     │  │    Extensions       │   │
│    └─────────────┘  └─────────────┘  └─────────────────────┘   │
├────────────────────────────────────────────────────────────────┤
│                  AI.Infrastructure (Adapters)                  │
│  ┌─────────────┐ ┌────────────┐ ┌─────────────┐                │
│  │ Adapters/AI │ │Adapters/   │ │Adapters/    │                │
│  │             │ │Persistence │ │External     │                │
│  └─────────────┘ └────────────┘ └─────────────┘                │
├────────────────────────────────────────────────────────────────┤
│                    AI.Application (Ports)                      │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Ports/Primary/UseCases (Driving Side)                    │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Ports/Secondary (Driven Side)                             │ │
│  │   Repositories/ | Services/ | Notifications/ | Scheduling/│ │
│  └───────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ UseCases/ (Use Case Implementations)                     │  │
│  └──────────────────────────────────────────────────────────┘  │
├────────────────────────────────────────────────────────────────┤
│                        AI.Domain (Core)                        │
│    ┌──────────┐  ┌───────────┐  ┌──────────┐  ┌────────────┐   │
│    │ Identity │  │  Chatbot  │  │  Memory  │  │    RAG     │   │
│    └──────────┘  └───────────┘  └──────────┘  └────────────┘   │
├────────────────────────────────────────────────────────────────┤
│                      AI.Scheduler (Background Jobs)            │
│    ┌─────────────────┐  ┌────────────────────────────────────┐ │
│    │  Hangfire Jobs  │  │     Notification Services          │ │
│    └─────────────────┘  └────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────┘
```

### 2.2 Katman Sorumlulukları

| Katman | Sorumluluk | Ana Bileşenler |
|--------|-----------|----------------|
| **AI.Api** | Composition Root, DI, HTTP/SignalR endpoint'leri | Endpoints, Hubs, Extensions |
| **AI.Application** | Ports (Primary/Secondary), Use Cases, Business Logic | Ports, UseCases, DTOs |
| **AI.Domain** | Domain modelleri, Entity tanımları, Enum'lar | Entities, Enums |
| **AI.Infrastructure** | Adapters (AI, Persistence, External, Common) | Adapters, EF DbContext |
| **AI.Scheduler** | Arka plan görevleri, Zamanlanmış işler | Hangfire Jobs |

### 2.3 Dependency Flow

```
Frontend (Angular) 
    ↓ HTTP/SignalR
AI.Api (Composition Root)
    ↓ wires up
AI.Infrastructure (Adapters) ─implements→ AI.Application (Ports)
    ↓                                           ↓ uses
External Services (OpenAI, Qdrant, etc.)    AI.Domain (Core)
```

---

## 3. Sistem Topolojisi

### 3.1 Genel Dağıtım Topolojisi

Aşağıdaki diyagram, sistemin tüm bileşenlerinin birbirleriyle nasıl iletişim kurduğunu göstermektedir:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           İSTEMCİ KATMANI                                   │
│                                                                             │
│  ┌──────────────────┐                      ┌─────────────────────────────┐  │
│  │   Angular SPA    │                      │    Statik Dosyalar          │  │
│  │   (Tarayıcı)     │◄──── HTTPS :443 ────►│    (wwwroot, CDN)           │  │
│  └────────┬─────────┘                      └─────────────────────────────┘  │
│           │                                                                 │
│           │ WebSocket (SignalR) + REST API                                  │
└───────────┼─────────────────────────────────────────────────────────────────┘
            │
            │ HTTPS :7041
            ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           API KATMANI                                       │
│                                                                             │
│  ┌───────────────────────────────────┐    ┌───────────────────────────────┐ │
│  │           AI.Api                  │    │        AI.Scheduler           │ │
│  │    (Ana Web Sunucusu)             │    │    (Arka Plan Görevleri)      │ │
│  │                                   │    │                               │ │
│  │  • Minimal API Endpoints          │    │  • Hangfire Worker            │ │
│  │  • SignalR Hub (AIHub)            │    │  • Zamanlanmış Raporlar       │ │
│  │  • JWT Kimlik Doğrulama           │    │  • E-posta/Teams Bildirimleri │ │
│  │  • Rate Limiting                  │    │                               │ │
│  │  • Health Checks                  │    │                               │ │
│  │                                   │    │                               │ │
│  │  Port: 7041 (HTTPS)               │    │  Port: Internal               │ │
│  └───────────────┬───────────────────┘    └───────────────┬───────────────┘ │
│                  │                                        │                 │
└──────────────────┼────────────────────────────────────────┼─────────────────┘
                   │                                        │
                   └────────────────┬───────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           VERİ KATMANI                                      │
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │   PostgreSQL    │  │     Redis       │  │          Qdrant             │  │
│  │   (Ana DB)      │  │   (Önbellek)    │  │     (Vektör DB)             │  │
│  │                 │  │                 │  │                             │  │
│  │ • Kullanıcılar  │  │ • Oturum Cache  │  │ • Doküman Vektörleri        │  │
│  │ • Konuşmalar    │  │ • Sohbet Cache  │  │ • Kullanıcı Hafızası        │  │
│  │ • Doküman Meta  │  │ • Rate Limit    │  │ • Hybrid Search Index       │  │
│  │ • Zamanl. Rap.  │  │                 │  │                             │  │
│  │                 │  │                 │  │                             │  │
│  │ Port: 5432     │  │ Port: 6379      │  │ REST: 6333, gRPC: 6334       │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                      RAPOR VERİTABANLARI                                ││
│  │  ┌─────────────────────────┐    ┌─────────────────────────────────────┐ ││
│  │  │    SQL Server           │    │                                     │ ││
│  │  │                         │    │                                     │ ││
│  │  │  • Satış Raporları.     │    │  • Demo/Test Veritabanı             │ ││
│  │  │  • Müşteri Raporları.   │    │  • Örnek Ürün/Satış Verileri        │ ││
│  │  │                         │    │                                     │ ││
│  │  │                         │    │                                     │ ││
│  │  │  Port: 1521             │    │  Port: 1433                         │ ││
│  │  └─────────────────────────┘    └─────────────────────────────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        HARİCİ SERVİSLER                                     │
│                                                                             │
│  ┌─────────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐  │
│  │    Azure OpenAI     │  │ Active Directory│  │   Gözlemlenebilirlik    │  │
│  │                     │  │     (LDAP)      │  │                         │  │
│  │  • GPT-5.2 (Chat)   │  │                 │  │  • OpenTelemetry Col.   │  │
│  │  • text-embedding-  │  │  • Kullanıcı    │  │    Port: 4317 (gRPC)    │  │
│  │    3-large (Embed)  │  │    Doğrulama    │  │  • Jaeger (Tracing)     │  │
│  │                     │  │  • Grup/Rol     │  │    Port: 14269          │  │
│  │  Endpoint: Azure    │  │    Bilgisi      │  │  • Elasticsearch (Log)  │  │
│  │  veya OpenAI API    │  │                 │  │    Port: 9200           │  │
│  └─────────────────────┘  └─────────────────┘  └─────────────────────────┘  │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                      BİLDİRİM SERVİSLERİ                                ││
│  │  ┌─────────────────────────┐    ┌─────────────────────────────────────┐ ││
│  │  │      SMTP Sunucusu      │    │      Microsoft Teams Webhook        │ ││
│  │  │                         │    │                                     │ ││
│  │  │  • Zamanlanmış Rapor    │    │  • Zamanlanmış Rapor Bildirimleri   │ ││
│  │  │    E-postaları          │    │  • Adaptive Card Formatı            │ ││
│  │  └─────────────────────────┘    └─────────────────────────────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Ağ Topolojisi ve Port Haritası

| Bileşen | Port | Protokol | Yön | Açıklama |
|---------|------|----------|-----|----------|
| **AI.Api** | 7041 | HTTPS | Gelen | Ana API ve SignalR Hub |
| **PostgreSQL** | 5432 | TCP | Dahili | Ana veritabanı bağlantısı |
| **Redis** | 6379 | TCP | Dahili | Önbellek bağlantısı |
| **Qdrant REST** | 6333 | HTTP | Dahili | Vektör DB REST API |
| **Qdrant gRPC** | 6334 | gRPC | Dahili | Vektör DB yüksek performans |
| **SQL Server** | 1433 | TCP | Dahili | AdventureWorks demo DB |
| **OTEL Collector** | 4317 | gRPC | Giden | Telemetri gönderimi |
| **Jaeger** | 14269 | HTTP | Dahili | Trace görüntüleme |
| **Elasticsearch** | 9200 | HTTP | Dahili | Log depolama |
| **LDAP** | 389/636 | TCP | Giden | Active Directory sorguları |

### 3.3 Veri Akış Topolojisi

```
                              ┌─────────────────────────────────────┐
                              │         KULLANICI İSTEĞİ            │
                              │   "Son ayın çağrı raporunu göster"  │
                              └─────────────────┬───────────────────┘
                                                │
                                                ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                                1. GİRİŞ NOKTASI                               │
│  ┌─────────────┐    ┌──────────────┐    ┌─────────────┐    ┌──────────────┐   │
│  │   Angular   │───►│   SignalR    │───►│   JWT       │───►│ Rate Limit   │   │
│  │   İstemci   │    │   Bağlantı   │    │   Doğrula   │    │   Kontrol    │   │
│  └─────────────┘    └──────────────┘    └─────────────┘    └──────────────┘   │
└───────────────────────────────────────────────────────────────────────────────┘
                                                │
                                                ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                            2. YÖNLENDİRME KATMANI                             │
│                                                                               │
│                        ┌───────────────────────────────┐                      │
│                        │   RouteConversationUseCase    │                      │
│                        │   (LLM ile analiz)            │                      │
│                        └───────────┬───────────────────┘                      │
│                                    │                                          │
│                   ┌────────────────┼────────────────┐                         │
│                   ▼                ▼                ▼                         │
│            ┌──────────┐     ┌──────────┐     ┌──────────┐                     │
│            │   CHAT   │     │ DOCUMENT │     │  REPORT  │                     │
│            │   Modu   │     │   Modu   │     │   Modu   │                     │
│            └──────────┘     └──────────┘     └──────────┘                     │
└───────────────────────────────────────────────────────────────────────────────┘
                                                │
                                    (REPORT modu seçildi)
                                                │
                                                ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                           3. İŞLEM KATMANI                                    │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐  │
│  │                        ReportService Pipeline                           │  │
│  │                                                                         │  │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────────────────┐   │  │
│  │  │ System Prompt│───►│ LLM SQL      │───►│ SQL Agent Pipeline       │   │  │
│  │  │ + User Memory│    │ Üretimi      │    │ (Doğrulama+Optimizasyon) │   │  │
│  │  └──────────────┘    └──────────────┘    └──────────────────────────┘   │  │
│  │                                                     │                   │  │
│  │                                                     ▼                   │  │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────────────────┐   │  │
│  │  │ Dashboard    │◄───│ Insight      │◄───│ Veritabanı Sorgusu       │   │  │
│  │  │ Oluşturma    │    │ Analizi      │    │ (SQL Server)             │   │  │
│  │  └──────────────┘    └──────────────┘    └──────────────────────────┘   │  │
│  └─────────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
                                                │
                                                ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                            4. ÇIKIŞ KATMANI                                  │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                         SignalR Streaming                               │ │
│  │                                                                         │ │
│  │    ReceiveStreaming ──► "Veriler analiz ediliyor..."                    │ │
│  │    ReceiveStreaming ──► "Grafikler hazırlanıyor..."                     │ │
│  │    ReceiveStreaming ──► "<div>Dashboard HTML parçası</div>"             │ │
│  │    ReceiveMessage   ──► { isSucceed: true, resultData: {...} }          │ │
│  │                                                                         │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
                                                │
                                                ▼
                              ┌─────────────────────────────────────┐
                              │         KULLANICI ARAYÜZÜ           │
                              │   📊 Dinamik Dashboard Görüntüsü    │
                              └─────────────────────────────────────┘
```

### 3.4 Yüksek Erişilebilirlik Topolojisi (Önerilen Üretim Ortamı)

```
                           ┌─────────────────┐
                           │   Load Balancer │
                           │   (NGINX/HAProxy)│
                           └────────┬────────┘
                                    │
                   ┌────────────────┼────────────────┐
                   ▼                ▼                ▼
            ┌──────────┐     ┌──────────┐     ┌──────────┐
            │ AI.Api   │     │ AI.Api   │     │ AI.Api   │
            │ Node 1   │     │ Node 2   │     │ Node 3   │
            └────┬─────┘     └────┬─────┘     └────┬─────┘
                 │                │                │
                 └────────────────┼────────────────┘
                                  │
        ┌─────────────────────────┼─────────────────────────┐
        ▼                         ▼                         ▼
┌───────────────┐         ┌───────────────┐         ┌───────────────┐
│  PostgreSQL   │         │ Redis Cluster │         │Qdrant Cluster │
│   Primary     │         │   (Sentinel)  │         │  (3 Node)     │
│      │        │         │ M ─ S ─ S     │         │ N1 ─ N2 ─ N3  │
│   Replica     │         └───────────────┘         └───────────────┘
└───────────────┘

  Açıklama:
  • Load Balancer: Trafiği API node'larına dağıtır
  • API Nodes: Stateless, yatay ölçeklenebilir
  • PostgreSQL: Primary-Replica yapısı ile yedeklilik
  • Redis Sentinel: Otomatik failover ile yüksek erişilebilirlik
  • Qdrant Cluster: Veri replikasyonu ve yük dağıtımı
```

---

## 4. Teknoloji Stack'i

### 4.1 Backend

| Kategori | Teknoloji | Versiyon | Kullanım Alanı |
|----------|-----------|----------|----------------|
| Framework | ASP.NET Core | 10.0 | Web API, SignalR |
| AI/ML | Microsoft Semantic Kernel | Latest | LLM Orchestration |
| LLM Provider | Azure OpenAI / OpenAI | GPT-5.2 | Chat, SQL Generation |
| Embedding | OpenAI | text-embedding-3-large | Vector Embeddings (3072 dim) |
| Vector DB | Qdrant | Latest | Hybrid Search (Dense + Sparse) |
| Primary DB | PostgreSQL | 16+ | Chat History, Users, Metadata |
| Report DBs | SQL Server | - | AdventureWorks |
| Cache | Redis | 7+ | Session, History Cache |
| Job Scheduler | Hangfire | Latest | Scheduled Reports |
| Logging | Serilog | Latest | Structured Logging |
| Tracing | OpenTelemetry | Latest | Distributed Tracing |
| Real-time | SignalR | Latest | Streaming Responses |

### 4.2 Frontend

| Kategori | Teknoloji | Versiyon |
|----------|-----------|----------|
| Framework | Angular | 21.0 |
| Real-time | @microsoft/signalr | 10.0 |
| State | Angular Signals | Built-in |
| Styling | SCSS | - |

### 4.3 Altyapı

| Kategori | Teknoloji | Kullanım |
|----------|-----------|----------|
| Container | Docker Compose | Geliştirme ortamı |
| Tracing | Jaeger | Distributed tracing UI |
| Metrics | OpenTelemetry Collector | Telemetri toplama |
| Search (Alt) | Elasticsearch | Log depolama (opsiyonel) |

---

## 5. Proje Yapısı

### 5.1 Dizin Ağacı

```
AIApplications/
├── AI.Api/                          # Web API Projesi
│   ├── Common/
│   ├── Endpoints/                   # Feature-based Minimal API endpoints
│   │   ├── Auth/
│   │   ├── Documents/
│   │   ├── History/
│   │   └── ...
│   ├── Hubs/                        # SignalR Hub (AIHub)
│   ├── Common/
│   │   └── SignalRHubContextWrapper.cs
│   ├── Extensions/                  # DI, Middleware extensions
│   ├── Program.cs                   # Application entry point
│   └── appsettings.json            # Configuration
│
├── AI.Application/                  # Ports Layer (Hexagonal)
│   ├── Ports/
│   │   ├── Primary/                 # Driving Side
│   │   │   └── UseCases/            # 19 Primary Port interfaces
│   │   └── Secondary/               # Driven Side
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
│   │       ├── Notifications/
│   │       └── Scheduling/
│   ├── UseCases/                    # Use Case implementations
│   ├── Services/                    # Business logic services
│   ├── Common/
│   │   └── Resources/
│   │       ├── Prompts/             # LLM Prompt templates
│   │       └── Templates/           # HTML templates
│   ├── Configuration/               # Settings classes
│   ├── DTOs/                        # Data Transfer Objects
│   └── Results/                     # Result pattern implementations
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
├── AI.Infrastructure/               # Adapters Layer (Hexagonal)
│   ├── Adapters/
│   │   ├── AI/                      # AI service adapters
│   │   │   ├── Agents/SqlAgents/
│   │   │   ├── DocumentServices/
│   │   │   ├── ExcelServices/
│   │   │   ├── VectorServices/
│   │   │   └── ...
│   │   ├── External/                # External service adapters
│   │   │   ├── Auth/                # TokenService, CurrentUserService
│   │   │   ├── Caching/
│   │   │   ├── DatabaseServices/
│   │   │   ├── Notifications/
│   │   │   └── Scheduling/
│   │   └── Persistence/             # Database adapters
│   │       ├── Configurations/
│   │       ├── Migrations/
│   │       ├── Repositories/
│   │       └── ChatDbContext.cs
│   └── Extensions/                  # DI Extensions
│
├── AI.Scheduler/                    # Background Job Service
│   ├── Jobs/                        # Hangfire job classes
│   ├── Services/                    # Notification services
│   └── Program.cs                   # Scheduler entry point
│
├── frontend/                        # Angular SPA
│   └── src/app/
│       ├── core/
│       │   ├── guards/              # Route guards
│       │   ├── interceptors/        # HTTP interceptors
│       │   ├── models/              # TypeScript interfaces
│       │   └── services/            # Angular services
│       ├── pages/
│       │   ├── chat/                # Main chat interface
│       │   └── login/               # Authentication page
│       └── shared/                  # Shared components
│
└── docs/                            # Documentation
```

### 5.2 Önemli Dosyalar

| Dosya | Satır | Açıklama |
|-------|-------|----------|
| `RouteConversationUseCase.cs` | 456 | Ana routing mantığı — Agent Registry dispatch |
| `ActionAgentRegistry.cs` | 68 | Agent discovery & dispatch |
| `AIChatUseCase.cs` | 434 | Streaming chat, memory entegrasyonu (Excel analiz ExcelAnalysisUseCase'e taşındı) |
| `SqlServerReportServiceBase.cs` | 2283 | SQL Server rapor base class'ı |
| `RagSearchUseCase.cs` | 777 | HyDE + Hybrid arama |
| `QdrantService.cs` | 810 | Vector DB operasyonları |
| `UserMemoryUseCase.cs` | 559 | L0/L1/L2 memory stratejisi |
| `SignalRService.ts` | 613 | Frontend SignalR client |

---

## 6. Ana Bileşenler

### 6.1 RouteConversationUseCase - İstek Yönlendirme Merkezi

**Dosya:** `AI.Application/UseCases/RouteConversationUseCase.cs`

RouteConversationUseCase, sistemin "beyni"dir. Gelen her kullanıcı isteğini analiz ederek **Agent Registry** üzerinden uygun agent'a yönlendirir:

```
Kullanıcı İsteği
       ↓
┌──────────────────────────┐
│ RouteConversationUseCase │
│   SelectModeWithLlmAsync()
└────────┬─────────────────┘
         ↓
   LLM Analizi (GPT-5.2)
   conversation-orchestrator.md
         ↓
   IActionAgentRegistry.FindAgent(action)
         ↓
┌────────┼────────────────┬──────────────────┐
↓        ↓                ↓                  ↓
CHAT   DOCUMENT        REPORT              ASK
↓        ↓                ↓                  ↓
ChatActionAgent  DocumentActionAgent  ReportActionAgent  AskActionAgent
```

**Registered Agents (Strategy + Registry Pattern):**

| Agent | Action | Hedef Servis |
|-------|--------|--------------|
| `ChatActionAgent` | `chat` | `IAIChatUseCase` |
| `DocumentActionAgent` | `document` | `IRagSearchUseCase` |
| `ReportActionAgent` | `report` | `IReportService` (Keyed) |
| `AskActionAgent` | `ask_*` | Dinamik template + SignalR |

### 6.2 Report Services - Dinamik SQL Üretimi

Sistem, **Keyed Services** pattern'i ile farklı veritabanları için rapor servisleri sunar:

```csharp
// Keyed Service Registration
services.AddKeyedScoped<IReportService, AdventureWorksReportService>("adventureworks");
```

**Rapor Servisleri:**

| Servis | Veritabanı | Prompt Dosyası |
|--------|-----------|----------------|
| AdventureWorksReportService | SQL Server | adventurerworks_server_assistant_prompt.md |

**Rapor Akışı:**

```
Kullanıcı: "Ekim ayı çağrı istatistikleri"
         ↓
1. LLM SQL üretir (System Prompt + User Context)
         ↓
2. SQL Agent Pipeline (Opsiyonel)
   ├─ ValidationAgent: Syntax, güvenlik kontrolü
   └─ OptimizationAgent: Performans iyileştirme
         ↓
3. Veritabanı sorgusu çalıştırılır
         ↓
4. Büyük veri seti? → Chunk Analysis
   ├─ Veri parçalara bölünür
   ├─ Her parça paralel analiz edilir
   └─ Sonuçlar birleştirilir (Insight Aggregation)
         ↓
5. Dashboard HTML üretilir (Chart.js)
         ↓
6. SignalR ile streaming gönderim
```

### 6.3 RAG Search - Semantik Doküman Arama

**Dosya:** `AI.Application/UseCases/RagSearchUseCase.cs`

RAG (Retrieval Augmented Generation) sistemi, yüklenen dokümanları semantik olarak aranabilir hale getirir.

**Arama Stratejisi: Hybrid Search**

```
Kullanıcı Sorgusu: "Ürün kalitesi sorunu"
                    ↓
┌───────────────────┴───────────────────┐
↓                                       ↓
Dense Embedding                    Sparse Vector
(OpenAI text-embedding-3-large)    (Lucene Stemming)
3072 boyutlu vektör                TF-IDF benzeri skor
Semantik benzerlik                 Anahtar kelime eşleşme
                    ↓
            ┌───────┴───────┐
            ↓               ↓
     Qdrant Search    +    Keyword Match
            ↓               ↓
            └───────┬───────┘
                    ↓
            RRF Fusion (k=60)
            Rank-based birleştirme
                    ↓
            Top-K sonuçlar
```

**HyDE (Hypothetical Document Embeddings):**

Kullanıcı sorgusu yetersiz olduğunda, LLM bir "hayali cevap" üretir ve bu cevabın embedding'i ile arama yapılır:

```
Sorgu: "Ürün kalitesi sorunu"
         ↓
LLM: "Ürün kalitesi, müşteri memnuniyetini etkileyen önemli bir faktördür.
      Defolu, bozuk veya son kullanma tarihi geçmiş ürünlerle ilgili
      şikayetler olabilir..."
         ↓
Bu metnin embedding'i ile arama → Daha alakalı sonuçlar
```

### 6.4 User Memory - Uzun Vadeli Hafıza

**Dosya:** `AI.Application/UseCases/UserMemoryUseCase.cs`

Sistem, her kullanıcı için kişiselleştirilmiş bir hafıza tutar:

**Memory Stratejisi (L0/L1/L2):**

| Seviye | Kaynak | Maliyet | Örnek |
|--------|--------|---------|-------|
| **L0** | Sistem zamanı | Bedava | "Bugün 21 Ocak 2025, Salı" |
| **L1** | JWT Token | Bedava | "Kullanıcı: Ahmet Yılmaz (Admin)" |
| **L2** | Qdrant Semantic | ~50-80 token | "Tercih ettiği format: Excel" |

**Memory Extraction:**

```
Kullanıcı: "Raporları her zaman Excel'de istiyorum"
         ↓
LLM Memory Extraction:
{
  "key": "preferred_format",
  "value": "Excel",
  "category": "preference",
  "confidence": 0.95
}
         ↓
Qdrant'a kaydet (user_memories collection)
         ↓
Sonraki sorgularda: "Kullanıcı Excel formatını tercih ediyor"
                     sistem prompt'a eklenir
```

### 6.5 Excel Analysis - DuckDB Entegrasyonu (Multi-Query)

**Dosya:** `AI.Infrastructure/Adapters/AI/ExcelServices/DuckDbExcelService.cs`

Büyük Excel/CSV dosyaları (100K+ satır) için DuckDB in-memory analizi.
**Çoklu-sorgu analiz planı mimarisine geçilmiştir.**

```
Kullanıcı: "Bu Excel'i analiz et"
         ↓
1. Dosya temp klasöre yazılır
2. DuckDB bağlantısı açılır
3. Şema otomatik çıkarılır (sütun adları, tipler)
4. LLM analiz planı üretir (single veya comprehensive)
   - Spesifik soru → 1 SQL
   - Genel analiz → 5-8 SQL
5. Her sorgu 3 retry ile DuckDB'de çalıştırılır
6. Ara sonuçlar Markdown tablo olarak anında gönderilir
7. Tüm sonuçlar LLM ile yorumlanır + grafikler üretilir (streaming)
```

**Desteklenen Formatlar:**

- `.xlsx`, `.xls` (Excel)
- `.csv` (Comma-separated)

### 6.6 SQL Agent Pipeline - Sorgu Doğrulama

**Dosya:** `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlAgentPipeline.cs`

LLM'in ürettiği SQL sorgularını doğrulama ve optimize etme:

```
LLM Üretilen SQL
       ↓
┌──────────────────┐
│ ValidationAgent  │
│ ├─ Syntax check  │
│ ├─ Security scan │
│ └─ Schema match  │
└────────┬─────────┘
         ↓
   Geçti? ──No──→ Auto-correct veya Hata
         ↓ Yes
┌──────────────────┐
│ OptimizationAgent│
│ ├─ Index usage   │
│ ├─ Join order    │
│ └─ Subquery opt  │
└────────┬─────────┘
         ↓
   Optimize Edilmiş SQL
```

### 6.7 Scheduled Reports - Zamanlanmış Raporlar

**Dosyalar:**

- `AI.Application/UseCases/ScheduledReportUseCase.cs`
- `AI.Scheduler/Jobs/ScheduledReportJob.cs`

Kullanıcılar, oluşturdukları raporları zamanlayabilir:

```
Kullanıcı: "Bu raporu her Pazartesi saat 09:00'da çalıştır"
         ↓
ScheduledReport Entity oluşturulur
├─ CronExpression: "0 9 * * 1"
├─ SqlQuery: (Orijinal SQL)
├─ NotificationEmail: user@company.com
└─ TeamsWebhookUrl: (opsiyonel)
         ↓
Hangfire job kaydı
         ↓
Her Pazartesi 09:00'da:
├─ SQL çalıştırılır
├─ Rapor üretilir
├─ E-posta gönderilir
└─ Teams'e bildirim (opsiyonel)
```

---

## 7. Veri Akışları

### 7.1 Chat Akışı (End-to-End)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND                                   │
│  ┌───────────┐    ┌──────────────┐    ┌─────────────────────────────┐   │
│  │ Chat Page │───→│ ChatService  │───→│ SignalRService              │   │
│  └───────────┘    └──────────────┘    │ - connect()                 │   │
│                                        │ - onMessageReceived$       │   │
│                                        │ - onStreamingReceived$     │   │
│                                        └─────────────┬──────────────┘   │
└──────────────────────────────────────────────────────┼──────────────────┘
                                                       ↓
                                              SignalR WebSocket
                                                       ↓
┌──────────────────────────────────────────────────────┼──────────────────┐
│                              BACKEND                  ↓                 │
│  ┌─────────────────┐    ┌────────────────────────────────────────────┐  │
│  │ POST /chatbot   │───►│ RouteConversationUseCase.OrchestrateAsync()│  │
│  └─────────────────┘    └────────────────┬───────────────────────────┘  │
│                                          ↓                              │
│                         ┌────────────────────────────────────────┐      │
│                         │ SelectModeWithLlmAsync()               │      │
│                         │ conversation-orchestrator.md prompt    │      │
│                         └────────────────┬───────────────────────┘      │
│                                          ↓                              │
│              ┌───────────────────────────┼───────────────────────────┐  │ 
│              ↓                           ↓                           ↓  │
│        ┌─────────┐                ┌─────────────┐              ┌──────┐ │
│        │ CHAT    │                │ DOCUMENT    │              │REPORT│ │
│        └────┬────┘                └──────┬──────┘              └───┬──┘ │
│             ↓                            ↓                         ↓    │
│     AIChatUseCase              RagSearchUseCase           IReportService│
│     ├─ BuildMemoryContext      ├─ HyDE generation         ├─ SQL Gen    │
│     ├─ GetChatHistory          ├─ Hybrid search           ├─ DB Query   │
│     └─ Stream response         └─ Highlight results       └─ Dashboard  │
│             ↓                            ↓                         ↓    │
│             └────────────────────────────┼─────────────────────────┘    │
│                                          ↓                              │
│                              SignalR SendAsync()                        │
│                              ├─ ReceiveStreaming (chunk by chunk)       │
│                              └─ ReceiveMessage (final result)           │
└─────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Document Upload Akışı

```
Frontend: File Upload
         ↓
POST /api/v1/documents/upload
         ↓
DocumentProcessingUseCase
├─ Detect file type
├─ Parse content
│   ├─ PdfDocumentParser
│   ├─ TextDocumentParser
│   └─ (Office parsers)
├─ Text chunking
│   └─ RecursiveCharacterTextSplitter
├─ Generate embeddings
│   └─ OpenAIEmbeddingService (text-embedding-3-large)
├─ Generate sparse vectors
│   └─ SparseVectorService (Lucene stemming)
└─ Store in Qdrant
    └─ Collection: doc_{category}_{name}
         ↓
PostgreSQL metadata kaydı
└─ DocumentMetadata, DocumentChunk entities
```

### 7.3 Report Generation Akışı

```
Kullanıcı: "Son 3 ayın şikayet analizi"
                    ↓
            RouteConversationUseCase
                    ↓
            mode: "report"
            reportName: "adventureworks"
                    ↓
        ┌───────────────────────┐
        │ AdventureWorksReportService │
        └───────────┬───────────┘
                    ↓
    1. Load System Prompt (adventurerworks_server_assistant_prompt.md)
                    ↓
    2. Add User Memory Context (L0+L1+L2)
                    ↓
    3. LLM generates SQL
                    ↓
    4. (Optional) SQL Agent Pipeline
       ├─ Validation
       └─ Optimization
                    ↓
    5. Execute on SQL Server (AdventureWorks)
       └─ IDatabaseService.ExecuteAsync()
                    ↓
    6. Large dataset? (>1000 rows)
       ├─ Yes: Chunk Analysis
       │       ├─ Split into chunks (1000 rows each)
       │       ├─ Parallel LLM analysis
       │       └─ InsightAggregator merge
       └─ No: Single pass analysis
                    ↓
    7. Dashboard generation
       ├─ Config prompt → Chart types, colors
       └─ HTML prompt → Chart.js code
                    ↓
    8. SignalR streaming
       ├─ ReceiveStreaming: Analiz parçaları
       └─ ReceiveMessage: Final dashboard
```

---

## 8. API Endpoint'leri

### 8.1 Endpoint Listesi

| Endpoint | Method | Açıklama | Auth |
|----------|--------|----------|------|
| `/api/v1/chatbot` | POST | Ana chatbot endpoint'i | ✅ |
| `/api/v1/conversations/{id}/title` | PUT | Konuşma başlığını güncelle | ✅ |
| `/api/v1/conversations/{id}` | DELETE | Konuşmayı sil | ✅ |
| `/api/v1/history` | GET | Konuşma geçmişi | ✅ |
| `/api/v1/history/{conversationId}` | GET | Belirli konuşma detayı | ✅ |
| `/api/v1/documents/upload` | POST | Doküman yükle | ✅ |
| `/api/v1/documents` | GET | Doküman listesi | ✅ |
| `/api/v1/documents/{id}` | GET | Doküman detayı | ✅ |
| `/api/v1/documents/{id}` | DELETE | Dokümanı sil | ✅ |
| `/api/v1/documents/categories` | GET | Doküman kategorileri | ✅ |
| `/api/v1/search` | POST | Semantik arama | ✅ |
| `/api/v1/scheduled-reports` | GET | Zamanlanmış raporlar | ✅ |
| `/api/v1/scheduled-reports` | POST | Zamanlanmış rapor oluştur | ✅ |
| `/api/v1/scheduled-reports/{id}` | DELETE | Zamanlanmış raporu sil | ✅ |
| `/api/v1/auth/login` | POST | Giriş (JWT) | ❌ |
| `/api/v1/auth/login-ad` | POST | AD ile giriş | ❌ |
| `/api/v1/auth/refresh` | POST | Token yenile | ❌ |

### 8.2 SignalR Hub

**Endpoint:** `/ai-hub`

| Event | Yön | Payload | Açıklama |
|-------|-----|---------|----------|
| `ReceiveStreaming` | Server→Client | `{htmlMessage, data}` | Streaming yanıt parçası |
| `ReceiveMessage` | Server→Client | `{isSucceed, resultData}` | Final yanıt |
| `ReceiveLoading` | Server→Client | `string message` | Yükleme durumu |
| `ReceiveError` | Server→Client | `string error` | Hata mesajı |
| `ReceiveProgress` | Server→Client | `AnalysisProgress` | Chunk analiz ilerlemesi |
| `ReceiveReActStep` | Server→Client | `ReActStep` | ReAct düşünce/gözlem adımları |
| `JoinGroup` | Client→Server | `string groupName` | Gruba katıl |
| `LeaveGroup` | Client→Server | `string groupName` | Gruptan ayrıl |

### 8.3 Request/Response Örnekleri

**Chatbot Request:**

```json
{
  "prompt": "Son 30 günün çağrı istatistiklerini göster",
  "connectionId": "AbCdEf123...",
  "conversationId": "guid-here",
  "fileBase64": null,
  "fileName": null
}
```

**Chatbot Response (SignalR ReceiveMessage):**

```json
{
  "isSucceed": true,
  "systemMessage": "report",
  "resultData": {
    "conversationId": "guid-here",
    "htmlMessage": "<div class='dashboard'>...</div>",
    "summary": "30 günde toplam 15,432 çağrı alınmıştır.",
    "suggestions": [
      "Haftalık dağılımı göster",
      "En çok aranan konular neler?"
    ]
  }
}
```

---

## 9. Veritabanı Şeması

### 9.1 PostgreSQL (Ana Veritabanı)

```
┌─────────────────────────────────────────────────────────────────┐
│                         ChatDb                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  IDENTITY                                                       │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │    Users     │    │    Roles     │    │  UserRoles   │       │
│  ├──────────────┤    ├──────────────┤    ├──────────────┤       │
│  │ Id (PK)      │    │ Id (PK)      │    │ UserId (FK)  │       │
│  │ Username     │    │ Name         │    │ RoleId (FK)  │       │
│  │ Email        │    │ Description  │    └──────────────┘       │
│  │ DisplayName  │    └──────────────┘                           │
│  │ PasswordHash │                                               │
│  │ AuthSource   │    ┌──────────────┐                           │
│  │ AdUsername   │    │RefreshTokens │                           │
│  │ IsActive     │    ├──────────────┤                           │
│  └──────────────┘    │ Id (PK)      │                           │
│                      │ UserId (FK)  │                           │
│                      │ Token        │                           │
│                      │ ExpiresAt    │                           │
│                      └──────────────┘                           │
│                                                                 │
│  CHATBOT                                                        │
│  ┌──────────────┐    ┌──────────────┐                           │
│  │ Conversations│    │   Messages   │                           │
│  ├──────────────┤    ├──────────────┤                           │
│  │ Id (PK)      │───<│ Id (PK)      │                           │
│  │ UserId       │    │ ConvId (FK)  │                           │
│  │ Title        │    │ Role         │                           │
│  │ CreatedAt    │    │ Content      │                           │
│  │ UpdatedAt    │    │ Type         │                           │
│  └──────────────┘    │ Metadata     │                           │
│                      │ CreatedAt    │                           │
│                      └──────────────┘                           │
│                                                                 │
│  RAG                                                            │
│  ┌──────────────────┐    ┌──────────────────┐                   │
│  │ DocumentMetadata │    │  DocumentChunks  │                   │
│  ├──────────────────┤    ├──────────────────┤                   │
│  │ Id (PK)          │───<│ Id (PK)          │                   │
│  │ FileName         │    │ DocumentId (FK)  │                   │
│  │ Title            │    │ ChunkIndex       │                   │
│  │ Category         │    │ Content          │                   │
│  │ ContentType      │    │ StartPosition    │                   │
│  │ FileSize         │    │ EndPosition      │                   │
│  │ ChunkCount       │    │ QdrantPointId    │                   │
│  │ UploadedAt       │    │ CreatedAt        │                   │
│  └──────────────────┘    └──────────────────┘                   │
│                                                                 │
│  MEMORY                                                         │
│  ┌──────────────────┐                                           │
│  │   UserMemories   │                                           │
│  ├──────────────────┤                                           │
│  │ Id (PK)          │                                           │
│  │ UserId           │                                           │
│  │ Key              │                                           │
│  │ Value            │                                           │
│  │ Category         │                                           │
│  │ Confidence       │                                           │
│  │ UsageCount       │                                           │
│  │ CreatedAt        │                                           │
│  │ LastAccessedAt   │                                           │
│  └──────────────────┘                                           │
│                                                                 │
│  SCHEDULING                                                     │
│  ┌──────────────────┐    ┌──────────────────┐                   │
│  │ ScheduledReports │    │ScheduledReportLogs│                  │
│  ├──────────────────┤    ├──────────────────┤                   │
│  │ Id (PK)          │───<│ Id (PK)          │                   │
│  │ UserId           │    │ ReportId (FK)    │                   │
│  │ Name             │    │ Status           │                   │
│  │ OriginalPrompt   │    │ StartedAt        │                   │
│  │ SqlQuery         │    │ CompletedAt      │                   │
│  │ CronExpression   │    │ FilePath         │                   │
│  │ ReportServiceType│    │ ErrorMessage     │                   │
│  │ IsActive         │    │ RecordCount      │                   │
│  │ LastRunAt        │    │ DurationMs       │                   │
│  │ NextRunAt        │    └──────────────────┘                   │
│  │ RunCount         │                                           │
│  └──────────────────┘                                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Qdrant (Vector Database)

**Collections:**

| Collection | Dense Vector | Sparse Vector | Kullanım |
|------------|--------------|---------------|----------|
| `documents` | text-embedding-3-large (3072) | Lucene TF-IDF | Default doküman koleksiyonu |
| `doc_{category}_{name}` | text-embedding-3-large (3072) | Lucene TF-IDF | Kategorize dokümanlar |
| `user_memories` | text-embedding-3-large (3072) | - | Kullanıcı hafızası |

**Point Payload Yapısı:**

```json
{
  "document_id": "guid",
  "chunk_id": "guid",
  "chunk_index": 0,
  "content": "Chunk metni...",
  "document_name": "Finans-Kılavuzu.pdf",
  "document_title": "Finans Kılavuzu",
  "category": "Genel",
  "file_name": "Finans-Kilavuzu.pdf",
  "uploaded_at": "2025-01-15T10:30:00Z",
  "start_position": 0,
  "end_position": 1500
}
```

### 8.3 SQL Server (AdventureWorks) - Rapor Veritabanı

> **Not:** AdventureWorks, Microsoft SQL Server demo veritabanıdır. Bisiklet satış şirketi simülasyonu içerir.

**Örnek Tablolar:**

- `CUSTOMER_FEEDBACK` - Müşteri geri bildirimleri
- `AGENT_PERFORMANCE` - Ajan performans metrikleri

### 8.4 SQL Server (AdventureWorks)

Demo/test amaçlı Microsoft örnek veritabanı.

---

## 10. AI/ML Yetenekleri

### 9.1 LLM Entegrasyonu

**Provider:** Azure OpenAI / OpenAI
**Model:** GPT-5.2

**Kullanım Alanları:**

| Alan | Açıklama | Prompt |
|------|----------|--------|
| Request Routing | İstek türü belirleme | conversation-orchestrator.md |
| SQL Generation | Doğal dil → SQL | *_report_system_prompt.md |
| SQL Validation | Syntax/güvenlik kontrolü | sql_validation_agent_prompt.md |
| SQL Optimization | Performans iyileştirme | sql_optimization_agent_prompt.md |
| HyDE | Arama iyileştirme | Built-in prompt |
| Dashboard Config | Grafik konfigürasyonu | dashboard_config_generator_prompt.md |
| Dashboard HTML | Chart.js kodu | dashboard_generator_prompt_adventureworks.md |
| Memory Extraction | Tercih çıkarma | Built-in prompt |
| Insight Analysis | Veri yorumlama | insight_analysis_prompt.md |
| Chunk Analysis | Parça analizi | chunk_analysis_prompt.md |

### 9.2 Embedding Model

**Model:** text-embedding-3-large
**Boyut:** 3072

**Kullanım:**

- Doküman chunk'ları için embedding oluşturma
- Kullanıcı sorgusu embedding'i
- Kullanıcı hafızası embedding'i

### 9.3 Semantic Kernel

Microsoft Semantic Kernel, LLM orkestrasyon katmanı olarak kullanılır:

```csharp
// Kernel yapılandırması
var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: "gpt-5.2",
        endpoint: config.Endpoint,
        apiKey: config.ApiKey);
```

### 9.4 Hybrid Search Algoritması

**Bileşenler:**

1. **Dense Vector Search (Semantik)**
   - OpenAI text-embedding-3-large
   - Cosine similarity
   - HNSW indeksleme (M=16, EfConstruct=200)

2. **Sparse Vector Search (Keyword)**
   - Lucene Snowball Stemmer (Turkish)
   - TF-IDF benzeri skor
   - Exact match boost

3. **RRF Fusion (Birleştirme)**

   ```
   RRF_score = Σ (1 / (k + rank_i))
   k = 60 (default)
   ```

**Ağırlıklar:**

- Dense: 0.7 (70%)
- Sparse: 0.3 (30%)

---

## 11. Güvenlik ve Kimlik Doğrulama

### 10.1 Authentication Yöntemleri

| Yöntem | Kullanıcı Tipi | Mekanizma |
|--------|----------------|-----------|
| Local | Lokal kullanıcılar | Username + Password (PBKDF2) |
| Active Directory | Kurumsal kullanıcılar | LDAP bind |

### 10.2 JWT Token Yapısı

```json
{
  "sub": "user-guid",
  "unique_name": "ahmet.yilmaz",
  "email": "ahmet.yilmaz@company.com",
  "display_name": "Ahmet Yılmaz",
  "role": ["Admin", "ReportViewer"],
  "auth_source": "ActiveDirectory",
  "exp": 1705852800
}
```

**Token Süreleri:**

- Access Token: 60 dakika
- Refresh Token: 7 gün

### 10.3 Authorization

| Özellik | Mekanizma |
|---------|-----------|
| Endpoint Koruma | `[Authorize]` attribute |
| SignalR Hub | JWT Bearer token |
| Rate Limiting | Fixed Window, Sliding Window, Token Bucket |

### 10.4 Rate Limiting Politikaları

```json
{
  "RateLimiting": {
    "FixedWindow": {
      "PermitLimit": 10,
      "Window": "00:01:00"
    },
    "SlidingWindow": {
      "PermitLimit": 50,
      "Window": "00:05:00"
    },
    "TokenBucket": {
      "TokenLimit": 100,
      "ReplenishmentPeriod": "00:00:10"
    },
    "Concurrency": {
      "PermitLimit": 5
    }
  }
}
```

### 10.5 SQL Injection Koruması

1. **Parameterized Queries:** Tüm DB sorgularında parametre kullanımı
2. **SQL Validation Agent:** LLM üretilen SQL'leri kontrol
3. **Whitelist Pattern:** İzin verilen SQL pattern'ları

---

## 12. İzleme ve Loglama

### 11.1 OpenTelemetry Tracing

**Konfigürasyon:**

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "ServiceName": "AI.Api",
    "TracesEnabled": true,
    "SqlClientTracingEnabled": true,
    "EntityFrameworkTracingEnabled": true,
    "RedisTracingEnabled": true,
  }
}
```

**Activity Sources:**

- `AI.Chat` - Chat operasyonları
- `AI.RagSearch` - Semantik arama
- `AI.DocumentProcessing` - Doküman işleme
- `AI.ChatHistory` - Konuşma geçmişi

### 11.2 Serilog Yapılandırması

**Sink'ler:**

- Console (development)
- File (rolling, günlük)

**Log Seviyeleri:**

- `Information`: Normal operasyonlar
- `Warning`: Beklenmeyen durumlar
- `Error`: Hatalar
- `Debug`: Geliştirme detayları

### 11.3 Health Checks

**Endpoint:** `/health`

| Check | Açıklama |
|-------|----------|
| PostgreSQL | Ana veritabanı bağlantısı |
| Redis | Cache bağlantısı |
| Qdrant | Vector DB bağlantısı |
| SQL Server | Rapor DB bağlantısı |
| OpenTelemetry | Collector bağlantısı |

---

## 13. Konfigürasyon Yönetimi

### 12.1 appsettings.json Yapısı

```json
{
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForSecurity!",
    "Issuer": "EnterpriseAI.API",
    "Audiences": [ "EnterpriseAI.Client", "EnterpriseAI.Mobile" ],
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "ClockSkewSeconds": 60
  },
  "ActiveDirectory": {
    "Enabled": true,
    "Domain": "MYCOMPANY",
    "LdapServer": "ldap://dc.company.com",
    "DefaultRole": "User"
  },
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;...",
    "AdventureWorks2022": "Server=localhost;...",
    "Redis": "localhost:6379"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "UseHttps": false,
    "ApiKey": "",
    "DefaultCollection": "documents",
    "EmbeddingModel": "text-embedding-3-large",
    "VectorSize": 3072,
    "CosineSimilarity": "Cosine",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "MinSimilarityScore": 0.0,
    "HyDEMaxTokens": 350,
    "DenseWeight": 0.7,
    "SparseWeight": 0.3,
    "RRF_K": 60,
    "HnswEf": 128
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
  "CacheSettings": {
    "ChatHistoryTtlMinutes": 15,
    "EnableCompression": true,
    "EnableStampedeProtection": true
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
    "FullPromptFileName": "dashboard_generator_prompt_adventureworks.md",
    "OutputFolder": "output-folder"
  }
}
```

### 12.2 Environment-Specific Ayarlar

| Ortam | Dosya | Farklılıklar |
|-------|-------|--------------|
| Development | appsettings.Development.json | Verbose logging, local endpoints |
| Production | appsettings.json + ENV vars | Secure settings, remote services |

---

## 14. Frontend Mimarisi

### 13.1 Angular Yapısı

```
frontend/src/app/
├── core/
│   ├── guards/
│   │   └── auth.guard.ts          # Route koruma
│   ├── interceptors/
│   │   └── auth.interceptor.ts    # JWT ekleme
│   ├── models/                     # TypeScript interfaces
│   └── services/
│       ├── auth.service.ts        # Giriş/çıkış
│       ├── chat.service.ts        # Mesaj gönderme
│       ├── signalr.service.ts     # Gerçek zamanlı iletişim
│       ├── history.service.ts     # Konuşma geçmişi
│       └── document.service.ts    # Doküman yönetimi
├── pages/
│   ├── chat/                       # Ana chat ekranı
│   └── login/                      # Giriş ekranı
└── shared/                         # Paylaşılan bileşenler
```

### 13.2 State Management

Angular 21 **Signals** kullanılır:

```typescript
// SignalRService
connectionState = signal<ConnectionState>('disconnected');
isStreaming = signal<boolean>(false);
messages = signal<ChatMessage[]>([]);
analysisProgress = signal<AnalysisProgress | null>(null);
```

### 13.3 SignalR İstemcisi

```typescript
// Bağlantı kurulumu
this.hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${environment.apiUrl}/ai-hub`, {
    accessTokenFactory: () => this.authService.getToken()
  })
  .withAutomaticReconnect()
  .build();

// Event dinleyicileri
this.hubConnection.on('ReceiveStreaming', (data) => { ... });
this.hubConnection.on('ReceiveMessage', (data) => { ... });
this.hubConnection.on('ReceiveProgress', (progress) => { ... });
```

---

## 15. Dağıtım ve DevOps

### 14.1 Docker Compose

```yaml
services:
  ai-api:
    build: ./AI.Api
    ports:
      - "7041:443"
    depends_on:
      - postgres
      - redis
      - qdrant

  ai-scheduler:
    build: ./AI.Scheduler
    depends_on:
      - ai-api

  postgres:
    image: postgres:16
    ports:
      - "5432:5432"

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  qdrant:
    image: qdrant/qdrant
    ports:
      - "6333:6333"
      - "6334:6334"

  otel-collector:
    image: otel/opentelemetry-collector
    ports:
      - "4317:4317"
```

### 14.2 Ortam Gereksinimleri

| Bileşen | Minimum | Önerilen |
|---------|---------|----------|
| .NET Runtime | 10.0 | 10.0 |
| PostgreSQL | 14+ | 16+ |
| Redis | 6+ | 7+ |
| Qdrant | 1.6+ | Latest |
| Node.js (Frontend) | 18+ | 22+ |

### 14.3 Başlatma Sırası

1. PostgreSQL, Redis, Qdrant başlat
2. `AI.Api` başlat (migration otomatik)
3. `AI.Scheduler` başlat
4. Frontend `ng serve`

---

## Ekler

### A. Prompt Dosyaları Listesi

**Aktif Promptlar (16 adet):**

| Dosya | Satır | Kullanım |
|-------|-------|----------|
| conversation-orchestrator.md | 601 | İstek yönlendirme |
| adventurerworks_server_assistant_prompt.md | ~200 | AdventureWorks raporu |
| sql_validation_agent_prompt.md | ~150 | SQL doğrulama |
| sql_optimization_agent_prompt.md | ~150 | SQL optimizasyon |
| dashboard_config_generator_prompt.md | ~100 | Dashboard config |
| dashboard_generator_prompt_adventureworks.md | ~200 | Dashboard HTML |
| chunk_analysis_prompt.md | ~100 | Chunk analizi |
| insight_analysis_prompt.md | ~150 | Insight çıkarma |
| file_analysis_prompt.md | ~100 | Dosya analizi |
| excel_analysis_plan_prompt.md | - | Excel çoklu SQL analiz planı |
| excel_sql_generator_prompt.md | ~150 | Excel SQL üretimi (fallback) |
| excel_interpret_prompt.md | ~100 | Excel tek sonuç yorumu |
| excel_multi_interpret_prompt.md | - | Excel çoklu sonuç yorumu |

### B. Sözlük

| Terim | Açıklama |
|-------|----------|
| **RAG** | Retrieval Augmented Generation - Bilgi tabanı destekli metin üretimi |
| **HyDE** | Hypothetical Document Embeddings - Hayali doküman gömme |
| **RRF** | Reciprocal Rank Fusion - Sıralama birleştirme algoritması |
| **Dense Vector** | Semantik anlam taşıyan yoğun vektör |
| **Sparse Vector** | Anahtar kelime tabanlı seyrek vektör |
| **Chunk** | Büyük metnin parçalara bölünmüş hali |
| **Embedding** | Metnin sayısal vektör temsili |
| **Token** | LLM'in işlediği metin birimi (~0.75 kelime) |
| **SignalR** | ASP.NET Core gerçek zamanlı iletişim kütüphanesi |
| **Semantic Kernel** | Microsoft LLM orkestrasyon framework'ü |

### C. İletişim ve Destek

- **Kod Repository:** Internal Git
- **Dokümantasyon:** `/docs` klasörü
- **API Dokümantasyonu:** Swagger UI (`/swagger`)

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [System-Overview.md](System-Overview.md) | Genel sistem mimarisi |
| [Hexagonal-Architecture.md](Hexagonal-Architecture.md) | Port/Adapter yapısı |
| [Conversation-Router.md](Conversation-Router.md) | İstek yönlendirme (Agent Registry) |
| [Infrastructure-Cross-Cutting.md](Infrastructure-Cross-Cutting.md) | Altyapı ve cross-cutting concerns |
| [Multi-Agent.md](Multi-Agent.md) | SQL Agent pipeline |
| [Chat-System.md](Chat-System.md) | Chat sistemi özellikleri |

---

**Döküman Sonu**
