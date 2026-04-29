using AI.Application.DTOs.Dashboard;

namespace AI.Application.DTOs;

public class DashboardProcessResult
{
    public bool Success { get; set; }
    public string ProjectPath { get; set; } = string.Empty;
    public string OutputApiUrl { get; set; } = string.Empty;
    public DashboardFiles Files { get; set; } = new DashboardFiles();
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public Dictionary<string, string> FilePathMapping { get; set; } = new Dictionary<string, string>();
    public TimeSpan ProcessingTime { get; set; }
}