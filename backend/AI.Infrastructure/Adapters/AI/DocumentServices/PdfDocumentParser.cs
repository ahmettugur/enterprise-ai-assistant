
using System.Text;
using AI.Application.Common.Helpers;
using AI.Application.Ports.Secondary.Services.Document;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace AI.Infrastructure.Adapters.AI.DocumentServices;

/// <summary>
/// PDF doküman parser'ı
/// </summary>
public class PdfDocumentParser : IDocumentParser
{
    private readonly ILogger<PdfDocumentParser> _logger;

    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        "application/pdf"
    };

    public PdfDocumentParser(ILogger<PdfDocumentParser> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> SupportedFileTypes => new[] { ".pdf", "application/pdf" };

    public bool CanParse(string fileTypeOrExtension)
    {
        return SupportedTypes.Contains(fileTypeOrExtension);
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await ExtractTextAsync(fileStream, Path.GetFileName(filePath), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var text = new StringBuilder();

            await Task.Run(() =>
            {
                using var document = PdfDocument.Open(stream);

                foreach (var page in document.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        // Türkçe karakterler encoding sorununu düzelt (merkezi helper kullanarak)
                        var correctedText = TurkishEncodingHelper.FixEncoding(pageText);
                        text.AppendLine(correctedText);
                        text.AppendLine(); // Sayfa arası boşluk
                    }
                }
            }, cancellationToken);

            var extractedText = text.ToString().Trim();

            _logger.LogDebug("Successfully extracted {Length} characters from PDF: {FileName}",
                extractedText.Length, fileName);

            return extractedText;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PDF text extraction was cancelled for file: {FileName}", fileName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF stream: {FileName}", fileName);
            throw new InvalidOperationException($"PDF text extraction failed for {fileName}: {ex.Message}", ex);
        }
    }
}