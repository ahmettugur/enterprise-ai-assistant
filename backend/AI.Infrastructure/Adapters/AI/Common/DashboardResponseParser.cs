using System.Text.RegularExpressions;
using AI.Application.Ports.Secondary.Services.Report;
using AI.Application.DTOs.Dashboard;

namespace AI.Infrastructure.Adapters.AI.Common;

public class DashboardResponseParser : IDashboardParser
{
    private readonly List<string> _expectedJsFiles = new List<string>
    {
        "dashboard-core.js",
        "dashboard-api.js", 
        "dashboard-kpi-card.js",
        "dashboard-chart.js",
        "dashboard-datatable.js"
    };

    public ParseResult ParseResponse(string response)
    {
        var result = new ParseResult { Success = true };
        
        try
        {
            result.Files.HtmlContent = ExtractHtmlContent(response);
            result.Files.CssContent = ExtractCssContent(response);
            result.Files.JsFiles = ExtractJavaScriptFiles(response);
            result.Files.UniqId = ExtractUniqueId(response);
            result.Files.Instructions = ExtractInstructions(response);

            ValidateFiles(result);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Parse error: {ex.Message}");
        }

        return result;
    }

    private string ExtractHtmlContent(string response)
    {
        var patterns = new[]
        {
            @"(?:HTML Dosyası İçeriği|📄.*?\.html)[\s\S]*?```html\s*([\s\S]*?)```",
            @"(?:1\.\s*\*\*HTML\s*Dosyası)[\s\S]*?```html\s*([\s\S]*?)```",
            @"(?:Ana HTML Dosyası)[\s\S]*?```html\s*([\s\S]*?)```",
            @"(?:##\s*1\.\s*`?dashboard\.html`?)[\s\S]*?```html\s*([\s\S]*?)```",
            @"dashboard\.html[\s\S]*?```html\s*([\s\S]*?)```",
            @"```html\s*([\s\S]*?)```",
            @"<html[\s\S]*?(</html>)"
        };
        return ExtractWithPatterns(response, patterns);
    }

    private string ExtractCssContent(string response)
    {
        var patterns = new[]
        {
            @"(?:CSS Dosyası İçeriği|📄.*?\.css)[\s\S]*?```css\s*([\s\S]*?)```",
            @"(?:2\.\s*\*\*CSS\s*Dosyası)[\s\S]*?```css\s*([\s\S]*?)```",
            @"(?:dashboard.*?\.css)[\s\S]*?```css\s*([\s\S]*?)```"
        };

        return ExtractWithPatterns(response, patterns);
    }

    private Dictionary<string, string> ExtractJavaScriptFiles(string response)
    {
        var jsFiles = new Dictionary<string, string>();
        
        // Her JavaScript dosyasını ayrı ayrı bul
        var allJsMatches = FindAllJavaScriptSections(response);
        
        foreach (var expectedFile in _expectedJsFiles)
        {
            var content = FindJavaScriptFileContent(allJsMatches, expectedFile);
            if (!string.IsNullOrEmpty(content))
            {
                jsFiles[expectedFile] = content;
            }
        }

        return jsFiles;
    }

    private List<(string fileName, string content, int position)> FindAllJavaScriptSections(string response)
    {
        var sections = new List<(string fileName, string content, int position)>();
        
        // Tüm JavaScript bölümlerini bul
        var jsPattern = @"(?:📄\s*`js/([^`]+\.js)`|(?:\*\*)?([^/\s]+\.js)(?:\*\*)?)[\s\S]*?```javascript\s*([\s\S]*?)```";
        var matches = Regex.Matches(response, jsPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        foreach (Match match in matches)
        {
            var fileName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            var content = match.Groups[3].Value.Trim();
            var position = match.Index;
            
            if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(content))
            {
                sections.Add((fileName, content, position));
            }
        }
        
        // Sıralama - pozisyona göre
        return sections.OrderBy(x => x.position).ToList();
    }

    private string FindJavaScriptFileContent(List<(string fileName, string content, int position)> allSections, string targetFileName)
    {
        // Tam eşleşme ara
        var exactMatch = allSections.FirstOrDefault(x => 
            string.Equals(x.fileName, targetFileName, StringComparison.OrdinalIgnoreCase));
        
        if (!string.IsNullOrEmpty(exactMatch.content))
            return exactMatch.content;
        
        // Kısmi eşleşme ara (dashboard-core için core, dashboard-api için api vs.)
        var fileKeyword = ExtractFileKeyword(targetFileName);
        var partialMatch = allSections.FirstOrDefault(x => 
            x.fileName.ToLower().Contains(fileKeyword.ToLower()));
        
        if (!string.IsNullOrEmpty(partialMatch.content))
            return partialMatch.content;
        
        // Sınıf adına göre ara
        var expectedClassName = GetExpectedClassName(targetFileName);
        var classMatch = allSections.FirstOrDefault(x => 
            x.content.Contains($"class {expectedClassName}"));
        
        return classMatch.content ?? string.Empty;
    }

    private string ExtractFileKeyword(string fileName)
    {
        // dashboard-core.js -> core
        // dashboard-api.js -> api
        // dashboard-kpi-card.js -> kpi
        if (fileName.StartsWith("dashboard-"))
        {
            var keyword = fileName.Substring(10).Replace(".js", "");
            if (keyword.Contains("-"))
                return keyword.Split('-')[0]; // kpi-card -> kpi
            return keyword;
        }
        return fileName.Replace(".js", "");
    }

    private string GetExpectedClassName(string fileName)
    {
        return fileName switch
        {
            "dashboard-core.js" => "DashboardCore",
            "dashboard-api.js" => "DashboardApi",
            "dashboard-kpi-card.js" => "DashboardKpiCard",
            "dashboard-chart.js" => "DashboardChart",
            "dashboard-datatable.js" => "DashboardDataTable",
            _ => ""
        };
    }

    private string ExtractUniqueId(string response)
    {
        var patterns = new[]
        {
            @"uniqueId['""]?\s*:\s*['""]([^'""]+)['""]",
            @"dashboard-([A-F0-9]{32})",
            @"dashboard-([a-f0-9]{32})",
            @"dashboard-([A-Za-z0-9]{8,32})"
        };

        return ExtractWithPatterns(response, patterns);
    }

    private string ExtractInstructions(string response)
    {
        var patterns = new[]
        {
            @"instructions['""]?\s*:\s*['""]([^'""]+)['""]",
            @"Dashboard\s*-\s*([^<\n]+)",
            @"title.*?Dashboard\s*-\s*([^<\n]+)"
        };

        return ExtractWithPatterns(response, patterns);
    }

    private string ExtractWithPatterns(string text, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }
        return string.Empty;
    }

    private void ValidateFiles(ParseResult result)
    {
        if (string.IsNullOrEmpty(result.Files.HtmlContent))
            result.Warnings.Add("HTML content not found");

        if (string.IsNullOrEmpty(result.Files.CssContent))
            result.Warnings.Add("CSS content not found");

        foreach (var expectedFile in _expectedJsFiles)
        {
            if (!result.Files.JsFiles.ContainsKey(expectedFile))
                result.Warnings.Add($"JavaScript file not found: {expectedFile}");
        }

        if (string.IsNullOrEmpty(result.Files.UniqId))
        {
            result.Files.UniqId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            result.Warnings.Add($"UniqId not found, generated: {result.Files.UniqId}");
        }
    }
}