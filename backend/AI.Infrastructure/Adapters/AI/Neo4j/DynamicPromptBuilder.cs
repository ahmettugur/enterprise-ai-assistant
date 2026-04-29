using AI.Application.DTOs.Neo4j;

using System.Text;
using System.Text.RegularExpressions;
using AI.Application.Common.Helpers;
using AI.Application.Configuration;
using AI.Application.DTOs.DynamicPrompt;
using AI.Application.Ports.Secondary.Services.AIChat;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Infrastructure.Adapters.AI.Neo4j;

/// <summary>
/// Kullanıcı sorgusuna göre dinamik SQL prompt'u oluşturan servis
/// Neo4j Schema Catalog'dan ilgili tabloları bulur ve optimize edilmiş prompt oluşturur
/// </summary>
public class DynamicPromptBuilder : IDynamicPromptBuilder
{
    private readonly ISchemaGraphService _schemaGraphService;
    private readonly Neo4jSettings _settings;
    private readonly ILogger<DynamicPromptBuilder> _logger;
    
    // V2 Prompt template dosya adı
    private const string V2PromptFileName = "adventurerworks_server_assistant_prompt_v2.md";
    private const string PromptFolder = "Common/Resources/Prompts";
    
    // Türkçe yaygın ekler - stemming için kullanılır
    // Sıralama önemli: Uzun ekler önce kontrol edilmeli
    private static readonly string[] TurkishSuffixes = new[]
    {
        // İsim hal ekleri (uzundan kısaya)
        "lerinden", "larından", "lerinde", "larında", "lerine", "larına",
        "leriyle", "larıyla", "lerini", "larını", "lerden", "lardan",
        "lerde", "larda", "lerin", "ların", "lere", "lara",
        "ından", "inden", "unda", "ünde", "ında", "inde",
        "ıyla", "iyle", "uyla", "üyle", "ını", "ini", "unu", "ünü",
        "dan", "den", "tan", "ten", "nın", "nin", "nun", "nün",
        "ler", "lar", "ın", "in", "un", "ün", "ya", "ye",
        "da", "de", "ta", "te", "na", "ne", "ı", "i", "u", "ü"
    };
    
