using System.Diagnostics;
using AI.Application.DTOs;
using AI.Application.DTOs.Chat;
using AI.Application.DTOs.Dashboard;
using AI.Application.Ports.Primary.UseCases;
using AI.Application.Ports.Secondary.Services.Common;
using AI.Application.Ports.Secondary.Services.Report;

namespace AI.Application.UseCases;

public class DashboardUseCase : IDashboardUseCase
{
    private readonly IDashboardParser _parser;
    private readonly IFileSaver _fileSaver;

    public DashboardUseCase(IDashboardParser parser,IFileSaver fileSaver)
    {
        _parser = parser;
        _fileSaver = fileSaver;
    }

    public async Task<DashboardProcessResult> ProcessDashboardResponse(string promptResponse, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output", string insightHtml = "")
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DashboardProcessResult();

        try
        {
            if (string.IsNullOrWhiteSpace(promptResponse))
            {
                result.Errors.Add("Prompt response is empty or null");
                return result;
            }

            // Parse the response
            var parseResult = _parser.ParseResponse(promptResponse);
            parseResult.Files.UniqId = dataForHtmlModel.UniqueId;
            parseResult.Files.InsightHtml = insightHtml; // AI Veri Analizi HTML'i ekle
            result.Files = parseResult.Files;
            result.Errors.AddRange(parseResult.Errors);
            result.Warnings.AddRange(parseResult.Warnings);

            if (!parseResult.Success)
            {
                result.Errors.Add("Failed to parse dashboard response");
                return result;
            }

            // Save files
            var (projectPath, outputApiUrl) = await _fileSaver.SaveDashboardFiles(parseResult.Files, dataForHtmlModel, basePath);
            result.OutputApiUrl = outputApiUrl;
            result.ProjectPath = projectPath;
            result.FilePathMapping = GenerateFilePathMapping(result.ProjectPath, parseResult.Files);
            result.Success = true;

        }
        catch (Exception ex)
        {
            result.Errors.Add($"Service error: {ex.Message}");
            result.Success = false;
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }

    private Dictionary<string, string> GenerateFilePathMapping(string projectPath, DashboardFiles files)
    {
        var mapping = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(files.HtmlContent))
            mapping["html"] = Path.Combine(projectPath, $"index.html");

        if (!string.IsNullOrEmpty(files.CssContent))
            mapping["css"] = Path.Combine(projectPath, "css", $"dashboard.css");

        foreach (var jsFile in files.JsFiles)
        {
            mapping[jsFile.Key] = Path.Combine(projectPath, "js", jsFile.Key);
        }

        mapping["readme"] = Path.Combine(projectPath, "README.md");

        return mapping;
    }

    /// <summary>
    /// Template-based hızlı dashboard işleme
    /// </summary>
    public async Task<DashboardProcessResult> ProcessTemplateDashboard(DashboardConfig config, DataForHtmlModel dataForHtmlModel, string basePath = "dashboard-output")
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DashboardProcessResult();

        try
        {
            if (config == null)
            {
                result.Errors.Add("Dashboard config is null");
                return result;
            }

            // Config ID'yi unique yap
            if (string.IsNullOrEmpty(config.Id))
            {
                config.Id = Guid.NewGuid().ToString("N");
            }

            // Template dosyalarını kaydet
            var (projectPath, outputApiUrl) = await _fileSaver.SaveTemplateDashboard(config, dataForHtmlModel, basePath);
            
            result.OutputApiUrl = outputApiUrl;
            result.ProjectPath = projectPath;
            result.FilePathMapping = GenerateTemplateFilePathMapping(projectPath);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Template dashboard processing error: {ex.Message}");
            result.Success = false;
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }

    private Dictionary<string, string> GenerateTemplateFilePathMapping(string projectPath)
    {
        return new Dictionary<string, string>
        {
            ["html"] = Path.Combine(projectPath, "index.html"),
            ["config"] = Path.Combine(projectPath, "js", "config.json"),
            ["data"] = Path.Combine(projectPath, "js", "data.json"),
            ["renderer"] = Path.Combine(projectPath, "js", "dashboard-renderer.js")
        };
    }
}