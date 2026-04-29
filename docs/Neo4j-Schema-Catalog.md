# Neo4j Schema Catalog — Detaylı Analiz

## 📋 İçindekiler

- [Genel Bakış](#-genel-bakış)
- [Neden Neo4j?](#-neden-neo4j)
- [Mimari Tasarım](#-mimari-tasarım)
- [Graph Veri Modeli](#-graph-veri-modeli)
- [Şema Yükleme Stratejileri](#-şema-yükleme-stratejileri)
- [Dinamik Prompt Oluşturma](#-dinamik-prompt-oluşturma)
- [API Endpoints](#-api-endpoints)
- [Dosya Yapısı](#-dosya-yapısı)
- [Konfigürasyon](#-konfigürasyon)
- [Hexagonal Architecture Uyumu](#-hexagonal-architecture-uyumu)
- [Veri Akışı](#-veri-akışı)
- [Cypher Sorgu Referansı](#-cypher-sorgu-referansı)
- [Health Check & İzleme](#-health-check--izleme)
- [Çoklu Veritabanı Desteği](#-çoklu-veritabanı-desteği)

---

## 🎯 Genel Bakış

Neo4j, bu projede **Graph-based Schema Catalog** olarak kullanılmaktadır. Temel amacı, SQL Server veritabanı şemasını (tablolar, kolonlar, foreign key ilişkileri) bir graf yapısında saklayarak:

1. **Kullanıcı sorgusuna göre ilgili tabloları bulmak** (Full-text search + graph traversal)
2. **Tablolar arası JOIN yollarını otomatik keşfetmek** (Shortest path algoritması)
3. **Dinamik SQL prompt'ları oluşturmak** (Sadece ilgili şema bilgisiyle LLM'e sorgu göndermek)

### Problem — Neden Static Prompt Yetmez?

Geleneksel yaklaşımda, tüm veritabanı şeması (tüm tablolar, tüm kolonlar) tek bir sabit prompt olarak LLM'e gönderilir. Bu yaklaşımın sorunları:

| Sorun | Açıklama |
|-------|----------|
| **Token israfı** | AdventureWorks gibi bir DB'de ~70 tablo, ~900+ kolon var. Tümünü her sorguda göndermek gereksiz token tüketimi |
| **Gürültü** | "Satış miktarı ne kadar?" sorusu için `HumanResources` şemasının gönderilmesi LLM'i yanıltabilir |
| **JOIN karmaşıklığı** | LLM, hangi tabloların nasıl birleştirileceğini bilemeyebilir |
| **Ölçeklenebilirlik** | Yeni tablolar eklendikçe prompt sürekli büyür |

### Çözüm — Neo4j Schema Catalog

```
Kullanıcı Sorusu: "En çok satan ürünler hangileri?"
        │
        ▼
┌─────────────────────────┐
│  DynamicPromptBuilder   │
│  (Keyword Extraction)   │
│  "satış", "ürün", "sat" │
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   Neo4j Full-text       │
│   Search                │
│   ───────────────       │
│   Sales.SalesOrderDetail│  score: 2.8
│   Production.Product    │  score: 2.5
│   Sales.SalesOrderHeader│  score: 2.1
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   Neo4j Graph Traversal │
│   (Shortest Path)       │
│   ───────────────       │
│   SalesOrderDetail      │
│     ─JOINS_WITH─►       │
│   SalesOrderHeader      │
│     ─JOINS_WITH─►       │
│   Product               │
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   Dinamik Prompt        │
│   (Sadece 3 tablo +     │
│    JOIN bilgileri)      │
│   ~500 token            │
│   (vs. tüm şema ~8000)  │
└─────────────────────────┘
```

---

## 🤔 Neden Neo4j?

### Relational DB vs. Graph DB Karşılaştırması

| Kriter | Relational (SQL Server) | Graph (Neo4j) |
|--------|------------------------|---------------|
| **Tablo ilişki keşfi** | Recursive CTE, yavaş | `shortestPath()`, doğal ve hızlı |
| **JOIN path bulma** | N×N sorgu gerekli | Tek Cypher sorgusu |
| **Full-text search** | `CONTAINS` / `FREETEXT` (sınırlı) | Native full-text index, relevance scoring |
| **Schema traversal** | `INFORMATION_SCHEMA` sorguları | Doğrudan graph traversal |
| **Yeni tablo ekleme** | ALTER TABLE, migration | `MERGE` node, anında |
| **Çoklu DB desteği** | Ayrı catalog view'lar | `source` property ile tek graph |

### Graph Modelin Avantajları

- **İlişki-merkezli**: FK ilişkileri birinci sınıf vatandaş (`JOINS_WITH` relationship)
- **Shortest path**: İki tablo arası en kısa JOIN yolunu O(1)'e yakın bulur
- **Full-text search**: Tablo/kolon adı ve açıklamalarında Lucene-based arama
- **Esneklik**: Yeni bir veritabanı eklemek = yeni node'lar + `source` property

---

## 🏗 Mimari Tasarım

### Hexagonal Architecture (Ports & Adapters)

```
┌───────────────────────────────────────────────────────────────────┐
│                         AI.Application                            │
│                                                                   │
│  ┌───────────────────────┐  ┌──────────────────────────────────┐  │
│  │    Ports (Interface)  │  │         DTOs                     │  │
│  │    ─────────────────  │  │  ────────────────────────────────│  │
│  │  ISchemaGraphService  │  │  Neo4j/                          │  │
│  │  ISchemaParserService │  │    TableInfo, ColumnInfo,        │  │
│  │  IDynamicPromptBuilder│  │    TableSchema, JoinPath,        │  │
│  │  IDatabaseSchemaReader│  │    SchemaParseResult, SchemaInfo │  │
│  │                       │  │  SchemaCatalog/                  │  │
│  │  Configuration/       │  │    SchemaCatalogStats            │  │
│  │    Neo4jSettings      │  │  DynamicPrompt/                  │  │
│  │    SchemaSourceType   │  │    DynamicPromptResult           │  │
│  └───────────────────────┘  └──────────────────────────────────┘  │
└───────────────────────────────┬───────────────────────────────────┘
                                │ implements
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                       AI.Infrastructure                         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              Adapters/AI/Neo4j/                         │    │
│  │  ─────────────────────────────────────────────────────  │    │
│  │  SchemaGraphService.cs      → ISchemaGraphService       │    │
│  │  SchemaParserService.cs     → ISchemaParserService      │    │
│  │  DynamicPromptBuilder.cs    → IDynamicPromptBuilder     │    │
│  │  DatabaseSchemaReader.cs    → IDatabaseSchemaReader     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              Extensions/                                │    │
│  │  ─────────────────────────────────────────────────────  │    │
│  │  Neo4jExtensions.cs  → DI registration, Initialization  │    │
│  │  Neo4jHealthCheck     → Health monitoring               │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                                │ exposes
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                           AI.Api                                │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │         Endpoints/Search/Neo4jEndpoints.cs              │    │
│  │  ─────────────────────────────────────────────────────  │    │
│  │  /api/v1/neo4j/health          GET                      │    │
│  │  /api/v1/neo4j/stats           GET                      │    │
│  │  /api/v1/neo4j/search/tables   GET                      │    │
│  │  /api/v1/neo4j/join-path       GET                      │    │
│  │  /api/v1/neo4j/dynamic-prompt  GET                      │    │
│  │  /api/v1/neo4j/import/...      POST/DELETE              │    │
│  │  /api/v1/neo4j/sources/...     GET/DELETE               │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔵 Graph Veri Modeli

### Node Tipleri

| Node Label | Özellikler | Açıklama |
|------------|-----------|----------|
| **Schema** | `name`, `description`, `tableCount`, `source`, `updatedAt` | Veritabanı şeması (örn: `Sales`, `Production`) |
| **Table** | `name`, `fullName`, `schema`, `description`, `type`, `columnCount`, `source`, `updatedAt` | Tablo veya View |
| **Column** | `key`, `name`, `tableName`, `dataType`, `alias`, `description`, `isPrimaryKey`, `isForeignKey`, `fkTable`, `fkColumn`, `source` | Tablo kolonu |

### Relationship Tipleri

| Relationship | Yön | Açıklama |
|-------------|-----|----------|
| `(:Schema)-[:CONTAINS]->(:Table)` | Schema → Table | Şema bir tabloyu içerir |
| `(:Table)-[:HAS_COLUMN]->(:Column)` | Table → Column | Tablo bir kolona sahiptir |
| `(:Column)-[:REFERENCES]->(:Table)` | Column → Table | FK kolonu bir tabloya referans verir |
| `(:Table)-[:JOINS_WITH]->(:Table)` | Table → Table | İki tablo JOIN ile birleştirilebilir |

### Görsel Graph Modeli

```
                    ┌──────────────┐
                    │   Schema     │
                    │  name: Sales │
                    └──────┬───────┘
                           │ CONTAINS
                           ▼
┌───────────────────────────────────────────────────────┐
│                     Table                              │
│  name: SalesOrderHeader                               │
│  fullName: Sales.SalesOrderHeader                     │
│  description: Satış siparişleri ana tablosu            │
└────┬──────────────────────────────────┬───────────────┘
     │ HAS_COLUMN                       │ JOINS_WITH
     ▼                                  ▼
┌──────────────┐              ┌──────────────────┐
│   Column     │              │     Table        │
│  CustomerID  │──REFERENCES──│  Sales.Customer  │
│  isForeignKey│              │                  │
│  = true      │              └──────────────────┘
└──────────────┘
```

### Index'ler

| Index Tipi | Index Adı | Hedef | Alanlar |
|-----------|-----------|-------|---------|
| **Uniqueness Constraint** | `schema_name` | Schema | `name` |
| **Uniqueness Constraint** | `table_fullname` | Table | `fullName` |
| **B-tree Index** | `table_name_idx` | Table | `name` |
| **B-tree Index** | `table_schema_idx` | Table | `schema` |
| **B-tree Index** | `column_name_idx` | Column | `name` |
| **B-tree Index** | `column_alias_idx` | Column | `alias` |
| **B-tree Index** | `column_tablename_idx` | Column | `tableName` |
| **Full-text Index** | `table_search` | Table | `name`, `description` |
| **Full-text Index** | `column_search` | Column | `name`, `alias`, `description` |

---

## 📥 Şema Yükleme Stratejileri

Sistem iki farklı kaynaktan şema yükleyebilir:

### 1. Markdown Dosyasından (Varsayılan)

```
SchemaSource = SchemaSourceType.Markdown
```

**Akış:**

```
adventurerworks_schema.md
        │
        ▼
┌─────────────────────────┐
│  SchemaParserService    │
│  ParseFromResourceAsync │
│  ParseMarkdown()        │
└────────────┬────────────┘
             │ SchemaParseResult
             ▼
┌─────────────────────────┐
│  GenerateCypherImport   │
│  Script()               │
│  ─────────────────────  │
│  CREATE CONSTRAINT ...  │
│  CREATE FULLTEXT INDEX..│
│  MERGE (s:Schema ...)   │
│  MERGE (t:Table ...)    │
│  MERGE (c:Column ...)   │
│  MERGE (t)-[:JOINS_WITH]│
└────────────┬────────────┘
             │ Cypher Script
             ▼
┌─────────────────────────┐
│  Neo4j Driver           │
│  ExecuteCypherImport    │
│  (Statement by stmt)    │
└─────────────────────────┘
```

**Markdown Format (adventurerworks_schema.md):**
- Her şema için başlık (`## Sales`)
- Her tablo için alt başlık ve kolon tablosu
- FK ilişkilerini tanımlayan bölüm
- Türkçe alias ve açıklamalar

### 2. SQL Server Veritabanından (Hibrit Yaklaşım)

```
SchemaSource = SchemaSourceType.Database
```

**Akış:**

```
SQL Server (AdventureWorks2022)
        │
        ▼
┌─────────────────────────┐
│  DatabaseSchemaReader   │
│  ReadSchemaFromDatabase │
│  ─────────────────────  │
│  INFORMATION_SCHEMA     │
│  sys.foreign_keys       │
│  sys.columns            │
└────────────┬────────────┘
             │ SchemaParseResult
             ▼
┌─────────────────────────┐
│  AliasConfiguration     │
│  (alias_config.json)    │
│  ─────────────────────  │
│  Türkçe kolon alias'ları│
│  Tablo açıklamaları     │
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│  Cypher Import          │
│  (Neo4j'ye yazma)       │
└─────────────────────────┘
```

### Uygulama Başlatma (Startup)

```csharp
// DependencyInjectionExtensions.cs
services.AddNeo4jSchemaCatalog(configuration);

// Program.cs veya Startup
await app.Services.InitializeNeo4jSchemaCatalogAsync();
```

`InitializeNeo4jSchemaCatalogAsync` şu adımları izler:

1. Neo4j bağlantısını test et
2. Mevcut şema verilerini kontrol et (`GetStatsAsync`)
3. Şema zaten varsa → atla (idempotent)
4. Yoksa → `SchemaSource` ayarına göre Markdown veya Database'den import et
5. Cypher script oluştur ve çalıştır
6. Sonucu doğrula ve logla

---

## 🧠 Dinamik Prompt Oluşturma

### DynamicPromptBuilder Akışı

Bu, Neo4j entegrasyonunun **en kritik** parçasıdır. Rapor servisleri (`SqlServerReportServiceBase`, `AdventureWorksReportService`) kullanıcı sorgusu geldiğinde bu builder'ı çağırır:

```
Kullanıcı: "Son 3 ayda en çok satan ürünlerin kategorilere göre dağılımı"
                    │
                    ▼
          ┌─────────────────────┐
     1.   │  ExtractKeywords    │
          │  (Türkçe stemming)  │
          │  ─────────────────  │
          │  Input: "Son 3 ayda │
          │   en çok satan      │
          │   ürünlerin..."     │
          │                     │
          │  Output:            │
          │  ["sat", "satış",   │
          │   "ürün", "ürünler",│
          │   "kategori", "3",  │
          │   "dağılım"]        │
          └────────┬────────────┘
                   │
                   ▼
          ┌─────────────────────┐
     2.   │  SearchTablesByKw   │
          │  (Neo4j Full-text)  │
          │  ─────────────────  │
          │  CALL db.index.     │
          │  fulltext.queryNodes│
          │  ('table_search',   │
          │    $query)          │
          │                     │
          │  Sonuç:             │
          │  Sales.SalesOrder   │
          │    Detail (2.8)     │
          │  Production.Product │
          │    (2.5)            │
          │  Production.Product │
          │    Category (2.1)   │
          └────────┬────────────┘
                   │
                   ▼
          ┌─────────────────────┐
     3.   │  GetTableSchemas    │
          │  (Kolon bilgileri)  │
          │  ─────────────────  │
          │  MATCH (t:Table)    │
          │  -[:HAS_COLUMN]->   │
          │  (c:Column)         │
          └────────┬────────────┘
                   │
                   ▼
          ┌─────────────────────┐
     4.   │  FindJoinPaths      │
          │  (Graph traversal)  │
          │  ─────────────────  │
          │  shortestPath(      │
          │    (t1)-[:JOINS_WITH│
          │    *1..4]-(t2))     │
          │                     │
          │  Sonuç:             │
          │  SalesOrderDetail   │
          │  → SalesOrderHeader │
          │  → Product          │
          │  → ProductCategory  │
          └────────┬────────────┘
                   │
                   ▼
          ┌─────────────────────┐
     5.   │  V2 Template        │
          │  ─────────────────  │
          │  {{DYNAMIC_SCHEMA}} │
          │  → 3 tablonun kolon │
          │    bilgileri        │
          │                     │
          │  {{DYNAMIC_JOIN_    │
          │    PATHS}}          │
          │  → SQL JOIN         │
          │    ifadeleri        │
          └─────────────────────┘
```

### Türkçe Stemming

`DynamicPromptBuilder`, kullanıcı sorgusundaki Türkçe ekleri kaldırarak daha iyi arama sonuçları üretir:

| Orijinal | Stemmed | Açıklama |
|----------|---------|----------|
| satışlardan | satış | `-lardan` eki kaldırıldı |
| müşteriye | müşteri | `-ye` eki kaldırıldı |
| ürünlerin | ürün | `-lerin` eki kaldırıldı |
| siparişlerinde | sipariş | `-lerinde` eki kaldırıldı |

Ayrıca stop words filtreleme yapılır: `ve`, `veya`, `ile`, `için`, `bir`, `bu`, `ne`, `nasıl` vb.

### Fallback Mekanizması

Neo4j devre dışıysa veya tablo bulunamazsa, `DynamicPromptBuilder` otomatik olarak static prompt'a fallback yapar:

```csharp
if (!_settings.Enabled || result.TableCount == 0)
{
    result.UsedFallback = true;
    result.Prompt = basePromptTemplate ?? string.Empty;
}
```

---

## 🌐 API Endpoints

Tüm endpoint'ler `/api/v1/neo4j` prefix'i altında tanımlıdır.

### Health & İstatistikler

| Method | Path | Açıklama |
|--------|------|----------|
| `GET` | `/health` | Neo4j bağlantı durumunu kontrol eder |
| `GET` | `/stats` | Schema Catalog istatistikleri (şema, tablo, kolon, FK sayıları) |

### Şema Sorguları

| Method | Path | Açıklama |
|--------|------|----------|
| `GET` | `/schemas` | Tüm şemaları listeler |
| `GET` | `/schemas/{schemaName}/tables` | Belirtilen şemadaki tabloları listeler |
| `GET` | `/tables/{tableName}` | Tablo şema bilgisini getirir (kolonlar dahil) |

### Arama İşlemleri

| Method | Path | Parametreler | Açıklama |
|--------|------|-------------|----------|
| `GET` | `/search/tables` | `query`, `maxResults` | Kullanıcı sorgusuna göre ilgili tabloları arar (full-text) |
| `GET` | `/search/columns` | `alias` | Türkçe alias ile kolon arar |
| `GET` | `/search/tables-by-source` | `query`, `source`, `maxResults` | Belirli bir kaynakta tablo arar |

### JOIN Path

| Method | Path | Parametreler | Açıklama |
|--------|------|-------------|----------|
| `GET` | `/join-path` | `table1`, `table2` | İki tablo arasındaki en kısa JOIN path'i bulur |

### Dinamik Prompt

| Method | Path | Parametreler | Açıklama |
|--------|------|-------------|----------|
| `GET` | `/dynamic-prompt` | `query` | Kullanıcı sorgusuna göre dinamik şema prompt'u oluşturur |

### Import İşlemleri

| Method | Path | Açıklama |
|--------|------|----------|
| `POST` | `/import/parse` | Markdown dosyasını parse eder |
| `POST` | `/import/execute` | Parse edilen şemayı Neo4j'ye import eder |
| `GET` | `/import/cypher-script` | Cypher import script'ini döndürür |
| `POST` | `/import/refresh` | Mevcut şemayı silip yeniden import eder (Force Refresh) |
| `DELETE` | `/import/clear` | Neo4j'deki tüm şema verilerini siler |
| `POST` | `/import/from-database` | SQL Server'dan şema çeker ve import eder |

### Çoklu Kaynak (Multi-Source)

| Method | Path | Açıklama |
|--------|------|----------|
| `GET` | `/sources` | Mevcut tüm veri kaynaklarını listeler |
| `GET` | `/sources/{source}/tables` | Kaynağa ait tabloları listeler |
| `GET` | `/sources/{source}/schemas` | Kaynağa ait şemaları listeler |
| `GET` | `/sources/{source}/stats` | Kaynak istatistiklerini getirir |
| `DELETE` | `/sources/{source}` | Kaynağa ait tüm verileri siler |

---

## 📁 Dosya Yapısı

```
backend/
├── AI.Application/
│   ├── Configuration/
│   │   └── Neo4jSettings.cs                    # Ayarlar + SchemaSourceType enum
│   ├── DTOs/
│   │   ├── Neo4j/
│   │   │   ├── TableInfo.cs                    # Tablo bilgisi DTO
│   │   │   ├── ColumnInfo.cs                   # Kolon bilgisi DTO
│   │   │   ├── TableSchema.cs                  # Tablo şeması (kolonlar dahil)
│   │   │   ├── JoinPath.cs                     # JOIN yol bilgisi + JoinInfo
│   │   │   └── SchemaParseResult.cs            # Parse sonucu + SchemaInfo + FK
│   │   ├── SchemaCatalog/
│   │   │   └── SchemaCatalogStats.cs           # İstatistik DTO
│   │   └── DynamicPrompt/
│   │       └── DynamicPromptResult.cs          # Dinamik prompt sonucu
│   └── Ports/Secondary/Services/
│       ├── Database/
│       │   ├── ISchemaGraphService.cs          # Neo4j CRUD + Search port
│       │   ├── ISchemaParserService.cs         # Markdown parser port
│       │   └── IDatabaseSchemaReader.cs        # SQL Server schema reader port
│       └── AIChat/
│           └── IDynamicPromptBuilder.cs        # Dinamik prompt builder port
│
├── AI.Infrastructure/
│   ├── Adapters/AI/Neo4j/
│   │   ├── SchemaGraphService.cs               # Neo4j driver ile graph CRUD (1015 satır)
│   │   ├── SchemaParserService.cs              # Markdown → SchemaParseResult (775 satır)
│   │   ├── DynamicPromptBuilder.cs             # Kullanıcı sorgusu → dinamik prompt (343 satır)
│   │   └── DatabaseSchemaReader.cs             # SQL Server → SchemaParseResult
│   └── Extensions/
│       └── Neo4jExtensions.cs                  # DI, initialization, health check (467 satır)
│
└── AI.Api/
    └── Endpoints/Search/
        └── Neo4jEndpoints.cs                   # 20+ REST endpoint (939 satır)
```

---

## ⚙ Konfigürasyon

### appsettings.json

```json
{
  "Neo4j": {
    "Enabled": false,
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "<YOUR_NEO4J_PASSWORD>",
    "Database": "neo4j",
    "MaxConnectionPoolSize": 100,
    "ConnectionAcquisitionTimeout": "00:01:00",
    "SchemaSource": "Markdown",
    "MarkdownSchemaFile": "adventurerworks_schema.md",
    "DatabaseConnectionName": "AdventureWorks2022",
    "AliasConfigFile": "alias_config_adventureworks.json",
    "DefaultSource": "AdventureWorks",
    "MaxRelevantTables": 6,
    "MinRelevanceScore": 0.3,
    "MaxJoinHops": 4
  }
}
```

### Ayar Açıklamaları

| Ayar | Tip | Varsayılan | Açıklama |
|------|-----|-----------|----------|
| `Uri` | string | `bolt://localhost:7687` | Neo4j Bolt protokol URI |
| `Username` | string | `neo4j` | Kullanıcı adı |
| `Password` | string | — | Şifre |
| `Database` | string | `neo4j` | Veritabanı adı |
| `MaxConnectionPoolSize` | int | `100` | Maksimum bağlantı havuzu |
| `ConnectionAcquisitionTimeout` | TimeSpan | `00:01:00` | Bağlantı edinme timeout |
| `Enabled` | bool | `false` | Schema Catalog aktif mi? |
| `SchemaSource` | enum | `Markdown` | Şema kaynağı: `Markdown` veya `Database` |
| `MarkdownSchemaFile` | string | `adventurerworks_schema.md` | Markdown dosya adı |
| `DatabaseConnectionName` | string | `AdventureWorks2022` | DB connection string key |
| `AliasConfigFile` | string | `alias_config_adventureworks.json` | Türkçe alias JSON dosyası |
| `DefaultSource` | string | `AdventureWorks` | Varsayılan kaynak adı |
| `MaxRelevantTables` | int | `6` | Dinamik prompt'a dahil edilecek max tablo |
| `MinRelevanceScore` | double | `0.3` | Full-text search minimum skor |
| `MaxJoinHops` | int | `4` | Shortest path max hop sayısı |

### Docker Compose

```yaml
neo4j:
  image: neo4j:5-community
  container_name: neo4j
  ports:
    - "7474:7474"   # Browser UI
    - "7687:7687"   # Bolt protocol
  environment:
    - NEO4J_AUTH=neo4j/<YOUR_PASSWORD>
  volumes:
    - neo4j_data:/data
```

---

## 🔗 Hexagonal Architecture Uyumu

### Port'lar (AI.Application)

```csharp
// Driven Port — Neo4j graph CRUD operasyonları
public interface ISchemaGraphService
{
    Task<IEnumerable<TableInfo>> FindRelevantTablesAsync(string userQuery, int maxResults);
    Task<JoinPath?> FindJoinPathAsync(string table1, string table2);
    Task<string> GenerateDynamicSchemaPromptAsync(string userQuery);
    Task<bool> TestConnectionAsync();
    Task<SchemaCatalogStats> GetStatsAsync();
    // ... + source-aware methods
}

// Driven Port — Markdown/dosya parse
public interface ISchemaParserService
{
    SchemaParseResult ParseMarkdown(string markdownContent);
    Task<SchemaParseResult> ParseFromResourceAsync(string resourceName);
    string GenerateCypherImportScript(SchemaParseResult parseResult);
}

// Driven Port — Dinamik prompt builder
public interface IDynamicPromptBuilder
{
    Task<DynamicPromptResult> BuildPromptAsync(string userQuery, string? basePromptTemplate);
    Task<IEnumerable<string>> ExtractKeywordsAsync(string userQuery);
}

// Driven Port — SQL Server'dan şema okuma
public interface IDatabaseSchemaReader
{
    Task<SchemaParseResult> ReadSchemaFromDatabaseAsync(string connStr, string source, AliasConfiguration? alias);
    Task<bool> TestConnectionAsync(string connectionString);
}
```

### Adapter'lar (AI.Infrastructure)

| Port | Adapter | Yaşam Süresi |
|------|---------|-------------|
| `ISchemaGraphService` | `SchemaGraphService` | Singleton |
| `ISchemaParserService` | `SchemaParserService` | Singleton |
| `IDynamicPromptBuilder` | `DynamicPromptBuilder` | Singleton |
| `IDatabaseSchemaReader` | `DatabaseSchemaReader` | Singleton |

### DI Kayıt (Neo4jExtensions.cs)

```csharp
public static IServiceCollection AddNeo4jSchemaCatalog(
    this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<Neo4jSettings>(configuration.GetSection("Neo4j"));
    
    services.AddSingleton<ISchemaParserService, SchemaParserService>();
    services.AddSingleton<ISchemaGraphService, SchemaGraphService>();
    services.AddSingleton<IDynamicPromptBuilder, DynamicPromptBuilder>();
    services.AddSingleton<IDatabaseSchemaReader, DatabaseSchemaReader>();

    // Health Check
    if (settings?.Enabled == true)
    {
        services.AddHealthChecks()
            .AddCheck<Neo4jHealthCheck>("neo4j", 
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "db", "neo4j", "graph" });
    }
}
```

---

## 🔄 Veri Akışı

### Rapor Üretiminde Neo4j'nin Rolü (End-to-End)

```
┌──────────────┐    ┌──────────────┐    ┌──────────────────────┐
│   Kullanıcı  │    │  SignalR Hub │    │ RouteConversation    │
│   "Satış     │───►│              │───►│ UseCase              │
│   raporu"    │    │              │    │ (→ ReportActionAgent)│
└──────────────┘    └──────────────┘    └──────────┬───────────┘
                                                   │
                                                   ▼
                                        ┌──────────────────────┐
                                        │ AdventureWorks       │
                                        │ ReportService        │
                                        │ (extends Base)       │
                                        └──────────┬───────────┘
                                                   │
                    ┌──────────────────────────────┼────────────────────┐
                    │                              │                    │
                    ▼                              ▼                    ▼
         ┌──────────────────┐         ┌──────────────────┐  ┌────────────────┐
         │ DynamicPrompt    │         │ SQL Agent        │  │ Dashboard      │
         │ Builder          │         │ Pipeline         │  │ Generator      │
         │ ════════════════ │         │ ════════════════ │  │                │
         │ 1. Keyword çıkar │         │ SQL üret → çalış │  │ HTML rapor     │
         │ 2. Neo4j search  │         │ tır → sonuç al   │  │ oluştur        │
         │ 3. JOIN path bul │         │                  │  │                │
         │ 4. V2 template   │         │                  │  │                │
         │    doldur        │         │                  │  │                │
         └────────┬─────────┘         └──────────────────┘  └────────────────┘
                  │
                  │ DynamicPromptResult
                  │ (sadece ilgili tablolar
                  │  + JOIN bilgileri)
                  ▼
         ┌──────────────────┐
         │ LLM (GPT-5.2)    │
         │ ════════════════ │
         │ Optimize edilmiş │
         │ prompt ile SQL   │
         │ üretimi          │
         └──────────────────┘
```

---

## 🔍 Cypher Sorgu Referansı

### Full-text Tablo Arama

```cypher
CALL db.index.fulltext.queryNodes('table_search', $query) 
YIELD node, score
WHERE score > $minScore
RETURN node.name AS name, 
       node.fullName AS fullName,
       node.schema AS schema,
       node.description AS description,
       score
ORDER BY score DESC
LIMIT $limit
```

### Keyword Tabanlı Arama

```cypher
CALL db.index.fulltext.queryNodes('table_search', $searchQuery) 
YIELD node, score
WHERE score > 0.1
WITH node, score
ORDER BY score DESC
LIMIT $limit
RETURN node.name AS name, 
       node.fullName AS fullName,
       node.description AS description,
       score
```

### Shortest Path (JOIN Yolu Bulma)

```cypher
MATCH (t1:Table {fullName: $table1}), (t2:Table {fullName: $table2})
MATCH path = shortestPath((t1)-[:JOINS_WITH*1..4]-(t2))
RETURN [n IN nodes(path) | n.fullName] AS tables,
       [r IN relationships(path) | {
           from: startNode(r).fullName,
           to: endNode(r).fullName,
           via: r.via,
           fkColumn: r.fkColumn
       }] AS joins
```

### Tablo Şeması Getirme

```cypher
MATCH (t:Table {fullName: $tableName})-[:HAS_COLUMN]->(c:Column)
RETURN t.name AS tableName,
       t.fullName AS fullName,
       t.schema AS schema,
       t.description AS description,
       t.type AS type,
       collect({
           name: c.name,
           dataType: c.dataType,
           alias: c.alias,
           description: c.description,
           isPrimaryKey: c.isPrimaryKey,
           isForeignKey: c.isForeignKey,
           fkTable: c.fkTable,
           fkColumn: c.fkColumn
       }) AS columns
```

### Tablo & Kolon Görüntüleme

```cypher
// Bir tablonun kolonlarını liste olarak getir
MATCH (t:Table {fullName: 'Sales.Customer'})-[:HAS_COLUMN]->(c:Column)
RETURN c.name AS Column, c.dataType AS DataType, c.alias AS Alias, c.description AS Description
ORDER BY c.name
```

```cypher
// Tablo + kolonları graf olarak görüntüle
MATCH (t:Table {fullName: 'Sales.Customer'})-[r:HAS_COLUMN]->(c:Column)
RETURN t, r, c
```

```cypher
// Tablo + kolonları + FK referanslarını graf olarak görüntüle
MATCH (t:Table {fullName: 'Sales.Customer'})-[r:HAS_COLUMN]->(c:Column)
OPTIONAL MATCH (c)-[ref:REFERENCES]->(fk:Table)
RETURN t, r, c, ref, fk
```

### İstatistik Sorgulama

```cypher
MATCH (s:Schema) WITH count(s) AS schemaCount
MATCH (t:Table) WITH schemaCount, 
    count(CASE WHEN t.type = 'Table' THEN 1 END) AS tableCount,
    count(CASE WHEN t.type = 'View' THEN 1 END) AS viewCount
OPTIONAL MATCH (t2:Table)-[:HAS_COLUMN]->(c:Column)
WITH schemaCount, tableCount, viewCount, count(DISTINCT c) AS columnCount
OPTIONAL MATCH ()-[j:JOINS_WITH]->()
RETURN schemaCount, tableCount, viewCount, columnCount, count(j) AS fkCount
```

### Tüm Verileri Silme

```cypher
MATCH ()-[r:REFERENCES]->() DELETE r;
MATCH ()-[r:JOINS_WITH]->() DELETE r;
MATCH ()-[r:HAS_COLUMN]->() DELETE r;
MATCH ()-[r:CONTAINS]->() DELETE r;
MATCH (c:Column) DELETE c;
MATCH (t:Table) DELETE t;
MATCH (s:Schema) DELETE s;
DROP INDEX table_search IF EXISTS;
DROP INDEX column_search IF EXISTS;
```

---

## 🏥 Health Check & İzleme

### Neo4jHealthCheck

`Neo4jHealthCheck` sınıfı, ASP.NET Core Health Check altyapısına entegre edilmiştir:

```
GET /health

Sağlıklı Yanıt:
{
  "status": "Healthy",
  "entries": {
    "neo4j": {
      "status": "Healthy",
      "description": "Neo4j Schema Catalog is healthy",
      "data": {
        "schemas": 5,
        "tables": 71,
        "views": 20,
        "columns": 916,
        "foreignKeys": 89
      }
    }
  }
}

Sorunlu Yanıt:
{
  "status": "Degraded",
  "entries": {
    "neo4j": {
      "status": "Degraded",
      "description": "Neo4j connection failed"
    }
  }
}
```

Health check `Degraded` olarak yapılandırılmıştır — Neo4j bağlantısı kopsa bile uygulama çalışmaya devam eder (fallback mekanizması sayesinde).

---

## 🌍 Çoklu Veritabanı Desteği (Multi-Source)

Sistem, birden fazla SQL Server veritabanını tek bir Neo4j graph'ında saklayabilir. Her veri kaynağı `source` property'si ile ayrılır:

```
┌──────────────────────────────────────────────────────────┐
│                      Neo4j Graph                         │
│                                                          │
│  ┌────────────────────┐    ┌────────────────────┐        │
│  │ source: Adventure  │    │ source: Northwind  │        │
│  │   Works            │    │                    │        │
│  │                    │    │                    │        │
│  │ Sales.Customer ────┼────┼─ Customers         │        │
│  │ Sales.SalesOrder   │    │ Orders             │        │
│  │ Production.Product │    │ Products           │        │
│  └────────────────────┘    └────────────────────┘        │
└──────────────────────────────────────────────────────────┘
```

### Mevcut Konfigürasyon (Tek Kaynak)

Varsayılan olarak sistem tek bir veritabanı kaynağı (`DefaultSource`) ile çalışır. Mevcut `appsettings.json`:

```json
{
  "Neo4j": {
    "Enabled": false,    
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "Ahmet1990.",
    "Database": "neo4j",
    "MaxConnectionPoolSize": 100,
    "ConnectionAcquisitionTimeout": "00:01:00",
    "SchemaSource": "Markdown",
    "MarkdownSchemaFile": "adventurerworks_schema.md",
    "DatabaseConnectionName": "AdventureWorks2022",
    "AliasConfigFile": "alias_config_adventureworks.json",
    "DefaultSource": "AdventureWorks",
    "MaxRelevantTables": 6,
    "MinRelevanceScore": 0.3,
    "MaxJoinHops": 4
  }
}
```

### Source-aware API Kullanımı

```
GET /api/v1/neo4j/sources
→ ["AdventureWorks", "Northwind"]

GET /api/v1/neo4j/search/tables-by-source?query=müşteri&source=AdventureWorks
→ [{ "name": "Customer", "fullName": "Sales.Customer", "score": 2.8 }]

DELETE /api/v1/neo4j/sources/Northwind
→ Northwind'e ait tüm node ve relationship'ler silinir
```

---

## 📊 Performans ve Ölçeklenebilirlik

| Metrik | Değer |
|--------|-------|
| **Full-text search** | < 10ms (Lucene index) |
| **Shortest path (4 hop)** | < 5ms |
| **Dinamik prompt oluşturma** | < 50ms (Neo4j + template) |
| **Connection pool** | 100 eşzamanlı bağlantı |
| **AdventureWorks graph boyutu** | ~71 tablo, ~900 kolon, ~90 FK |
| **Token tasarrufu** | ~%85-90 (tam şema vs. dinamik prompt) |

---

## 🧩 Entegrasyon Noktaları

| Bileşen | Neo4j Kullanımı |
|---------|----------------|
| `SqlServerReportServiceBase` | `IDynamicPromptBuilder.BuildPromptAsync()` ile her rapor sorgusunda dinamik prompt alır |
| `AdventureWorksReportService` | Üst sınıftan miras, Neo4j'den dinamik şema bilgisi kullanır |
| `SocialMediaReportService` | Üst sınıftan miras, Neo4j'den dinamik şema bilgisi kullanır |
| `Neo4jEndpoints` | REST API ile şema yönetimi, arama, import işlemleri |
| `Neo4jHealthCheck` | ASP.NET Core health monitoring entegrasyonu |
| `DependencyInjectionExtensions` | Startup'ta `AddNeo4jSchemaCatalog()` ile DI kaydı |

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Report-System.md](Report-System.md) | SQL rapor sistemi (Neo4j dinamik prompt kullanımı) |
| [Multi-Agent.md](Multi-Agent.md) | SQL Agent pipeline |
| [System-Overview.md](System-Overview.md) | Genel sistem mimarisi |
| [Hexagonal-Architecture.md](Hexagonal-Architecture.md) | Port/Adapter yapısı |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
