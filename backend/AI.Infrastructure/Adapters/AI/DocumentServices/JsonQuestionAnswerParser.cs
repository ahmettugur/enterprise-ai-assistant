using System.Text.Json;
using AI.Application.Common.Helpers;
using AI.Application.Ports.Secondary.Services.Document;
using AI.Domain.Documents;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.DocumentServices;

/// <summary>
/// Soru-Cevap formatındaki JSON dosyalarını parse eden adapter
/// DocumentType.QuestionAnswer olarak işaretlenen dökümanlar için kullanılır
/// </summary>
public class JsonQuestionAnswerParser : IJsonQuestionAnswerParser
{
    private readonly ILogger<JsonQuestionAnswerParser> _logger;

    public JsonQuestionAnswerParser(ILogger<JsonQuestionAnswerParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// JSON dosyasındaki soru-cevap verilerini DocumentChunk'lara dönüştürür
    /// Her Q&A bir chunk olur, tags'lar metadata'ya eklenir
    /// </summary>
    public List<DocumentChunk> ParseQuestionsAnswers(
        Stream fileStream,
        Guid documentId,
        string fileName)
    {
        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        try
        {
            using var reader = new StreamReader(fileStream);
            var jsonContent = reader.ReadToEnd();

            // JSON'u parse et
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("JSON dosyası array değil: {FileName}", fileName);
                return chunks;
            }

            var startPosition = 0;

            foreach (var item in root.EnumerateArray())
            {
                try
                {
                    // Türkçe karakter encoding sorunlarını düzelt
                    var question = TurkishEncodingHelper.FixEncoding(
                        item.GetProperty("question").GetString() ?? string.Empty);
                    var answer = TurkishEncodingHelper.FixEncoding(
                        item.GetProperty("answer").GetString() ?? string.Empty);
                    var tags = TurkishEncodingHelper.FixEncoding(
                        item.TryGetProperty("tags", out var tagsElement)
                            ? tagsElement.GetString() ?? string.Empty
                            : string.Empty);

                    // Q&A içeriğini birleştir (embedding için)
                    var content = BuildChunkContent(question, answer, tags);

                    // Metadata oluştur
                    var metadata = new Dictionary<string, object>
                    {
                        ["question"] = question,
                        ["answer"] = answer,
                        ["tags"] = tags,
                        ["type"] = "question_answer",
                        ["id"] = item.TryGetProperty("id", out var idElement)
                            ? idElement.GetString() ?? Guid.NewGuid().ToString()
                            : Guid.NewGuid().ToString()
                    };

                    var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);

                    var chunk = DocumentChunk.Create(
                        documentId: documentId,
                        chunkIndex: chunkIndex,
                        content: content,
                        startPosition: startPosition,
                        endPosition: startPosition + content.Length,
                        metadata: metadataJson
                    );

                    chunks.Add(chunk);
                    chunkIndex++;
                    startPosition += content.Length + 1; // +1 for newline
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Soru-cevap parse edilemedi: {FileName}", fileName);
                    continue;
                }
            }

            _logger.LogInformation(
                "JSON dosyası parse edildi: {FileName}, Chunk sayısı: {ChunkCount}",
                fileName,
                chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON dosyası parse edilemedi: {FileName}", fileName);
        }

        return chunks;
    }

    /// <summary>
    /// Soru, cevap ve tags'ları embedding için optimize edilmiş şekilde birleştirir
    /// </summary>
    private static string BuildChunkContent(string question, string answer, string tags)
    {
        var contentParts = new List<string>
        {
            $"Soru: {question}",
            $"Cevap: {answer}"
        };

        if (!string.IsNullOrWhiteSpace(tags))
        {
            contentParts.Add($"Etiketler: {tags}");
        }

        return string.Join("\n", contentParts);
    }
}

/// <summary>
/// JSON Q&A dosyasının yapısı
/// </summary>
public class JsonQuestionAnswerItem
{
    public string? Id { get; set; }
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? Tags { get; set; }
}
