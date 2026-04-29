
using System.Text;
using AI.Application.Common.Helpers;
using AI.Application.Ports.Secondary.Services.Document;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.DocumentServices;

/// <summary>
/// Text doküman parser'ı (TXT, MD, CSV vb.)
/// </summary>
public class TextDocumentParser : IDocumentParser
{
    private readonly ILogger<TextDocumentParser> _logger;

    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".csv", ".json", ".xml", ".log",
        "text/plain", "text/markdown", "text/csv", "application/json", "text/xml", "application/xml"
    };

    public TextDocumentParser(ILogger<TextDocumentParser> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> SupportedFileTypes => new[] { ".txt", ".md", ".csv", ".json", ".xml", ".log" };

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
            _logger.LogError(ex, "Failed to extract text from file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            
            // Türkçe karakter encoding sorunlarını düzelt
            var correctedText = TurkishEncodingHelper.FixEncoding(text);
            
            _logger.LogDebug("Successfully extracted {Length} characters from text file: {FileName}", 
                correctedText.Length, fileName);
            
            return correctedText;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Text extraction was cancelled for file: {FileName}", fileName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from stream: {FileName}", fileName);
            throw new InvalidOperationException($"Text extraction failed for {fileName}: {ex.Message}", ex);
        }
    }
}