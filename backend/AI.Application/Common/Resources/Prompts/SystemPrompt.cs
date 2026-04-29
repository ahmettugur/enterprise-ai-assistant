using AI.Application.Common.Helpers;

namespace AI.Application.Common.Resources.Prompts;

public static class SystemPrompt
{

    // Instructions kaldırıldı - Dashboard talimatları dashboard_generator_prompt_adventureworks.md dosyasında zaten detaylı olarak mevcut
    // Kullanıcının özel talimatları için instructions alanı dinamik olarak kullanılıyor
    public const string Instructions = "";

    /// <summary>
    /// Yeni rapor seçim prompt'unu asenkron olarak getirir
    /// </summary>
    /// <returns>Rapor seçim prompt'u</returns>
    public static async Task<string> GetNewReportSelectionPromptAsync()
    {
        // Burada prompt dosyasından okuma yapılabilir
        // Şimdilik basit bir prompt döndürüyoruz
        return await Task.FromResult("""
            Sen bir sohbet asistanısın. Kullanıcının yazdıklarını Türkçe olarak anlayıp, doğal ve akıcı bir şekilde Türkçe cevaplar ver.  
            Cevapların açık, samimi ve bilgilendirici olsun.  
            Eğer kullanıcı teknik bir konu sorarsa doğru ve detaylı açıklama yap.  
            Eğer kullanıcı günlük bir konu hakkında sohbet ediyorsa, kısa ve doğal cevaplar ver.  
            Kullanıcının niyetini anlamaya çalış, konuyu devam ettirecek şekilde diyalog kur.  
            Sadece Türkçe konuş, İngilizce veya başka bir dilde cevap verme.  
            Yanıtlarda gereksiz tekrar yapma, konu dışına çıkma.  
            Kullanıcının özel verilerini veya kişisel bilgilerini isteme ya da saklama.  
            Kullanıcı teknik yardım isterse adım adım açıklama yap, gerekiyorsa örnek kod ekle.  
            Kullanıcının yazım hatalarını doğal biçimde düzelt ama bunu açıkça belirtme.  
            Tüm yanıtlar sıcak, saygılı ve doğal bir Türkçe tonunda olmalı.
            """);
    }

    /// <summary>
    /// Dosya analiz prompt'unu asenkron olarak getirir
    /// </summary>
    /// <returns>Dosya analiz prompt'u</returns>
    public static async Task<string> FileAnalysisPromptAsync()
    {
        return await Task.FromResult(
            Helper.ReadFileContent("Common/Resources/Prompts", "file_analysis_prompt.md"));
    }

    /// <summary>
    /// Excel SQL üretici prompt'unu dinamik olarak oluşturur
    /// </summary>
    /// <param name="tableName">Sanitize edilmiş tablo adı</param>
    /// <param name="rowCount">Toplam satır sayısı</param>
    /// <param name="columns">Sütun bilgileri (ad ve tip)</param>
    /// <param name="sampleData">Örnek veri JSON</param>
    /// <param name="userQuery">Kullanıcının sorusu</param>
    /// <returns>Hazırlanmış prompt</returns>
    public static string GetExcelSqlGeneratorPrompt(
        string tableName,
        long rowCount,
        string columns,
        string sampleData,
        string userQuery)
    {
        var template = Helper.ReadFileContent("Common/Resources/Prompts", "excel_sql_generator_prompt.md");
        
        return template
            .Replace("{{tableName}}", tableName)
            .Replace("{{rowCount}}", rowCount.ToString())
            .Replace("{{columns}}", columns)
            .Replace("{{sampleData}}", sampleData)
            .Replace("{{userQuery}}", userQuery);
    }

    /// <summary>
    /// Excel çoklu analiz planı prompt'unu dinamik olarak oluşturur.
    /// Spesifik sorularda tek SQL, genel analiz isteklerinde çoklu SQL döndürür.
    /// </summary>
    /// <param name="tableName">Sanitize edilmiş tablo adı</param>
    /// <param name="rowCount">Toplam satır sayısı</param>
    /// <param name="columns">Sütun bilgileri (ad ve tip)</param>
    /// <param name="sampleData">Örnek veri JSON</param>
    /// <param name="userQuery">Kullanıcının sorusu</param>
    /// <returns>Hazırlanmış prompt</returns>
    public static string GetExcelAnalysisPlanPrompt(
        string tableName,
        long rowCount,
        string columns,
        string sampleData,
        string userQuery)
    {
        var template = Helper.ReadFileContent("Common/Resources/Prompts", "excel_analysis_plan_prompt.md");
        
        return template
            .Replace("{{tableName}}", tableName)
            .Replace("{{rowCount}}", rowCount.ToString())
            .Replace("{{columns}}", columns)
            .Replace("{{sampleData}}", sampleData)
            .Replace("{{userQuery}}", userQuery);
    }

    /// <summary>
    /// Excel veri yorumlama prompt'unu dinamik olarak oluşturur
    /// </summary>
    /// <param name="userQuery">Kullanıcının sorusu</param>
    /// <param name="rowCount">Sonuç satır sayısı</param>
    /// <param name="executionTime">Sorgu çalışma süresi (ms)</param>
    /// <param name="dataJson">Veri JSON formatında</param>
    /// <returns>Hazırlanmış prompt</returns>
    public static string GetExcelInterpretPrompt(
        string userQuery,
        long rowCount,
        long executionTime,
        string dataJson)
    {
        var template = Helper.ReadFileContent("Common/Resources/Prompts", "excel_interpret_prompt.md");
        
        return template
            .Replace("{{userQuery}}", userQuery)
            .Replace("{{rowCount}}", rowCount.ToString())
            .Replace("{{executionTime}}", executionTime.ToString())
            .Replace("{{dataJson}}", dataJson);
    }

    /// <summary>
    /// Excel çoklu sonuç yorumlama prompt'unu dinamik olarak oluşturur.
    /// Birden fazla sorgu sonucunu tek seferde yorumlar.
    /// </summary>
    /// <param name="userQuery">Kullanıcının sorusu</param>
    /// <param name="queryCount">Toplam sorgu sayısı</param>
    /// <param name="allResults">Tüm sorgu sonuçları birleştirilmiş TOON formatında</param>
    /// <returns>Hazırlanmış prompt</returns>
    public static string GetExcelMultiInterpretPrompt(
        string userQuery,
        int queryCount,
        string allResults)
    {
        var template = Helper.ReadFileContent("Common/Resources/Prompts", "excel_multi_interpret_prompt.md");
        
        return template
            .Replace("{{userQuery}}", userQuery)
            .Replace("{{queryCount}}", queryCount.ToString())
            .Replace("{{allResults}}", allResults);
    }
}

