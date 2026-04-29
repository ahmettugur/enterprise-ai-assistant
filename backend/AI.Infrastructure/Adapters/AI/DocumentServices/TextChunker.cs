using AI.Application.Ports.Secondary.Services.Document;
using AI.Domain.Documents;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.DocumentServices;

/// <summary>
/// Metin parçalama servisi implementasyonu
/// </summary>
public class TextChunker : ITextChunker
{
    private readonly ILogger<TextChunker> _logger;

    public TextChunker(ILogger<TextChunker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// RecursiveCharacterTextSplitter kullanarak metni parçalara böler
    /// Python LangChain implementasyonuyla aynı parametreleri kullanır
    /// </summary>
    public List<DocumentChunk> ChunkText(string text, Guid documentId, int chunkSize = 1000, int overlapSize = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<DocumentChunk>();
        }

        try
        {
            // RecursiveCharacterTextSplitter'ı Python'daki gibi yapılandır
            var splitter = new RecursiveCharacterTextSplitter(
                chunkSize: chunkSize,
                chunkOverlap: overlapSize,
                separators: new List<string> { "\n\n" },  // Paragraflarla ayır
                isSeparatorRegex: false,
                lengthFunction: text => text.Length
            );

            // Metni böl
            var splitTexts = splitter.SplitText(text);

            var chunks = new List<DocumentChunk>();
            var chunkIndex = 0;
            var currentPosition = 0;

            foreach (var chunkContent in splitTexts)
            {
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    // Chunk'ın orijinal metinde pozisyonunu bul
                    var startPosition = text.IndexOf(chunkContent, currentPosition);
                    if (startPosition < 0)
                    {
                        startPosition = currentPosition;
                    }

                    var endPosition = startPosition + chunkContent.Length;

                    var chunk = DocumentChunk.Create(
                        documentId: documentId,
                        chunkIndex: chunkIndex,
                        content: chunkContent,
                        startPosition: startPosition,
                        endPosition: endPosition
                    );

                    chunks.Add(chunk);
                    chunkIndex++;
                    currentPosition = endPosition;
                }
            }

            _logger.LogDebug(
                "Text chunked into {ChunkCount} chunks using RecursiveCharacterTextSplitter for document {DocumentId} " +
                "(chunkSize={ChunkSize}, overlap={Overlap})",
                chunks.Count, documentId, chunkSize, overlapSize);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking text for document {DocumentId}", documentId);
            throw;
        }
    }

    /// <summary>
    /// RecursiveCharacterTextSplitter kullanarak metni semantik olarak parçalara böler
    /// Paragraf, satır, kelime ve karakter seviyeleri ile bölmeyi dener
    /// </summary>
    public List<DocumentChunk> ChunkTextSemantic(string text, Guid documentId, int maxChunkSize = 500, int minChunkSize = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<DocumentChunk>();
        }

        try
        {
            // RecursiveCharacterTextSplitter'ı semantic separators ile yapılandır
            // Önce paragraflarla böl, sonra satırlarla, sonra kelimelerle, sonra karakterlerle
            var splitter = new RecursiveCharacterTextSplitter(
                chunkSize: maxChunkSize,
                chunkOverlap: 0,  // Semantic chunking için overlap kullanmıyoruz
                separators: new List<string>
                {
                    "\n\n",      // Paragraf seviyesi
                    "\n",        // Satır seviyesi
                    " ",         // Kelime seviyesi
                    ""           // Karakter seviyesi (fallback)
                },
                isSeparatorRegex: false,
                lengthFunction: str => str.Length
            );

            // Metni semantik olarak böl
            var splitTexts = splitter.SplitText(text);

            var chunks = new List<DocumentChunk>();
            var chunkIndex = 0;
            var currentPosition = 0;

            foreach (var chunkContent in splitTexts)
            {
                // Minimum boyut kontrolü
                if (chunkContent.Length < minChunkSize)
                {
                    _logger.LogDebug("Skipping chunk smaller than minChunkSize ({Length} < {MinSize})",
                        chunkContent.Length, minChunkSize);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    // Chunk'ın orijinal metinde pozisyonunu bul
                    var startPosition = text.IndexOf(chunkContent, currentPosition);
                    if (startPosition < 0)
                    {
                        startPosition = currentPosition;
                    }

                    var endPosition = startPosition + chunkContent.Length;

                    var chunk = DocumentChunk.Create(
                        documentId: documentId,
                        chunkIndex: chunkIndex,
                        content: chunkContent,
                        startPosition: startPosition,
                        endPosition: endPosition
                    );

                    chunks.Add(chunk);
                    chunkIndex++;
                    currentPosition = endPosition;
                }
            }

            _logger.LogDebug(
                "Text semantically chunked into {ChunkCount} chunks using RecursiveCharacterTextSplitter " +
                "for document {DocumentId} (maxChunkSize={MaxSize}, minChunkSize={MinSize})",
                chunks.Count, documentId, maxChunkSize, minChunkSize);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error semantically chunking text for document {DocumentId}", documentId);
            throw;
        }
    }
}