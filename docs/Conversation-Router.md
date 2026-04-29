# 🔀 RouteConversationUseCase - İstek Yönlendirme Sistemi

## 📋 İçindekiler

- [Genel Bakış](#genel-bakış)
- [Mimari Yapı](#mimari-yapı)
- [Dosya Yapısı](#dosya-yapısı)
- [Ana Akış](#ana-akış)
- [Action Türleri](#action-türleri)
- [LLM Tabanlı Yönlendirme](#llm-tabanlı-yönlendirme)
- [Template Sistemi](#template-sistemi)
- [State Machine](#state-machine)
- [Metadata Servisleri](#metadata-servisleri)
- [Keyed Services ile Rapor Seçimi](#keyed-services-ile-rapor-seçimi)
- [Hata Yönetimi](#hata-yönetimi)

---

## Genel Bakış

`RouteConversationUseCase`, kullanıcı isteklerini analiz ederek **üç ana moda** yönlendiren akıllı bir yönlendirme sistemidir:

| Mod | Açıklama | Servis |
|-----|----------|--------|
| **Chat** | Genel sohbet, soru-cevap | `IAIChatUseCase` |
| **Document** | Doküman arama (RAG) | `IRagSearchUseCase` |
| **Report** | Veritabanı raporları | `IReportService` (Keyed) |

### Temel Özellikler

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CONVERSATION ROUTER ÖZELLİKLERİ                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✅ LLM Tabanlı İstek Analizi                                               │
│     → GPT-5.2 ile kullanıcı niyetini anlama                                 │
│     → Türkçe/İngilizce destek                                               │
│                                                                             │
│  ✅ Dinamik Template Sistemi                                                │
│     → Veritabanından kategori/doküman listesi                               │
│     → Runtime HTML generation                                               │
│                                                                             │
│  ✅ State Machine                                                           │
│     → Çok adımlı wizard akışları                                            │
│     → Bağlam koruma (reportName, documentName)                              │
│                                                                             │
│  ✅ Agent Registry Pattern                                                  │
│     → IActionAgentRegistry ile agent dispatch                               │
│     → Strategy + Registry pattern (Open/Closed Principle)                   │
│     → DI ile loosely coupled agent yönetimi                                 │
│                                                                             │
│  ✅ Keyed Services Pattern                                                  │
│     → Rapor türüne göre dinamik servis seçimi                               │
│     → DI ile loosely coupled                                                │
│                                                                             │
│  ✅ Long-Term Memory Entegrasyonu                                           │
│     → Kullanıcı bağlamı prompt'a inject                                     │
│     → Kişiselleştirilmiş yanıtlar                                           │
│                                                                             │
│  ✅ ReAct Pattern Entegrasyonu                                              │
│     → IReActUseCase ile merkezi düşünme-gözlem döngüsü                      │
│     → SignalR ile frontend'e ReAct adımları gönderimi                       │
│     → appsettings.json ile açma/kapama                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Mimari Yapı

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       CONVERSATION ROUTER ARCHITECTURE                      │
└─────────────────────────────────────────────────────────────────────────────┘

                              USER REQUEST
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         RouteConversationUseCase                                  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                    RouteAsync(ChatRequest)                            │  │
│  │                                                                       │  │
│  │  1. ValidateRequest()                                                 │  │
│  │  2. ReAct THOUGHT (IReActUseCase)    ────────────┐                    │  │
│  │  3. SelectModeWithLlmAsync()  ───────────────────┤                    │  │
│  │  4. ReAct OBSERVATION (IReActUseCase)             │                    │  │
│  │  5. Route to appropriate service                 │                    │  │
│  │                                                  │                    │  │
│  └──────────────────────────────────────────────────│────────────────────┘  │
│                                                     │                       │
│                                                     ▼                       │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                    LLM Analysis Pipeline                              │  │
│  │                                                                       │  │
│  │  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐   │  │
│  │  │ Build Prompt    │ →  │ Inject Metadata │ →  │ LLM Analysis    │   │  │
│  │  │ (router-v3.md)  │    │ (DB lists)      │    │ (GPT-5.2)       │   │  │
│  │  └─────────────────┘    └─────────────────┘    └────────┬────────┘   │  │
│  │                                                         │            │  │
│  │                                                         ▼            │  │
│  │                                              ┌─────────────────┐     │  │
│  │                                              │ Parse JSON      │     │  │
│  │                                              │ RouterResponse  │     │  │
│  │                                              └─────────────────┘     │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
└───────────────────────────────────────┬─────────────────────────────────────┘
                                        │
              ┌─────────────────────────┼─────────────────────────┐
              │                         │                         │
              ▼                         ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐   ┌─────────────────────┐
│    IAIChatUseCase   │   │   IRagSearchUseCase │   │    IReportService   │
│                     │   │                     │   │    (Keyed)          │
│  • Streaming chat   │   │  • Vector search    │   │  • SQL generation   │
│  • Excel analysis   │   │  • HyDE + Hybrid    │   │  • Report rendering │
│  • Function calling │   │  • Highlighting     │   │  • Comparison       │
└─────────────────────┘   └─────────────────────┘   └─────────────────────┘
```

---

## Dosya Yapısı

```
AI.Application/
├── Ports/Secondary/Services/AgentCore/    # Agent Registry Interfaces
│   ├── IActionAgent.cs                    # Agent interface (ActionName, CanHandle, HandleAsync)
│   └── IActionAgentRegistry.cs            # Registry interface (FindAgent, GetAllAgents)
│
├── UseCases/
│   ├── IRouteConversationUseCase.cs       # Interface (19 satır)
│   ├── RouteConversationUseCase.cs        # Ana implementasyon (456 satır)
│   ├── ActionAgents/                      # Agent Implementations
│   │   ├── ActionAgentRegistry.cs         # Registry — agent discovery & dispatch
│   │   ├── ChatActionAgent.cs             # "chat" → IAIChatUseCase
│   │   ├── DocumentActionAgent.cs         # "document" → IRagSearchUseCase
│   │   ├── ReportActionAgent.cs           # "report" → IReportService (Keyed)
│   │   └── AskActionAgent.cs              # "ask_*" → Dynamic template + SignalR
│
├── (Infrastructure Layer — Report Implementations)
│   AI.Infrastructure/Adapters/AI/Reports/SqlServer/
│       ├── SqlServerReportServiceBase.cs   # SQL Server abstract base
│       └── AdventureWorksReportService.cs  # Keyed: "adventureworks"
│
├── Common/Resources/Prompts/
│   └── conversation-orchestrator.md        # LLM yönlendirme prompt'u (601 satır)
│
└── UseCases/
    ├── DocumentMetadataUseCase.cs         # Doküman metadata (306 satır)
    └── ReportMetadataUseCase.cs           # Rapor metadata (427 satır)
```

---

## Ana Akış

### OrchestrateAsync Metodu

```csharp
public async Task<Result<dynamic>> OrchestrateAsync(ChatRequest request, CancellationToken ct)
{
    // 1. Request Validation
    ValidateRequest(request);  // ConnectionId, Prompt boş olamaz
    
    // 2. ReAct THOUGHT — LLM ile düşünme adımı
    await _reActUseCase.SendThoughtAsync(request, "Routing");
    
    // 3. LLM ile İstek Analizi
    var llmResult = await SelectModeWithLlmAsync(request);
    
    // 4. ReAct OBSERVATION — Yönlendirme sonucu
    await _reActUseCase.SendObservationAsync(request, 
        $"Kullanıcı isteği '{llmResult.Action}' moduna yönlendirildi.");
    
    // 5. Agent Registry ile Yönlendirme (Strategy + Registry Pattern)
    var agent = _agentRegistry.FindAgent(llmResult.Action)
        ?? throw new InvalidOperationException($"Action agent not found: {llmResult.Action}");
    
    var context = new ActionContext(request, llmResult);
    var apiResult = await agent.HandleAsync(context, ct);
    return apiResult;
}
```

> **Not:** Önceki sürümde `OrchestrateAsync` içinde `if/else` zinciri ve `HandleAskActionAsync()`, `GetTemplateContentAsync()`, `GetReportService()` gibi private metotlar bulunuyordu. Agent Registry refactoring ile bu mantık `ChatActionAgent`, `DocumentActionAgent`, `ReportActionAgent` ve `AskActionAgent` sınıflarına taşınmıştır.

### Request Validation

```csharp
private static void ValidateRequest(ChatRequest request)
{
    ArgumentNullException.ThrowIfNull(request);
    
    if (string.IsNullOrWhiteSpace(request.ConnectionId))
        throw new ArgumentException("ConnectionId boş olamaz");
        
    if (string.IsNullOrWhiteSpace(request.Prompt))
        throw new ArgumentException("Prompt boş olamaz");
}
```

---

## Action Türleri

### Terminal Actions (İşlem Yapanlar)

| Action | Açıklama | Servis |
|--------|----------|--------|
| `chat` | Sohbet modu - streaming yanıt | `IAIChatUseCase.GetStreamingChatResponseAsync()` |
| `document` | Doküman arama - RAG | `IAIChatUseCase.SearchVectorStoreAsync()` |
| `report` | Rapor oluşturma - SQL | `IReportService.GetReportsWithHtmlAsync()` |
| `compare` | Rapor karşılaştırma | `IReportService` (aynı servis) |

### Wizard Actions (Adım Adım)

| Action | Açıklama | Template |
|--------|----------|----------|
| `welcome` | Ana menü | `welcome.html` |
| `ask_chat` | Chat onayı | `ask_chat.html` |
| `ask_mode` | Mod seçimi (belirsiz) | `ask_mode.html` |
| `ask_document` | Kategori seçimi | Dinamik |
| `ask_document_category` | Doküman seçimi | Dinamik |
| `ask_report` | Veritabanı seçimi | Dinamik |
| `ask_database` | Veritabanı seçimi (alt) | Dinamik |
| `ask_report_type` | Rapor türü seçimi | Dinamik |
| `ask_dynamic_report_type` | Dinamik rapor kategorisi | Dinamik |
| `ask_ready_report` | Hazır rapor linkleri | Dinamik |
| `error` | Hata durumu | - |

### RouterResponse Model

```csharp
public class RouterResponse
{
    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonProperty("reportName")]
    public string ReportName { get; set; } = string.Empty;  // "adventureworks"
    
    [JsonProperty("documentName")]
    public string DocumentName { get; set; } = string.Empty;  // "hukuk.pdf"
    
    [JsonProperty("templateName")]
    public string TemplateName { get; set; } = string.Empty;  // "ask_document_category_hukuk"
    
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonProperty("errorType")]
    public string ErrorType { get; set; } = string.Empty;
    
    [JsonProperty("suggestion")]
    public string Suggestion { get; set; } = string.Empty;
}
```

---

## LLM Tabanlı Yönlendirme

### SelectModeWithLlmAsync

```csharp
private async Task<RouterResponse?> SelectModeWithLlmAsync(ChatRequest request)
{
    for (var attempt = 0; attempt < MaxRetries; attempt++)  // Max 3 retry
    {
        try
        {
            // 1. LLM analizi yap
            var result = await ExecuteLlmAnalysisAsync(request);
            
            // 2. JSON parse et
            var analysisResult = await ParseLlmResponseAsync(result, request);
            
            return analysisResult;
        }
        catch (Exception ex)
        {
            if (attempt < MaxRetries - 1)
            {
                // Exponential backoff: 500ms, 1000ms, 2000ms
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
                continue;
            }
        }
    }
    return null;
}
```

### ExecuteLlmAnalysisAsync

```csharp
private async Task<string> ExecuteLlmAnalysisAsync(ChatRequest request)
{
    // 1. Base prompt'u oku
    var basePrompt = Helper.ReadFileContent("Common/Resources/Prompts", "conversation-orchestrator.md");
    
    // 2. Dinamik listeleri inject et
    var categoryList = await _documentMetadataService.GenerateCategoryListForPromptAsync();
    var documentList = await _documentMetadataService.GenerateDocumentListForPromptAsync();
    var databaseList = _reportMetadataService.GenerateDatabaseListForPrompt();
    var reportTypeList = _reportMetadataService.GenerateReportTypeListForPrompt();
    var dynamicReportList = _reportMetadataService.GenerateDynamicReportCategoryListForPrompt();
    
    // 3. Long-Term Memory bağlamını al
    var memoryContext = await _userMemoryService.BuildMemoryContextAsync(request.Prompt);
    
    // 4. Prompt'u oluştur
    var fullSystemPrompt = basePrompt
        .Replace("{{CATEGORY_LIST}}", categoryList)
        .Replace("{{DOCUMENT_LIST}}", documentList)
        .Replace("{{DATABASE_LIST}}", databaseList)
        .Replace("{{REPORT_TYPE_LIST}}", reportTypeList)
        .Replace("{{DYNAMIC_REPORT_CATEGORY_LIST}}", dynamicReportList);
    
    // 5. Memory context ekle
    if (!string.IsNullOrEmpty(memoryContext))
    {
        fullSystemPrompt = $"{fullSystemPrompt}\n\n## User Context\n{memoryContext}";
    }
    
    // 6. Chat history ile LLM çağır
    var chatHistory = await _historyService.ReplaceSystemPromptAsync(request, fullSystemPrompt);
    await _historyService.AddUserMessageAsync(request, request.Prompt);
    
    var response = await _chatCompletionService.GetChatMessageContentsAsync(
        chatHistory,
        new OpenAIPromptExecutionSettings { Temperature = 0.1F },
        _kernel);
    
    return response[0].ToString();
}
```

### Prompt Yapısı (conversation-orchestrator.md)

```markdown
# OPERASYONEL KARAR VE YÖNLENDİRME MOTORU

## JSON Yanıt Formatı (ZORUNLU)
{
   "action": "string",
   "reportName": "string",
   "documentName": "string",
   "templateName": "string",
   "message": "string"
}

## Öncelik Sırası
P0: Güvenlik kontrolü → Tehdit varsa reddet
P1: Menü isteği → "menü", "ana menü" → welcome
P2: Dosya kontrolü → fileName dolu mu?
P3: History kontrolü → Son action'a göre devam
P4: İçerik analizi → Anahtar kelime puanlama

## Dinamik Listeler (Runtime Inject)
{{CATEGORY_LIST}}        → Doküman kategorileri
{{DOCUMENT_LIST}}        → Dokümanlar
{{DATABASE_LIST}}        → Veritabanları
{{REPORT_TYPE_LIST}}     → Rapor türleri
{{DYNAMIC_REPORT_CATEGORY_LIST}}  → Dinamik rapor kategorileri
```

### JSON Parse

```csharp
private async Task<RouterResponse> ParseLlmResponseAsync(string result, ChatRequest request)
{
    string jsonString = result;
    
    // Markdown kod bloğu içinde JSON ara
    var match = JsonBlockRegex.Match(result);  // ```json ... ```
    if (match.Success)
    {
        jsonString = match.Groups[1].Value;
    }
    
    // Satır sonlarını temizle
    jsonString = LineBreakRegex.Replace(jsonString, " ");
    
    try
    {
        return JsonConvert.DeserializeObject<RouterResponse>(jsonString);
    }
    catch (JsonException)
    {
        // Retry için history'ye hata mesajı ekle
        await _historyService.AddUserMessageAsync(request, 
            "JSON formatı bulunamadı. Tekrar dene.");
        throw;
    }
}
```

---

## Template Sistemi

> **Not:** `GetTemplateContentAsync` metodu Agent Registry refactoring ile `AskActionAgent` sınıfına taşınmıştır. Aşağıdaki kod artık `AI.Application/UseCases/ActionAgents/AskActionAgent.cs` dosyasındadır.

### AskActionAgent — GetTemplateContentAsync

```csharp
// AskActionAgent.cs içinde
private async Task<string> GetTemplateContentAsync(string templateName, string fallbackMessage)
{
    // === DOKÜMAN İŞLEMLERİ ===
    
    if (templateName == "ask_document")
        return await _documentMetadataService.GenerateDynamicCategorySelectionTemplateAsync();
    
    if (templateName.StartsWith("ask_document_category_"))
    {
        var category = templateName.Replace("ask_document_category_", "");
        return await _documentMetadataService.GenerateDynamicDocumentTemplateAsync(category);
    }
    
    // === RAPOR İŞLEMLERİ ===
    
    if (templateName == "ask_report" || templateName == "ask_database")
        return _reportMetadataService.GenerateDynamicDatabaseSelectionTemplate();
    
    if (templateName.StartsWith("ask_report_type_"))
    {
        var databaseId = templateName.Replace("ask_report_type_", "");
        return _reportMetadataService.GenerateDynamicReportTypeTemplate(databaseId);
    }
    
    if (templateName.StartsWith("ask_dynamic_report_type_"))
    {
        var databaseId = templateName.Replace("ask_dynamic_report_type_", "");
        return _reportMetadataService.GenerateDynamicReportCategoryTemplate(databaseId);
    }
    
    if (templateName == "ask_ready_report")
        return _reportMetadataService.GenerateReadyReportTemplate();
    
    // === STATİK TEMPLATE ===
    var htmlContent = Helper.ReadFileContent("Common/Resources/Templates", $"{templateName}.html");
    if (!string.IsNullOrWhiteSpace(htmlContent))
        return htmlContent;
    
    return fallbackMessage;
}
```

### Template Akış Örneği

```
Kullanıcı: "Rapor oluşturmak istiyorum"

1. LLM Analizi:
   → action: "ask_report"
   → templateName: "ask_report"

2. GetTemplateContentAsync("ask_report"):
   → _reportMetadataService.GenerateDynamicDatabaseSelectionTemplate()
   → HTML: Veritabanı seçim kartları (SQL Server)

3. SignalR ile frontend'e gönder:
   → ReceiveMessage(htmlContent)

4. Kullanıcı "SQL Server" seçer:
   → action: "ask_report_type"
   → templateName: "ask_report_type_adventureworks"
   → reportName: "adventureworks"

5. GetTemplateContentAsync("ask_report_type_adventureworks"):
   → _reportMetadataService.GenerateDynamicReportTypeTemplate("adventureworks")
   → HTML: Rapor türü kartları (Dinamik, Hazır)

... (akış devam eder)
```

---

## State Machine

### Rapor Akışı

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           RAPOR STATE MACHINE                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌───────────┐    "Rapor"     ┌─────────────┐
│  welcome  │ ─────────────► │ ask_report  │
└───────────┘                └──────┬──────┘
                                    │
                                    ▼
                            ┌─────────────────┐
                            │  ask_database   │  ← Veritabanı seçimi
                            └───────┬─────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │ "AdventureWorks" │
                    ▼                               ▼
           ┌─────────────────┐             ┌─────────────────┐
           │ask_report_type  │             │ask_report_type  │
           │ (adventureworks)│
           └───────┬─────────┘             └───────┬─────────┘
                   │                               │
          ┌────────┴────────┐              ┌───────┴───────┐
          │ "Dinamik"       │ "Hazır"      │               │
          ▼                 ▼              ▼               ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ask_dynamic_rep. │  │ask_ready_report │  │ask_dynamic_rep. │
│(adventureworks) │
└───────┬─────────┘  └─────────────────┘  └───────┬─────────┘
        │                                          │
        │ "AdventureWorks Reports"                 │ "Sales"
        ▼                                          ▼
┌─────────────────┐                       ┌─────────────────┐
│     report      │                       │     report      │
│ adventureworks  │
└─────────────────┘                       └─────────────────┘
```

### Doküman Akışı

```
┌───────────┐   "Doküman"    ┌──────────────┐
│  welcome  │ ─────────────► │ ask_document │  ← Kategori seçimi
└───────────┘                └──────┬───────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │ "Hukuk"            "Teknik"   │
                    ▼                               ▼
           ┌─────────────────────┐         ┌─────────────────────┐
           │ask_document_category│         │ask_document_category│
           │      (hukuk)        │         │      (teknik)       │
           └─────────┬───────────┘         └─────────┬───────────┘
                     │                               │
                     │ "anayasa.pdf"                 │ "proje.pdf"
                     ▼                               ▼
              ┌─────────────┐                 ┌─────────────┐
              │  document   │                 │  document   │
              │ anayasa.pdf │                 │ proje.pdf   │
              └─────────────┘                 └─────────────┘
```

### State Geçiş Kuralları

| Mevcut State | Kullanıcı Girdisi | Sonraki State | Korunan Değer |
|--------------|-------------------|---------------|---------------|
| `report` | Aynı bağlamda soru | `report` | `reportName` KORU |
| `report` | Farklı rapor türü | `report` | `reportName` = yeni |
| `report` | "Karşılaştır" | `compare` | `reportName` KORU |
| `document` | Aynı dokümanda arama | `document` | `documentName` KORU |
| `document` | Farklı doküman | `ask_document_category` | `documentName` = yeni |
| `*` | "Menü", "Ana menü" | `welcome` | Tümünü sıfırla |

---

## Metadata Servisleri

### IDocumentMetadataService

Doküman ve kategori bilgilerini veritabanından çeker, prompt'a inject eder.

```csharp
public interface IDocumentMetadataService
{
    // Veritabanından dokümanları çek (kullanıcı bazlı filtreleme)
    Task<List<PromptDocumentInfo>> GetAllDocumentsAsync();
    
    // Prompt için format: | Kategori | Doküman | Anahtar Kelimeler |
    Task<string> GenerateDocumentListForPromptAsync();
    
    // Prompt için kategori listesi
    Task<string> GenerateCategoryListForPromptAsync();
    
    // Dinamik HTML template (doküman kartları)
    Task<string> GenerateDynamicDocumentTemplateAsync(string categoryId);
    
    // Dinamik kategori seçim template'i
    Task<string> GenerateDynamicCategorySelectionTemplateAsync();
}
```

### IReportMetadataService

Rapor, veritabanı ve dinamik rapor kategorisi bilgilerini yönetir.

```csharp
public interface IReportMetadataService
{
    // Veritabanı listesi (SQL Server)
    Task<List<DatabaseInfo>> GetAllDatabasesAsync();
    
    // Veritabanına göre rapor türleri
    Task<List<ReportTypeInfo>> GetReportTypesByDatabaseAsync(string databaseId);
    
    // Dinamik rapor kategorileri (adventureworks)
    Task<List<DynamicReportCategory>> GetDynamicReportCategoriesAsync();
    
    // Prompt inject formatları
    string GenerateDatabaseListForPrompt();
    string GenerateReportTypeListForPrompt();
    string GenerateDynamicReportCategoryListForPrompt();
    
    // Dinamik HTML template'leri
    string GenerateDynamicDatabaseSelectionTemplate();
    string GenerateDynamicReportTypeTemplate(string databaseId);
    string GenerateDynamicReportCategoryTemplate(string databaseId);
    string GenerateReadyReportTemplate();
}
```

### Prompt Inject Örneği

```markdown
## Veritabanları
{{DATABASE_LIST}}
↓ Runtime'da:
| ID | Adı | Açıklama |
|----|-----|----------|
| adventureworks | SQL Server AdventureWorks | Demo veritabanı |
| adventureworks | AdventureWorks | Demo SQL Server DB |

## Dinamik Rapor Kategorileri
{{DYNAMIC_REPORT_CATEGORY_LIST}}
↓ Runtime'da:
| ID | Veritabanı | Adı | reportName |
|----|------------|-----|------------|
| adventureworks | adventureworks | AdventureWorks | adventureworks |
| adventureworks | adventureworks | Satış Raporu | adventureworks |
```

---

## Keyed Services ile Rapor Seçimi

### DI Kaydı

```csharp
// DependencyInjectionExtensions.cs
services.AddKeyedScoped<IReportService, AdventureWorksReportService>("adventureworks");

// ApplicationExtensions.cs — Agent Registry
services.AddScoped<IActionAgent, ChatActionAgent>();
services.AddScoped<IActionAgent, DocumentActionAgent>();
services.AddScoped<IActionAgent, ReportActionAgent>();
services.AddScoped<IActionAgent, AskActionAgent>();
services.AddScoped<IActionAgentRegistry, ActionAgentRegistry>();
```

### GetReportService (ReportActionAgent içinde)

> **Not:** `GetReportService` metodu artık `ReportActionAgent` sınıfındadır.

```csharp
// ReportActionAgent.cs içinde — Keyed service ile dinamik servis çözümleme
private IReportService GetReportService(string reportType)
{
    if (string.IsNullOrWhiteSpace(reportType))
        throw new ArgumentException("Rapor tipi boş olamaz");
    
    try
    {
        return _serviceProvider.GetRequiredKeyedService<IReportService>(
            reportType.ToLowerInvariant());
    }
    catch (InvalidOperationException)
    {
        throw new ArgumentException($"Rapor tipi bulunamadı: {reportType}");
    }
}
```

### Keyed Service Akışı

```
LLM Response: { action: "report", reportName: "adventureworks" }
                                        │
                                        ▼
              GetReportService("adventureworks")
                                        │
                                        ▼
    _serviceProvider.GetRequiredKeyedService<IReportService>("adventureworks")
                                        │
                                        ▼
              ┌─────────────────────────────────────────┐
              │  AdventureWorksReportService            │
              │                                         │
              │  • SqlServerReportServiceBase'den türer │
              │  • SQL Server AdventureWorks'a bağlanır│
              │  • SQL Agent ile sorgu üretir           │
              │  • HTML rapor render eder               │
              └─────────────────────────────────────────┘
```

---

## Hata Yönetimi

### Error Action Handling

```csharp
if (llmSelectionResult.Action == HandleErrorAction)
{
    // Error durumunda suggestion'daki action'a geç
    llmSelectionResult.Action = llmSelectionResult.Suggestion;
}
```

### Error Types

| errorType | Açıklama | suggestion |
|-----------|----------|------------|
| `ambiguous_request` | Belirsiz istek | `ask_mode` |
| `invalid_context` | Geçersiz bağlam | `welcome` |
| `security_violation` | Güvenlik ihlali | `chat` |
| `invalid_selection` | Geçersiz seçim | İlgili `ask_*` |

### Retry Mekanizması

```csharp
private const int MaxRetries = 3;
private const int BaseDelayMs = 500;

for (var attempt = 0; attempt < MaxRetries; attempt++)
{
    try
    {
        // LLM analizi
        return await ExecuteLlmAnalysisAsync(request);
    }
    catch (Exception ex)
    {
        if (attempt < MaxRetries - 1)
        {
            // Exponential backoff
            var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
            // 500ms → 1000ms → 2000ms
            await Task.Delay(delay);
            continue;
        }
        _logger.LogError(ex, "Tüm denemeler başarısız");
    }
}
```

### Exception Handling

```csharp
catch (ArgumentException ex)
{
    _logger.LogWarning(ex, "Geçersiz parametre");
    activity?.SetStatus(ActivityStatusCode.Error, "Geçersiz parametre");
    return Result<dynamic>.Error("Geçersiz parametre.");
}
catch (Exception ex)
{
    _logger.LogError(ex, "RouteAsync metodunda hata oluştu");
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.SetTag("exception.type", ex.GetType().Name);
    return Result<dynamic>.Error("İstek işlenirken bir hata oluştu.");
}
```

---

## OpenTelemetry Tracing

```csharp
using var activity = ActivitySources.Chat.StartActivity("ChatRoute");
if (activity != null)
{
    activity.SetTag("conversation.id", request.ConversationId);
    activity.SetTag("connection.id", request.ConnectionId);
    activity.SetTag("prompt", request.Prompt);
    activity.SetTag("action", action);  // LLM sonucu
    
    BaggageHelper.SetContextBaggage(
        conversationId: request.ConversationId, 
        requestId: request.ConnectionId);
    BaggageHelper.AddBaggageToActivity(activity);
}
```

---

## Özet

| Özellik | Değer |
|---------|-------|
| **Dosya** | `RouteConversationUseCase.cs` (456 satır) |
| **Ana Metod** | `OrchestrateAsync(ChatRequest)` |
| **LLM Modeli** | GPT-5.2 (Temperature: 0.1) |
| **Retry** | 3 deneme, exponential backoff |
| **Actions** | 15+ farklı action türü |
| **Keyed Services** | 6 rapor servisi |
| **Prompt** | `conversation-orchestrator.md` (601 satır) |

### Kritik Bağımlılıklar

| Servis | Amaç |
|--------|------|
| `IActionAgentRegistry` | Agent Registry — action dispatch |
| `IActionAgent` (×4) | `ChatActionAgent`, `DocumentActionAgent`, `ReportActionAgent`, `AskActionAgent` |
| `IAIChatUseCase` | Chat ve doküman arama |
| `IReportService` (Keyed) | Rapor oluşturma |
| `IConversationUseCase` | Conversation history |
| `IDocumentMetadataService` | Doküman listesi inject |
| `IReportMetadataService` | Rapor listesi inject |
| `IUserMemoryService` | Long-term memory |
| `IChatCompletionService` | LLM çağrısı |
| `ISignalRHubContext` | Real-time iletişim |
| `IReActUseCase` | ReAct düşünme-gözlem döngüsü |

---

## İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [System-Overview.md](System-Overview.md) | Genel sistem mimarisi |
| [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) | Agentic AI pattern'leri |
| [Multi-Agent.md](Multi-Agent.md) | SQL Agent pipeline |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
| [Chat-System.md](Chat-System.md) | Chat sistemi özellikleri |
| [Report-System.md](Report-System.md) | Rapor sistemi detayları |

---

> **Not:** Bu döküman RouteConversationUseCase ve Agent Registry sistemi için referans olarak kullanılacaktır.
