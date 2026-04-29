namespace AI.Application.Common.Constants;

/// <summary>
/// Qdrant vector database'deki collection isimleri için yardımcı sınıf
/// </summary>
public static class QdrantCollections
{
    /// <summary>
    /// Collection adı suffix'i
    /// </summary>
    private const string CollectionSuffix = "_documents";

    /// <summary>
    /// Türkçe karakter dönüşüm haritası
    /// </summary>
    private static readonly Dictionary<char, string> TurkishCharMap = new()
    {
        { 'ç', "c" }, { 'Ç', "c" },
        { 'ğ', "g" }, { 'Ğ', "g" },
        { 'ı', "i" }, { 'İ', "i" },
        { 'ö', "o" }, { 'Ö', "o" },
        { 'ş', "s" }, { 'Ş', "s" },
        { 'ü', "u" }, { 'Ü', "u" }
    };

    /// <summary>
    /// Dosya adından collection adını dinamik olarak oluşturur
    /// Türkçe karakterler ASCII karşılıklarına dönüştürülür
    /// Örnek: "döküman.pdf" -> "dokuman_documents"
    /// Örnek: "Soru Cevap.json" -> "soru_cevap_documents"
    /// Örnek: "Türkçe Döküman.pdf" -> "turkce_dokuman_documents"
    /// </summary>
    /// <param name="fileName">Dosya adı (uzantılı veya uzantısız)</param>
    /// <returns>Qdrant collection adı</returns>
    public static string GetCollectionName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Dosya adı boş olamaz", nameof(fileName));

        // Uzantıyı kaldır
        var name = Path.GetFileNameWithoutExtension(fileName);

        // Türkçe karakterleri dönüştür ve normalize et
        var normalized = new System.Text.StringBuilder(name.Length);
        
        foreach (var c in name.ToLowerInvariant())
        {
            if (TurkishCharMap.TryGetValue(c, out var replacement))
            {
                normalized.Append(replacement);
            }
            else if (c == ' ' || c == '-' || c == '.')
            {
                normalized.Append('_');
            }
            else if (char.IsLetterOrDigit(c) || c == '_')
            {
                normalized.Append(c);
            }
            // Diğer özel karakterler atlanır
        }

        var result = normalized.ToString();

        // Ardışık alt çizgileri tekli yap
        while (result.Contains("__"))
        {
            result = result.Replace("__", "_");
        }

        // Baş ve sondaki alt çizgileri kaldır
        result = result.Trim('_');

        // Boş kalırsa varsayılan isim ver
        if (string.IsNullOrEmpty(result))
            result = "document";

        return $"{result}{CollectionSuffix}";
    }

    /// <summary>
    /// Birden fazla dosya için collection adlarını döndürür
    /// </summary>
    public static List<string> GetCollectionNames(IEnumerable<string> fileNames)
    {
        return fileNames.Select(GetCollectionName).Distinct().ToList();
    }

    /// <summary>
    /// Collection adından dosya adı prefix'ini çıkarır
    /// Örnek: "dokuman_documents" -> "dokuman"
    /// </summary>
    public static string GetFileNamePrefixFromCollection(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            return string.Empty;

        return collectionName.EndsWith(CollectionSuffix)
            ? collectionName[..^CollectionSuffix.Length]
            : collectionName;
    }
}