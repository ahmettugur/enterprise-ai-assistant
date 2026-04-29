using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using AI.Application.Common.Constants;
using AI.Application.Common.Telemetry;
using AI.Application.Configuration;
using AI.Application.DTOs;
using AI.Application.DTOs.AdvancedRag;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.AIChat;
using AI.Application.Ports.Secondary.Services.Vector;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Tartarus.Snowball.Ext;
using Lucene.Net.Util;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AI.Application.UseCases;

/// <summary>
/// Basitleştirilmiş RAG arama servisi - Sadece temel arama işlevi
/// </summary>
public class RagSearchUseCase : IRagSearchUseCase, IDisposable
{
    private readonly IQdrantService _qdrantService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISparseVectorService _sparseVectorService;
    private readonly Analyzer _analyzer;
    private readonly QdrantSettings _qdrantSettings;
    private readonly AdvancedRagSettings _advancedRagSettings;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IReranker _reranker;
    private readonly ISelfQueryExtractor _selfQueryExtractor;

    // Stemming cache - Aynı kelimenin tekrar stem'lenmesini önler
    // Thread-safe: ConcurrentDictionary kullanılıyor (ASP.NET Core concurrent requests için)
    private readonly ConcurrentDictionary<string, List<string>> _stemCache = new(StringComparer.OrdinalIgnoreCase);
    private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

    public RagSearchUseCase(
        IQdrantService qdrantService,
        IEmbeddingService embeddingService,
        ISparseVectorService sparseVectorService,
        QdrantSettings qdrantSettings,
        AdvancedRagSettings advancedRagSettings,
        IChatCompletionService chatCompletionService,
        IReranker reranker,
        ISelfQueryExtractor selfQueryExtractor)
    {
        _qdrantService = qdrantService;
        _embeddingService = embeddingService;
        _sparseVectorService = sparseVectorService;
        _qdrantSettings = qdrantSettings;
        _advancedRagSettings = advancedRagSettings;
        _analyzer = new StandardAnalyzer(AppLuceneVersion);
        _chatCompletionService = chatCompletionService;
        _reranker = reranker;
        _selfQueryExtractor = selfQueryExtractor;
    }

