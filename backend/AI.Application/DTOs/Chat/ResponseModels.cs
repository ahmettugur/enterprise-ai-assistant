using System.Text.Json.Serialization;

namespace AI.Application.DTOs.Chat;

/// <summary>
/// LLM yanıt modeli
/// </summary>
public class LLmResponseModel
{
    public bool IsSuccess { get; set; } = true;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConversationId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; set; }
    
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Query { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HtmlMessage { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> Suggestions { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public dynamic? Data { get; set; }
}

/// <summary>
/// HTML oluşturma modeli
/// </summary>
public class GenerateHtmlModel
{
    public string? Instructions { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public List<DataForHtmlModel> Data { get; set; } = new();
}

/// <summary>
/// HTML oluşturma için veri modeli
/// </summary>
public class DataForHtmlModel
{
    public string? Instructions { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public dynamic? Data { get; set; }
}

/// <summary>
/// LLM'e gönderilecek optimize edilmiş veri yapısı.
/// Tüm veri yerine şema + istatistik + örnek veri içerir.
/// Token kullanımını %90-95 azaltır.
/// </summary>
public class LlmOptimizedData
{
    public string? Instructions { get; set; }
    public string UniqueId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    
    /// <summary>Toplam kayıt sayısı</summary>
    public int TotalRecords { get; set; }
    
    /// <summary>Veri şeması - alan adları, tipleri ve istatistikler</summary>
    public List<FieldSchema> DataSchema { get; set; } = new();
    
    /// <summary>Örnek veri - ilk N satır (varsayılan 20)</summary>
    public dynamic? SampleData { get; set; }
}

/// <summary>
/// Veri alanı şeması - LLM'in tasarım kararları vermesi için yeterli bilgi
/// </summary>
public class FieldSchema
{
    /// <summary>Alan adı</summary>
    public string FieldName { get; set; } = string.Empty;
    
    /// <summary>Alan tipi: String, Number, DateTime, Boolean</summary>
    public string FieldType { get; set; } = "String";
    
    /// <summary>Sayısal alanlar için minimum değer</summary>
    public double? Min { get; set; }
    
    /// <summary>Sayısal alanlar için maksimum değer</summary>
    public double? Max { get; set; }
    
    /// <summary>Sayısal alanlar için ortalama değer</summary>
    public double? Avg { get; set; }
    
    /// <summary>Sayısal alanlar için toplam değer</summary>
    public double? Sum { get; set; }
    
    /// <summary>String/kategori alanları için benzersiz değerler (max 20)</summary>
    public List<string>? DistinctValues { get; set; }
    
    /// <summary>Benzersiz değer sayısı</summary>
    public int? DistinctCount { get; set; }
    
    /// <summary>Örnek değerler (ilk 5)</summary>
    public List<string>? SampleValues { get; set; }
}
