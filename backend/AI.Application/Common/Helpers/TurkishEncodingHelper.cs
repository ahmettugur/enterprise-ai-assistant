namespace AI.Application.Common.Helpers;

/// <summary>
/// Türkçe karakter encoding sorunlarını düzelten yardımcı sınıf
/// PDF extraction, JSON escaping ve Windows-1252 encoding hatalarını handle eder
/// </summary>
public static class TurkishEncodingHelper
{
    /// <summary>
    /// Türkçe karakter encoding sorunlarını düzeltir
    /// - Windows-1252 (Latin-5) encoding hataları
    /// - Unicode escape sequence hataları
    /// - PDF font encoding hataları
    /// </summary>
    public static string FixEncoding(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var corrected = text
            // Windows-1252 (Latin-5) encoding hatalarından kaynaklanan yanlış karakterleri düzelt
            // ş (s with cedilla) ← þ (thorn)
            .Replace("þ", "ş")
            .Replace("Þ", "Ş")

            // ı (dotless i) ← ý (y with acute)
            .Replace("ý", "ı")
            .Replace("Ý", "I")

            // ğ (g with breve) ← ð (eth)
            .Replace("ð", "ğ")
            .Replace("Ð", "Ğ")

            // ç (c with cedilla)
            .Replace("¢", "ç")
            .Replace("¢", "Ç")

            // İ (dotted capital I)
            .Replace("Í", "İ")

            // Unicode escape sequence hatalarından kaynaklanan karakterler
            // \u00FC → ü (Latin Small Letter U with Diaeresis)
            .Replace("\\u00FC", "ü")
            .Replace("\\u00FD", "ý")
            .Replace("\\u00FE", "þ")

            // \u00DF → ß (German beta / Turkish s sesi)
            .Replace("\\u00DF", "ß")

            // \u0131 → ı (Dotless I)
            .Replace("\\u0131", "ı")

            // \u00F6 → ö (Latin Small Letter O with Diaeresis)
            .Replace("\\u00F6", "ö")

            // \u00D6 → Ö (Latin Capital Letter O with Diaeresis)
            .Replace("\\u00D6", "Ö")

            // \u00DC → Ü (Latin Capital Letter U with Diaeresis)
            .Replace("\\u00DC", "Ü")

            // Büyük harfler
            .Replace("\\u0130", "İ")  // İ (Dotted Capital I)
            .Replace("\\u00C7", "Ç")  // Ç
            .Replace("\\u00E7", "ç")  // ç
            .Replace("\\u011E", "Ğ")  // Ğ
            .Replace("\\u011F", "ğ")  // ğ
            .Replace("\\u015E", "Ş")  // Ş
            .Replace("\\u015F", "ş"); // ş

        return corrected;
    }

    /// <summary>
    /// Metni temizler ve normalize eder (embedding için)
    /// </summary>
    public static string CleanForEmbedding(string text, int maxLength = 8000)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Türkçe karakter encoding sorunlarını düzelt
        var corrected = FixEncoding(text);

        // Fazla boşlukları temizle
        var cleaned = System.Text.RegularExpressions.Regex.Replace(corrected.Trim(), @"\s+", " ");

        // Çok uzun metinleri kısalt (OpenAI token limiti için)
        if (cleaned.Length > maxLength)
        {
            cleaned = cleaned.Substring(0, maxLength);
            // Son kelimeyi tamamla
            var lastSpace = cleaned.LastIndexOf(' ');
            if (lastSpace > maxLength - 100)
            {
                cleaned = cleaned.Substring(0, lastSpace);
            }
        }

        return cleaned;
    }

    /// <summary>
    /// Dosya adından Türkçe karakterleri, özel karakterleri ve boşlukları temizler.
    /// Qdrant collection adı, dosya sistemi ve veritabanı tutarlılığı için kullanılır.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unnamed";

        // Dosya adı ve uzantıyı ayır
        var extension = System.IO.Path.GetExtension(fileName);
        var nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

        // Türkçe karakterleri ASCII karşılıklarına dönüştür
        var turkishMap = new Dictionary<char, string>
        {
            { 'ç', "c" }, { 'Ç', "C" },
            { 'ğ', "g" }, { 'Ğ', "G" },
            { 'ı', "i" }, { 'İ', "I" },
            { 'ö', "o" }, { 'Ö', "O" },
            { 'ş', "s" }, { 'Ş', "S" },
            { 'ü', "u" }, { 'Ü', "U" }
        };

        var sanitized = new System.Text.StringBuilder(nameWithoutExtension.Length);

        foreach (var c in nameWithoutExtension)
        {
            if (turkishMap.TryGetValue(c, out var replacement))
            {
                sanitized.Append(replacement);
            }
            else if (c == ' ')
            {
                sanitized.Append('_');
            }
            else if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                sanitized.Append(c);
            }
            // Diğer özel karakterler atlanır
        }

        var result = sanitized.ToString();
        
        // Birden fazla alt çizgiyi tek alt çizgiye indir
        while (result.Contains("__"))
        {
            result = result.Replace("__", "_");
        }

        // Baş ve sondaki alt çizgileri temizle
        result = result.Trim('_');

        // Boş kalırsa varsayılan isim ver
        if (string.IsNullOrEmpty(result))
            result = "file";

        return result + extension.ToLowerInvariant();
    }

    /// <summary>
    /// Dosya adından DuckDB tablo adı oluşturur.
    /// Uzantı hariç, sadece harf, rakam ve alt çizgi içerir.
    /// </summary>
    public static string SanitizeTableName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "data_table";

        // Uzantıyı çıkar
        var nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

        // Türkçe karakterleri ASCII karşılıklarına dönüştür
        var turkishMap = new Dictionary<char, string>
        {
            { 'ç', "c" }, { 'Ç', "C" },
            { 'ğ', "g" }, { 'Ğ', "G" },
            { 'ı', "i" }, { 'İ', "I" },
            { 'ö', "o" }, { 'Ö', "O" },
            { 'ş', "s" }, { 'Ş', "S" },
            { 'ü', "u" }, { 'Ü', "U" }
        };

        var sanitized = new System.Text.StringBuilder(nameWithoutExtension.Length);

        foreach (var c in nameWithoutExtension)
        {
            if (turkishMap.TryGetValue(c, out var replacement))
            {
                sanitized.Append(replacement);
            }
            else if (c == ' ' || c == '-' || c == '.')
            {
                sanitized.Append('_');
            }
            else if (char.IsLetterOrDigit(c) || c == '_')
            {
                sanitized.Append(char.ToLowerInvariant(c));
            }
            // Diğer özel karakterler atlanır
        }

        var result = sanitized.ToString();

        // Birden fazla alt çizgiyi tek alt çizgiye indir
        while (result.Contains("__"))
        {
            result = result.Replace("__", "_");
        }

        // Baş ve sondaki alt çizgileri temizle
        result = result.Trim('_');

        // Sayı ile başlıyorsa önüne t_ ekle (SQL identifier kuralı)
        if (!string.IsNullOrEmpty(result) && char.IsDigit(result[0]))
        {
            result = "t_" + result;
        }

        // Boş kalırsa varsayılan isim ver
        if (string.IsNullOrEmpty(result))
            result = "data_table";

        return result;
    }
}
