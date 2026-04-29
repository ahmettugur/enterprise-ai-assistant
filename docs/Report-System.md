# 📊 Rapor Oluşturma Sistemi - Detaylı Analiz

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Akış Diyagramı](#akış-diyagramı)
- [Dosya Yapısı](#dosya-yapısı)
- [RouteConversationUseCase](#1-conversationorchestrator---istek-yönlendirme)
- [SqlServerReportServiceBase](#2-sqlserverreportservicebase---rapor-üretim-motoru)
- [SQL Agent Pipeline](#3-sql-agent-pipeline)
- [Database Execution](#4-database-execution)
- [Dashboard Generation](#5-dashboard-generation)
- [Chunk-Based Insight Analizi](#6-chunk-based-insight-analizi)
- [LLM Optimize Veri Yapısı](#7-llm-optimize-veri-yapısı)
- [Retry Mekanizması](#8-retry-mekanizması)
- [Kayıtlı Rapor Servisleri](#kayıtlı-rapor-servisleri)
- [Önemli Özellikler](#önemli-özellikler)

---

## Genel Bakış

Rapor sistemi, kullanıcının doğal dilde sorduğu soruları analiz ederek:

1. SQL sorgusu üretir (LLM ile)
2. Veritabanında çalıştırır (SQL Server)
3. HTML dashboard oluşturur (Fast Mode: Template-based, Legacy Mode: Full HTML)
4. AI Veri Analizi (Insight) üretir (Chunk-based paralel analiz)
5. SignalR ile frontend'e gönderir

### Temel Özellikler

- **Database Desteği:** SQL Server (AdventureWorks)
- **Keyed Services Pattern:** Rapor türüne göre dinamik servis seçimi
- **Long-Term Memory:** Kullanıcı tercihlerini hatırlama
- **Chunk-Based Analiz:** Büyük veri setleri için paralel işleme
- **LLM Optimize Veri:** Token kullanımını %90-95 azaltma
- **Fail-Open Yaklaşımı:** Pipeline hatalarında sistem çalışmaya devam eder

---

## Akış Diyagramı

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          KULLANICI İSTEĞİ                                       │
│              "Son ayın satış raporunu göster"                                   │
└─────────────────────────────────────┬───────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        1️⃣ CONVERSATION ROUTER                                   │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │ • LLM ile istek analizi (SelectModeWithLlmAsync)                          │  │
│  │ • Dinamik kategori/rapor listelerini inject et                            │  │
│  │ • Long-Term Memory context'i ekle                                         │  │
│  │ • Karar: action="report", reportName="adventureworks"                      │  │
│  │ • İlgili ReportService'i seç (Keyed Services pattern)                     │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────┬───────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      2️⃣ REPORT SERVICE (GetReportsWithHtmlAsync)                │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │ a. System Prompt yükle (SystemPromptFileName)                             │  │
│  │ b. Memory Context ekle (UserMemoryService)                                │  │
│  │ c. Chat History'ye ekle (ReplaceSystemPromptAsync)                        │  │
│  │ d. LLM'e gönder → JSON yanıt al (Query, Summary, Suggestions)             │  │
│  │ e. Memory extraction (fire-and-forget)                                    │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────┬───────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      3️⃣ SQL AGENT PIPELINE (Opsiyonel)                          │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │ • SQL Validation Agent - Syntax kontrolü + otomatik düzeltme              │  │
│  │ • SQL Optimization Agent - Performans iyileştirme                         │  │
│  │ • Security Check - SQL Injection tespiti                                  │  │
│  │ • Fail-open yaklaşımı - başarısız olursa orijinal SQL kullanılır          │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────┬───────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      4️⃣ DATABASE EXECUTION                                      │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │ • SQL Server'da SQL çalıştır                                              │  │
│  │ • DataTable olarak sonuç al (ExpandoObject)                               │  │
│  │ • Hata durumunda retry (5 deneme, exponential backoff + jitter)           │  │
│  │ • Hata mesajını history'e ekle (LLM düzeltsin diye)                       │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────┬───────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      5️⃣ DASHBOARD GENERATION (Paralel)                          │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │ A) Dashboard HTML:                                                        │  │
│  │    Fast Mode: LLM → JSON Config → Template-based HTML (5-10x hızlı)       │  │
│  │    Legacy Mode: LLM → Full HTML generation (esnek ama yavaş)              │  │
│  │                                                                           │  │
│  │ B) Insight HTML (Paralel):                                                │  │
│  │    Küçük veri: Doğrudan analiz                                            │  │
│  │    Büyük veri (>1000 kayıt): Chunk-based paralel analiz                   │  │
│  │    • Veryi chunk'lara böl → Paralel LLM analizi → Sonuçları birleştir     │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────┬───────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      6️⃣ SIGNALR RESPONSE                                        │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │ • "ReceiveLoadingMessage" → "Rapor oluşturuluyor..."                      │  │
│  │ • "OnProgress" → Chunk analizi progress bildirimi                         │  │
│  │ • "ReceiveMessage" → HTML dashboard URL                                   │  │
│  │ • History'e kaydet (Assistant message + metadata)                         │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Dosya Yapısı

```
AI.Application/
├── Ports/Secondary/Services/AgentCore/        # Agent Registry Interfaces
│   ├── IActionAgent.cs                        # Agent interface
│   └── IActionAgentRegistry.cs                # Registry interface
├── UseCases/
│   ├── IReportService.cs                      # Interface (2 metod)
│   ├── IRouteConversationUseCase.cs           # Router interface
│   ├── RouteConversationUseCase.cs            # İstek yönlendirme (456 satır)
│   ├── ActionAgents/                          # Agent Implementations
│   │   ├── ActionAgentRegistry.cs             # Registry — agent dispatch
│   │   ├── ChatActionAgent.cs                 # "chat" action
│   │   ├── DocumentActionAgent.cs             # "document" action
│   │   ├── ReportActionAgent.cs               # "report" action (Keyed service)
│   │   └── AskActionAgent.cs                  # "ask_*" actions (template)
│
AI.Infrastructure/Adapters/AI/Reports/SqlServer/
├── SqlServerReportServiceBase.cs       # SQL Server base class (2283 satır)
└── AdventureWorksReportService.cs      # AdventureWorks raporları

AI.Infrastructure/Adapters/AI/Agents/SqlAgents/
├── ISqlAgentPipeline.cs                 # Pipeline interface (in AI.Application/Ports)
├── ISqlValidationAgent.cs               # Validation agent interface
├── ISqlOptimizationAgent.cs             # Optimization agent interface
├── SqlAgentPipeline.cs                  # Pipeline implementasyonu (559 satır)
├── SqlValidationAgent.cs                # Syntax kontrolü + düzeltme
└── SqlOptimizationAgent.cs              # Performans optimizasyonu

AI.Application/DTOs/
└── InsightAnalysisDtos.cs               # Chunk analizi DTO'ları (550 satır)

AI.Application/UseCases/Common/
├── DashboardFileService.cs              # Dashboard dosya işlemleri (163 satır)
└── DashboardConfig.cs                   # Dashboard JSON konfigürasyonu (419 satır)

AI.Application/DTOs/
└── ResponseModel.cs                     # LLM response modelleri
```

---

## 1. RouteConversationUseCase - İstek Yönlendirme

**Dosya:** `AI.Application/UseCases/RouteConversationUseCase.cs` (456 satır)

Kullanıcı isteğini LLM ile analiz ederek **Agent Registry** üzerinden uygun agent'a yönlendirir:

### Ana Akış

```csharp
public async Task<Result<dynamic>> OrchestrateAsync(ChatRequest request, CancellationToken cancellationToken)
{
    // 1. LLM ile istek analizi
    var llmSelectionResult = await SelectModeWithLlmAsync(request);
    
    // 2. Agent Registry ile yönlendirme (Strategy + Registry Pattern)
    var agent = _agentRegistry.FindAgent(llmSelectionResult.Action)
        ?? throw new InvalidOperationException($"Action agent not found: {llmSelectionResult.Action}");
    
    var context = new ActionContext(request, llmSelectionResult);
    var apiResult = await agent.HandleAsync(context, cancellationToken);
    return apiResult;
}
```

> **Not:** Önceki `if/else` yapısı kaldırılmış, `HandleAskActionAsync()`, `GetTemplateContentAsync()` ve `GetReportService()` metotları ilgili Agent sınıflarına (`AskActionAgent`, `ReportActionAgent`) taşınmıştır.

### Dinamik Template Sistemi

> **Not:** Dinamik template sistemi artık `AskActionAgent` sınıfında (`AI.Application/UseCases/ActionAgents/AskActionAgent.cs`) yönetilmektedir. `GetTemplateContentAsync` metodu `RouteConversationUseCase`'den kaldırılmıştır.

### LLM İstek Analizi

```csharp
private async Task<string> ExecuteLlmAnalysisAsync(ChatRequest request)
{
    var basePrompt = Helper.ReadFileContent("Common/Resources/Prompts", "conversation-orchestrator.md");
    
    // Dinamik listeleri inject et
    var categoryList = await _documentMetadataService.GenerateCategoryListForPromptAsync();
    var documentList = await _documentMetadataService.GenerateDocumentListForPromptAsync();
    var databaseList = _reportMetadataService.GenerateDatabaseListForPrompt();
    var reportTypeList = _reportMetadataService.GenerateReportTypeListForPrompt();
    var dynamicReportCategoryList = _reportMetadataService.GenerateDynamicReportCategoryListForPrompt();
    
    // Long-Term Memory: Kullanıcı bağlamını al
    var memoryContext = await _userMemoryService.BuildMemoryContextAsync(request.Prompt);
    
    var fullSystemPrompt = basePrompt
        .Replace("{{CATEGORY_LIST}}", categoryList)
        .Replace("{{DOCUMENT_LIST}}", documentList)
        .Replace("{{DATABASE_LIST}}", databaseList)
        .Replace("{{REPORT_TYPE_LIST}}", reportTypeList)
        .Replace("{{DYNAMIC_REPORT_CATEGORY_LIST}}", dynamicReportCategoryList);
    
    // Long-Term Memory: Kullanıcı bağlamını system prompt'a ekle
    if (!string.IsNullOrEmpty(memoryContext))
        fullSystemPrompt = $"{fullSystemPrompt}\n\n## User Context\n{memoryContext}";
    
    // ... LLM çağrısı
}
```

### RouterResponse Model

```csharp
public class RouterResponse
{
    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;  // welcome, chat, document, report, ask_*

    [JsonProperty("reportName")]
    public string ReportName { get; set; } = string.Empty;  // adventureworks
    
    [JsonProperty("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("templateName")]
    public string TemplateName { get; set; } = string.Empty;  // ask_report, ask_database, etc.

    [JsonProperty("errorType")]
    public string ErrorType { get; set; } = string.Empty;

    [JsonProperty("suggestion")]
    public string Suggestion { get; set; } = string.Empty;
}
```

### Retry Mekanizması (Router)

```csharp
private const int MaxRetries = 3;
private const int BaseDelayMs = 500;

private async Task<RouterResponse?> SelectModeWithLlmAsync(ChatRequest request)
{
    for (var attempt = 0; attempt < MaxRetries; attempt++)
    {
        try
        {
            var result = await ExecuteLlmAnalysisAsync(request);
            var analysisResult = await ParseLlmResponseAsync(result, request);
            return analysisResult;
        }
        catch (Exception ex)
        {
            if (attempt < MaxRetries - 1)
            {
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
                continue;
            }
        }
    }
    return null;
}
```

---

## 2. SqlServerReportServiceBase - Rapor Üretim Motoru

**Dosya:** `AI.Infrastructure/Adapters/AI/Reports/SqlServer/SqlServerReportServiceBase.cs` (2283 satır)

SQL Server rapor servislerinin abstract base class'ı.

### Abstract Members

```csharp
public abstract class SqlServerReportServiceBase : IReportService
{
    // Her rapor servisi kendi system prompt dosya adını belirtmelidir
    protected abstract string SystemPromptFileName { get; }
    
    // Rapor servis tipi (adventureworks vb.)
    protected abstract string ReportServiceType { get; }
    
    // Veritabanı tipi - sabit: "sqlserver"
    protected string ReportDatabaseType => "sqlserver";
    
    // Veritabanı servis tipi - varsayılan: "adventureworks"
    protected virtual string ReportDatabaseServiceType => "adventureworks";
}
```

### Virtual Prompt Properties

```csharp
// Prompt dosyalarının bulunduğu klasör yolu
protected virtual string PromptFolder => "Common/Resources/Prompts";

// Chunk analizi prompt dosya adı
protected virtual string ChunkAnalysisPromptFile => "chunk_analysis_prompt.md";

// Insight analizi prompt dosya adı
protected virtual string InsightAnalysisPromptFile => "insight_analysis_prompt.md";

// Dashboard config prompt dosya adı
protected virtual string DashboardConfigPromptFile => "dashboard_config_generator_prompt.md";

// Dashboard HTML prompt dosya adı
protected virtual string DashboardHtmlPromptFile => "dashboard_generator_prompt_adventureworks.md";
```

### Constants

```csharp
// Retry configuration
private const int MaxRetryAttempts = 5;
private const int BaseDelayMs = 1000;
private const int MaxDelayMs = 10000;

// Chunk modu ayarı: true = token bazlı, false = sabit 1000 kayıt bazlı
private const bool UseTokenBasedChunking = false;

// Token bazlı chunk ayarları
private const int TokenBasedChunkingThreshold = 700_000;
private const int TargetTokensPerChunk = 80_000;
private const int TokensPerRowEstimate = 100;

// Kayıt bazlı chunk ayarları
private const int RecordBasedChunkingThreshold = 1000;
private const int RecordsPerChunk = 1000;
private const int MaxChunks = 10;
private const int MaxParallelism = 3;

// Chunk retry ayarları
private const int MaxChunkRetryAttempts = 3;
private const int ChunkRetryBaseDelayMs = 2000;
```

### Ana Metod: GetReportsWithHtmlAsync

```csharp
public virtual async Task<Result<LLmResponseModel>> GetReportsWithHtmlAsync(ChatRequest request)
{
    // 1. System Prompt yükle
    var systemPrompt = Helper.ReadFileContent("Common/Resources/Prompts", SystemPromptFileName);
    
    // 2. Long-Term Memory ekle
    if (UserMemoryService != null)
    {
        var memoryContext = await UserMemoryService.BuildMemoryContextAsync(request.Prompt);
        if (!string.IsNullOrEmpty(memoryContext))
            systemPrompt = systemPrompt + "\n\n" + memoryContext;
    }
    
    // 3. History'e system prompt ekle
    await HistoryUseCase.ReplaceSystemPromptAsync(request, systemPrompt);
    
    // 4. LLM'den JSON yanıt al (Retry mekanizması ile)
    var response = await ExecuteWithRetryAsync(async () =>
    {
        var chatHistory = await HistoryUseCase.GetChatHistoryAsync(request);
        var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(chatHistory, settings, Kernel);
        return await ParseResponseAsync(resultPrompt[0].ToString(), request);
    }, onRetryAsync, request.ConnectionId, "LLM Chat Completion ve Rapor İşleme");
    
    // 5. Memory extraction (fire-and-forget)
    if (UserMemoryService != null && CurrentUserService != null)
    {
        var currentUserId = CurrentUserService.UserId;
        if (!string.IsNullOrEmpty(currentUserId) && response.IsSucceed)
        {
            _ = Task.Run(async () =>
            {
                await UserMemoryService.ExtractAndStoreMemoriesAsync(
                    request.Prompt, response.ResultData.HtmlMessage, currentUserId, CancellationToken.None);
            });
        }
    }
    
    return response;
}
```

### LLM Yanıt Formatı (JSON)

```json
{
  "summary": "Son 30 günün satış istatistikleri",
  "query": "SELECT CAST(OrderDate AS DATE) AS SalesDate, COUNT(*) AS OrderCount, SUM(TotalDue) AS TotalSales FROM Sales.SalesOrderHeader WHERE OrderDate >= DATEADD(DAY, -30, GETDATE()) GROUP BY CAST(OrderDate AS DATE) ORDER BY SalesDate",
  "suggestions": ["Haftalık karşılaştırma", "Bölge bazlı analiz", "Ürün kategorisi dağılımı"]
}
```

### Concrete Implementations

```csharp
// AdventureWorksReportService.cs (SQL Server)
public class AdventureWorksReportService : SqlServerReportServiceBase
{
    protected override string SystemPromptFileName => "adventurerworks_server_assistant_prompt.md";
    protected override string ReportServiceType => "adventureworks";
    
    public AdventureWorksReportService(
        [FromKeyedServices("adventureworks")] IDatabaseService databaseService,
        // ... diğer bağımlılıklar
    ) : base(...) { }
}
```

---

## 3. SQL Agent Pipeline

**Dosya:** `AI.Infrastructure/Adapters/AI/Agents/SqlAgents/SqlAgentPipeline.cs` (559 satır)

SQL doğrulama ve optimizasyon pipeline'ı:

### Pipeline Akışı

```
[LLM Generated SQL] 
    → [SQL Validation Agent] - Syntax kontrolü + otomatik düzeltme
    → [SQL Optimization Agent] - Performans iyileştirme
    → [Security Check] - SQL Injection tespiti (implicit)
    → [Final SQL]
```

### ProcessSqlWithAgentPipelineAsync

```csharp
protected virtual async Task<string> ProcessSqlWithAgentPipelineAsync(string sql, ChatRequest request)
{
    // Pipeline veya ayarlar yoksa direkt SQL döndür
    if (SqlAgentPipeline == null || MultiAgentSettings == null)
        return sql;

    // Multi-Agent veya SQL Agents devre dışıysa direkt SQL döndür
    if (!MultiAgentSettings.Enabled || !MultiAgentSettings.SqlAgents.Enabled)
        return sql;

    try
    {
        var sqlAgentSettings = MultiAgentSettings.SqlAgents;
        var pipelineOptions = new SqlPipelineOptions
        {
            EnableValidation = sqlAgentSettings.EnableValidation,
            EnableOptimization = sqlAgentSettings.EnableOptimization,
            EnableSecurityCheck = sqlAgentSettings.EnableSecurityCheck,
            EnableAutoCorrection = sqlAgentSettings.EnableAutoCorrection,
            MaxRetries = sqlAgentSettings.MaxRetries
        };

        var pipelineResult = await SqlAgentPipeline.ProcessAsync(sql, ReportDatabaseType, pipelineOptions);

        if (!pipelineResult.IsSuccess)
        {
            Logger.LogWarning("SQL Agent Pipeline başarısız - orijinal SQL kullanılacak");
            return sql; // Fail-open yaklaşımı
        }

        if (pipelineResult.FinalSql != sql)
        {
            Logger.LogInformation("SQL Agent Pipeline SQL'i güncelledi");
        }

        return pipelineResult.FinalSql;
    }
    catch (Exception ex)
    {
        Logger.LogWarning(ex, "SQL Agent Pipeline hatası - orijinal SQL kullanılacak");
        return sql; // Fail-open yaklaşımı
    }
}
```

### Pipeline Implementasyonu

```csharp
public class SqlAgentPipeline : ISqlAgentPipeline
{
    private readonly ISqlValidationAgent _validationAgent;
    private readonly ISqlOptimizationAgent _optimizationAgent;

    public async Task<SqlPipelineResult> ProcessAsync(string sql, string databaseType, SqlPipelineOptions? options = null)
    {
        options ??= SqlPipelineOptions.Default;
        var stages = new List<SqlPipelineStage>();
        var currentSql = sql;

        // Stage 1: Validation
        if (options.EnableValidation)
        {
            var validationResult = await _validationAgent.ValidateAsync(currentSql, databaseType, options.SchemaInfo);
            
            if (!validationResult.IsValid && options.EnableAutoCorrection && !string.IsNullOrEmpty(validationResult.CorrectedSql))
            {
                currentSql = validationResult.CorrectedSql;
                
                // Düzeltilmiş SQL'i tekrar validate et
                if (options.MaxRetries > 0)
                {
                    var retryResult = await RetryValidationAsync(currentSql, databaseType, options, stages);
                    if (!retryResult.IsSuccess)
                        return SqlPipelineResult.Failure(sql, retryResult.ErrorMessage, validationResult, stages);
                    currentSql = retryResult.FinalSql;
                }
            }
        }

        // Stage 2: Optimization
        if (options.EnableOptimization)
        {
            var optimizationResult = await _optimizationAgent.OptimizeAsync(currentSql, databaseType, options.SchemaInfo);
            if (optimizationResult.IsOptimized)
                currentSql = optimizationResult.OptimizedSql;
        }

        return SqlPipelineResult.Success(currentSql, validationResult, optimizationResult, stages);
    }
}
```

---

## 4. Database Execution

Veritabanı sorgusu çalıştırma (hata yönetimi ile):

```csharp
// ProcessReportsAsync içinde
var finalQuery = llmResponse.Query!;

// SQL Agent Pipeline ile validate et ve optimize et
if (SqlAgentPipeline != null)
{
    finalQuery = await ProcessSqlWithAgentPipelineAsync(llmResponse.Query!, request);
}

// Database sorgusu - ayrı try-catch ile
DbResponseModel dbResponse;
try
{
    dbResponse = await DatabaseService.GetDataTableWithExpandoObjectAsync(finalQuery, request.ConversationId);
}
catch (Exception dbEx)
{
    Logger.LogError(dbEx, "SQL sorgusu çalıştırılırken hata oluştu");

    // History'e hata mesajı ekle (SQL sorgusu ile birlikte - LLM düzeltsin diye)
    var errorMessage = $"""
        Sql Sorgusu çalıştırılırken hata oluştu. SQL Server'da çalışacak şekilde sql sorgusu oluştur.
        
        Çalıştırılan SQL:
        {finalQuery}
        
        Alınan hata: {dbEx.Message}
        """;

    await HistoryUseCase.AddUserMessageAsync(request, errorMessage, MessageType.Temporary);
    throw; // Exception'ı yukarıya fırlat - retry mekanizması yakalayacak
}
```

---

## 5. Dashboard Generation

### İki Mod Destekleniyor

#### Fast Mode (Template-based) - Önerilen

```csharp
if (DashboardSettings?.UseFastDashboard == true)
{
    return await ProcessFastDashboard(dataForHtmlModel, conversationId, outputFolder);
}

protected virtual async Task<string> ProcessFastDashboard(DataForHtmlModel dataForHtmlModel, Guid conversationId, string outputFolder)
{
    var jsonData = JsonConvert.SerializeObject(dataForHtmlModel);
    
    // LLM sadece JSON config üretir
    var config = await GenerateDashboardConfig(jsonData, conversationId);
    
    if (config == null)
    {
        // Fallback to legacy method
        var htmlMessage = await GenerateHtml(jsonData, conversationId);
        var fallbackResult = await DashboardService.ProcessDashboardResponse(htmlMessage, dataForHtmlModel, outputFolder);
        return fallbackResult.OutputApiUrl;
    }

    // Template-based HTML üretimi (5-10x hızlı)
    var dashboardProcessResult = await DashboardService.ProcessTemplateDashboard(config, dataForHtmlModel, outputFolder);
    return dashboardProcessResult.OutputApiUrl;
}
```

#### Legacy Mode (Full HTML Generation)

```csharp
// LLM tam HTML üretir (daha yavaş ama esnek)
var htmlMessage = await GenerateHtml(jsonData, conversationId);
var dashboardProcessResult = await DashboardService.ProcessDashboardResponse(
    htmlMessage, dataForHtmlModel, outputFolder, insightHtml);
return dashboardProcessResult.OutputApiUrl;
```

### Dashboard Config Generation

```csharp
protected virtual async Task<DashboardConfig?> GenerateDashboardConfig(string jsonData, Guid conversationId)
{
    var history = new ChatHistory();
    var promptFileName = DashboardSettings?.ConfigPromptFileName ?? DashboardConfigPromptFile;
    var systemPrompt = Helper.ReadFileContent(PromptFolder, promptFileName);
    history.AddSystemMessage(systemPrompt);
    history.AddUserMessage(jsonData);

    var openAiSettings = new OpenAIPromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        Temperature = 0.1F  // Düşük temperature = tutarlı çıktı
    };

    var resultPrompt = await ChatCompletionService.GetChatMessageContentsAsync(history, openAiSettings, Kernel);
    var responseText = resultPrompt[0].ToString()!;
    
    // JSON bloğunu parse et
    var jsonMatch = JsonBlockRegex.Match(responseText);
    var configJson = jsonMatch.Success ? jsonMatch.Groups[1].Value : responseText;
    
    return JsonConvert.DeserializeObject<DashboardConfig>(configJson);
}
```

### DashboardConfig Model

```csharp
public class DashboardConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public List<KpiConfig> Kpis { get; set; } = [];      // KPI kartları
    public List<ChartConfig> Charts { get; set; } = [];  // Grafikler
    public TableConfig? Table { get; set; }               // Tablo
    public AnalysisConfig? Analysis { get; set; }         // AI analiz bölümü
    public string? CustomCss { get; set; }                // Özel stiller
}

public class KpiConfig
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }    // sum, avg, count, min, max, countDistinct
    public string Column { get; set; }  // Hesaplamada kullanılacak kolon
    public string Icon { get; set; }    // Emoji
    public string Color { get; set; }   // blue, green, red, purple, etc.
    public string Format { get; set; }  // number, currency, percent, duration
}
```

---

## 6. Chunk-Based Insight Analizi

Büyük veri setleri için paralel chunk analizi:

### Chunking Kararı

```csharp
protected virtual async Task<string> GenerateInsightHtml(string jsonData, string uniqueId, Guid conversationId, string connectionId = "")
{
    var dataObject = JsonConvert.DeserializeObject<dynamic>(jsonData);
    var data = dataObject?.data ?? dataObject?.Data;
    
    if (data is JArray dataArray && dataArray.Count > 0)
    {
        var recordCount = dataArray.Count;
        bool shouldChunk;
        
        if (UseTokenBasedChunking)
        {
            var estimatedTokens = recordCount * TokensPerRowEstimate;
            shouldChunk = estimatedTokens > TokenBasedChunkingThreshold; // 700K token
        }
        else
        {
            shouldChunk = recordCount > RecordBasedChunkingThreshold; // 1000 kayıt
        }
        
        if (shouldChunk)
        {
            return await GenerateInsightHtmlChunked(dataArray.ToObject<List<dynamic>>()!, ...);
        }
    }
    
    // Küçük veri seti - doğrudan analiz
    return await GenerateInsightHtmlDirect(jsonData, uniqueId, conversationId);
}
```

### Chunk Oluşturma ve Paralel Analiz

```csharp
private async Task<string> GenerateInsightHtmlChunked(List<dynamic> data, ...)
{
    // 1. Chunking ayarları
    int rowsPerChunk = UseTokenBasedChunking 
        ? Math.Max(200, TargetTokensPerChunk / tokensPerRow)
        : RecordsPerChunk; // 1000
    
    var totalChunks = (int)Math.Ceiling(data.Count / (double)rowsPerChunk);
    
    // 2. Sampling gerekli mi? (MaxChunks = 10)
    if (totalChunks > MaxChunks)
    {
        var targetRecords = MaxChunks * rowsPerChunk;
        workingData = ApplyStratifiedSampling(data, targetRecords);
        totalChunks = MaxChunks;
    }
    
    // 3. Veri şemasını çıkar
    var dataSchema = ExtractDataSchema(workingData);
    
    // 4. Chunk'lara ayır
    var chunks = new List<DataChunk>();
    for (var i = 0; i < workingData.Count; i += rowsPerChunk)
    {
        chunks.Add(new DataChunk
        {
            Index = chunkIndex++,
            TotalChunks = totalChunks,
            Data = workingData.Skip(i).Take(rowsPerChunk).ToList(),
            RecordCount = chunkData.Count,
            EstimatedTokens = chunkData.Count * tokensPerRow
        });
    }
    
    // 5. Paralel chunk analizi (MaxParallelism = 3)
    var chunkSummaries = await AnalyzeChunksParallel(chunks, reportSummary, dataSchema, data.Count, conversationId, connectionId);
    
    // 6. Sonuçları birleştir
    var aggregatedData = AggregateChunkResults(chunkSummaries, data.Count);
    
    // 7. Final LLM analizi
    return await GenerateFinalInsightHtml(aggregatedData, uniqueId, conversationId, reportSummary);
}
```

### Paralel Chunk Analizi

```csharp
private async Task<List<ChunkSummary>> AnalyzeChunksParallel(List<DataChunk> chunks, ...)
{
    var semaphore = new SemaphoreSlim(MaxParallelism); // 3
    var results = new ChunkSummary?[chunks.Count];
    var completedCount = 0;
    
    // İlk progress bildirimi
    await SendProgressAsync(connectionId, new AnalysisProgress
    {
        Stage = "ChunkAnalysis",
        TotalChunks = chunks.Count,
        CompletedChunks = 0,
        PercentComplete = 0,
        Message = $"Veri analizi başlıyor ({chunks.Count} parça)..."
    });
    
    var tasks = chunks.Select(async (chunk, index) =>
    {
        await semaphore.WaitAsync();
        try
        {
            var summary = await AnalyzeSingleChunk(chunk, reportSummary, dataSchema, totalRecords);
            results[index] = summary;
            
            // Progress güncelle
            var completed = Interlocked.Increment(ref completedCount);
            var percentComplete = (int)(completed * 90.0 / chunks.Count);
            
            await SendProgressAsync(connectionId, new AnalysisProgress
            {
                Stage = "ChunkAnalysis",
                CurrentChunk = chunk.Index,
                TotalChunks = chunks.Count,
                CompletedChunks = completed,
                PercentComplete = percentComplete,
                Message = $"Parça {completed}/{chunks.Count} tamamlandı..."
            });
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    await Task.WhenAll(tasks);
    return results.Where(r => r != null).Cast<ChunkSummary>().ToList();
}
```

### Tek Chunk Analizi (Retry ile)

```csharp
private async Task<ChunkSummary> AnalyzeSingleChunk(DataChunk chunk, ...)
{
    for (var attempt = 1; attempt <= MaxChunkRetryAttempts; attempt++) // 3 deneme
    {
        try
        {
            var prompt = BuildChunkAnalysisPrompt(chunk, reportSummary, dataSchema, totalRecords);
            
            var history = new ChatHistory();
            history.AddSystemMessage(GetChunkAnalysisSystemPrompt());
            history.AddUserMessage(prompt);
            
            var response = await ChatCompletionService.GetChatMessageContentsAsync(history, settings, Kernel);
            return ParseChunkSummary(response[0].ToString(), chunk.Index, chunk.RecordCount);
        }
        catch (Exception ex)
        {
            if (attempt < MaxChunkRetryAttempts)
            {
                var delay = ChunkRetryBaseDelayMs * attempt; // 2000ms * attempt
                await Task.Delay(delay);
            }
        }
    }
    
    return CreateEmptyChunkSummary(chunk.Index, chunk.RecordCount);
}
```

### Chunk Sonuçlarını Birleştirme

```csharp
private AggregatedInsightData AggregateChunkResults(List<ChunkSummary> chunkSummaries, int totalRecords)
{
    var result = new AggregatedInsightData
    {
        TotalRecords = totalRecords,
        ChunkCount = chunkSummaries.Count,
        ChunkSummaries = chunkSummaries,
        AllThemes = new Dictionary<string, MergedTheme>(),
        AllCriticalCases = new List<CriticalCase>(),
        AllPatterns = new List<string>(),
        CategoryMetrics = new Dictionary<string, int>(),
        SeverityMetrics = new Dictionary<string, int>(),
        StoresMentioned = new Dictionary<string, StoreInfo>()
    };
    
    foreach (var chunk in chunkSummaries)
    {
        // Temaları birleştir (normalize ederek)
        if (chunk.Themes != null)
        {
            foreach (var theme in chunk.Themes)
            {
                var normalizedName = NormalizeThemeName(theme.Name ?? "Diğer");
                if (!result.AllThemes.ContainsKey(normalizedName))
                    result.AllThemes[normalizedName] = new MergedTheme { Name = normalizedName };
                
                result.AllThemes[normalizedName].TotalCount += theme.Count;
                result.AllThemes[normalizedName].ChunksFound.Add(chunk.ChunkId);
            }
        }
        
        // Kritik vakaları topla
        if (chunk.CriticalCases != null)
            result.AllCriticalCases.AddRange(chunk.CriticalCases);
        
        // Pattern'ları topla
        if (chunk.Patterns != null)
            result.AllPatterns.AddRange(chunk.Patterns);
        
        // Metrikleri topla
        // Mağaza bilgilerini topla
    }
    
    // Pattern'ları deduplicate et
    result.AllPatterns = result.AllPatterns
        .GroupBy(p => p.ToLowerInvariant())
        .OrderByDescending(g => g.Count())
        .Select(g => g.First())
        .Take(30).ToList();
    
    // En kritik vakaları seç (maksimum 20)
    result.AllCriticalCases = result.AllCriticalCases.Take(20).ToList();
    
    return result;
}
```

### Tema Normalizasyon Mapping

```csharp
private static readonly Dictionary<string, string[]> ThemeMappings = new()
{
        { "Ürün Kalitesi Sorunları", new[] { "ürün kalitesi", "defolu", "bozuk", "hasarlı", "quality", "defect", "damaged" } },
        { "Teslimat Gecikmeleri", new[] { "teslimat", "kargo", "gecikme", "shipment", "delivery", "delay", "late" } },
        { "Fiyat Uyumsuzlukları", new[] { "fiyat", "etiket", "kampanya", "indirim", "price", "discount", "pricing" } },
        { "Müşteri Memnuniyeti", new[] { "müşteri", "memnuniyet", "şikayet", "customer", "satisfaction", "complaint" } },
        { "Stok Sorunları", new[] { "stok", "envanter", "tükenen", "yetersiz", "inventory", "stock", "out of stock" } },
        { "Sipariş İşleme Sorunları", new[] { "sipariş", "işleme", "onay", "order", "processing", "approval" } },
        { "Ödeme Sorunları", new[] { "ödeme", "fatura", "kredi kartı", "payment", "invoice", "credit card" } },
        { "Satış Temsilcisi Performansı", new[] { "satış temsilcisi", "salesperson", "sales rep", "performans", "performance" } },
        { "Bölgesel Satış Sorunları", new[] { "bölge", "territory", "region", "satış", "sales" } },
        { "Üretim Gecikmeleri", new[] { "üretim", "manufacturing", "production", "gecikme", "delay" } },
        { "İade/İptal Sorunları", new[] { "iade", "iptal", "geri ödeme", "return", "cancel", "refund" } },
        { "Müşteri Hizmetleri", new[] { "müşteri hizmetleri", "customer service", "destek", "support" } }
};
```

---

## 7. LLM Optimize Veri Yapısı

Büyük veri setlerinde token kullanımını %90-95 azaltır:

### BuildLlmOptimizedData

```csharp
protected virtual LlmOptimizedData BuildLlmOptimizedData(
    string? instructions, 
    string uniqueId, 
    string? summary, 
    dynamic? data,
    int sampleSize = 20)
{
    var result = new LlmOptimizedData
    {
        Instructions = instructions,
        UniqueId = uniqueId,
        Summary = summary,
        TotalRecords = 0,
        DataSchema = new List<FieldSchema>(),
        SampleData = null
    };

    var itemsList = (data as IEnumerable<object>).ToList();
    result.TotalRecords = itemsList.Count;

    // İlk öğeden şemayı çıkar
    var firstItem = itemsList.First();
    var properties = GetDynamicProperties(firstItem);

    // Her alan için şema ve istatistik hesapla
    foreach (var propName in properties)
    {
        var fieldSchema = new FieldSchema { FieldName = propName };
        var values = itemsList.Select(item => GetPropertyValue(item, propName)).Where(v => v != null).ToList();
        
        var fieldType = DetermineFieldType(values.First());
        fieldSchema.FieldType = fieldType;

        if (fieldType == "Number")
        {
            var numericValues = values.Select(v => ConvertToDouble(v)).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            fieldSchema.Min = Math.Round(numericValues.Min(), 2);
            fieldSchema.Max = Math.Round(numericValues.Max(), 2);
            fieldSchema.Avg = Math.Round(numericValues.Average(), 2);
            fieldSchema.Sum = Math.Round(numericValues.Sum(), 2);
            fieldSchema.SampleValues = values.Take(5).Select(v => v?.ToString()).ToList();
        }
        else if (fieldType == "DateTime")
        {
            var dateValues = values.Select(v => ConvertToDateTime(v)).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            fieldSchema.SampleValues = new List<string> { $"Min: {dateValues.Min():yyyy-MM-dd}", $"Max: {dateValues.Max():yyyy-MM-dd}" };
        }
        else // String/Category
        {
            var distinctValues = values.Select(v => v?.ToString()).Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
            fieldSchema.DistinctCount = distinctValues.Count;
            fieldSchema.DistinctValues = distinctValues.Count <= 20 ? distinctValues : distinctValues.Take(20).ToList();
            fieldSchema.SampleValues = values.Take(5).Select(v => v?.ToString()).ToList();
        }

        result.DataSchema.Add(fieldSchema);
    }

    // Örnek veri - ilk N satır
    result.SampleData = itemsList.Take(sampleSize).ToList();

    return result;
}
```

### LlmOptimizedData ve FieldSchema Modelleri

```csharp
public class LlmOptimizedData
{
    public string? Instructions { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int TotalRecords { get; set; }                    // Toplam kayıt sayısı
    public List<FieldSchema> DataSchema { get; set; } = [];  // Şema + istatistikler
    public dynamic? SampleData { get; set; }                 // İlk 20 satır
}

public class FieldSchema
{
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = "String";    // String, Number, DateTime, Boolean
    public double? Min { get; set; }                      // Sayısal min
    public double? Max { get; set; }                      // Sayısal max
    public double? Avg { get; set; }                      // Sayısal ortalama
    public double? Sum { get; set; }                      // Sayısal toplam
    public List<string>? DistinctValues { get; set; }    // Kategorik: benzersiz değerler (max 20)
    public int? DistinctCount { get; set; }              // Benzersiz değer sayısı
    public List<string>? SampleValues { get; set; }      // Örnek değerler (ilk 5)
}
```

---

## 8. Retry Mekanizması

Exponential backoff + jitter ile retry:

### ExecuteWithRetryAsync

```csharp
private async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    Func<Exception, int, int, Task>? onRetryAsync,
    string connectionId,
    string operationName,
    CancellationToken cancellationToken = default)
{
    var exceptions = new List<Exception>();

    for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)  // 5 deneme
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (attempt < MaxRetryAttempts)
        {
            exceptions.Add(ex);
            var delay = CalculateDelay(attempt);  // Exponential backoff + jitter

            Logger.LogWarning(ex,
                "{OperationName} başarısız oldu. Deneme: {Attempt}/{MaxRetries}, Bekleme: {Delay}ms",
                operationName, attempt, MaxRetryAttempts, delay);

            // Retry callback'i çağır (varsa)
            if (onRetryAsync != null)
                await onRetryAsync(ex, attempt, delay);

            await Task.Delay(delay, cancellationToken);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
            throw new AggregateException($"{operationName} {MaxRetryAttempts} deneme sonrasında başarısız oldu.", exceptions);
        }
    }

    throw new AggregateException($"{operationName} maksimum deneme sayısına ulaştı.", exceptions);
}
```

### Delay Hesaplama (Exponential Backoff + Jitter)

```csharp
private static int CalculateDelay(int attempt)
{
    // Exponential backoff: 2^(attempt-1) * baseDelay
    // attempt 1: 1000ms, attempt 2: 2000ms, attempt 3: 4000ms, attempt 4: 8000ms
    var exponentialDelay = (int)Math.Pow(2, attempt - 1) * BaseDelayMs;  // BaseDelayMs = 1000

    // Jitter ekle (±%25 rastgele değişkenlik) - thundering herd problemini önler
    var jitter = Random.Shared.Next(-exponentialDelay / 4, exponentialDelay / 4);

    var totalDelay = exponentialDelay + jitter;

    // Max delay sınırını uygula
    return Math.Min(totalDelay, MaxDelayMs);  // MaxDelayMs = 10000
}
```

---

## Kayıtlı Rapor Servisleri

### DI Registration

```csharp
// AI.Api/Extensions/DependencyInjectionExtensions.cs

// SQL Server Raporları
services.AddKeyedScoped<IReportService, AdventureWorksReportService>("adventureworks");

```

### Servis Detayları

| Service Key | Database | Prompt Dosyası | Açıklama |
|-------------|----------|----------------|----------|
| `adventureworks` | SQL Server | `adventurerworks_server_assistant_prompt.md` | AdventureWorks demo raporları |

---

## Önemli Özellikler

| Özellik | Açıklama |
|---------|----------|
| **Keyed Services** | Rapor türüne göre DI ile dinamik servis seçimi |
| **Abstract Base Class** | Kod tekrarını önleyen `SqlServerReportServiceBase` (2283 satır) |
| **SQL Agent Pipeline** | LLM ürettiği SQL'i doğrulama, optimize etme ve güvenlik kontrolü |
| **Long-Term Memory** | Kullanıcı tercihlerini hatırlama ve kişiselleştirme |
| **Retry Mechanism** | 5 deneme, exponential backoff + jitter |
| **Chunk-Based Analysis** | 1000+ kayıt için paralel chunk analizi (MaxParallelism=3) |
| **LLM Optimize Data** | Şema + istatistik + örnek veri ile %90-95 token tasarrufu |
| **Fast Dashboard** | Template-based hızlı HTML üretimi (5-10x hızlı) |
| **Legacy Dashboard** | Full HTML generation (esnek ama yavaş) |
| **SignalR Streaming** | Loading mesajları + progress bildirimleri + sonuç gönderimi |
| **History Tracking** | Tüm konuşma ve metadata PostgreSQL'de saklanır |
| **Fail-Open** | Pipeline hatalarında sistem çalışmaya devam eder |
| **Dynamic Templates** | Veritabanı ve rapor listelerini dinamik inject etme |

---

## Veri Akışı Özeti

```
Kullanıcı Sorusu
    ↓
RouteConversationUseCase (LLM analizi + dinamik listeler + memory context)
    ↓
ReportService seçimi (Keyed Services)
    ↓
System Prompt + Memory Context
    ↓
LLM → JSON (Summary + SQL Query + Suggestions)
    ↓
SQL Agent Pipeline (validation/optimization/security)
    ↓
SQL Server execution (retry ile)
    ↓
┌───────────────────────────────────────────┐
│         PARALEL İŞLEME                    │
├───────────────────────────────────────────┤
│ Dashboard HTML     │ Insight HTML         │
│ (Fast/Legacy)      │ (Chunk-based)        │
│                    │                      │
│ • LLM Optimize     │ • Chunk oluştur      │
│   Data (şema)      │ • Paralel analiz     │
│ • JSON Config      │ • Sonuç birleştir    │
│ • Template HTML    │ • Final analiz       │
└───────────────────────────────────────────┘
    ↓
DashboardService (dosya kaydetme)
    ↓
SignalR → Frontend
    ↓
Memory Extraction (fire-and-forget)
```

---

## İlgili Dosyalar

| Dosya | Satır Sayısı | Açıklama |
|-------|--------------|----------|
| `RouteConversationUseCase.cs` | 456 | İstek yönlendirme (Agent Registry dispatch) |
| `SqlServerReportServiceBase.cs` | 2283 | SQL Server base class |
| `SqlAgentPipeline.cs` | 559 | SQL validation/optimization |
| `InsightAnalysisDtos.cs` | 550 | Chunk analizi DTO'ları |
| `DashboardConfig.cs` | 419 | Dashboard JSON konfigürasyonu |
| `DashboardFileService.cs` | 163 | Dashboard dosya işlemleri |
| `ResponseModel.cs` | ~100 | LLM response modelleri |
| `AdventureWorksReportService.cs` | ~50 | AdventureWorks raporları |

---

## Sonuç

Rapor sistemi, **doğal dil → SQL → HTML dashboard** dönüşümünü tam otomatik olarak gerçekleştiren, hataya dayanıklı ve ölçeklenebilir bir mimariye sahiptir. Özellikle:

- ✅ LLM ile akıllı SQL üretimi ve doğrulama
- ✅ SQL Server desteği (AdventureWorks)
- ✅ SQL Agent Pipeline (validation + optimization + security)
- ✅ Otomatik retry mekanizması (exponential backoff + jitter)
- ✅ Chunk-based paralel veri analizi (büyük veri setleri için)
- ✅ LLM optimize veri yapısı (%90-95 token tasarrufu)
- ✅ Kişiselleştirilmiş yanıtlar (Long-Term Memory)
- ✅ Gerçek zamanlı iletişim (SignalR + progress bildirimleri)
- ✅ Fail-open yaklaşımı (pipeline hatalarında sistem çalışmaya devam eder)
- ✅ Template-based hızlı dashboard üretimi (5-10x performans artışı)

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Multi-Agent.md](Multi-Agent.md) | SQL Agent pipeline |
| [Scheduled-Reports.md](Scheduled-Reports.md) | Zamanlanmış rapor sistemi (Hangfire) |
| [Application-Layer.md](Application-Layer.md) | Application Layer detayları |
| [Conversation-Router.md](Conversation-Router.md) | İstek yönlendirme (Agent Registry) |
| [DuckDB-Excel.md](DuckDB-Excel.md) | DuckDB Excel analiz sistemi |
| [Infrastructure-Cross-Cutting.md](Infrastructure-Cross-Cutting.md) | Cache, Rate Limiting, Error Handling |
| [System-Overview.md](System-Overview.md) | Genel sistem analizi |

---

> **Not:** Bu döküman rapor sisteminin teknik analizi için referans olarak kullanılacaktır.
