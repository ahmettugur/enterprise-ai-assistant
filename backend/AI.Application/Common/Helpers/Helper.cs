namespace AI.Application.Common.Helpers;

public class Helper
{
    public static string ReadFileContent(string directoryPath, string fileName)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directoryPath, fileName);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Dosya bulunamadı: {filePath}");
        return File.ReadAllText(filePath);
    }

    /// <summary>
    /// Base64 string'i MemoryStream'e dönüştürür
    /// </summary>
    /// <param name="base64String">Base64 formatındaki string</param>
    /// <returns>MemoryStream</returns>
    /// <exception cref="ArgumentException">Geçersiz base64 string</exception>
    public static MemoryStream ConvertBase64ToStream(string base64String)
    {
        try
        {
            // Base64 string'in başında data URL prefix'i varsa temizle
            if (base64String.Contains(','))
            {
                base64String = base64String.Split(',')[1];
            }

            var bytes = Convert.FromBase64String(base64String);
            return new MemoryStream(bytes);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Geçersiz base64 string formatı", nameof(base64String), ex);
        }
    }

    /// <summary>
    /// Dosya uzantısından MIME türünü belirler
    /// </summary>
    /// <param name="fileName">Dosya adı</param>
    /// <returns>MIME türü</returns>
    public static string GetMimeTypeFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Dosya uzantısının desteklenip desteklenmediğini kontrol eder
    /// </summary>
    /// <param name="fileName">Dosya adı</param>
    /// <returns>Destekleniyorsa true</returns>
    public static bool IsFileExtensionSupported(string fileName)
    {
        var allowedExtensions = new[] { ".pdf", ".txt", ".docx", ".doc" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(fileExtension);
    }
}