    /// <summary>
    /// Verilen kelime veya cümle için semantik arama yapar
    /// Advanced RAG özellikleri: Self-Query (metadata filtering) + Reranking
    /// </summary>
    public async Task<SearchResponse> SearchAsync(SearchRequestDto request, CancellationToken cancellationToken = default)
    {
        // Collection adını belirle (DocumentName'den veya default'tan)
        var collectionName = string.IsNullOrWhiteSpace(request.DocumentName)
            ? _qdrantSettings.DefaultCollection
            : QdrantCollections.GetCollectionName(request.DocumentName);

        using var activity = ActivitySources.RagSearch.StartActivity("SemanticSearch");
        if (activity != null)
        {
            activity.SetTag("search.query", request.Query);
            activity.SetTag("search.collection", collectionName);
            activity.SetTag("search.document_name", request.DocumentName ?? "all");
            activity.SetTag("search.min_score", _qdrantSettings.MinSimilarityScore);
            activity.SetTag("search.reranking_enabled", _reranker.IsEnabled);
            activity.SetTag("search.self_query_enabled", _selfQueryExtractor.IsEnabled);

            BaggageHelper.SetBaggage(BaggageHelper.OperationNameBaggage, "SemanticSearch");
            BaggageHelper.AddBaggageToActivity(activity);
        }

        try
        {
            var startTime = DateTime.UtcNow;
            var searchMetadata = new Dictionary<string, object>();

            // === STEP 1: Self-Query (Metadata Extraction) ===
            SelfQueryResult? selfQueryResult = null;
            Dictionary<string, object>? metadataFilters = null;
            var queryForSearch = request.Query ?? "";

            if (_selfQueryExtractor.IsEnabled && queryForSearch.Length >= _advancedRagSettings.SelfQueryMinLength)
            {
                selfQueryResult = await _selfQueryExtractor.ExtractAsync(queryForSearch, null, cancellationToken);
                
                if (selfQueryResult.HasFilters)
                {
                    queryForSearch = selfQueryResult.SemanticQuery;
                    metadataFilters = selfQueryResult.Filters;
                    searchMetadata["selfQuery"] = true;
                    searchMetadata["extractedFilters"] = selfQueryResult.Filters;
                }
            }

            // === STEP 2: Query Processing (Spelling + HyDE) ===
            var processingResult = await ProcessQueryWithLLM(
                queryForSearch,
                request.DocumentName,
                cancellationToken);

            // === STEP 3: Search with Weighted Embeddings ===
            // Reranking aktifse daha fazla aday çek
            var searchLimit = _reranker.IsEnabled 
                ? _advancedRagSettings.RerankCandidateCount 
                : request.Limit;

            var searchResults = await SearchWithWeightedEmbeddings(
                processingResult,
                collectionName,
                searchLimit,
                cancellationToken,
                metadataFilters);

            // Eğer sonuç bulunamadıysa, daha düşük threshold ile tekrar dene (fallback)
            if (searchResults.Count == 0)
            {
                // Fallback: daha düşük threshold ile sadece corrected query ile arama
                var fallbackThreshold = Math.Max(0.1f, _qdrantSettings.MinSimilarityScore * 0.5f);
                var correctedEmbedding = await _embeddingService.GenerateEmbeddingAsync(
                    processingResult.CorrectedQuery,
                    cancellationToken);

                searchResults = await _qdrantService.SearchAsync(
                    collectionName,
                    correctedEmbedding,
                    searchLimit,
                    fallbackThreshold,
                    metadataFilters,
                    cancellationToken);
            }

            // === STEP 4: Convert to SearchResult ===
            var results = new List<SearchResult>();
            foreach (var result in searchResults)
            {
                var content = result.Content ?? "";
                var highlightedContent = BuildSnippetAndHighlight(content, processingResult.CorrectedQuery);
                
                var searchResult = new SearchResult
                {
                    ChunkId = result.ChunkId,
                    DocumentId = result.DocumentId,
                    Content = highlightedContent,
                    SimilarityScore = result.Score,
                    Score = result.Score,
                    DocumentTitle = result.DocumentTitle ?? result.DocumentName ?? "Untitled",
                    DocumentCategory = result.DocumentCategory ?? result.Category,
                    ChunkIndex = result.ChunkIndex,
                    StartPosition = result.StartPosition,
                    EndPosition = result.EndPosition,
                    Metadata = result.Metadata ?? new Dictionary<string, object>
                    {
                        ["fileName"] = result.DocumentName ?? "",
                        ["uploadedAt"] = result.UploadedAt ?? DateTime.UtcNow,
                        ["contentLength"] = content.Length,
                        ["highlightedLength"] = highlightedContent.Length
                    }
                };
                
                results.Add(searchResult);
            }

            // === STEP 5: Reranking ===
            if (_reranker.IsEnabled && results.Count > 0)
            {
                results = await _reranker.RerankAsync(
                    request.Query ?? "",
                    results,
                    request.Limit,
                    cancellationToken);
                
                searchMetadata["reranking"] = true;
                searchMetadata["rerankCandidates"] = searchResults.Count;
                searchMetadata["rerankTopK"] = results.Count;
            }

            var processingTime = DateTime.UtcNow - startTime;

            // Build final metadata
            searchMetadata["embeddingModel"] = _embeddingService.ModelName;
            searchMetadata["searchType"] = "advanced-rag";
            searchMetadata["hydeWeight"] = _qdrantSettings.HyDEWeight;
            searchMetadata["minSimilarityScore"] = _qdrantSettings.MinSimilarityScore;
            searchMetadata["maxResults"] = request.Limit;

            return new SearchResponse
            {
                Query = request.Query!,
                Results = results,
                TotalResults = results.Count,
                ProcessingTimeMs = (int)processingTime.TotalMilliseconds,
                SearchMetadata = searchMetadata
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Arama başarısız: {ex.Message}", ex);
        }
    }


    /// <summary>
    /// Verilen içerikte sorgu kelimelerini vurgular ve snippet oluşturur
    /// Mevcut <mark> tag'larını çıkartarak clean metin üzerinde işlem yapar
    /// </summary>
    private string BuildSnippetAndHighlight(string content, string query)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(query))
            return content;

