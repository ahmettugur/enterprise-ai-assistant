
using AI.Application.Common.Helpers;
using AI.Application.Common.Telemetry;
using AI.Application.Configuration;
using AI.Application.Ports.Secondary.Services.Vector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.DocumentServices;

/// <summary>
/// OpenAI embedding servisi implementasyonu
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
    private readonly ILogger<OpenAIEmbeddingService> _logger;
    private readonly QdrantSettings _qdrantSettings;

    public OpenAIEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingService,
        ILogger<OpenAIEmbeddingService> logger,
        QdrantSettings qdrantSettings)
    {
        _embeddingService = embeddingService;
        _logger = logger;
        _qdrantSettings = qdrantSettings ?? throw new ArgumentNullException(nameof(qdrantSettings));
    }

    public int EmbeddingDimension => _qdrantSettings.VectorSize;

    public string ModelName => _qdrantSettings.EmbeddingModel;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.EmbeddingGeneration.StartActivity("GenerateEmbedding");
        if (activity != null)
        {
            activity.SetTag("embedding.model", ModelName);
            activity.SetTag("embedding.dimension", EmbeddingDimension);
            activity.SetTag("embedding.input_length", text?.Length ?? 0);

            BaggageHelper.SetEmbeddingBaggage(ModelName, EmbeddingDimension);
            BaggageHelper.AddBaggageToActivity(activity);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or empty", nameof(text));
            }

            // Metni temizle ve normalize et
            var cleanedText = CleanText(text);

            var embeddings = await _embeddingService.GenerateAsync([cleanedText], null, cancellationToken);
            var embeddingArray = embeddings.First().Vector.ToArray();

            _logger.LogDebug("Generated embedding for text of length {Length}, embedding dimension: {Dimension}",
                cleanedText.Length, embeddingArray.Length);

            return embeddingArray;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Embedding generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text of length {Length}", text?.Length ?? 0);
            throw new InvalidOperationException($"Embedding generation failed: {ex.Message}", ex);
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        try
        {
            var textList = texts?.ToList() ?? throw new ArgumentNullException(nameof(texts));

            if (!textList.Any())
            {
                return new List<float[]>();
            }

            // Metinleri temizle
            var cleanedTexts = textList.Select(CleanText).ToList();

            var embeddings = await _embeddingService.GenerateAsync(cleanedTexts, null, cancellationToken);
            var embeddingArrays = embeddings.Select(e => e.Vector.ToArray()).ToList();

            _logger.LogDebug("Generated {Count} embeddings", embeddingArrays.Count);

            return embeddingArrays;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Batch embedding generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings for {Count} texts", texts?.Count() ?? 0);
            throw new InvalidOperationException($"Batch embedding generation failed: {ex.Message}", ex);
        }
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Türkçe karakter encoding sorunlarını düzelt (merkezi helper kullanarak)
        var corrected = TurkishEncodingHelper.FixEncoding(text);

        // Fazla boşlukları temizle
        var cleaned = System.Text.RegularExpressions.Regex.Replace(corrected.Trim(), @"\s+", " ");

        // NOT: ToLowerInvariant() KALDIRILDI!
        // Semantic embedding modelleri case-insensitive çalışır
        // Türkçe karakterler için case değişikliği sorun yaratır (İ → i, I → ı)

        // Çok uzun metinleri kısalt (OpenAI token limiti için)
        const int maxLength = 8000; // Yaklaşık 2000 token
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
}