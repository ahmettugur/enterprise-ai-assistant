namespace AI.Application.DTOs.Dashboard;

/// <summary>
/// Dashboard parse sonucu için DTO
/// </summary>
public class ParseResult
{
    public bool Success { get; set; }
    public DashboardFiles Files { get; set; } = new DashboardFiles();
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
}
