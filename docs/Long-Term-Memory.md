# 🧠 Long-Term Memory - Implementasyon Detayları

## 📋 İçindekiler

- [Genel Bakış](#genel-bakis)
- [Mimari Tasarım](#mimari-tasarim)
- [Implementasyon Detayları](#implementasyon-detaylari)
- [Dosya Listesi](#dosya-listesi)
- [Karşılaşılan Sorunlar ve Çözümler](#karsilasilan-sorunlar)
- [Test Senaryoları](#test-senaryolari)

---

<a id="genel-bakis"></a>

## 🎯 Genel Bakış

### Ne Yapıldı?

Kullanıcı tercihlerini ve bilgilerini kalıcı olarak saklayan, konuşmalardan otomatik bilgi çıkaran bir **Long-Term Memory** sistemi implementasyonu yapıldı.

### Amaç

| Önceki Durum | Yeni Durum |
|--------------|------------|
| Her sohbet bağımsız, kullanıcı her seferinde aynı bilgileri söylemek zorunda | Sistem kullanıcıyı tanıyor, tercihlerini hatırlıyor |
| Kişiselleştirme yok | Kişiselleştirilmiş yanıtlar ve öneriler |
| Sadece session bazlı context | Kalıcı context (PostgreSQL + Qdrant) |

### Temel Özellikler

- ✅ **L0 Context:** Tarih bilgisi (LLM tarih hesaplamaları için)
- ✅ **L1 Context:** JWT'den kullanıcı bilgileri (isim, rol)
- ✅ **L2 Context:** Öğrenilen tercihler (semantic search ile)
- ✅ **Otomatik Extraction:** Konuşmadan bilgi çıkarma
- ✅ **Fire-and-Forget:** Ana akışı bloklamadan arka planda kayıt
- ✅ **GDPR Uyumlu:** "Beni Unut" özelliği

---

<a id="mimari-tasarim"></a>

## 🏗️ Mimari Tasarım

### Katmanlı Memory Stratejisi

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           MEMORY CONTEXT KATMANLARI                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ L0: Tarih Bilgisi                                                       │    │
│  │ "Tarih: 16 Aralık 2025, Pazartesi (Saat: 14:30)"                        │    │
│  │ → LLM "son 3 ay", "geçen hafta" gibi ifadeleri çözümleyebilir           │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ L1: JWT Claims                                                          │    │
│  │ "Kullanıcı: Ahmet Yılmaz (Admin, Supervisor)"                           │    │
│  │ → Zaten authentication'da mevcut, ek maliyet yok                        │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ L2: Öğrenilen Tercihler                                                 │    │
│  │ "Context: tercih_edilen_format: Excel; departman: Finans"               │    │
│  │ → Qdrant semantic search ile en alakalı 5 memory getirilir              │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Veri Akışı

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                              MEMORY OKUMA AKIŞI                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  Kullanıcı Mesajı                                                                │
│       │                                                                          │
│       ▼                                                                          │
│  ┌────────────────────────┐                                                      │
│  │ BuildMemoryContextAsync│                                                      │
│  └───────────┬────────────┘                                                      │
│              │                                                                   │
│   ┌──────────┼──────────┐                                                        │
│   │          │          │                                                        │
│   ▼          ▼          ▼                                                        │
│ L0:Tarih  L1:JWT   L2:Qdrant                                                     │
│   │          │          │                                                        │
│   └──────────┼──────────┘                                                        │
│              │                                                                   │
│              ▼                                                                   │
│  "Tarih: 16 Aralık 2025. Kullanıcı: Ahmet (Admin).                               │
│   Context: tercih_edilen_format: Excel; departman: Finans"                       │
│              │                                                                   │
│              ▼                                                                   │
│  ┌────────────────────────┐                                                      │
│  │   System Prompt'a Ekle │                                                      │
│  └────────────────────────┘                                                      │
│                                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────────┐
│                              MEMORY YAZMA AKIŞI                                  │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  Chat Yanıtı Tamamlandı                                                          │
│       │                                                                          │
│       ▼                                                                          │
│  ┌─────────────────────────────┐                                                 │
│  │ userId = CurrentUserService │  ← Task.Run öncesi yakala!                      │
│  └───────────┬─────────────────┘                                                 │
│              │                                                                   │
│              ▼                                                                   │
│  ┌─────────────────────────────┐                                                 │
│  │ Task.Run (fire-and-forget) │  ← Ana akışı bloklamaz                           │
│  └───────────┬─────────────────┘                                                 │
│              │                                                                   │
│              ▼                                                                   │
│  ┌─────────────────────────────┐                                                 │
│  │ ExtractAndStoreMemoriesAsync│                                                 │
│  └───────────┬─────────────────┘                                                 │
│              │                                                                   │
│              ▼                                                                   │
│  ┌─────────────────────────────┐                                                 │
│  │ LLM Extraction Prompt       │                                                 │
│  │ (Türkçe prompt)             │                                                 │
│  └───────────┬─────────────────┘                                                 │
│              │                                                                   │
│              ▼                                                                   │
│  ┌─────────────────────────────┐                                                 │
│  │ JSON Parse                  │                                                 │
│  │ [{"key":"..","value":".."}] │                                                 │
│  └───────────┬─────────────────┘                                                 │
│              │                                                                   │
│   ┌──────────┴──────────┐                                                        │
│   ▼                     ▼                                                        │
│ PostgreSQL           Qdrant                                                      │
│ (UserMemory)     (Vector Embed)                                                  │
│                                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

<a id="implementasyon-detaylari"></a>

## 💻 Implementasyon Detayları

### 1. Domain Entity

```csharp
// AI.Domain/Memory/UserMemory.cs
public class UserMemory : BaseEntity
{
    public string UserId { get; private set; }
    public string Key { get; private set; }           // "tercih_edilen_format"
    public string Value { get; private set; }         // "Excel"
    public MemoryCategory Category { get; private set; }
    public float Confidence { get; private set; }     // 0.0 - 1.0
    public int UsageCount { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }
    
    public static UserMemory Create(string userId, string key, string value, 
        MemoryCategory category, float confidence = 0.8f);
    
    public void UpdateValue(string newValue);
    public void IncrementUsage();
    public void BoostConfidence();
    public string ToEmbeddingText();  // Qdrant için
}

public enum MemoryCategory
{
    Preference,    // Tercihler
    Interaction,   // Etkileşim kalıpları
    Feedback,      // Geri bildirimler
    WorkContext    // İş bağlamı
}
```

### 2. Repository (IDbContextFactory Pattern)

```csharp
// AI.Infrastructure/Adapters/Persistence/Repositories/UserMemoryRepository.cs
public class UserMemoryRepository : IUserMemoryRepository
{
    private readonly IDbContextFactory<ChatDbContext> _contextFactory;
    
    // Her metod kendi DbContext'ini oluşturur - Thread-safe!
    public async Task<UserMemory?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.UserMemories
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
    }
}
```

### 3. Service Layer

```csharp
// AI.Application/UseCases/UserMemoryUseCase.cs
public sealed class UserMemoryUseCase : IUserMemoryUseCase
{
    private const string MEMORY_COLLECTION = "user_memories";
    private const int MAX_MEMORIES_PER_USER = 100;
    private const float MIN_EXTRACTION_CONFIDENCE = 0.6f;
    private const int MIN_MESSAGE_LENGTH_FOR_EXTRACTION = 10;

    // Memory Context Oluşturma (Okuma)
    public async Task<string> BuildMemoryContextAsync(string query, CancellationToken ct)
    {
        var sb = new StringBuilder();
        
        // L0: Tarih
        sb.Append($"Tarih: {DateTime.Now:d MMMM yyyy, dddd} (Saat: {DateTime.Now:HH:mm})");
        
        // L1: JWT Claims
        if (_currentUserService.IsAuthenticated)
            sb.Append($". Kullanıcı: {_currentUserService.DisplayName}");
        
        // L2: Semantic Search ile alakalı memories
        var relevantMemories = await GetRelevantMemoriesAsync(query, topK: 5, ct);
        if (relevantMemories.Count > 0)
            sb.Append($". Context: {string.Join("; ", memories)}");
        
        return sb.ToString();
    }

    // Memory Extraction (Yazma)
    public async Task ExtractAndStoreMemoriesAsync(
        string userMessage, string assistantResponse, string userId, CancellationToken ct)
    {
        // Türkçe extraction prompt
        var extractionPrompt = @"Bu konuşmayı analiz et...";
        
        var response = await _chatCompletionService.GetChatMessageContentAsync(...);
        var extractedMemories = ParseExtractedMemories(response.Content);
        
        foreach (var memory in extractedMemories.Where(m => m.Confidence >= 0.6f))
        {
            await AddOrUpdateMemoryAsync(userId, memory, ct);
        }
    }
}
```

### 4. Servis Entegrasyonu

```csharp
// AI.Application/UseCases/AIChatUseCase.cs
public async Task<Result<LLmResponseModel>> GetStreamingChatResponseAsync(ChatRequest request, ...)
{
    // Memory context'i system prompt'a ekle
    var memoryContext = await _userMemoryService.BuildMemoryContextAsync(request.Prompt, ct);
    if (!string.IsNullOrEmpty(memoryContext))
        systemPrompt = $"{systemPrompt}\n\n## User Context\n{memoryContext}";
    
    // ... chat işlemi ...
    
    // Yanıt tamamlandıktan sonra memory extraction (fire-and-forget)
    var currentUserId = _currentUserService.UserId;
    if (!string.IsNullOrEmpty(currentUserId))
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _userMemoryService.ExtractAndStoreMemoriesAsync(
                    request.Prompt, fullResponse, currentUserId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Memory extraction failed");
            }
        }, CancellationToken.None);
    }
}
```

### 5. Rapor Servislerinde Entegrasyon

```csharp
// AI.Infrastructure/Adapters/AI/Reports/SqlServer/SqlServerReportServiceBase.cs

// Constructor'a eklendi:
protected readonly IUserMemoryUseCase? UserMemoryService;
protected readonly ICurrentUserService? CurrentUserService;

// GetReportsWithHtmlAsync metodunda:
// 1. Memory okuma (system prompt'a ekleme)
var memoryContext = await UserMemoryService.BuildMemoryContextAsync(request.Prompt);
systemPrompt = systemPrompt + "\n\n" + memoryContext;

// 2. Memory yazma (rapor sonunda)
if (UserMemoryService != null && CurrentUserService != null)
{
    var currentUserId = CurrentUserService.UserId;
    if (!string.IsNullOrEmpty(currentUserId) && response.IsSucceed)
    {
        _ = Task.Run(async () =>
        {
            await UserMemoryService.ExtractAndStoreMemoriesAsync(
                request.Prompt, response.ResultData.HtmlMessage, currentUserId, ...);
        }, CancellationToken.None);
    }
}
```

---

<a id="dosya-listesi"></a>

## 📁 Dosya Listesi

### Yeni Oluşturulan Dosyalar

| Dosya | Açıklama |
|-------|----------|
| `AI.Domain/Memory/UserMemory.cs` | Domain entity |
| `AI.Domain/Enums/MemoryCategory.cs` | Enum |
| `AI.Domain/Memory/IUserMemoryRepository.cs` | Repository interface |
| `AI.Application/Ports/Primary/UseCases/IUserMemoryUseCase.cs` | UseCase interface |
| `AI.Application/UseCases/UserMemoryUseCase.cs` | UseCase implementasyonu |
| `AI.Application/Ports/Primary/UseCases/IUserMemoryUseCase.cs` içinde `UserMemoryDto` | DTO (record) |
| `AI.Application/Ports/Primary/UseCases/IUserMemoryUseCase.cs` içinde `ExtractedMemoryDto` | Extraction DTO (record) |
| `AI.Infrastructure/Adapters/Persistence/Repositories/UserMemoryRepository.cs` | Repository implementasyonu |

### Güncellenen Dosyalar

| Dosya | Değişiklik |
|-------|------------|
| `AI.Infrastructure/Adapters/Persistence/ChatDbContext.cs` | UserMemories DbSet eklendi |
| `AI.Api/Extensions/ConversationServiceExtensions.cs` | `AddPooledDbContextFactory` |
| `AI.Application/UseCases/AIChatUseCase.cs` | Memory entegrasyonu |
| `AI.Infrastructure/Adapters/AI/Reports/SqlServer/SqlServerReportServiceBase.cs` | Memory entegrasyonu |
| `AI.Infrastructure/Adapters/AI/Reports/SqlServer/AdventureWorksReportService.cs` | Constructor güncelleme |
| SQL Server rapor servisleri | Constructor güncelleme |

---

<a id="karsilasilan-sorunlar"></a>

## 🐛 Karşılaşılan Sorunlar ve Çözümler

### 1. DbContext Concurrency Hatası

**Sorun:**

```
A second operation was started on this context instance before a previous 
operation completed. This is usually caused by different threads concurrently 
using the same instance of DbContext.
```

**Sebep:** `Task.Run` içinde DbContext kullanılıyordu, ama DbContext thread-safe değil.

**Çözüm:** `IDbContextFactory<ChatDbContext>` pattern'i kullanıldı:

```csharp
// Her metod kendi DbContext'ini oluşturur
await using var context = await _contextFactory.CreateDbContextAsync(ct);
```

---

### 2. DI Lifetime Conflict

**Sorun:**

```
Error while validating the service descriptor 'ServiceType: IDbContextFactory<ChatDbContext> 
Lifetime: Singleton': Cannot consume scoped service 'ChatDbContext' from singleton.
```

**Sebep:** `AddDbContext` + `AddDbContextFactory` birlikte kullanılıyordu.

**Çözüm:** `AddPooledDbContextFactory` tek başına kullanıldı:

```csharp
services.AddPooledDbContextFactory<ChatDbContext>(options => ...);

// Scoped DbContext için factory'den al
services.AddScoped<ChatDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<ChatDbContext>>().CreateDbContext());
```

---

### 3. Background Thread'de UserId Null

**Sorun:** `Task.Run` içinde `ICurrentUserService.UserId` null dönüyordu.

**Sebep:** `ICurrentUserService` scoped bir servis, `Task.Run` içinde scope kayboluyordu.

**Çözüm:** UserId'yi `Task.Run` öncesinde yakala:

```csharp
// ✅ Doğru
var currentUserId = _currentUserService.UserId;  // Task.Run öncesi
_ = Task.Run(async () =>
{
    await _userMemoryService.ExtractAndStoreMemoriesAsync(..., currentUserId, ...);
});

// ❌ Yanlış
_ = Task.Run(async () =>
{
    var userId = _currentUserService.UserId;  // NULL olacak!
});
```

---

### 4. LLM Boş JSON Döndürüyor

**Sorun:** Memory extraction'da LLM sürekli `[]` döndürüyordu.

**Sebep:** Prompt çok kısıtlayıcıydı ve sadece iş odaklı bilgiler listelenmişti.

**Çözüm:** Prompt genişletildi:

- Kullanıcı adı/ismi eklendi
- "Hatırlanmasını istediği herhangi bir bilgi" eklendi
- Confidence threshold 0.8'den 0.6'ya düşürüldü
- Prompt Türkçe'ye çevrildi

---

<a id="test-senaryolari"></a>

## 🧪 Test Senaryoları

### Memory Extraction Testleri

| Mesaj | Beklenen Extraction |
|-------|---------------------|
| "Benim adım Ahmet, aklında tut" | `kullanici_adi: Ahmet` |
| "Ben finans departmanında çalışıyorum" | `departman: Finans` |
| "Raporları Excel formatında göster" | `tercih_edilen_format: Excel` |
| "Haftalık satış raporlarına bakıyorum" | `ilgilenilen_rapor: Haftalık satış` |
| "Grafikleri seviyorum" | `tercih_edilen_gosterim: Grafik` |

### Memory Context Testleri

| Senaryo | Beklenen Context |
|---------|------------------|
| Yeni kullanıcı | `"Tarih: 16 Aralık 2025. Kullanıcı: [JWT'den]"` |
| Memory'si olan kullanıcı | `"Tarih: ... Kullanıcı: ... Context: key1: value1; key2: value2"` |
| Anonim kullanıcı | `"Tarih: 16 Aralık 2025"` |

---

## 📊 Metrikler

| Metrik | Değer |
|--------|-------|
| **Ortalama extraction süresi** | ~1-2 saniye |
| **Memory context token maliyeti** | ~50-80 token |
| **Maksimum memory/kullanıcı** | 100 |
| **Minimum confidence threshold** | 0.6 |
| **Desteklenen servis sayısı** | 8+ |

---

## 📚 Referanslar

- [Semantic Kernel Memory](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Qdrant Vector Database](https://qdrant.tech/)
- [EF Core DbContextFactory](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

---

## 📚 İlgili Dökümanlar

| Döküman | Açıklama |
|---------|----------|
| [Agentic-AI-Patterns.md](Agentic-AI-Patterns.md) | Agentic AI pattern'leri (Memory Pattern) |
| [Qdrant-Vector-Search.md](Qdrant-Vector-Search.md) | Qdrant vektör arama |
| [System-Overview.md](System-Overview.md) | Genel sistem analizi |
| [Chat-System.md](Chat-System.md) | Chat sistemi özellikleri |
| [Application-Layer.md](Application-Layer.md) | UseCase katmanı detayları |
| [Authentication-Authorization.md](Authentication-Authorization.md) | JWT Auth + kullanıcı kimliği |

---

> **Not:** Bu döküman gelecek geliştirmeler için referans olarak kullanılacaktır.
