using System.Text;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.DTOs.Dashboard;
using AI.Application.DTOs.Chat;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AI.Infrastructure.Adapters.AI.Common;

public class DashboardFileSaver(IConfiguration configuration) : IFileSaver
{
    public async Task<(string projectPath, string outputApiUrl)> SaveDashboardFiles(DashboardFiles files, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output")
    {
        var folderPath = $"dashboard-{DateTime.Now:yyyyMMddHHmmss}";
        var projectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,basePath,folderPath );
        var outputUrl = configuration.GetValue<string>("OutputApiEndpoints:ApiUrl")!;
        var outputApiUrl = $"{outputUrl}/{basePath}/{folderPath}/index.html";
        CreateDirectoryStructure(projectPath);
        
        await SaveHtmlFile(files, projectPath);
        await SaveCssFile(files, projectPath);
        await SaveJavaScriptFiles(files, projectPath);
        await CreateReadmeFile(files, projectPath);
        await CreateJsonDataFile(dataForHtmlModel, projectPath);
        CopyResourceFiles(projectPath);
        
        return (projectPath, outputApiUrl);
    }

    /// <summary>
    /// Template-based hızlı dashboard kaydetme
    /// </summary>
    public async Task<(string projectPath, string outputApiUrl)> SaveTemplateDashboard(DashboardConfig config, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output")
    {
        var folderPath = $"dashboard-{config.Id}";
        var projectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, basePath, folderPath);
        var outputUrl = configuration.GetValue<string>("OutputApiEndpoints:ApiUrl")!;
        var outputApiUrl = $"{outputUrl}/{basePath}/{folderPath}/index.html";
        
        CreateDirectoryStructure(projectPath);
        
        // Template dosyalarını kopyala
        CopyTemplateFiles(projectPath);
        
        // Config JSON dosyasını kaydet
        await SaveConfigJsonFile(config, projectPath);
        
        // Data JSON dosyasını kaydet
        await CreateJsonDataFile(dataForHtmlModel, projectPath);
        
        // Ortak kaynakları kopyala
        CopyResourceFiles(projectPath);
        
        return (projectPath, outputApiUrl);
    }
    
    /// <summary>
    /// Template dosyalarını (HTML, JS) dashboard klasörüne kopyalar
    /// </summary>
    private void CopyTemplateFiles(string projectPath)
    {
        var templateBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Common", "Resources", "Templates", "Dashboard");
        
        // HTML template
        var htmlSource = Path.Combine(templateBasePath, "dashboard-template.html");
        var htmlDest = Path.Combine(projectPath, "index.html");
        if (File.Exists(htmlSource))
        {
            File.Copy(htmlSource, htmlDest, true);
        }
        
        // JS renderer
        var jsSource = Path.Combine(templateBasePath, "js", "dashboard-renderer.js");
        var jsDest = Path.Combine(projectPath, "js", "dashboard-renderer.js");
        if (File.Exists(jsSource))
        {
            File.Copy(jsSource, jsDest, true);
        }
    }
    
