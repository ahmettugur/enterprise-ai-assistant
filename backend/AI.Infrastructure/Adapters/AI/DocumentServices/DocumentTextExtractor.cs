using System.Text;
using System.Globalization;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using CsvHelper;
using UglyToad.PdfPig;
using AI.Application.Ports.Secondary.Services.Document;

namespace AI.Infrastructure.Adapters.AI.DocumentServices;

/// <summary>
/// Dosya formatlarından metin çıkarmak için adapter
/// PDF, Excel, Word, CSV, TXT, PowerPoint formatlarını destekler
/// </summary>
public class DocumentTextExtractor : IDocumentTextExtractor
{
    private const int MaxRows = 100000;
    private const int MaxColumns = 20;
    private const int MaxPages = 20;
    private const int MaxSlides = 20;

    /// <summary>
    /// Dosya türüne göre metni çıkar
    /// </summary>
    public string ExtractText(string fileName, Stream fileStream)
    {
        if (fileStream == null || !fileStream.CanRead)
            return "Dosya okunamadı";

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            return extension switch
            {
                ".xlsx" or ".xls" => ExtractExcel(fileStream),
                ".csv" => ExtractCsv(fileStream),
                ".pdf" => ExtractPdf(fileStream),
                ".docx" or ".doc" => ExtractDocx(fileStream),
                ".txt" => ExtractTxt(fileStream),
                ".pptx" or ".ppt" => ExtractPptx(fileStream),
                _ => $"Desteklenmeyen dosya türü: {extension}"
            };
        }
        catch (Exception ex)
        {
            return $"Dosya işleme hatası ({extension}): {ex.Message}";
        }
    }

    // ===== TXT =====
    private static string ExtractTxt(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            return $"TXT parsing error: {ex.Message}";
        }
    }

    // ===== CSV =====
    private static string ExtractCsv(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<dynamic>().ToList();
            var text = new StringBuilder();

            // Headers
            if (csv.HeaderRecord != null)
            {
                text.AppendLine("COLUMNS: " + string.Join(" | ", csv.HeaderRecord));
            }

            // Data rows (max 100)
            int rowCount = 0;
            foreach (var record in records.Take(MaxRows))
            {
                var dict = (IDictionary<string, object>)record;
                var values = dict.Values.Take(MaxColumns);
                text.AppendLine(string.Join(" | ", values));
                rowCount++;
            }

            if (records.Count > MaxRows)
                text.AppendLine($"\n... ({records.Count - MaxRows} daha fazla satır)");

            return text.ToString();
        }
        catch (Exception ex)
        {
            return $"CSV parsing error: {ex.Message}";
        }
    }

    // ===== EXCEL (ClosedXML) =====
    private static string ExtractExcel(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var text = new StringBuilder();

            int sheetCount = 0;
            foreach (var worksheet in workbook.Worksheets)
            {
                if (sheetCount >= 10) break; // Max 10 sheets

                text.AppendLine($"\n=== Sheet: {worksheet.Name} ===\n");

                var usedRange = worksheet.RangeUsed();
                if (usedRange == null)
                {
                    text.AppendLine("(Boş Sheet)");
                    sheetCount++;
                    continue;
                }

                int maxRow = Math.Min(usedRange.LastCell().Address.RowNumber, MaxRows);
                int maxCol = Math.Min(usedRange.LastCell().Address.ColumnNumber, MaxColumns);

                // Headers (Row 1)
                for (int col = 1; col <= maxCol; col++)
                {
                    var cellValue = worksheet.Cell(1, col).Value.ToString() ?? "";
                    text.Append(cellValue);
                    if (col < maxCol) text.Append(" | ");
                }
                text.AppendLine();

                // Data rows (Row 2+)
                for (int row = 2; row <= maxRow; row++)
                {
                    for (int col = 1; col <= maxCol; col++)
                    {
                        var cellValue = worksheet.Cell(row, col).Value.ToString() ?? "";
                        text.Append(cellValue);
                        if (col < maxCol) text.Append(" | ");
                    }
                    text.AppendLine();
                }

                if (usedRange.LastCell().Address.RowNumber > MaxRows)
                    text.AppendLine($"... ({usedRange.LastCell().Address.RowNumber - MaxRows} daha fazla satır)");

                sheetCount++;
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            return $"Excel parsing error: {ex.Message}";
        }
    }

    // ===== PDF (UglyToad.PdfPig) =====
    private static string ExtractPdf(Stream stream)
    {
        try
        {
            var text = new StringBuilder();

            try
            {
                using var document = PdfDocument.Open(stream);
                int pageCount = Math.Min(document.NumberOfPages, MaxPages);

                for (int pageIndex = 1; pageIndex <= pageCount; pageIndex++)
                {
                    text.AppendLine($"\n--- Page {pageIndex} ---\n");

                    try
                    {
                        var page = document.GetPage(pageIndex);
                        var pageText = new StringBuilder();

                        // Extract text from all words on the page
                        foreach (var word in page.GetWords())
                        {
                            pageText.Append(word.Text);
                            pageText.Append(" ");
                        }

                        if (pageText.Length > 0)
                        {
                            text.AppendLine(pageText.ToString());
                        }
                        else
                        {
                            text.AppendLine("(No text content on this page)");
                        }
                    }
                    catch
                    {
                        // If page extraction fails for this page, continue
                        text.AppendLine("(Text extraction unavailable for this page)");
                    }
                }

                if (document.NumberOfPages > MaxPages)
                    text.AppendLine($"\n... ({document.NumberOfPages - MaxPages} daha fazla sayfa)");
            }
            catch (Exception innerEx)
            {
                return $"PDF parsing error: {innerEx.Message}";
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            return $"PDF parsing error: {ex.Message}";
        }
    }

    // ===== DOCX (DocumentFormat.OpenXml) =====
    private static string ExtractDocx(Stream stream)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            var text = new StringBuilder();

            try
            {
                // Extract all text from document parts
                foreach (var part in doc.Parts)
                {
                    try
                    {
                        var partObj = part.OpenXmlPart;
                        if (partObj != null)
                        {
                            // Extract text from all elements in the part
                            var allElements = partObj.RootElement?.Descendants<Paragraph>() ?? Enumerable.Empty<Paragraph>();
                            foreach (var para in allElements)
                            {
                                text.AppendLine(para.InnerText);
                            }
                        }
                    }
                    catch
                    {
                        // Skip parts that can't be read
                    }
                }
            }
            catch
            {
                // If document structure is different, return what we have
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            return $"Word parsing error: {ex.Message}";
        }
    }

    // ===== PPTX (DocumentFormat.OpenXml) =====
    private static string ExtractPptx(Stream stream)
    {
        try
        {
            using var doc = PresentationDocument.Open(stream, false);
            var text = new StringBuilder();
            var presentationPart = doc.PresentationPart;

            if (presentationPart?.Presentation?.SlideIdList == null)
                return "PowerPoint parsing error: Invalid presentation structure";

            int slideNum = 0;
            int totalSlides = 0;

            try
            {
                foreach (var slideId in presentationPart.Presentation.SlideIdList)
                {
                    totalSlides++;
                    if (slideNum >= MaxSlides) break;
                    slideNum++;

                    try
                    {
                        // Try to get relationship ID from the slide ID element
                        // SlideId has an Embed property that contains the relationship ID
                        string relId = (slideId as dynamic)?.Embed?.Value ?? (slideId as dynamic)?.Id?.Value ?? "";
                        if (string.IsNullOrEmpty(relId)) continue;

                        var slidePart = presentationPart.GetPartById(relId) as DocumentFormat.OpenXml.Packaging.SlidePart;
                        if (slidePart?.Slide == null) continue;

                        var slide = slidePart.Slide;

                        text.AppendLine($"\n=== Slide {slideNum} ===\n");

                        var slideText = new StringBuilder();
                        foreach (var shape in slide.Descendants<DocumentFormat.OpenXml.Presentation.Shape>())
                        {
                            var textBody = shape.Descendants<DocumentFormat.OpenXml.Presentation.TextBody>()
                                .FirstOrDefault();
                            if (textBody != null)
                            {
                                slideText.AppendLine(textBody.InnerText);
                            }
                        }

                        text.AppendLine(slideText.ToString());
                    }
                    catch
                    {
                        // Skip slides that can't be read
                        continue;
                    }
                }
            }
            catch
            {
                // If iteration fails, return what we have
            }

            if (totalSlides > MaxSlides)
                text.AppendLine($"\n... ({totalSlides - MaxSlides} daha fazla slide)");

            return text.ToString();
        }
        catch (Exception ex)
        {
            return $"PowerPoint parsing error: {ex.Message}";
        }
    }
}
