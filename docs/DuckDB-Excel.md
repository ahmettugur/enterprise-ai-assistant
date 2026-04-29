# 🦆 DuckDB Excel/CSV Analiz Sistemi - Detaylı Analiz

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Dosya Yapısı](#dosya-yapısı)
- [Desteklenen Formatlar](#desteklenen-formatlar)
- [Akış Diyagramı](#akış-diyagramı)
- [Ana Metodlar](#ana-metodlar)
- [ExcelAnalysisUseCase Entegrasyonu](#excelanalysisusecase-entegrasyonu)
- [Güvenlik Kontrolleri](#güvenlik-kontrolleri)
- [DuckDB SQL Örnekleri](#duckdb-sql-örnekleri)
- [Örnek Kullanım Senaryosu](#örnek-kullanım-senaryosu)
- [Teknik Detaylar](#teknik-detaylar)

---

## Genel Bakış

DuckDB, projede **Excel ve CSV dosyalarını in-memory SQL ile analiz etmek** için kullanılıyor. Kullanıcı bir Excel/CSV dosyası yüklediğinde, sistem LLM ile SQL üretir ve DuckDB'de çalıştırır.

### Neden DuckDB?

| Özellik | Açıklama |
|---------|----------|
| **In-Memory** | Disk I/O yok, hızlı analiz |
| **SQL Desteği** | Standart SQL syntax |
| **Büyük Dosyalar** | 100K+ satır verimli işleme |
| **Extension Sistemi** | Excel için spatial extension |
| **Zero-Copy** | Verimli bellek kullanımı |

---

## Dosya Yapısı

```
AI.Application/Ports/Secondary/Services/Report/
└── IExcelAnalysisService.cs          # Interface tanımları + DTO'lar (50 satır)

AI.Infrastructure/Adapters/AI/ExcelServices/
└── DuckDbExcelService.cs             # DuckDB implementasyonu (436 satır)
```

---

## Desteklenen Formatlar

| Format | Extension | DuckDB Fonksiyonu | Extension Gerekli |
|--------|-----------|-------------------|-------------------|
| Excel | `.xlsx` | `st_read()` | ✅ spatial |
| Excel (Eski) | `.xls` | `st_read()` | ✅ spatial |
| CSV | `.csv` | `read_csv()` | ❌ Yok |

### Limitler

| Parametre | Değer | Açıklama |
|-----------|-------|----------|
| Max Sample Rows | 5 | Şema çıkarmada örnek satır |
| Max Result Rows | 1000 | Sorgu sonucu limit |
| Query Timeout | 30 saniye | SQL timeout |
| Max Retry | 3 deneme (sorgu başına) | LLM SQL üretimi retry |

---

## Akış Diyagramı

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         EXCEL/CSV ANALİZ AKIŞI                                  │
└─────────────────────────────────────────────────────────────────────────────────┘

┌───────────────┐
│  Kullanıcı    │
│  Excel yükler │
│  + soru sorar │
└───────┬───────┘
        │ Base64
        ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                  ExcelAnalysisUseCase.ProcessExcelQueryAsync()                │
│  ┌─────────────────────────────────────────────────────────────────────────┐  │
│  │ 1. Base64 → Stream dönüşümü                                             │  │
│  │ 2. DuckDB ile şema çıkar (GetSchemaAsync)                               │  │
│  │ 3. LLM'den analiz planı al (GetAnalysisPlanAsync)                       │  │
│  │    - Spesifik soru → "single" plan (1 SQL)                              │  │
│  │    - Genel analiz → "comprehensive" plan (5-8 SQL)                      │  │
│  │ 4. Her sorguyu 3 retry ile DuckDB'de çalıştır                           │  │
│  │    (ExecuteSingleQueryWithRetryAsync × N sorgu)                         │  │
│  │ 5. Çoklu sorgularda ara tablolar Markdown ile gönder                    │  │
│  │    (SendIntermediateResultAsync → ReceiveMessage)                       │  │
│  │ 6. Tüm sonuçları birleştir, LLM ile yorumlat                            │  │
│  │    (BuildMultiResultInterpretData → GetExcelMultiInterpretPrompt)       │  │
│  │ 7. Streaming yanıt gönder (ReceiveStreamingMessage)                     │  │
│  └─────────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                        DuckDbExcelService                                     │
│                                                                               │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐                          │
│  │ GetSchema   │   │ LoadExtend  │   │ ExecuteQuery│                          │
│  │    Async    │──►│    Async    │──►│    Async    │ (×N sorgu, ×3 retry)     │
│  └─────────────┘   └─────────────┘   └─────────────┘                          │
│        │                                   │                                  │
│        ▼                                   ▼                                  │
│  ┌─────────────┐                   ┌─────────────┐                            │
│  │ - Sütunlar  │                   │ - Data[]    │                            │
│  │ - Satır sayı│                   │ - RowCount  │                            │
│  │ - Örnekler  │                   │ - ExecTime  │                            │
│  └─────────────┘                   └─────────────┘                            │
│                                                                               │
└───────────────────────────────────────────────────────────────────────────────┘
```

---

## Ana Metodlar

### 1️⃣ GetSchemaAsync - Şema Çıkarma

**Dosya:** `DuckDbExcelService.cs`

Excel/CSV dosyasından sütun bilgileri, satır sayısı ve örnek verileri çıkarır.

```csharp
public async Task<ExcelSchemaResult> GetSchemaAsync(
    Stream fileStream, 
    string fileName, 
    CancellationToken ct)
{
    // 1. Geçici dosyaya kaydet
    var tempPath = await SaveToTempFileAsync(fileStream, fileName, ct);
    
    // 2. DuckDB bağlantısı (in-memory)
    await using var connection = new DuckDBConnection("DataSource=:memory:");
    await connection.OpenAsync(ct);
    
    // 3. Extension yükle (Excel için spatial)
    await LoadExtensionsAsync(connection, fileName, ct);
    
    // 4. Read function belirle
    var readFunction = GetReadFunction(fileName, tempPath);
    // Excel: st_read('/path/file.xlsx')
    // CSV: read_csv('/path/file.csv', auto_detect=true, header=true)
    
    // 5. Sütun bilgilerini al
    result.Columns = await GetColumnsAsync(connection, readFunction, ct);
    
    // 6. Satır sayısını al
    result.RowCount = await GetRowCountAsync(connection, readFunction, ct);
    
    // 7. Örnek satırları al (5 adet)
    result.SampleRows = await GetSampleRowsAsync(connection, readFunction, 5, ct);
    
    // 8. Geçici dosyayı temizle
    CleanupTempFile(tempPath);
    
    return result;
}
```

**Çıktı DTO:**

```csharp
public class ExcelSchemaResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string TableName { get; set; }        // "satis_raporu"
    public long RowCount { get; set; }           // 15432
    public List<ColumnInfo> Columns { get; set; } // Sütun listesi
    public List<Dictionary<string, object?>> SampleRows { get; set; } // 5 örnek satır
}

public class ColumnInfo
{
    public string Name { get; set; }      // "kategori"
    public string DataType { get; set; }  // "VARCHAR"
    public bool IsNullable { get; set; }  // true/false
}
```

---

### 2️⃣ ExecuteQueryAsync - SQL Çalıştırma

**Dosya:** `DuckDbExcelService.cs`

Verilen SQL sorgusunu DuckDB'de çalıştırır.

```csharp
public async Task<ExcelQueryResult> ExecuteQueryAsync(
    Stream fileStream, 
    string fileName, 
    string sqlQuery, 
    CancellationToken ct)
{
    // 1. SQL güvenlik kontrolü (sadece SELECT)
    ValidateSql(sqlQuery);
    
    // 2. Geçici dosyaya kaydet
    var tempPath = await SaveToTempFileAsync(fileStream, fileName, ct);
    var tableName = TurkishEncodingHelper.SanitizeTableName(fileName);
    
    // 3. DuckDB bağlantısı
    await using var connection = new DuckDBConnection("DataSource=:memory:");
    await connection.OpenAsync(ct);
    
    // 4. Extension yükle
    await LoadExtensionsAsync(connection, fileName, ct);
    
    // 5. Tablo adını read fonksiyonu ile değiştir
    var readFunction = GetReadFunction(fileName, tempPath);
    var executableSql = ReplacePlaceholders(sqlQuery, tableName, readFunction);
    // "SELECT * FROM data" → "SELECT * FROM st_read('/tmp/file.xlsx')"
    
    // 6. Sorguyu çalıştır
    await using var command = connection.CreateCommand();
    command.CommandText = executableSql;
    command.CommandTimeout = 30;  // 30 saniye timeout
    
    await using var reader = await command.ExecuteReaderAsync(ct);
    
    // 7. Sütun adlarını al
    var columns = new List<string>();
    for (int i = 0; i < reader.FieldCount; i++)
    {
        columns.Add(reader.GetName(i));
    }
    
    // 8. Sonuçları oku (max 1000 satır)
    var data = new List<Dictionary<string, object?>>();
    while (await reader.ReadAsync(ct) && data.Count < 1000)
    {
        var row = new Dictionary<string, object?>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
            row[columns[i]] = value;
        }
        data.Add(row);
    }
    
    // 9. Temizlik ve sonuç
    CleanupTempFile(tempPath);
    
    return new ExcelQueryResult
    {
        Success = true,
        ExecutedSql = executableSql,
        Columns = columns.ToArray(),
        Data = data,
        RowCount = data.Count,
        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
    };
}
```

**Çıktı DTO:**

```csharp
public class ExcelQueryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ExecutedSql { get; set; }      // Çalıştırılan SQL
    public string[] Columns { get; set; }        // Sütun adları
    public List<Dictionary<string, object?>> Data { get; set; } // Sonuç verileri
    public int RowCount { get; set; }            // Dönen satır sayısı
    public long ExecutionTimeMs { get; set; }    // Çalışma süresi (ms)
}
```

---

### 3️⃣ AnalyzeAsync - Tam Analiz

**Dosya:** `DuckDbExcelService.cs`

Şema çıkarma + SQL üretimi için hazırlık yapar.

```csharp
public async Task<ExcelAnalysisResult> AnalyzeAsync(
    Stream fileStream, 
    string fileName, 
    string userQuery, 
    CancellationToken ct)
{
    // Stream'i kopyala (iki kez okumak için)
    using var memoryStream = new MemoryStream();
    await fileStream.CopyToAsync(memoryStream, ct);

    // Şemayı al
    memoryStream.Position = 0;
    result.Schema = await GetSchemaAsync(memoryStream, fileName, ct);

    // NOT: SQL üretimi ExcelAnalysisUseCase tarafından LLM ile yapılacak
    // Bu method sadece şemayı hazırlar

    return result;
}
```

---

### 4️⃣ GenerateHtmlTable - HTML Tablo Üretimi

**Dosya:** `DuckDbExcelService.cs`

Sorgu sonucundan Bootstrap HTML tablosu oluşturur.

```csharp
public static string GenerateHtmlTable(ExcelQueryResult result)
{
    if (!result.Success || result.Data.Count == 0)
        return "<p>Sonuç bulunamadı.</p>";

    var sb = new StringBuilder();
    sb.AppendLine("<div class=\"table-responsive\">");
    sb.AppendLine("<table class=\"table table-striped table-hover\">");

    // Header
    sb.AppendLine("<thead class=\"table-dark\">");
    sb.AppendLine("<tr>");
    foreach (var column in result.Columns)
    {
        sb.AppendLine($"<th>{HttpUtility.HtmlEncode(column)}</th>");
    }
    sb.AppendLine("</tr>");
    sb.AppendLine("</thead>");

    // Body
    sb.AppendLine("<tbody>");
    foreach (var row in result.Data)
    {
        sb.AppendLine("<tr>");
        foreach (var column in result.Columns)
        {
            var value = row.TryGetValue(column, out var v) ? v?.ToString() ?? "" : "";
            sb.AppendLine($"<td>{HttpUtility.HtmlEncode(value)}</td>");
        }
        sb.AppendLine("</tr>");
    }
    sb.AppendLine("</tbody>");

    sb.AppendLine("</table>");
    sb.AppendLine("</div>");

    // Footer
    sb.AppendLine($"<p class=\"text-muted\"><small>Toplam {result.RowCount} satır, {result.ExecutionTimeMs}ms</small></p>");

    return sb.ToString();
}
```

---

## ExcelAnalysisUseCase Entegrasyonu

**Dosya:** `AI.Application/UseCases/ExcelAnalysisUseCase.cs`

> [!NOTE]
> Excel analiz mantığı `AIChatUseCase`'den ayrılarak bağımsız `ExcelAnalysisUseCase`'e taşınmıştır (SRP).

### ProcessExcelQueryAsync Metodu (Multi-Query Analiz)

> [!NOTE]
> Tek-sorgu yaklaşımından **çoklu-sorgu analiz planı** mimarisine geçilmiştir.

```csharp
private async Task<Result<LLmResponseModel>> ProcessExcelQueryAsync(ChatRequest request)
{
    // 1. Base64 → Stream
    byte[] fileBytes = Convert.FromBase64String(request.FileBase64);
    using var stream = new MemoryStream(fileBytes);

    // 2. Şemayı çıkar
    var schemaResult = await _excelAnalysisService.GetSchemaAsync(stream, request.FileName);

    // 3. Sütun bilgilerini formatla + örnek verileri JSON'a çevir
    var columnsText = string.Join("\n", schemaResult.Columns.Select(c => ...));
    var sampleDataJson = JsonConvert.SerializeObject(schemaResult.SampleRows, Formatting.Indented);

    // 4. LLM'den analiz planı al (tek veya çoklu SQL)
    var analysisPlan = await GetAnalysisPlanAsync(
        schemaResult.TableName, schemaResult.RowCount, columnsText, sampleDataJson, request.Prompt);
    // analysisPlan.AnalysisType = "single" veya "comprehensive"
    // analysisPlan.Queries = [{Title, Description, Sql}, ...]

    // 5. Her sorguyu DuckDB'de çalıştır (3 retry ile)
    var allResults = new List<AnalysisQueryResult>();
    foreach (var query in analysisPlan.Queries)
    {
        var queryResult = await ExecuteSingleQueryWithRetryAsync(stream, request, query, schemaResult);
        if (queryResult.Success)
        {
            allResults.Add(queryResult);

            // Çoklu sorguda ara sonucu Markdown tablo olarak gönder
            if (analysisPlan.Queries.Count > 1)
                await SendIntermediateResultAsync(request.ConnectionId, request.ConversationId, queryResult);
        }
    }

    // 6. Sonuçları LLM'e gönder ve yorumlatarak streaming yanıt al
    string interpretPrompt;
    if (allResults.Count > 1)
    {
        // Çoklu sonuç → GetExcelMultiInterpretPrompt
        var allResultsText = BuildMultiResultInterpretData(allResults);
        interpretPrompt = SystemPrompt.GetExcelMultiInterpretPrompt(
            request.Prompt, allResults.Count, allResultsText);
    }
    else
    {
        // Tek sonuç → GetExcelInterpretPrompt (mevcut davranış)
        interpretPrompt = SystemPrompt.GetExcelInterpretPrompt(...);
    }

    // Streaming yanıt gönder (ReceiveStreamingMessage — Markdown)
    await foreach (var content in _chatCompletionService.GetStreamingChatMessageContentsAsync(...))
    {
        // SignalR ile frontend'e gönder
    }
}
```

### Yardımcı Metotlar

| Metot | Satır | İşlev |
|-------|-------|-------|
| `GetAnalysisPlanAsync` | 632-653 | LLM'den `ExcelAnalysisPlan` alır (single/comprehensive) |
| `ParseAnalysisPlanFromResponse` | 655-713 | JSON yanıtı → `ExcelAnalysisPlan` parse (fallback: eski tek-SQL format) |
| `ExecuteSingleQueryWithRetryAsync` | 715-804 | Tek sorguyu 3 retry ile çalıştırır, hata olursa LLM'den düzeltme ister |
| `SendIntermediateResultAsync` | 806-879 | Ara sonucu **Markdown tablo** olarak `ReceiveMessage` ile gönderir |
| `BuildMultiResultInterpretData` | 881-908 | Çoklu sonuçları TOON formatında birleştirir (LLM yorumlama için) |
| `BuildContextMessage` | 910-940 | Bağlam mesajı oluşturur (sonraki sorgular için history'e kaydedilir) |

### Multi-Query Akış

```
┌───────────────────────────────────────────────────────────────────────────────┐
│                    ANALİZ PLANI + ÇOKLU SQL AKIŞI                             │
└───────────────────────────────────────────────────────────────────────────────┘

Kullanıcı: "Bu dosyayı analiz et"
  → LLM analiz planı: comprehensive (5 sorgu)

Sorgu 1: "Genel İstatistikler"
  SELECT COUNT(*), AVG(satis_tutari) FROM data
  DuckDB: ✅ → Markdown tablo → ReceiveMessage (bağımsız mesaj)

Sorgu 2: "Kategori Dağılımı"
  SELECT kategori, COUNT(*) FROM data GROUP BY kategori
  DuckDB: ❌ → Retry 1: LLM düzeltir → DuckDB: ✅ → Markdown tablo

... (3-5 arasında sorgu daha)

Tüm sonuçlar birleştirilir → GetExcelMultiInterpretPrompt
  → LLM kapsamlı rapor + çoklu grafik üretir (streaming)
  → ReceiveStreamingMessage (Markdown chunk)
  → ReceiveMessage (final mesaj + MessageId)

─────────────────────────────────────────────────────────────────────
Kullanıcı: "Satışları şehre göre grupla"
  → LLM analiz planı: single (1 sorgu)

Sorgu 1: SELECT sehir, SUM(satis_tutari) FROM data GROUP BY sehir
  DuckDB: ✅
  → GetExcelInterpretPrompt (eski davranış)
  → LLM yorum + grafik (streaming)
```

---

## Güvenlik Kontrolleri

### SQL Validation

```csharp
private void ValidateSql(string sql)
{
    if (string.IsNullOrWhiteSpace(sql))
        throw new ArgumentException("SQL sorgusu boş olamaz.");

    var upperSql = sql.ToUpperInvariant();

    // Sadece SELECT izin ver
    if (!upperSql.TrimStart().StartsWith("SELECT"))
        throw new SecurityException("Sadece SELECT sorguları desteklenir.");

    // Tehlikeli komutları engelle
    var forbidden = new[] { 
        "DROP", "DELETE", "INSERT", "UPDATE", 
        "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE" 
    };
    
    foreach (var keyword in forbidden)
    {
        if (upperSql.Contains(keyword))
            throw new SecurityException($"Tehlikeli SQL komutu tespit edildi: {keyword}");
    }
}
```

### Tablo Adı Sanitization

```csharp
// Türkçe karakterler ve özel karakterler temizlenir
var tableName = TurkishEncodingHelper.SanitizeTableName(fileName);
// "Satış_Raporu (2025).xlsx" → "satis_raporu_2025"
```

### Placeholder Değiştirme

```csharp
private string ReplacePlaceholders(string sql, string tableName, string readFunction)
{
    // Olası tablo adı placeholder'larını değiştir
    var result = sql
        .Replace($"FROM {tableName}", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
        .Replace($"FROM \"{tableName}\"", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
        .Replace("FROM data", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
        .Replace("FROM DATA", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
        .Replace("FROM tablo", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase)
        .Replace("FROM table", $"FROM {readFunction}", StringComparison.OrdinalIgnoreCase);

    return result;
}
```

---

## DuckDB SQL Örnekleri

### Extension Yükleme

```sql
-- Excel dosyaları için spatial extension gerekli
INSTALL spatial;
LOAD spatial;
```

### Dosya Okuma

```sql
-- Excel dosyası okuma
SELECT * FROM st_read('/tmp/duckdb_abc123.xlsx')

-- CSV dosyası okuma (auto-detect ile)
SELECT * FROM read_csv('/tmp/duckdb_abc123.csv', auto_detect=true, header=true)
```

### Şema Çıkarma Sorguları

```sql
-- Sütun bilgilerini al
DESCRIBE SELECT * FROM st_read('/tmp/file.xlsx')

-- Satır sayısını al
SELECT COUNT(*) FROM st_read('/tmp/file.xlsx')

-- Örnek satırları al
SELECT * FROM st_read('/tmp/file.xlsx') LIMIT 5
```

### LLM Tarafından Üretilen Örnek Sorgular

```sql
-- Toplam satış
SELECT SUM(satis_tutari) as toplam_satis FROM data

-- Kategori bazlı analiz
SELECT 
    kategori, 
    COUNT(*) as adet, 
    SUM(satis_tutari) as toplam,
    AVG(satis_tutari) as ortalama
FROM data
GROUP BY kategori
ORDER BY toplam DESC

-- Tarih filtresi
SELECT * FROM data
WHERE tarih >= '2025-01-01' AND tarih < '2025-02-01'

-- En yüksek satışlar
SELECT * FROM data
ORDER BY satis_tutari DESC
LIMIT 10

-- Aylık trend
SELECT 
    strftime('%Y-%m', tarih) as ay,
    SUM(satis_tutari) as toplam
FROM data
GROUP BY strftime('%Y-%m', tarih)
ORDER BY ay
```

---

## Örnek Kullanım Senaryosu

### Senaryo: Kategori Bazlı Satış Analizi

```
Kullanıcı: [satis_raporu.xlsx yükler]
          "Bu dosyadaki kategori bazlı toplam satışları göster"
```

### Sistem Akışı

**1. Şema Çıkarma:**

```
Tablo: satis_raporu
Satır: 15,432

Sütunlar:
- kategori (VARCHAR)
- urun_adi (VARCHAR)
- satis_tutari (DOUBLE)
- tarih (DATE)
- musteri_id (INTEGER)

Örnek Veriler:
| kategori    | urun_adi      | satis_tutari | tarih      |
|-------------|---------------|--------------|------------|
| Elektronik  | Laptop X1     | 15000.00     | 2025-01-15 |
| Giyim       | T-Shirt Basic | 150.00       | 2025-01-15 |
| ...         | ...           | ...          | ...        |
```

**2. LLM SQL Üretimi:**

```sql
SELECT 
    kategori,
    COUNT(*) as urun_adedi,
    SUM(satis_tutari) as toplam_satis,
    AVG(satis_tutari) as ortalama_satis
FROM data
GROUP BY kategori
ORDER BY toplam_satis DESC
```

**3. DuckDB Sonucu:**

```
| kategori    | urun_adedi | toplam_satis | ortalama_satis |
|-------------|------------|--------------|----------------|
| Elektronik  | 4,532      | 1,234,567.00 | 272.45         |
| Giyim       | 3,210      | 876,543.00   | 273.07         |
| Ev & Yaşam  | 2,890      | 654,321.00   | 226.41         |
| ...         | ...        | ...          | ...            |
```

**4. LLM Yorumu (Streaming):**

```
📊 **Kategori Bazlı Satış Analizi**

Toplam 15,432 satır analiz edildi.

**Öne Çıkan Bulgular:**

1. **Elektronik** kategorisi en yüksek satışı gerçekleştirmiş:
   - Toplam: ₺1,234,567 (4,532 ürün)
   - Ortalama satış: ₺272.45

2. **Giyim** kategorisi ikinci sırada:
   - Toplam: ₺876,543 (3,210 ürün)
   
3. Tüm kategorilerde ortalama satış tutarı benzer (~₺250-275)

**Öneri:** Elektronik kategorisinde stok optimizasyonu yapılabilir.
```

---

## Teknik Detaylar

### Yapılandırma Parametreleri

| Parametre | Değer | Açıklama |
|-----------|-------|----------|
| `DataSource` | `:memory:` | In-memory veritabanı |
| `MaxSampleRows` | 5 | Şema örnek satır sayısı |
| `MaxResultRows` | 1000 | Sorgu sonucu limit |
| `QueryTimeoutSeconds` | 30 | SQL timeout |
| `MaxRetries` | 3 | Sorgu başına LLM SQL retry (her sorgu bağımsız) |
| `Temperature` (plan) | 0.1 | Analiz planı / SQL üretimi |
| `Temperature` (yorum) | 0.3 | Sonuç yorumlama |

### Geçici Dosya Yönetimi

```csharp
// Geçici dosya oluşturma
var tempPath = Path.Combine(Path.GetTempPath(), $"duckdb_{Guid.NewGuid()}{extension}");
// Örnek: C:\Users\...\Temp\duckdb_abc123.xlsx

// İşlem sonunda silme
private void CleanupTempFile(string tempPath)
{
    if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
    {
        try
        {
            File.Delete(tempPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geçici dosya silinemedi: {TempPath}", tempPath);
        }
    }
}
```

### OpenTelemetry Tracing

```csharp
using var activity = ActivitySources.DocumentProcessing.StartActivity("DuckDB_ExecuteQuery");
activity?.SetTag("file.name", fileName);
activity?.SetTag("result.rows", result.RowCount);
activity?.SetTag("execution.ms", result.ExecutionTimeMs);
activity?.SetStatus(ActivityStatusCode.Ok);
```

---

## İlgili Dosyalar

| Dosya | Satır | Açıklama |
|-------|-------|----------|
| `DuckDbExcelService.cs` | 436 | DuckDB implementasyonu |
| `IExcelAnalysisService.cs` | 50 | Interface ve DTO'lar |
| `ExcelAnalysisUseCase.cs` | - | Excel analiz orkestrasyon use case (eski AIChatUseCase'den ayrıldı) |
| `SystemPrompt.cs` | - | 4 prompt metodu (plan, SQL, interpret, multi-interpret) |
| `ExcelAnalysisPlan.cs` | - | Analiz planı DTO (AnalysisType, Queries) |
| `AnalysisQueryResult.cs` | - | Sorgu sonucu DTO (Title, QueryResult, Success) |
| `excel_analysis_plan_prompt.md` | - | LLM'den çoklu SQL planı üretme prompt'u |
| `excel_multi_interpret_prompt.md` | - | Çoklu sonuç yorumlama prompt'u |
| `excel_interpret_prompt.md` | - | Tek sonuç yorumlama prompt'u |
| `excel_sql_generator_prompt.md` | - | SQL üretme prompt'u (fallback) |
| `TurkishEncodingHelper.cs` | - | Tablo adı sanitization |

---

## Özet

DuckDB projede şu amaçlarla kullanılıyor:

| # | Özellik | Açıklama |
|---|---------|----------|
| 1 | **Şema Çıkarma** | Sütun adları, tipleri, satır sayısı |
| 2 | **In-Memory SQL** | Disk I/O olmadan hızlı analiz |
| 3 | **Multi-Query Analiz Planı** | LLM otomatik olarak tek veya çoklu SQL üretir |
| 4 | **Retry Mekanizması** | Sorgu başına 3 retry, hata durumunda LLM'e geri besleme |
| 5 | **Güvenlik** | Sadece SELECT, tehlikeli komut engelleme |
| 6 | **Ara Tablo Gönderimi** | Markdown tablo olarak ReceiveMessage ile bağımsız mesaj |
| 7 | **Streaming Yorum** | LLM kapsamlı rapor + çoklu grafik streaming |
| 8 | **Bağlam Kaydı** | Sonraki sorgular için history'e context kaydedilir |

Bu yapı, kullanıcıların teknik bilgi olmadan Excel/CSV dosyalarını doğal dilde sorgulamasını sağlıyor.

---

## 📚 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) | Agentic AI pattern'leri (Tool Use Pattern) |
| [DuckDB-Usage.md](DuckDB-Usage.md) | DuckDB kullanım kılavuzu ve akış dökümanı |
| [System-Overview.md](System-Overview.md) | Genel sistem analizi |
| [Chat-System.md](Chat-System.md) | Chat sistemi özellikleri |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
| [Report-System.md](Report-System.md) | Rapor sistemi detayları |

---

> **Not:** Bu döküman DuckDB Excel/CSV analiz sistemi için referans olarak kullanılacaktır.
