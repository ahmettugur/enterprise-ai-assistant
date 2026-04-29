using System.Text.RegularExpressions;

namespace AI.Infrastructure.Adapters.AI.DocumentServices;

public class RecursiveCharacterTextSplitter
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;
    private readonly List<string> _separators;
    private readonly KeepSeparatorOption _keepSeparator;
    private readonly bool _isSeparatorRegex;
    private readonly Func<string, int> _lengthFunction;

    public enum KeepSeparatorOption
    {
        None,
        Start,
        End
    }

    public RecursiveCharacterTextSplitter(
        int chunkSize = 4000,
        int chunkOverlap = 200,
        List<string>? separators = null,
        KeepSeparatorOption keepSeparator = KeepSeparatorOption.End,
        bool isSeparatorRegex = false,
        Func<string, int>? lengthFunction = null)
    {
        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
        _keepSeparator = keepSeparator;
        _isSeparatorRegex = isSeparatorRegex;
        _lengthFunction = lengthFunction ?? (text => text.Length);

        // Varsayılan ayırıcılar (LangChain'deki gibi)
        _separators = separators ?? new List<string>
            {
                "\n\n",  // Paragraf
                "\n",    // Satır
                " ",     // Kelime
                ""       // Karakter
            };

        if (_chunkOverlap >= _chunkSize)
        {
            throw new ArgumentException("chunkOverlap, chunkSize'dan küçük olmalıdır");
        }
    }

    /// <summary>
    /// Metni parçalara böler
    /// </summary>
    public List<string> SplitText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string>();
        }

        return RecursiveSplit(text, _separators);
    }

    /// <summary>
    /// Recursive olarak metni böler
    /// Python'daki _split_text metodunun karşılığı
    /// </summary>
    private List<string> RecursiveSplit(string text, List<string> separators)
    {
        var finalChunks = new List<string>();

        // Uygun ayırıcıyı bul
        var separator = separators.LastOrDefault() ?? "";
        var newSeparators = new List<string>();

        for (int i = 0; i < separators.Count; i++)
        {
            var _s = separators[i];
            var _separator = _isSeparatorRegex ? _s : Regex.Escape(_s);

            if (_s == "")
            {
                separator = _s;
                break;
            }

            if (Regex.IsMatch(text, _separator))
            {
                separator = _s;
                newSeparators = separators.Skip(i + 1).ToList();
                break;
            }
        }

        // Metni ayırıcıya göre böl
        var _separatorPattern = _isSeparatorRegex ? separator : Regex.Escape(separator);
        var splits = SplitTextWithRegex(text, _separatorPattern, _keepSeparator);

        // Her parçayı işle
        var goodSplits = new List<string>();
        var separatorToUse = _keepSeparator != KeepSeparatorOption.None ? "" : separator;

        foreach (var split in splits)
        {
            if (_lengthFunction(split) < _chunkSize)
            {
                goodSplits.Add(split);
            }
            else
            {
                // Mevcut iyi parçaları birleştir
                if (goodSplits.Any())
                {
                    var mergedText = MergeSplits(goodSplits, separatorToUse);
                    finalChunks.AddRange(mergedText);
                    goodSplits.Clear();
                }

                // Eğer yeni ayırıcı yoksa, parçayı olduğu gibi ekle
                if (!newSeparators.Any())
                {
                    finalChunks.Add(split);
                }
                else
                {
                    // Recursive olarak daha küçük parçalara böl
                    var otherInfo = RecursiveSplit(split, newSeparators);
                    finalChunks.AddRange(otherInfo);
                }
            }
        }

        // Kalan iyi parçaları birleştir
        if (goodSplits.Any())
        {
            var mergedText = MergeSplits(goodSplits, separatorToUse);
            finalChunks.AddRange(mergedText);
        }

        return finalChunks;
    }

    /// <summary>
    /// Metni ayırıcıya göre böler ve separator'ı korur veya atmaz
    /// Python'daki _split_text_with_regex fonksiyonunun karşılığı
    /// </summary>
    private List<string> SplitTextWithRegex(string text, string separator, KeepSeparatorOption keepSeparator)
    {
        if (string.IsNullOrEmpty(separator))
        {
            return text.Select(c => c.ToString()).ToList();
        }

        var splits = new List<string>();

        if (keepSeparator != KeepSeparatorOption.None)
        {
            // Regex ile böl ve ayırıcıyı yakala
            var _splits = Regex.Split(text, $"({separator})");

            if (keepSeparator == KeepSeparatorOption.End)
            {
                // Ayırıcıyı parçanın sonuna ekle
                for (int i = 0; i < _splits.Length - 1; i += 2)
                {
                    if (i + 1 < _splits.Length)
                    {
                        splits.Add(_splits[i] + _splits[i + 1]);
                    }
                }
                if (_splits.Length % 2 == 0)
                {
                    splits.Add(_splits[_splits.Length - 1]);
                }
            }
            else // KeepSeparatorOption.Start
            {
                // Ayırıcıyı parçanın başına ekle
                splits.Add(_splits[0]);
                for (int i = 1; i < _splits.Length; i += 2)
                {
                    if (i + 1 < _splits.Length)
                    {
                        splits.Add(_splits[i] + _splits[i + 1]);
                    }
                }
                if (_splits.Length % 2 == 0)
                {
                    splits.Add(_splits[_splits.Length - 1]);
                }
            }
        }
        else
        {
            // Ayırıcıyı atarak böl
            splits = Regex.Split(text, separator).ToList();
        }

        return splits.Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    /// <summary>
    /// Parçaları chunk size'a göre birleştirir
    /// </summary>
    private List<string> MergeSplits(List<string> splits, string separator)
    {
        var docs = new List<string>();
        var currentDoc = new List<string>();
        var total = 0;

        foreach (var split in splits)
        {
            var len = _lengthFunction(split);

            if (total + len + (currentDoc.Any() ? _lengthFunction(separator) : 0) > _chunkSize)
            {
                if (currentDoc.Any())
                {
                    var doc = JoinDocs(currentDoc, separator);
                    if (!string.IsNullOrEmpty(doc))
                    {
                        docs.Add(doc);
                    }

                    // Overlap için son parçaları tut
                    while (total > _chunkOverlap || (total + len + (currentDoc.Any() ? _lengthFunction(separator) : 0) > _chunkSize && total > 0))
                    {
                        total -= _lengthFunction(currentDoc[0]) + (currentDoc.Count > 1 ? _lengthFunction(separator) : 0);
                        currentDoc.RemoveAt(0);
                    }
                }
            }

            currentDoc.Add(split);
            total += len + (currentDoc.Count > 1 ? _lengthFunction(separator) : 0);
        }

        var finalDoc = JoinDocs(currentDoc, separator);
        if (!string.IsNullOrEmpty(finalDoc))
        {
            docs.Add(finalDoc);
        }

        return docs;
    }

    /// <summary>
    /// Dokümanları birleştirir
    /// </summary>
    private string? JoinDocs(List<string> docs, string separator)
    {
        var text = string.Join(separator, docs).Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// Dokümanları metadata ile birlikte böler (opsiyonel)
    /// </summary>
    public List<TextDocument> CreateDocuments(List<string> texts, List<Dictionary<string, object>>? metadatas = null)
    {
        var documents = new List<TextDocument>();
        metadatas = metadatas ?? texts.Select(_ => new Dictionary<string, object>()).ToList();

        for (int i = 0; i < texts.Count; i++)
        {
            var text = texts[i];
            var metadata = i < metadatas.Count ? metadatas[i] : new Dictionary<string, object>();

            var chunks = SplitText(text);
            foreach (var chunk in chunks)
            {
                var newMetadata = new Dictionary<string, object>(metadata);
                documents.Add(new TextDocument
                {
                    PageContent = chunk,
                    Metadata = newMetadata
                });
            }
        }

        return documents;
    }
}

/// <summary>
/// Doküman sınıfı
/// </summary>
public class TextDocument
{
    public string PageContent { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}
