# 🔍 Qdrant Vector Embedding & Search Sistemi - Detaylı Analiz

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Temel Kavramlar](#temel-kavramlar)
- [Mimari Yapı](#mimari-yapı)
- [Dosya Yapısı](#dosya-yapısı)
- [Vector Embedding Süreci](#vector-embedding-süreci)
- [Hybrid Search (Dense + Sparse)](#hybrid-search-dense--sparse)
- [HyDE (Hypothetical Document Embeddings)](#hyde-hypothetical-document-embeddings)
- [Qdrant Service Detayları](#qdrant-service-detayları)
- [Konfigürasyon](#konfigürasyon)
- [Akış Diyagramları](#akış-diyagramları)
- [Örnek Senaryolar](#örnek-senaryolar)

---

## Genel Bakış

Proje, **Retrieval-Augmented Generation (RAG)** için Qdrant vector database kullanıyor. Sistem şu özelliklere sahip:

| Özellik | Teknoloji | Açıklama |
|---------|-----------|----------|
| **Dense Embeddings** | OpenAI text-embedding-3-large | Semantik benzerlik için 3072 boyutlu vektörler |
| **Sparse Vectors** | Lucene.NET Turkish Analyzer | BM25-style keyword matching |
| **Hybrid Search** | Qdrant RRF Fusion | Dense + Sparse kombinasyonu |
| **HyDE** | GPT-5.2 | Hypothetical Document Embeddings |
| **HNSW Index** | Qdrant Native | Hızlı approximate nearest neighbor arama |

---

## Temel Kavramlar

> 💡 **Bu bölüm**, Dense, Sparse ve HyDE kavramlarını teknik olmayan bir dille açıklar. Bu kavramları anlamak, sistemin nasıl çalıştığını kavramak için önemlidir.

### 🧠 Dense Vector (Yoğun Vektör) Nedir?

**Dense Vector**, bir metnin anlamını yakalayan sayısal bir temsildir. Bunu şöyle düşünebilirsiniz:

```
📝 Metin: "Bugün hava çok güzel"
      ↓
🔢 Dense Vector: [0.23, -0.45, 0.12, 0.89, ..., -0.34]  (3072 sayı)
```

**Nasıl Çalışır?**

- OpenAI'nin yapay zeka modeli (text-embedding-3-large), metni okur ve 3072 adet sayıdan oluşan bir dizi üretir
- Bu sayılar, metnin "anlamını" matematiksel olarak temsil eder
- Benzer anlama sahip metinler, birbirine yakın sayı dizileri üretir

**Örnek:**

```
"araba" → [0.2, 0.5, 0.1, ...]
"otomobil" → [0.21, 0.49, 0.11, ...]  ← Çok benzer!
"elma" → [0.8, -0.3, 0.6, ...]  ← Farklı
```

**Avantajı:** Eş anlamlı kelimeleri ve benzer kavramları bulabilir.
**Dezavantajı:** Kesin kelime eşleşmesi (örn. "ISO-27001") zayıf kalabilir.

---

### 📑 Sparse Vector (Seyrek Vektör) Nedir?

**Sparse Vector**, geleneksel anahtar kelime aramasının modern versiyonudur. "Seyrek" denmesinin nedeni, çoğu değerin sıfır olmasıdır.

```
📝 Metin: "Komisyon oranları vadeli işlemlerde"
      ↓
📊 Sparse Vector:
   - "komisyon" pozisyonu: 2.3 (önemli kelime)
   - "oran" pozisyonu: 1.8
   - "vadeli" pozisyonu: 1.5
   - "işlem" pozisyonu: 2.1
   - Diğer 499,996 pozisyon: 0 (boş)
```

**Nasıl Çalışır?**

- Lucene.NET kütüphanesi, Türkçe metni kelimelere ayırır
- Her kelimeye BM25 algoritmasıyla bir önem skoru verir
- Yaygın kelimeler ("bir", "ve", "için") filtrelenir (stopwords)
- Kelime kökleri çıkarılır: "komisyonları" → "komisyon"

**Avantajı:** Kesin kelime eşleşmesinde çok güçlü ("ISO-27001" → "ISO-27001").
**Dezavantajı:** Eş anlamlıları bulamaz ("araba" ≠ "otomobil").

---

### 🔮 HyDE (Hypothetical Document Embeddings) Nedir?

**HyDE**, arama kalitesini artırmak için kullanılan akıllı bir tekniktir. Kullanıcının sorusunu, olası bir cevaba dönüştürür.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         HyDE NE YAPAR?                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Kullanıcı Sorusu: "komisyon oranı nedir?"                                  │
│                                                                             │
│  Problem: Soru kısa ve soyut. Veritabanındaki detaylı dokümanlarla          │
│           pek eşleşmiyor.                                                   │
│                                                                             │
│  HyDE Çözümü:                                                               │
│  GPT, soruya varsayımsal bir cevap üretir:                                  │
│                                                                             │
│  "Komisyon oranı, finansal aracı kurumların müşterilerine sağladıkları      │
│   işlem hizmetleri karşılığında aldıkları ücret yüzdesidir. Genellikle      │
│   işlem tutarının %0.1 ile %1 arasında değişir. Vadeli işlemler             │
│   piyasasında komisyon oranları pay piyasasına göre farklılık gösterir."    │
│                                                                             │
│  Bu varsayımsal cevap, gerçek dokümanlarla çok daha iyi eşleşir!            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Neden Çalışır?**

- Sorular genelde kısa ve soyuttur
- Cevaplar/dokümanlar detaylı ve spesifiktir
- HyDE, soruyu cevap formatına dönüştürerek "köprü" görevi görür

**Sistemdeki Kullanımı:**

1. Kullanıcı sorusu alınır
2. GPT, varsayımsal bir cevap üretir (HyDE)
3. Hem soru hem de varsayımsal cevap için embedding oluşturulur
4. İki aramanın sonuçları ağırlıklı olarak birleştirilir (HyDE: %60, Soru: %40)

---

### 🎯 Hybrid Search Nedir?

**Hybrid Search**, Dense ve Sparse vektörleri birlikte kullanarak en iyi sonucu bulmaya çalışır.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         HYBRID SEARCH                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Kullanıcı Sorusu: "ISO-27001 güvenlik sertifikası"                         │
│                                                                             │
│  Dense (Semantik):                         Sparse (Anahtar Kelime):         │
│  "güvenlik standardı" → Sonuç A (0.85)     "ISO-27001" → Sonuç B (0.92)     │
│  "bilgi güvenliği" → Sonuç C (0.82)        "sertifika" → Sonuç A (0.78)     │
│                                                                             │
│  RRF Fusion (Birleştirme):                                                  │
│  Sonuç A: Dense'de 1. + Sparse'da 2. = En yüksek skor                       │
│  Sonuç B: Sparse'da 1. = İkinci sıra                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Ağırlıklar:**

- Dense (Semantik): %70
- Sparse (Anahtar Kelime): %30

---

### Neden Hybrid Search?

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PURE SEMANTIC vs HYBRID SEARCH                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Pure Semantic (Dense Only):                                                │
│  ✅ Eş anlamlıları yakalar ("araba" → "otomobil")                           │
│  ❌ Exact keyword matching zayıf ("ISO-27001" → "güvenlik standardı" ?)     │
│                                                                             │
│  Keyword Matching (Sparse Only):                                            │
│  ✅ Exact term matching güçlü ("ISO-27001" → "ISO-27001")                   │
│  ❌ Semantik benzerliği yakalayamaz                                         │
│                                                                             │
│  Hybrid (Dense + Sparse):                                                   │
│  ✅ Her iki avantajı birleştirir                                            │
│  ✅ RRF Fusion ile optimal sıralama                                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Mimari Yapı

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          RAG SYSTEM ARCHITECTURE                            │
└─────────────────────────────────────────────────────────────────────────────┘

                           DOCUMENT INGESTION
                           ═══════════════════
                                   │
┌──────────────────┐               ▼
│   PDF/TXT/JSON   │ ───► ┌────────────────────────────────────────────────┐
│     Upload       │      │        DocumentProcessingUseCase               │
└──────────────────┘      │                                                │
                          │  1. Parse (PDF, TXT, JSON)                     │
                          │  2. Chunk (Semantic chunking)                  │
                          │  3. Encoding fix (Turkish chars)               │
                          │  4. Generate Dense Embedding (OpenAI)          │
                          │  5. Generate Sparse Vector (BM25)              │
                          │  6. Upsert to Qdrant                           │
                          │                                                │
                          └────────────────────┬───────────────────────────┘
                                               │
                                               ▼
                          ┌────────────────────────────────────────────────┐
                          │              QDRANT VECTOR DB                  │
                          │                                                │
                          │  Collection: {document}_documents              │
                          │  ┌──────────────────────────────────────────┐  │
                          │  │ Named Vectors:                           │  │
                          │  │   • dense: float[3072] (HNSW indexed)    │  │
                          │  │   • sparse: (indices[], values[])        │  │
                          │  │                                          │  │
                          │  │ Payload:                                 │  │
                          │  │   • document_id, chunk_id                │  │
                          │  │   • content, metadata                    │  │
                          │  │   • fileName, category, title            │  │
                          │  └──────────────────────────────────────────┘  │
                          └────────────────────────────────────────────────┘

                           SEARCH / RETRIEVAL
                           ═══════════════════
                                   │
┌──────────────────┐               ▼
│   User Query     │ ───► ┌────────────────────────────────────────────────┐
│ "komisyon oranı" │      │           RagSearchUseCase                     │
└──────────────────┘      │                                                │
                          │  1. Spelling Correction (LLM)                  │
                          │  2. HyDE Generation (LLM)                      │
                          │  3. Generate Embeddings:                       │
                          │     • Corrected query embedding                │
                          │     • HyDE document embedding                  │
                          │     • Sparse vector (BM25)                     │
                          │  4. Hybrid Search (Qdrant RRF)                 │
                          │  5. Weighted Merge Results                     │
                          │  6. Highlight & Return                         │
                          │                                                │
                          └────────────────────────────────────────────────┘
```

---

## Dosya Yapısı

```
AI.Application/
├── Configuration/
│   └── QdrantSettings.cs              # Qdrant yapılandırma (147 satır)
│
├── Common/Constants/
│   └── QdrantCollections.cs           # Collection adı helper (101 satır)
│
├── UseCases/
│   ├── RagSearchUseCase.cs            # HyDE + Hybrid search (777 satır)
│   └── DocumentProcessingUseCase.cs   # Doküman işleme (731 satır)
│
├── Ports/Secondary/Services/
│   └── Vector/
│       ├── IQdrantService.cs          # Interface (133 satır)
│       ├── IEmbeddingService.cs       # Interface (33 satır)
│       └── ISparseVectorService.cs    # BM25 interface
│
AI.Infrastructure/Adapters/AI/VectorServices/
├── QdrantService.cs                   # Qdrant implementasyonu (810 satır)
├── OpenAIEmbeddingService.cs          # OpenAI embedding (141 satır)
└── SparseVectorService.cs             # BM25 sparse vector (138 satır)

AI.Domain/Documents/
└── DocumentChunk.cs                   # Chunk entity (185 satır)
```

---

## Vector Embedding Süreci

### 1️⃣ Dense Embedding (OpenAI)

**Dosya:** `OpenAIEmbeddingService.cs`

OpenAI'nin `text-embedding-3-large` modeli kullanılarak semantik embedding oluşturulur.

```csharp
public class OpenAIEmbeddingService : IEmbeddingService
{
    public int EmbeddingDimension => 3072;  // text-embedding-3-large
    public string ModelName => "text-embedding-3-large";

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        // 1. Metni temizle (Türkçe karakter fix + boşluk normalize)
        var cleanedText = CleanText(text);
        
        // 2. OpenAI API çağrısı
        var embeddings = await _embeddingService.GenerateAsync([cleanedText], null, ct);
        
        // 3. Float array olarak döndür
        return embeddings.First().Vector.ToArray();
    }
    
    private static string CleanText(string text)
    {
        // Türkçe encoding fix
        var corrected = TurkishEncodingHelper.FixEncoding(text);
        
        // Fazla boşlukları temizle
        var cleaned = Regex.Replace(corrected.Trim(), @"\s+", " ");
        
        // Max 8000 karakter (token limiti için)
        if (cleaned.Length > 8000)
            cleaned = cleaned.Substring(0, 8000);
            
        return cleaned;
    }
}
```

**Embedding Özellikleri:**

| Parametre | Değer | Açıklama |
|-----------|-------|----------|
| Model | text-embedding-3-large | OpenAI embedding modeli |
| Boyut | 3072 | Vector dimension |
| Max Input | ~8000 karakter | Token limit (~2000 token) |
| Normalizasyon | L2 | Cosine similarity için |

---

### 2️⃣ Sparse Vector (BM25)

**Dosya:** `SparseVectorService.cs`

Lucene.NET Turkish Analyzer kullanarak BM25-style sparse vector oluşturur.

```csharp
public class SparseVectorService : ISparseVectorService
{
    private const uint VocabularySize = 500000;  // Hash collision minimize
    private const float DefaultIDF = 1.5f;       // Stateless IDF
    
    public (uint[] indices, float[] values) GenerateSparseVector(string text)
    {
        var termFrequency = new Dictionary<string, int>();
        
        // 1. Lucene Turkish Analyzer ile tokenize
        using var analyzer = new TurkishAnalyzer(LuceneVersion.LUCENE_48);
        using var stream = analyzer.GetTokenStream("content", new StringReader(text));
        
        // 2. Term frequency hesapla (stopwords hariç)
        while (stream.IncrementToken())
        {
            var term = termAttr.ToString().ToLowerInvariant();
            if (term.Length >= 3 && !TurkishStopwords.Contains(term))
            {
                termFrequency[term] = termFrequency.GetValueOrDefault(term, 0) + 1;
            }
        }
        
        // 3. BM25 scoring
        // Score = IDF * (tf * (k1 + 1)) / (tf + k1 * lengthNorm)
        foreach (var (term, tf) in termFrequency)
        {
            var termIndex = GetTermIndex(term);  // FNV-1a hash
            var bm25Score = CalculateBM25(tf, docLength);
            indices.Add(termIndex);
            values.Add(bm25Score);
        }
        
        return (indices.ToArray(), values.ToArray());
    }
    
    // Deterministic hash (restart-proof, distributed-safe)
    private static uint GetTermIndex(string term)
    {
        unchecked
        {
            uint hash = 2166136261;  // FNV offset basis
            foreach (char c in term.ToLowerInvariant())
            {
                hash ^= c;
                hash *= 16777619;    // FNV prime
            }
            return hash % VocabularySize;
        }
    }
}
```

**Sparse Vector Özellikleri:**

| Parametre | Değer | Açıklama |
|-----------|-------|----------|
| Vocabulary Size | 500,000 | Hash space (collision ~5%) |
| Min Term Length | 3 karakter | Kısa kelimeler atlanır |
| Stopwords | 70+ Türkçe | Anlamsız kelimeler filtrelenir |
| Scoring | BM25 | k1=1.5, b=0.75 |
| Hash Algorithm | FNV-1a | Deterministic, hızlı |

**Türkçe Stopwords Örneği:**

```
"bir", "bu", "şu", "o", "ve", "veya", "ama", "fakat", "için", "ile",
"gibi", "kadar", "daha", "çok", "az", "en", "mi", "mı", "mu", "mü"...
```

---

### 3️⃣ Doküman İşleme Pipeline

**Dosya:** `DocumentProcessingUseCase.cs`

```csharp
public async Task<DocumentProcessingResult> ProcessDocumentAsync(
    DocumentMetadata metadata, Stream fileStream, CancellationToken ct)
{
    // 1. Türkçe karakter encoding fix
    FixDocumentMetadataEncoding(metadata);
    
    // 2. Dosyayı kaydet
    var savedPath = await SaveFileToUploadsAsync(fileStream, metadata, ct);
    
    // 3. Collection adını belirle
    var collectionName = QdrantCollections.GetCollectionName(metadata.FileName);
    // "Satış Raporu.pdf" → "satis_raporu_documents"
    
    // 4. Duplicate check (file hash ile)
    var existing = await _qdrantService.SearchAsync(collectionName, 
        dummyVector, 1, 0.0f, 
        new Dictionary<string, object> { ["fileHash"] = metadata.FileHash }, ct);
    if (existing.Any()) return DuplicateError();
    
    // 5. Parse & Chunk
    List<DocumentChunk> chunks;
    if (metadata.DocumentType == DocumentType.QuestionAnswer)
    {
        // JSON Q&A parser
        chunks = jsonParser.ParseQuestionsAnswers(fileStream, metadata.Id);
    }
    else
    {
        // PDF/TXT parser + semantic chunking
        var text = await parser.ExtractTextAsync(fileStream, metadata.FileName, ct);
        chunks = _textChunker.ChunkTextSemantic(text, metadata.Id);
    }
    
    // 6. Her chunk için sparse vector oluştur
    foreach (var chunk in chunks)
    {
        chunk.Content = TurkishEncodingHelper.FixEncoding(chunk.Content);
        var sparseVector = _sparseVectorService.GenerateSparseVectorResult(chunk.Content);
        chunk.SparseIndices = sparseVector.Indices;
        chunk.SparseValues = sparseVector.Values;
    }
    
    // 7. Dense embeddings + Qdrant upsert
    await ProcessChunksAsync(chunks, metadata, collectionName, ct);
    
    return Success(chunks.Count);
}

private async Task<int> ProcessChunksAsync(List<DocumentChunk> chunks, ...)
{
    // Batch processing (10 chunk per batch)
    for (int i = 0; i < chunks.Count; i += 10)
    {
        var batch = chunks.Skip(i).Take(10).ToList();
        
        // Dense embedding oluştur (orijinal content, stemming YOK)
        var texts = batch.Select(c => c.Content).ToList();
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, ct);
        
        // Metadata ekle
        foreach (var chunk in batch)
        {
            chunk.Metadata = JsonSerializer.Serialize(new {
                fileName = metadata.FileName,
                category = metadata.Category,
                title = metadata.Title,
                // ...
            });
        }
        
        // Qdrant'a upsert (dense + sparse)
        await _qdrantService.UpsertVectorsAsync(collectionName, batch, embeddings, ct);
    }
}
```

---

## Hybrid Search (Dense + Sparse)

### Qdrant Collection Yapısı

```csharp
// Collection oluşturma - Named vectors
await _client.CreateCollectionAsync(
    collectionName: collectionName,
    vectorsConfig: new VectorParamsMap {
        Map = {
            ["dense"] = new VectorParams {
                Size = 3072,
                Distance = Distance.Cosine,
                HnswConfig = new HnswConfigDiff {
                    M = 16,           // Bağlantı sayısı
                    EfConstruct = 200 // Index kalitesi
                }
            }
        }
    },
    sparseVectorsConfig: new SparseVectorConfig {
        Map = {
            ["sparse"] = new SparseVectorParams()
        }
    }
);
```

### Hybrid Search Implementasyonu

**Dosya:** `QdrantService.cs` (HybridSearchAsync)

```csharp
public async Task<List<SearchResult>> HybridSearchAsync(
    string collectionName,
    float[] denseVector,        // Semantic embedding
    uint[] sparseIndices,       // BM25 term indices
    float[] sparseValues,       // BM25 scores
    int limit = 10,
    float minScore = 0.0f,
    float denseWeight = 0.7f,   // Semantic ağırlık
    float sparseWeight = 0.3f,  // Keyword ağırlık
    CancellationToken ct = default)
{
    // Weight validation
    var totalWeight = denseWeight + sparseWeight;
    if (Math.Abs(totalWeight - 1.0f) > 0.01f)
    {
        // Normalize weights
        denseWeight /= totalWeight;
        sparseWeight /= totalWeight;
    }
    
    // Sparse vector tuples
    var sparseVectorTuples = sparseValues
        .Zip(sparseIndices, (value, index) => (value, index))
        .ToArray();
    
    // Qdrant RRF Fusion Query
    var queryResults = await _client.QueryAsync(
        collectionName: collectionName,
        prefetch: new[] {
            // 1. Sparse search (keyword matching)
            new PrefetchQuery {
                Query = sparseVectorTuples,
                Using = "sparse",
                Limit = (ulong)(limit * 2)
            },
            // 2. Dense search (semantic similarity)
            new PrefetchQuery {
                Query = denseVector,
                Using = "dense",
                Limit = (ulong)(limit * 2)
            }
        },
        query: new Rrf { K = 60 },  // RRF k parameter
        limit: (ulong)limit,
        scoreThreshold: 0.0f,        // RRF için düşük threshold
        cancellationToken: ct
    );
    
    return ParseResults(queryResults);
}
```

### RRF (Reciprocal Rank Fusion) Formülü

```
RRF Score = Σ (1 / (k + rank))

k = 60 (default, configurable)
rank = 1-based position in each result list

Örnek:
- Chunk A: Dense'de 1., Sparse'da 3.
  Score = 1/(60+1) + 1/(60+3) = 0.0164 + 0.0159 = 0.0323

- Chunk B: Dense'de 5., Sparse'da 1.
  Score = 1/(60+5) + 1/(60+1) = 0.0154 + 0.0164 = 0.0318

→ Chunk A daha yüksek RRF score alır
```

---

## HyDE (Hypothetical Document Embeddings)

### Konsept

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         HyDE APPROACH                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Traditional RAG:                                                           │
│  Query: "komisyon oranı nedir?"                                             │
│        ↓ embed                                                              │
│  [0.1, -0.3, 0.5, ...]  ──search──►  [Documents]                            │
│                                                                             │
│  Problem: Query embeddings ≠ Document embeddings (semantic gap)             │
│                                                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  HyDE:                                                                      │
│  Query: "komisyon oranı nedir?"                                             │
│        ↓ LLM generates hypothetical answer                                  │
│  "Komisyon oranı, aracı kurumların işlem başına aldığı ücrettir.            │
│   Genellikle %0.1 ile %1 arasında değişir. Türev ürünlerde..."              │
│        ↓ embed                                                              │
│  [0.4, 0.2, 0.7, ...]  ──search──►  [Documents]                             │
│                                                                             │
│  Benefit: Hypothetical doc embedding ≈ Actual doc embedding                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### RagSearchUseCase HyDE Implementasyonu

**Dosya:** `RagSearchUseCase.cs`

```csharp
private async Task<QueryProcessingResult> ProcessQuery(
    string query, string? documentName, CancellationToken ct)
{
    var systemPrompt = $"""
        Sen Türkçe query processing asistanısın. İki görevin var:
        
        1. SPELLING CORRECTION: Query'deki yazım hatalarını düzelt
        2. HYPOTHETICAL DOCUMENT: Bu query'ye cevap verebilecek, 
           varsayımsal bir doküman paragrafı üret
        
        HYPOTHETICAL DOCUMENT kuralları:
        - Kullanıcının sorusuna doğrudan cevap veren detaylı paragraf yaz
        - Sanki gerçek bir referans dokümanından alınmış gibi yaz
        - 50-150 kelime uzunluğunda olmalı
        - Formal, bilgilendirici ve uzman üslubu kullan
        
        Çıktını şu formatta ver:
        CORRECTED: <düzeltilmiş query>
        DOCUMENT: <hypothetical döküman paragrafı>
    """;

    var response = await _chatCompletionService.GetChatMessageContentAsync(
        chatHistory,
        new OpenAIPromptExecutionSettings {
            MaxTokens = _qdrantSettings.HyDEMaxTokens + 50,  // 400 token
            Temperature = 0.3
        },
        cancellationToken: ct);

    // Parse output
    var correctedQuery = ParseField(response, "CORRECTED:");
    var hypotheticalDoc = ParseField(response, "DOCUMENT:");

    return new QueryProcessingResult {
        OriginalQuery = query,
        CorrectedQuery = correctedQuery,
        HypotheticalDocument = hypotheticalDoc,
        ProcessingMode = "hyde"
    };
}
```

### Weighted Dual Embedding Search

```csharp
private async Task<List<SearchResult>> SearchWithWeightedEmbeddings(
    QueryProcessingResult processingResult,
    string collectionName,
    int limit,
    CancellationToken ct)
{
    // 1. Corrected query embedding
    var correctedEmbedding = await _embeddingService.GenerateEmbeddingAsync(
        processingResult.CorrectedQuery, ct);

    // 2. HyDE embedding
    var hydeText = processingResult.HypotheticalDocument;
    var hydeEmbedding = await _embeddingService.GenerateEmbeddingAsync(hydeText, ct);

    // 3. Sparse vector (keyword matching)
    var querySparseVector = _sparseVectorService.GenerateSparseVectorResult(
        processingResult.CorrectedQuery);

    // 4. Parallel hybrid searches
    var correctedTask = _qdrantService.HybridSearchAsync(
        collectionName, correctedEmbedding,
        querySparseVector.Indices, querySparseVector.Values,
        limit, minScore, denseWeight, sparseWeight, ct);

    var hydeTask = _qdrantService.HybridSearchAsync(
        collectionName, hydeEmbedding,
        querySparseVector.Indices, querySparseVector.Values,
        limit, minScore, denseWeight, sparseWeight, ct);

    var results = await Task.WhenAll(correctedTask, hydeTask);

    // 5. Weighted merge
    return MergeWeightedResults(results[0], results[1], hydeWeight: 0.6f);
}

private List<SearchResult> MergeWeightedResults(
    List<SearchResult> correctedResults,
    List<SearchResult> hydeResults,
    float hydeWeight)
{
    var mergedScores = new Dictionary<Guid, (SearchResult Result, float Score)>();

    // Corrected: (1 - 0.6) = 0.4 weight
    foreach (var result in correctedResults)
    {
        var score = result.Score * (1 - hydeWeight);
        mergedScores[result.ChunkId] = (result, score);
    }

    // HyDE: 0.6 weight
    foreach (var result in hydeResults)
    {
        var score = result.Score * hydeWeight;
        if (mergedScores.ContainsKey(result.ChunkId))
        {
            // Both searches found same chunk → boost score
            var existing = mergedScores[result.ChunkId];
            mergedScores[result.ChunkId] = (existing.Result, existing.Score + score);
        }
        else
        {
            mergedScores[result.ChunkId] = (result, score);
        }
    }

    return mergedScores
        .OrderByDescending(kvp => kvp.Value.Score)
        .Select(kvp => { kvp.Value.Result.Score = kvp.Value.Score; return kvp.Value.Result; })
        .ToList();
}
```

---

## Qdrant Service Detayları

### Collection Yönetimi

```csharp
// Collection oluştur
Task<bool> CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct);

// Collection var mı?
Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct);

// Collection sil
Task<bool> DeleteCollectionAsync(string collectionName, CancellationToken ct);

// Tüm collection'ları listele
Task<List<string>> GetCollectionsAsync(CancellationToken ct);

// Collection bilgisi al
Task<object?> GetCollectionInfoAsync(string collectionName, CancellationToken ct);
```

### Vector Upsert

```csharp
// Tek vector upsert
Task<Guid?> UpsertVectorAsync(
    string collectionName,
    DocumentChunk chunk,
    float[] vector,
    CancellationToken ct);

// Batch upsert
Task<bool> UpsertVectorsAsync(
    string collectionName,
    List<DocumentChunk> chunks,
    List<float[]> vectors,
    CancellationToken ct);
```

**Upsert Point Yapısı:**

```csharp
var point = new PointStruct
{
    Id = new PointId { Uuid = vectorId.ToString() },
    Vectors = new Vectors {
        Vectors_ = new NamedVectors {
            Vectors = {
                ["dense"] = denseVector,
                ["sparse"] = (sparseValues, sparseIndices)
            }
        }
    },
    Payload = {
        ["document_id"] = chunk.DocumentId.ToString(),
        ["chunk_id"] = chunk.Id.ToString(),
        ["content"] = chunk.Content,
        ["fileName"] = metadata.FileName,
        ["category"] = metadata.Category,
        // ...
    }
};
```

### Search Methods

```csharp
// Pure semantic search
Task<List<SearchResult>> SearchAsync(
    string collectionName,
    float[] queryVector,
    int limit = 10,
    float minScore = 0.7f,
    Dictionary<string, object>? filter = null,
    CancellationToken ct = default);

// Hybrid search (Dense + Sparse + RRF)
Task<List<SearchResult>> HybridSearchAsync(
    string collectionName,
    float[] denseVector,
    uint[] sparseIndices,
    float[] sparseValues,
    int limit = 10,
    float minScore = 0.0f,
    float denseWeight = 0.7f,
    float sparseWeight = 0.3f,
    CancellationToken ct = default);
```

### Vector Silme

```csharp
// Tek vector sil
Task<bool> DeleteVectorAsync(string collectionName, Guid vectorId, CancellationToken ct);

// Document'a ait tüm vektörleri sil
Task<int> DeleteVectorsByDocumentIdAsync(string collectionName, Guid documentId, CancellationToken ct);
```

---

## Konfigürasyon

### QdrantSettings

**Dosya:** `QdrantSettings.cs`

```csharp
public class QdrantSettings
{
    // Connection
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;        // gRPC port
    public bool UseHttps { get; set; } = false;
    public string? ApiKey { get; set; }
    
    // Collection
    public string DefaultCollection { get; set; } = "documents";
    
    // Embedding
    public string EmbeddingModel { get; set; } = "text-embedding-3-large";
    public int VectorSize { get; set; } = 3072;
    
    // Search
    public string CosineSimilarity { get; set; } = "cosine";  // cosine | euclidean | dot
    public float MinSimilarityScore { get; set; } = 0.7f;
    
    // Hybrid Search Weights
    public float DenseWeight { get; set; } = 0.7f;
    public float SparseWeight { get; set; } = 0.3f;
    
    // HyDE
    public int HyDEMaxTokens { get; set; } = 350;
    public float HyDEWeight { get; set; } = 0.6f;
    
    // HNSW
    public int HnswEf { get; set; } = 128;  // Search quality (16-512)
    
    // RRF
    public int RRF_K { get; set; } = 60;    // Fusion parameter
    
    // Timeouts
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
```

### appsettings.json Örneği

```json
{
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
  }
}
```

---

## Akış Diyagramları

### Document Ingestion Flow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    DOCUMENT INGESTION PIPELINE                               │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────┐
│ PDF Upload  │
│ "rapor.pdf" │
└──────┬──────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 1. PARSE                                                                     │
│    ┌─────────────┐                                                           │
│    │ PDFParser   │ → Extract text (UglyToad.PdfPig)                          │
│    │ TxtParser   │ → Read content                                            │
│    │ JsonParser  │ → Parse Q&A pairs                                         │
│    └─────────────┘                                                           │
│    Output: "Komisyon oranları aşağıdaki şekilde belirlenir..."               │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 2. CHUNK (Semantic)                                                          │
│    ┌──────────────────────────────────────────────────────────────────────┐  │
│    │ Chunk 1: "Komisyon oranları aşağıdaki şekilde belirlenir..."         │  │
│    │ Chunk 2: "Vadeli işlem piyasalarında komisyon oranları..."           │  │
│    │ Chunk 3: "Pay piyasası işlemlerinde işlem başına..."                 │  │
│    └──────────────────────────────────────────────────────────────────────┘  │
│    Parameters: maxChunkSize=1000, minChunkSize=100, overlap=200              │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 3. ENCODING FIX                                                              │
│    "KomÃ¼syon" → "Komisyon"                                                  │
│    TurkishEncodingHelper.FixEncoding()                                       │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 4. VECTORIZATION                                                             │
│                                                                              │
│    ┌─────────────────────────────────────────────────────────────────────┐   │
│    │ Dense Embedding (OpenAI)                                            │   │
│    │ "Komisyon oranları..." → [0.012, -0.034, 0.087, ...] (3072 dims)    │   │
│    └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│    ┌─────────────────────────────────────────────────────────────────────┐   │
│    │ Sparse Vector (BM25)                                                │   │
│    │ Terms: komisyon(2.3), oran(1.8), piyasa(1.5), işlem(2.1)            │   │
│    │ → indices: [12847, 98234, 45123, 78901]                             │   │
│    │ → values:  [2.3,   1.8,   1.5,   2.1]                               │   │
│    └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 5. QDRANT UPSERT                                                             │
│                                                                              │
│    Collection: "rapor_documents"                                             │
│    ┌─────────────────────────────────────────────────────────────────────┐   │
│    │ Point ID: "550e8400-e29b-41d4-a716-446655440000"                    │   │
│    │ Vectors:                                                            │   │
│    │   dense:  [0.012, -0.034, 0.087, ...]                               │   │
│    │   sparse: indices=[12847, 98234, ...], values=[2.3, 1.8, ...]       │   │
│    │ Payload:                                                            │   │
│    │   document_id, chunk_id, content, fileName, category, ...           │   │
│    └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Search Flow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         SEARCH PIPELINE                                      │
└──────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│ User Query          │
│ "komsiyon oranları" │  (typo: komisyon)
└──────────┬──────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 1. LLM PROCESSING (Spelling + HyDE)                                          │
│                                                                              │
│    Input:  "komsiyon oranları"                                               │
│    Output:                                                                   │
│      CORRECTED: "komisyon oranları"                                          │
│      DOCUMENT: "Komisyon oranları, finansal aracı kurumların müşterilerine   │
│                sağladıkları işlem hizmetleri karşılığında aldıkları ücret    │
│                yüzdesidir. Genellikle işlem tutarının %0.1 ile %1 arasında   │
│                değişir. Vadeli işlemler piyasasında..."                      │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 2. EMBEDDING GENERATION                                                      │
│                                                                              │
│    ┌───────────────────────────────────────────────────────────────────┐     │
│    │ Corrected Query Embedding                                         │     │
│    │ "komisyon oranları" → [0.023, 0.045, -0.012, ...]                 │     │
│    └───────────────────────────────────────────────────────────────────┘     │
│                                                                              │
│    ┌───────────────────────────────────────────────────────────────────┐     │
│    │ HyDE Document Embedding                                           │     │
│    │ "Komisyon oranları, finansal..." → [0.018, 0.067, 0.034, ...]     │     │
│    └───────────────────────────────────────────────────────────────────┘     │
│                                                                              │
│    ┌───────────────────────────────────────────────────────────────────┐     │
│    │ Sparse Vector (BM25)                                              │     │
│    │ "komisyon oranları" → indices=[12847, 98234], values=[2.1, 1.9]   │     │
│    └───────────────────────────────────────────────────────────────────┘     │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 3. PARALLEL HYBRID SEARCH                                                    │
│                                                                              │
│    ┌─────────────────────────────────┐  ┌─────────────────────────────────┐  │
│    │ Corrected Query Search          │  │ HyDE Document Search            │  │
│    │                                 │  │                                 │  │
│    │ HybridSearch(                   │  │ HybridSearch(                   │  │
│    │   correctedEmbedding,           │  │   hydeEmbedding,                │  │
│    │   sparseIndices,                │  │   sparseIndices,                │  │
│    │   sparseValues)                 │  │   sparseValues)                 │  │
│    │                                 │  │                                 │  │
│    │ Prefetch:                       │  │ Prefetch:                       │  │
│    │   - Sparse (keyword)            │  │   - Sparse (keyword)            │  │
│    │   - Dense (semantic)            │  │   - Dense (semantic)            │  │
│    │ Fusion: RRF (k=60)              │  │ Fusion: RRF (k=60)              │  │
│    └─────────────────────────────────┘  └─────────────────────────────────┘  │
│                     │                                    │                   │
│                     ▼                                    ▼                   │
│              Results A                            Results B                  │
└──────────────────────────────────────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 4. WEIGHTED MERGE                                                            │
│                                                                              │
│    HyDE Weight: 0.6 | Corrected Weight: 0.4                                  │
│                                                                              │
│    Chunk X in A (score=0.8) + Chunk X in B (score=0.9)                       │
│    → Final: 0.8 × 0.4 + 0.9 × 0.6 = 0.32 + 0.54 = 0.86                       │
│                                                                              │
│    Chunk Y only in A (score=0.7)                                             │
│    → Final: 0.7 × 0.4 = 0.28                                                 │
│                                                                              │
│    Sorted: [Chunk X (0.86), Chunk Z (0.45), Chunk Y (0.28), ...]             │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│ 5. HIGHLIGHT & RETURN                                                        │
│                                                                              │
│    "...işlem başına <mark>komisyon</mark> <mark>oranı</mark> %0.15'tir..."   │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Örnek Senaryolar

### Senaryo 1: Doküman Yükleme

```
Input: "Borsa_Komisyon_Oranlari.pdf" (15 sayfa)

1. Parse → 8500 karakter metin
2. Chunk → 12 semantic chunk
3. Encoding → Türkçe karakterler düzeltildi
4. Dense Embedding → 12 × [3072 floats]
5. Sparse Vector → 12 × (avg 45 terms each)
6. Qdrant Upsert → "borsa_komisyon_oranlari_documents" collection

Result: ✅ 12 chunks indexed, 2.3 saniye
```

### Senaryo 2: Typo ile Arama

```
Query: "komsiyon ornları nedir"  (2 typo)

1. LLM Processing:
   - Corrected: "komisyon oranları nedir"
   - HyDE: "Komisyon oranları, aracı kurumların işlem karşılığında 
           aldıkları ücret yüzdesidir. Türkiye'de genellikle..."

2. Embeddings Generated:
   - Corrected: [0.023, 0.045, ...]
   - HyDE: [0.018, 0.067, ...]
   - Sparse: komisyon(2.3), oran(1.9)

3. Hybrid Search Results:
   - Corrected search → 8 results
   - HyDE search → 7 results
   - 5 overlapping chunks

4. Weighted Merge (HyDE=0.6):
   - Chunk 1: 0.92 (overlapping, boosted)
   - Chunk 2: 0.78
   - Chunk 3: 0.65
   - ...

5. Highlighted Response:
   "...işlem başına <mark>komisyon</mark> <mark>oranı</mark>..."

Result: ✅ Top 5 relevant chunks, 450ms
```

### Senaryo 3: Exact Keyword Search

```
Query: "ISO-27001 sertifikası"

1. LLM Processing:
   - Corrected: "ISO-27001 sertifikası"
   - HyDE: "ISO-27001, bilgi güvenliği yönetim sistemi standardıdır..."

2. Sparse Vector → "iso-27001"(3.5), "sertifika"(2.1)

3. Hybrid Search:
   - Sparse (keyword) finds exact "ISO-27001" matches
   - Dense (semantic) finds "güvenlik standardı" related docs
   - RRF combines both rankings

Result: ✅ Documents with exact "ISO-27001" term ranked higher
```

---

## Performans Metrikleri

| Metrik | Değer | Açıklama |
|--------|-------|----------|
| **Embedding Generation** | ~100ms / text | OpenAI API latency |
| **Sparse Vector** | ~5ms / text | Local Lucene processing |
| **Hybrid Search** | ~50-150ms | Qdrant gRPC + RRF fusion |
| **Full RAG Query** | ~500-800ms | LLM + embeddings + search |
| **Document Ingestion** | ~2-5s / doc | Depends on chunk count |

---

## İlgili Dosyalar Özeti

| Dosya | Satır | Ana Fonksiyon |
|-------|-------|---------------|
| `QdrantService.cs` | 810 | Collection yönetimi, upsert, search |
| `IQdrantService.cs` | 133 | Interface tanımları |
| `QdrantSettings.cs` | 147 | Yapılandırma |
| `QdrantCollections.cs` | 101 | Collection adı helper |
| `OpenAIEmbeddingService.cs` | 141 | Dense embedding |
| `SparseVectorService.cs` | 138 | BM25 sparse vector |
| `RagSearchUseCase.cs` | 777 | HyDE + hybrid search |
| `DocumentProcessingUseCase.cs` | 731 | Doküman işleme pipeline |
| `DocumentChunk.cs` | 185 | Chunk entity |

---

## 📚 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) | Agentic AI pattern'leri (RAG Pattern) |
| [Advanced-RAG.md](Advanced-RAG.md) | Advanced RAG (Reranking, Self-Query, HyDE) |
| [Long-Term-Memory.md](Long-Term-Memory.md) | Kullanıcı hafıza sistemi |
| [System-Overview.md](System-Overview.md) | Genel sistem analizi |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
| [Chat-System.md](Chat-System.md) | Chat sistemi özellikleri |

---

> **Not:** Bu döküman Qdrant Vector Search sistemi için referans olarak kullanılacaktır.