        try
        {
            // Mevcut <mark> tag'larını çıkar (eğer content zaten marked ise)
            // Bu, Regex.Split() içinde tag'ların parçalanmasını önler
            var cleanContent = Regex.Replace(content, @"</?mark>", "", RegexOptions.IgnoreCase);

            // Query'den kelimeleri çıkar ve stem'lerini al
            var queryStems = ExtractStems(query);
            if (!queryStems.Any())
                return cleanContent;

            // İçerikteki kelimeleri analiz et ve eşleşenleri vurgula
            var highlightedContent = HighlightMatches(cleanContent, queryStems);

            return highlightedContent;
        }
        catch (Exception)
        {
            return content;
        }
    }

    /// <summary>
    /// Verilen metinden kelimelerin stem'lerini çıkarır
    /// Lucene.Net Turkish stemming kullanarak morfolojik analiz yapar
    /// Turkish plural/suffix'leri handle eder (komisyonları → komisyon)
    /// Cache kullanarak aynı kelime 5 kez geçiyorsa, 5 kez highlight edilir (consistency)
    /// </summary>
    private List<string> ExtractStems(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Cache check (thread-safe) - Aynı kelime hemen tekrar geçiyorsa, cache'den al
        var textKey = text.ToLowerInvariant();
        if (_stemCache.TryGetValue(textKey, out var cachedStems))
        {
            return cachedStems;
        }

        var stems = new List<string>();

        try
        {
            using (var reader = new StringReader(text))
            {
                TokenStream tokenStream = new StandardTokenizer(AppLuceneVersion, reader);
                tokenStream = new LowerCaseFilter(AppLuceneVersion, tokenStream);
                tokenStream = new SnowballFilter(tokenStream, new TurkishStemmer());

                try
                {
                    var termAttribute = tokenStream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();
                    tokenStream.Reset();

                    while (tokenStream.IncrementToken())
                    {
                        var term = termAttribute.ToString();
                        if (!string.IsNullOrWhiteSpace(term) && term.Length > 1)
                        {
                            stems.Add(term);

                            // Turkish suffix removal (fallback for plurals)
                            // Eğer stemmed term halen Turkish suffix içeriyorsa, kaldır
                            var withoutSuffix = RemoveTurkishSuffix(term);
                            if (withoutSuffix != term && !string.IsNullOrWhiteSpace(withoutSuffix) && withoutSuffix.Length > 1)
                            {
                                stems.Add(withoutSuffix);
                            }
                        }
                    }

                    tokenStream.End();
                }
                finally
                {
                    tokenStream?.Dispose();
                }
            }
        }
        catch (Exception)
        {
        }

        var distinctStems = stems.Distinct().ToList();

        // Cache'e kaydet (thread-safe - zaten varsa üzerine yazmaz)
        _stemCache.TryAdd(textKey, distinctStems);

        return distinctStems;
    }

    /// <summary>
    /// Partial stem matching - Lucene inconsistency için fallback
    /// "komisyo" ile "komisyon" eşleştirir (stem farklılıkları için)
    /// Min 80% karakter eşleşmesi gerekir
    /// </summary>
    private static bool IsPartialStemMatch(List<string> wordStems, List<string> queryStems)
    {
        foreach (var wordStem in wordStems)
        {
            foreach (var queryStem in queryStems)
            {
                // Aynı tabanı (root) kontrol et
                var minLength = Math.Min(wordStem.Length, queryStem.Length);
                if (minLength < 3) continue; // Root çok kısa

                // İlk 80%'inde eşleşme var mı kontrol et
                var matchLength = 0;
                for (int i = 0; i < minLength; i++)
                {
                    if (wordStem[i] == queryStem[i])
                        matchLength++;
                    else
                        break;
                }

                // En az root uzunluğunun 80%'i eşleşirse match et
                var matchPercentage = (double)matchLength / minLength;
                if (matchPercentage >= 0.8)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Turkish suffix'lerini kaldırır
    /// Örnek: "komisyonları" → "komisyon"
    /// </summary>
    private static string RemoveTurkishSuffix(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 3)
            return word;

        // Turkish suffixes (ordered by length, longest first to avoid partial matches)
        var turkishSuffixes = new[]
        {
            // Plural suffixes
            "ları", "leri", "lar", "ler",
            // Case suffixes (accusative, genitive, locative, dative, ablative)
            "ını", "ini", "unu", "ünü",  // Accusative
            "ının", "inin", "unun", "ünün",  // Genitive
            "ında", "inde", "unda", "ünde",  // Locative
            "ıdan", "idan", "udan", "üden",  // Ablative
            "ına", "ine", "una", "üne",  // Dative
            // Other common suffixes
            "ı", "i", "u", "ü",  // Vowel suffixes (last resort)
        };

        foreach (var suffix in turkishSuffixes)
        {
            if (word.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var root = word.Substring(0, word.Length - suffix.Length);
                if (root.Length > 2)  // Root must be at least 3 characters
                {
                    return root;
                }
            }
        }

        return word;
    }

    /// <summary>
    /// İçerikte eşleşen kelimeleri vurgular (case-insensitive ve stem-aware)
    /// Lucene.Net stemming kullanarak Türkçe morfoloji desteği sağlar
    /// Sayılar ve özel karakterleri kaldırarak eşleştirme yapar
    /// </summary>
    private string HighlightMatches(string content, List<string> queryStems)
    {
        if (!queryStems.Any())
            return content;

        // queryStems'i HashSet'e dönüştür (faster lookup)
        var queryStemSet = new HashSet<string>(queryStems);

        var words = Regex.Split(content, @"(\W+)"); // Kelimeleri ve ayırıcıları koruyarak böl
        var highlightedContent = new StringBuilder();
        var matchCount = 0;

        foreach (var word in words)
        {
            if (Regex.IsMatch(word, @"\w")) // En az bir word character içeriyorsa kontrol et
            {
                var shouldHighlight = false;

                // Kelimeden sadece harfleri çıkar (sayıları kaldır)
                // "47Kapalı" → "Kapalı"
                var cleanedWord = Regex.Replace(word, @"[0-9]", "");

                if (!string.IsNullOrWhiteSpace(cleanedWord))
                {
                    // Strategi 1: Türkçe stem matching (direct)
                    // queryStems zaten stemmed olduğu için, word'ü stem'lemiz ve compare edelim
                    try
                    {
                        var wordStems = ExtractStems(cleanedWord);

                        // Strategi 1: Exact stem match
                        shouldHighlight = wordStems.Any(stem => queryStemSet.Contains(stem));

                        // Strategi 2: Partial stem match (Lucene inconsistency için fallback)
                        // "komisyo" ≈ "komisyon" (Lucene "n" harfini trim'liyor olabilir)
                        if (!shouldHighlight && wordStems.Any() && queryStems.Any())
                        {
                            shouldHighlight = IsPartialStemMatch(wordStems, queryStems);
                        }

                        if (shouldHighlight)
                        {
                            matchCount++;
                        }
                    }
                    catch (Exception)
                    {
                        // Stemming başarısız olursa, lowercase matching'e bağlı kal
                        var wordLower = cleanedWord.ToLowerInvariant();
                        shouldHighlight = queryStemSet.Contains(wordLower);
                    }
                }

                if (shouldHighlight)
                {
                    highlightedContent.Append($"<mark>{word}</mark>");
                }
                else
                {
                    highlightedContent.Append(word);
                }
            }
            else
            {
                highlightedContent.Append(word); // Ayırıcıları olduğu gibi ekle
            }
        }

        return highlightedContent.ToString();
    }

    /// <summary>
    /// LLM kullanarak query'yi işler: Spelling correction + HyDE generation
    /// </summary>
    private async Task<QueryProcessingResult> ProcessQueryWithLLM(
        string query,
        string? documentName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = ActivitySources.RagSearch.StartActivity("QueryProcessing");
            activity?.SetTag("processing.original_query", query);

            return await ProcessQuery(query, documentName, cancellationToken);
        }
        catch (Exception)
        {
            return new QueryProcessingResult
            {
                OriginalQuery = query,
                CorrectedQuery = query,
                NormalizedQuery = query,
                ProcessingMode = "fallback"
            };
        }
    }

    /// <summary>
    /// Query processing: Spelling correction + HyDE generation
    /// </summary>
    private async Task<QueryProcessingResult> ProcessQuery(
        string query,
        string? documentName,
        CancellationToken cancellationToken)
    {
        var domainContext = GetDomainContext(documentName);

        var systemPrompt = $"""
            Sen Türkçe query processing asistanısın. İki görevin var:
            
            1. SPELLING CORRECTION: Query'deki yazım hatalarını düzelt
            2. HYPOTHETICAL DOCUMENT: Bu query'ye cevap verebilecek, varsayımsal bir doküman paragrafı üret
            
            {domainContext}

            Çıktını şu formatta ver:
            CORRECTED: <düzeltilmiş query>
            DOCUMENT: <hypothetical döküman paragrafı>

            HYPOTHETICAL DOCUMENT kuralları:
            - Kullanıcının sorusuna doğrudan cevap veren, detaylı bir paragraf yaz
            - Sanki gerçek bir referans dokümanından alınmış gibi yaz
            - 50-150 kelime uzunluğunda olmalı (çok kısa olmamalı!)
            - Formal, bilgilendirici ve uzman üslubu kullan
            - Domain bağlamına uygun terimler, kavramlar ve detaylar ekle
            - Sorunun kendisini tekrarlama, sadece cevabı yaz
            - Tutarlı ve mantıklı bilgiler içermeli
            - Doğal dil kullan, liste veya madde işaretleri kullanma
        """;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(query);

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            new OpenAIPromptExecutionSettings
            {
                MaxTokens = _qdrantSettings.HyDEMaxTokens + 50,
                Temperature = 0.3
            },
            cancellationToken: cancellationToken);

        var content = response?.Content?.Trim() ?? "";
        var correctedQuery = query;
        var hypotheticalDoc = query;

        // Parse output
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("CORRECTED:", StringComparison.OrdinalIgnoreCase))
            {
                correctedQuery = line.Substring("CORRECTED:".Length).Trim();
            }
            else if (line.StartsWith("DOCUMENT:", StringComparison.OrdinalIgnoreCase))
            {
                hypotheticalDoc = line.Substring("DOCUMENT:".Length).Trim();
            }
        }

        return new QueryProcessingResult
        {
            OriginalQuery = query,
            CorrectedQuery = correctedQuery,
            HypotheticalDocument = hypotheticalDoc,
            NormalizedQuery = correctedQuery,
            ProcessingMode = "hyde"
        };
    }

    /// <summary>
    /// Döküman tipine göre domain context döndürür
    /// Veritabanından döküman bilgisini alır, bulunamazsa genel context döner
    /// </summary>
    private string GetDomainContext(string? documentName)
    {
        if (string.IsNullOrWhiteSpace(documentName))
            return "Domain: Genel bilgi dökümanları";

        // Döküman adından collection name oluştur ve domain context döndür
        var baseName = Path.GetFileNameWithoutExtension(documentName).ToLowerInvariant();
        return $"Domain: {baseName} dökümanı içeriği";
    }

    /// <summary>
    /// Hybrid mode + both-weighted: Hem corrected hem HyDE ile ara, sonuçları merge et
    /// metadataFilters varsa filtered search yapar (hybrid search henüz filter desteklemiyor)
    /// </summary>
    private async Task<List<SearchResult>> SearchWithWeightedEmbeddings(
        QueryProcessingResult processingResult,
        string collectionName,
        int limit,
        CancellationToken cancellationToken,
        Dictionary<string, object>? metadataFilters = null)
    {
        // Corrected query embedding (doğrudan kullanıcı sorgusu - stemming YOK)
        var correctedEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            processingResult.CorrectedQuery,
            cancellationToken);

        // HyDE embedding (hypothetical document - stemming YOK, doğal dil olarak embed)
        var hydeText = processingResult.HypotheticalDocument ?? processingResult.CorrectedQuery;
        var hydeEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            hydeText,
            cancellationToken);

        // Sparse vector for hybrid search (keyword matching)
        var querySparseVector = _sparseVectorService.GenerateSparseVectorResult(processingResult.CorrectedQuery);

        // İki arama paralel yap
        Task<List<SearchResult>> correctedTask;
        Task<List<SearchResult>> hydeTask;

        // Eğer metadataFilters varsa, filtered semantic search kullan (hybrid search henüz filter desteklemiyor)
        if (metadataFilters != null && metadataFilters.Count > 0)
        {
            // Filtered semantic search
            correctedTask = _qdrantService.SearchAsync(
                collectionName,
                correctedEmbedding,
                limit,
                _qdrantSettings.MinSimilarityScore,
                metadataFilters,
                cancellationToken);

            hydeTask = _qdrantService.SearchAsync(
                collectionName,
                hydeEmbedding,
                limit,
                _qdrantSettings.MinSimilarityScore,
                metadataFilters,
                cancellationToken);
        }
        else if (querySparseVector.NonZeroCount > 0)
        {
            // Native hybrid search kullan
            correctedTask = _qdrantService.HybridSearchAsync(
                collectionName,
                correctedEmbedding,
                querySparseVector.Indices,
                querySparseVector.Values,
                limit,
                _qdrantSettings.MinSimilarityScore,
                _qdrantSettings.DenseWeight,
                _qdrantSettings.SparseWeight,
                cancellationToken);

            hydeTask = _qdrantService.HybridSearchAsync(
                collectionName,
                hydeEmbedding,
                querySparseVector.Indices,
                querySparseVector.Values,
                limit,
                _qdrantSettings.MinSimilarityScore,
                _qdrantSettings.DenseWeight,
                _qdrantSettings.SparseWeight,
                cancellationToken);
        }
        else
        {
            // Pure semantic search
            correctedTask = _qdrantService.SearchAsync(
                collectionName,
                correctedEmbedding,
                limit,
                _qdrantSettings.MinSimilarityScore,
                null,
                cancellationToken);

            hydeTask = _qdrantService.SearchAsync(
                collectionName,
                hydeEmbedding,
                limit,
                _qdrantSettings.MinSimilarityScore,
                null,
                cancellationToken);
        }

        var results = await Task.WhenAll(correctedTask, hydeTask);

        // Sonuçları weighted merge et
        var mergedResults = MergeWeightedResults(
            results[0], // corrected results
            results[1], // HyDE results
            _qdrantSettings.HyDEWeight);

        return mergedResults;
    }

    /// <summary>
    /// İki search sonucunu weighted scoring ile merge eder
    /// Corrected: (1 - HyDEWeight), HyDE: HyDEWeight ağırlık alır
    /// Her iki aramada bulunan sonuçlar boost edilir
    /// </summary>
    private List<SearchResult> MergeWeightedResults(
        List<SearchResult> correctedResults,
        List<SearchResult> hydeResults,
        float hydeWeight)
    {
        var mergedScores = new Dictionary<Guid, (SearchResult Result, float Score)>();

        // Corrected results (1 - hydeWeight)
        foreach (var result in correctedResults)
        {
            var chunkId = result.ChunkId;
            var score = result.Score * (1 - hydeWeight);
            mergedScores[chunkId] = (result, score);
        }

        // HyDE results (hydeWeight)
        foreach (var result in hydeResults)
        {
            var chunkId = result.ChunkId;
            var score = result.Score * hydeWeight;

            if (mergedScores.ContainsKey(chunkId))
            {
                // Aynı chunk her iki aramada da bulundu, skorları topla
                var existing = mergedScores[chunkId];
                mergedScores[chunkId] = (existing.Result, existing.Score + score);
            }
            else
            {
                mergedScores[chunkId] = (result, score);
            }
        }

        // Merge edilmiş skorları SearchResult'a geri yaz ve sırala
        return mergedScores
            .OrderByDescending(kvp => kvp.Value.Score)
            .Select(kvp =>
            {
                var result = kvp.Value.Result;
                result.Score = kvp.Value.Score; // Updated merged score
                return result;
            })
            .ToList();
    }

    public void Dispose()
    {
        _analyzer?.Dispose();
        // ConcurrentDictionary GC tarafından temizlenecek, manuel clear gereksiz
    }
}

/// <summary>
/// Query processing sonucu (spelling correction + HyDE)
/// </summary>
internal class QueryProcessingResult
{
    /// <summary>
    /// Orijinal kullanıcı query'si
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// LLM tarafından düzeltilmiş query (spelling fixed)
    /// </summary>
    public string CorrectedQuery { get; set; } = string.Empty;

    /// <summary>
    /// HyDE: Query'den üretilmiş hypothetical document
    /// </summary>
    public string? HypotheticalDocument { get; set; }

    /// <summary>
    /// Stemmed version (embedding için)
    /// </summary>
    public string NormalizedQuery { get; set; } = string.Empty;

    /// <summary>
    /// Processing mode kullanıldı
    /// </summary>
    public string ProcessingMode { get; set; } = string.Empty;
}