    /// <summary>
    /// Dashboard config JSON dosyasını kaydet
    /// </summary>
    private async Task SaveConfigJsonFile(DashboardConfig config, string projectPath)
    {
        var path = Path.Combine(projectPath, "js", "config.json");
        var jsonData = JsonConvert.SerializeObject(config, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        });
        await File.WriteAllTextAsync(path, jsonData, Encoding.UTF8);
    }

    /// <summary>
    /// Ortak kaynak dosyalarını (CSS, Assets, Language files) dashboard klasörüne kopyalar
    /// </summary>
    private void CopyResourceFiles(string projectPath)
    {
        var filesToCopy = new[]
        {
            "css/reports.css",
            "css/variables.css",
            "assets/chart.png",
            "assets/excel.png",
            "assets/pdf.png",
            "js/tr.json"
        };

        foreach (var relativePath in filesToCopy)
        {
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Common", "Resources", relativePath);
            var destPath = Path.Combine(projectPath, relativePath);
            var destDir = Path.GetDirectoryName(destPath)!;
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destPath, true);
            }
        }
    }
    private void CreateDirectoryStructure(string projectPath)
    {
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "css"));
        Directory.CreateDirectory(Path.Combine(projectPath, "js"));
        Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
    }

    private async Task SaveHtmlFile(DashboardFiles files, string projectPath)
    {
        if (string.IsNullOrEmpty(files.HtmlContent)) return;

        var htmlPath = Path.Combine(projectPath, $"index.html");
        var processedHtml = ProcessHtmlContent(files.HtmlContent, files.UniqId);
        
        // AI Veri Analizi placeholder'ını insight HTML ile değiştir
        processedHtml = ReplaceInsightPlaceholder(processedHtml, files.InsightHtml, files.UniqId);
        
        await File.WriteAllTextAsync(htmlPath, processedHtml, Encoding.UTF8);
    }
    
    /// <summary>
    /// AI Veri Analizi placeholder'ını gerçek insight HTML ile değiştirir
    /// </summary>
    private string ReplaceInsightPlaceholder(string html, string insightHtml, string uniqueId)
    {
        if (string.IsNullOrEmpty(insightHtml))
            return html;
        
        // Placeholder patterns - uniqueId ile ve olmadan
        var placeholderPatterns = new[]
        {
            $"<div id=\"ai-insights-placeholder-{uniqueId}\" class=\"mt-8\">",
            $"<div id=\"ai-insights-placeholder-${{uniqueId}}\" class=\"mt-8\">",
            "<div id=\"ai-insights-placeholder-{uniqueId}\" class=\"mt-8\">"
        };
        
        foreach (var pattern in placeholderPatterns)
        {
            if (html.Contains(pattern))
            {
                // Placeholder div'in tamamını bul ve değiştir
                var startIndex = html.IndexOf(pattern, StringComparison.Ordinal);
                if (startIndex >= 0)
                {
                    // </div> closing tag'ini bul
                    var searchStart = startIndex + pattern.Length;
                    var endIndex = html.IndexOf("</div>", searchStart, StringComparison.Ordinal);
                    if (endIndex >= 0)
                    {
                        var placeholderFull = html.Substring(startIndex, endIndex - startIndex + 6); // 6 = "</div>".Length
                        html = html.Replace(placeholderFull, insightHtml);
                        break;
                    }
                }
            }
        }
        
        return html;
    }

    private async Task SaveCssFile(DashboardFiles files, string projectPath)
    {
        if (string.IsNullOrEmpty(files.CssContent)) return;

        var cssPath = Path.Combine(projectPath, "css", $"dashboard.css");
        await File.WriteAllTextAsync(cssPath, files.CssContent, Encoding.UTF8);
    }

    private async Task SaveJavaScriptFiles(DashboardFiles files, string projectPath)
    {
        foreach (var jsFile in files.JsFiles)
        {
            var jsPath = Path.Combine(projectPath, "js", jsFile.Key);
            var processedJs = ProcessJavaScriptContent(jsFile.Value, files.UniqId);
            await File.WriteAllTextAsync(jsPath, processedJs, Encoding.UTF8);
        }
    }

    private string ProcessHtmlContent(string htmlContent, string uniqId)
    {
        return htmlContent
            .Replace("${uniqueId}", uniqId)
            .Replace("{uniqueId}", uniqId)
            .Replace("{UNIQUEID}", uniqId)
            .Replace("dashboard-{uniqueId}", $"dashboard-{uniqId}");
    }

    private string ProcessJavaScriptContent(string jsContent, string uniqId)
    {
        return jsContent
            .Replace("${uniqueId}", uniqId)   
            .Replace("'{uniqueId}'", $"'{uniqId}'")
            .Replace("\"{uniqueId}\"", $"\"{uniqId}\"")
            .Replace("{uniqueId}", uniqId);
    }

    private async Task CreateReadmeFile(DashboardFiles files, string projectPath)
    {
        var readme = GenerateReadmeContent(files);
        await File.WriteAllTextAsync(Path.Combine(projectPath, "README.md"), readme, Encoding.UTF8);
    }
    
    private async Task CreateJsonDataFile(DataForHtmlModel dataForHtmlModel, string projectPath)
    {
        var path = Path.Combine(projectPath,"js","data.json");
        var jsonData = JsonConvert.SerializeObject(dataForHtmlModel,new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        await File.WriteAllTextAsync(path, jsonData, Encoding.UTF8);
    }


    private string GenerateReadmeContent(DashboardFiles files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Dashboard Application");
        sb.AppendLine();
        sb.AppendLine("This is an auto-generated dashboard application created by AI.");
        sb.AppendLine();
        sb.AppendLine("## Contents");
        sb.AppendLine();
        sb.AppendLine("- **index.html**: Main HTML file");
        sb.AppendLine("- **css/dashboard.css**: Main stylesheet");

        if (files.JsFiles?.Count > 0)
        {
            sb.AppendLine("- **js/**: JavaScript files");
            foreach (var jsFile in files.JsFiles)
            {
                sb.AppendLine($"  - {jsFile.Key}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Usage");
        sb.AppendLine();
        sb.AppendLine("1. Open `index.html` in a web browser");
        sb.AppendLine("2. The dashboard will load and display the data");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"Generated on: {DateTime.UtcNow:O}");

        return sb.ToString();
    }
}