    // Stop words - aramada görmezden gelinecek kelimeler
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ve", "veya", "ile", "için", "bir", "bu", "şu", "o", "ne", "nasıl",
        "hangi", "kaç", "en", "daha", "çok", "az", "tüm", "her", "olan",
        "olan", "gibi", "göre", "arasında", "üzerinde", "altında", "içinde",
        "the", "and", "or", "for", "in", "on", "at", "to", "from", "of", "with"
    };

    public DynamicPromptBuilder(
        ISchemaGraphService schemaGraphService,
        IOptions<Neo4jSettings> settings,
        ILogger<DynamicPromptBuilder> logger)
    {
        _schemaGraphService = schemaGraphService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<DynamicPromptResult> BuildPromptAsync(string userQuery, string? basePromptTemplate = null)
    {
        var result = new DynamicPromptResult();

        try
        {
            // 1. Neo4j etkin mi kontrol et
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Neo4j Schema Catalog devre dışı, fallback kullanılacak");
                result.UsedFallback = true;
                result.IsSuccess = true;
                result.Prompt = basePromptTemplate ?? string.Empty;
                return result;
            }

            // 2. Anahtar kelimeleri çıkar
            var keywords = await ExtractKeywordsAsync(userQuery);
            result.ExtractedKeywords = keywords.ToList();
            
            _logger.LogDebug("Çıkarılan anahtar kelimeler: {Keywords}", string.Join(", ", result.ExtractedKeywords));

            // 3. İlgili tabloları bul - KEYWORD'LERİ KULLAN!
            IEnumerable<TableInfo> relevantTables;
            
            if (result.ExtractedKeywords.Any())
            {
                // Önce keyword'lerle ara
                relevantTables = await _schemaGraphService.SearchTablesByKeywordsAsync(result.ExtractedKeywords);
                
                // Eğer sonuç yetersizse, ham sorgu ile de dene ve birleştir
                if (relevantTables.Count() < 3)
                {
                    var additionalTables = await _schemaGraphService.FindRelevantTablesAsync(userQuery, _settings.MaxRelevantTables);
                    relevantTables = relevantTables
                        .Union(additionalTables, new TableInfoComparer())
                        .Take(_settings.MaxRelevantTables);
                }
            }
            else
            {
                // Keyword yoksa ham sorgu ile ara
                relevantTables = await _schemaGraphService.FindRelevantTablesAsync(userQuery, _settings.MaxRelevantTables);
            }
            
            result.RelevantTables = relevantTables.Select(t => t.FullName).Distinct().ToList();
            result.TableCount = result.RelevantTables.Count;

            if (result.TableCount == 0)
            {
                _logger.LogWarning("İlgili tablo bulunamadı, fallback kullanılacak");
                result.UsedFallback = true;
                result.IsSuccess = true;
                result.Prompt = basePromptTemplate ?? string.Empty;
                return result;
            }

            // 4. Tablo şemalarını al
            var tableSchemas = await _schemaGraphService.GetTableSchemasAsync(result.RelevantTables);
            result.ColumnCount = tableSchemas.Sum(t => t.Columns.Count);

            // 5. JOIN path'leri bul
            var joinPaths = (await _schemaGraphService.FindJoinPathsAsync(result.RelevantTables)).ToList();
            result.JoinPathCount = joinPaths.Count;

            // 6. Dinamik şema markdown'ı oluştur
            var schemaMarkdown = GenerateSchemaMarkdown(tableSchemas);
            var joinPathsMarkdown = GenerateJoinPathsMarkdown(joinPaths);

            // 7. V2 template'i yükle ve placeholder'ları doldur
            var v2Template = LoadV2Template();
            var finalPrompt = v2Template
                .Replace("{{DYNAMIC_SCHEMA}}", schemaMarkdown)
                .Replace("{{DYNAMIC_JOIN_PATHS}}", joinPathsMarkdown);

            result.Prompt = finalPrompt;

            // 8. Token sayısını tahmin et (yaklaşık 4 karakter = 1 token)
            result.EstimatedTokens = finalPrompt.Length / 4;

            result.IsSuccess = true;

            _logger.LogInformation(
                "Dinamik prompt oluşturuldu: {Tables} tablo, {Columns} kolon, {Joins} JOIN, ~{Tokens} token",
                result.TableCount, result.ColumnCount, result.JoinPathCount, result.EstimatedTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dinamik prompt oluşturma hatası");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.UsedFallback = true;
            result.Prompt = basePromptTemplate ?? string.Empty;
        }

        return result;
    }

    /// <summary>
    /// V2 prompt template'ini yükler
    /// </summary>
    private string LoadV2Template()
    {
        try
        {
            return Helper.ReadFileContent(PromptFolder, V2PromptFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 template yüklenemedi: {FileName}", V2PromptFileName);
            throw;
        }
    }

    /// <summary>
    /// Tablo şemalarından Markdown formatında şema bilgisi oluşturur
    /// </summary>
    private string GenerateSchemaMarkdown(IEnumerable<TableSchema> tableSchemas)
    {
        var sb = new StringBuilder();

        foreach (var table in tableSchemas)
        {
            // Şema ve tablo adını ayır
            var schemaName = table.Schema ?? "dbo";
            var tableName = table.Name ?? table.FullName;
            
            sb.AppendLine($"- **Şema:** {schemaName}");
            sb.AppendLine($"- **Tablo:** {tableName}");
            
            if (!string.IsNullOrWhiteSpace(table.Description))
            {
                sb.AppendLine($"- **Tablo Açıklaması:** {table.Description}");
            }
            
            sb.AppendLine();
            sb.AppendLine("| Kolon Adı | Veri Tipi | Alias | Açıklama |");
            sb.AppendLine("|-----------|-----------|-------|----------|");

            foreach (var column in table.Columns)
            {
                var keyInfo = "";
                if (column.IsPrimaryKey) keyInfo = " 🔑";
                else if (column.IsForeignKey) keyInfo = $" → {column.FkTable}";
                
                sb.AppendLine($"| {column.Name}{keyInfo} | {column.DataType} | {column.Alias} | {column.Description} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// JOIN path'lerinden Markdown formatında ilişki bilgisi oluşturur
    /// </summary>
    private string GenerateJoinPathsMarkdown(IEnumerable<JoinPath> joinPaths)
    {
        var sb = new StringBuilder();
        var pathList = joinPaths.ToList();

        if (!pathList.Any())
        {
            sb.AppendLine("> ⚠️ **Not:** Seçilen tablolar arasında otomatik JOIN yolu bulunamadı.");
            sb.AppendLine("> Eğer birden fazla tablo kullanmanız gerekiyorsa, tablolardaki Foreign Key (FK) kolonlarını kontrol ederek manuel JOIN yazabilirsiniz.");
            return sb.ToString();
        }

        sb.AppendLine("Aşağıdaki JOIN ifadelerini SQL sorgularınızda kullanabilirsiniz:");
        sb.AppendLine();

        foreach (var path in pathList)
        {
            sb.AppendLine($"**{string.Join(" ↔ ", path.Tables)}** ({path.HopCount} adım)");
            sb.AppendLine();
            sb.AppendLine("```sql");
            foreach (var join in path.Joins)
            {
                var joinSql = join.ToSqlJoin();
                if (!string.IsNullOrEmpty(joinSql))
                {
                    sb.AppendLine(joinSql);
                }
            }
            sb.AppendLine("```");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Kullanıcı sorgusundan anahtar kelimeleri çıkarır
    /// Türkçe ekleri kaldırır ve stop words'leri filtreler
    /// Bu metod veritabanından bağımsız çalışır - Neo4j'deki tablo açıklamalarında arama yapar
    /// </summary>
    public Task<IEnumerable<string>> ExtractKeywordsAsync(string userQuery)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // 1. Sorguyu kelimelere ayır (noktalama işaretlerini temizle)
        var words = Regex.Split(userQuery, @"[\s\.,;:!?\""'()\[\]{}]+")
            .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= 2)
            .ToList();
        
        foreach (var word in words)
        {
            // Stop words'leri atla
            if (StopWords.Contains(word))
                continue;
            
            // Sayıları atla (yıllar hariç - 4 basamaklı)
            if (Regex.IsMatch(word, @"^\d+$") && word.Length != 4)
                continue;
            
            // Türkçe ekleri kaldır (stemming)
            var stemmedWord = RemoveTurkishSuffixes(word);
            
            // Minimum 2 karakter kontrolü
            if (stemmedWord.Length >= 2)
            {
                keywords.Add(stemmedWord);
            }
            
            // Orijinal kelimeyi de ekle (bazı durumlarda ek kaldırma yanlış sonuç verebilir)
            if (word.Length >= 3 && word != stemmedWord)
            {
                keywords.Add(word);
            }
        }
        
        _logger.LogDebug("Extracted keywords from query: {Keywords}", string.Join(", ", keywords));
        
        return Task.FromResult<IEnumerable<string>>(keywords);
    }
    
    /// <summary>
    /// Basit Türkçe stemmer - yaygın ekleri kaldırır
    /// Örnek: "satışlardan" -> "satış", "müşteriye" -> "müşteri"
    /// </summary>
    private static string RemoveTurkishSuffixes(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 3)
            return word;
        
        var result = word.ToLowerInvariant();
        
        // Uzun eklerden kısa eklere doğru kontrol et
        foreach (var suffix in TurkishSuffixes)
        {
            if (result.Length > suffix.Length + 1 && result.EndsWith(suffix))
            {
                result = result[..^suffix.Length];
                break; // Sadece bir ek kaldır
            }
        }
        
        return result;
    }
}

/// <summary>
/// TableInfo karşılaştırıcısı (Union için)
/// </summary>
internal class TableInfoComparer : IEqualityComparer<TableInfo>
{
    public bool Equals(TableInfo? x, TableInfo? y)
    {
        if (x == null || y == null)
            return false;
        return x.FullName == y.FullName;
    }

    public int GetHashCode(TableInfo obj)
    {
        return obj.FullName?.GetHashCode() ?? 0;
    }
}
