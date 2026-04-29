using AI.Application.DTOs;
using AI.Application.Results;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Zamanlanmış rapor yönetimi için Use Case interface'i
/// </summary>
public interface IScheduledReportUseCase
{
    #region CRUD Operations

    /// <summary>
    /// Yeni zamanlanmış rapor oluşturur
    /// </summary>
    Task<Result<ScheduledReportDto>> CreateAsync(CreateScheduledReportDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mevcut bir mesajdan zamanlanmış rapor oluşturur (Frontend'den kullanılır)
    /// MessageId'den SQL sorgusu ve prompt bilgileri çekilir
    /// </summary>
    Task<Result<ScheduledReportDto>> CreateFromMessageAsync(CreateScheduledReportFromMessageDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID'ye göre zamanlanmış rapor getirir
    /// </summary>
    Task<Result<ScheduledReportDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// ID'ye göre zamanlanmış raporu loglarıyla birlikte getirir
    /// </summary>
    Task<Result<ScheduledReportDetailDto>> GetByIdWithLogsAsync(Guid id, int logLimit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının tüm zamanlanmış raporlarını getirir
    /// </summary>
    Task<Result<List<ScheduledReportDto>>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mevcut kullanıcının zamanlanmış raporlarını getirir
    /// </summary>
    Task<Result<List<ScheduledReportDto>>> GetMyReportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamanlanmış raporu günceller
    /// </summary>
    Task<Result<ScheduledReportDto>> UpdateAsync(Guid id, UpdateScheduledReportDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zamanlanmış raporu siler
    /// </summary>
    Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Status Operations

    /// <summary>
    /// Raporu aktif/pasif yapar
    /// </summary>
    Task<Result<ScheduledReportDto>> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raporu duraklatır
    /// </summary>
    Task<Result<ScheduledReportDto>> PauseAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raporu devam ettirir
    /// </summary>
    Task<Result<ScheduledReportDto>> ResumeAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Execution Operations

    /// <summary>
    /// Raporu manuel olarak çalıştırır
    /// </summary>
    Task<Result<ScheduledReportLogDto>> RunNowAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raporun log geçmişini getirir
    /// </summary>
    Task<Result<List<ScheduledReportLogDto>>> GetLogsAsync(Guid reportId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);

    #endregion
}
