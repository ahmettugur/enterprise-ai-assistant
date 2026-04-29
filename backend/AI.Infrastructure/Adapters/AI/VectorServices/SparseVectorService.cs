
using AI.Application.DTOs.SparseVector;
using AI.Application.Ports.Secondary.Services.Vector;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Tr;
using Lucene.Net.Util;

namespace AI.Infrastructure.Adapters.AI.VectorServices;

public class SparseVectorService : ISparseVectorService
{
    private static readonly LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

    // Deterministic hash için vocabulary size
    // 500K alan collision'ı minimize eder (~%5 collision at 20K unique terms)
    private const uint VocabularySize = 500000;

    // BM25 parametreleri (stateless - IDF yerine sabit değer kullanılır)
    private const float DefaultIDF = 1.5f; // Ortalama IDF değeri (stateless için)

    // Turkish stopwords - yaygın anlamsız kelimeler
    private static readonly HashSet<string> TurkishStopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bir", "bu", "şu", "o", "ve", "veya", "ama", "fakat", "ancak", "lakin",
        "için", "ile", "gibi", "kadar", "daha", "çok", "az", "en", "çünkü",
        "mi", "mı", "mu", "mü", "ki", "da", "de", "ta", "te",
        "var", "yok", "ben", "sen", "o", "biz", "siz", "onlar",
        "şey", "şu", "bu", "olan", "olarak", "ne", "nasıl", "nerede", "kim",
        "hangi", "niye", "niçin", "neden", "her", "bazı", "hiç", "tüm",
        "sonra", "önce", "içinde", "dışında", "altında", "üstünde", "arasında",
        "göre", "karşı", "doğru", "rağmen", "beri", "itibaren", "kadar", "dek",
        "eder", "olur", "yapar", "demek", "etmek", "olmak", "yapmak"
    };

    public SparseVectorService()
    {
    }

    public (uint[] indices, float[] values) GenerateSparseVector(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (Array.Empty<uint>(), Array.Empty<float>());

        try
        {
            // Tokenize ve term frequency hesapla
            var termFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using var analyzer = new TurkishAnalyzer(AppLuceneVersion);
            using var stream = analyzer.GetTokenStream("content", new StringReader(text));

            var termAttr = stream.AddAttribute<ICharTermAttribute>();
            stream.Reset();

            int totalTerms = 0;
            while (stream.IncrementToken())
            {
                var term = termAttr.ToString().ToLowerInvariant();

                // Minimum uzunluk ve stopword kontrolü
                if (term.Length >= 3 && !TurkishStopwords.Contains(term))
                {
                    termFrequency[term] = termFrequency.GetValueOrDefault(term, 0) + 1;
                    totalTerms++;
                }
            }

            stream.End();

            if (termFrequency.Count == 0)
                return (Array.Empty<uint>(), Array.Empty<float>());

            // BM25 scoring parametreleri (stateless - IDF için sabit değer)
            const float k1 = 1.5f; // Term frequency saturation parameter
            const float b = 0.75f; // Length normalization parameter
            const float avgDocLength = 100f; // Estimated average document length

            var docLength = (float)totalTerms;
            var indices = new List<uint>();
            var values = new List<float>();

            // BM25 scoring ile sparse vector oluştur (deterministic hash kullanarak)
            foreach (var kvp in termFrequency.OrderByDescending(x => x.Value))
            {
                var term = kvp.Key;
                var tf = kvp.Value;

                // Deterministic hash ile term index hesapla (her zaman aynı sonuç)
                var termIndex = GetTermIndex(term);

                // BM25 score calculation (stateless IDF ile)
                // Score = IDF * (tf * (k1 + 1)) / (tf + k1 * (1 - b + b * (docLength / avgDocLength)))
                var lengthNorm = 1 - b + b * (docLength / avgDocLength);
                var bm25Score = DefaultIDF * (tf * (k1 + 1)) / (tf + k1 * lengthNorm);

                indices.Add(termIndex);
                values.Add(bm25Score);
            }

            return (indices.ToArray(), values.ToArray());
        }
        catch (Exception)
        {
            return (Array.Empty<uint>(), Array.Empty<float>());
        }
    }

    public SparseVectorResult GenerateSparseVectorResult(string text)
    {
        var (indices, values) = GenerateSparseVector(text);

        return new SparseVectorResult
        {
            Indices = indices,
            Values = values
        };
    }

    /// <summary>
    /// Deterministic hash ile term index hesaplar
    /// FNV-1a algoritması kullanır - her zaman aynı term için aynı index döner
    /// Restart'tan etkilenmez, distributed sistemlerde tutarlıdır
    /// </summary>
    private static uint GetTermIndex(string term)
    {
        unchecked
        {
            // FNV-1a hash algorithm - hızlı ve iyi dağılım
            uint hash = 2166136261; // FNV offset basis
            foreach (char c in term.ToLowerInvariant())
            {
                hash ^= c;
                hash *= 16777619; // FNV prime
            }
            return hash % VocabularySize;
        }
    }
}
