using AI.Application.DTOs;
using AI.Application.Ports.Primary.UseCases;
using AI.Domain.Conversations;
using AI.Domain.Scheduling;
using AI.Application.Results;
using Cronos;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AI.Application.Ports.Secondary.Services.Auth;

namespace AI.Application.UseCases;

/// <summary>
/// Zamanlanmış rapor yönetimi servis implementasyonu
/// </summary>
public sealed class ScheduledReportUseCase : IScheduledReportUseCase
{
    private readonly IScheduledReportRepository _repository;
    private readonly IConversationRepository _historyRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ScheduledReportUseCase> _logger;

    public ScheduledReportUseCase(
        IScheduledReportRepository repository,
        IConversationRepository historyRepository,
        ICurrentUserService currentUserService,
        ILogger<ScheduledReportUseCase> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region CRUD Operations

    /// <inheritdoc />
    public async Task<Result<ScheduledReportDto>> CreateAsync(CreateScheduledReportDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<ScheduledReportDto>.Error("Kullanıcı kimliği bulunamadı", "User not authenticated");
            }

            // Cron expression validasyonu
            if (!IsValidCronExpression(request.CronExpression))
            {
                return Result<ScheduledReportDto>.Error("Geçersiz cron ifadesi", $"Invalid cron expression: {request.CronExpression}");
            }

            // İsim benzersizlik kontrolü
            if (!await _repository.IsNameUniqueAsync(userId, request.Name, cancellationToken: cancellationToken))
            {
                return Result<ScheduledReportDto>.Error("Bu isimde bir zamanlanmış rapor zaten mevcut", "Duplicate name");
            }

            var report = ScheduledReport.Create(
                userId: userId,
                name: request.Name,
                originalPrompt: request.OriginalPrompt,
                sqlQuery: request.SqlQuery,
                cronExpression: request.CronExpression,
                reportServiceType: request.ReportServiceType,
                reportDatabaseType: request.ReportDatabaseType,
                reportDatabaseServiceType: request.ReportDatabaseServiceType,
                originalMessageId: request.OriginalMessageId,
                originalConversationId: request.OriginalConversationId,
                notificationEmail: request.NotificationEmail,
                teamsWebhookUrl: request.TeamsWebhookUrl
            );

            // İlk çalışma zamanını hesapla
            var nextRun = CalculateNextRun(request.CronExpression);
            if (nextRun.HasValue)
            {
                report.SetNextRunAt(nextRun); // İlk nextRunAt'ı set etmek için
            }

            await _repository.CreateAsync(report, cancellationToken);

            _logger.LogInformation("Zamanlanmış rapor oluşturuldu - Id: {Id}, UserId: {UserId}, Name: {Name}",
                report.Id, userId, report.Name);

            return Result<ScheduledReportDto>.Success(MapToDto(report), "Zamanlanmış rapor başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor oluşturulurken hata - Name: {Name}", request.Name);
            return Result<ScheduledReportDto>.Error(ex, nameof(CreateAsync));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ScheduledReportDto>> CreateFromMessageAsync(CreateScheduledReportFromMessageDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Result<ScheduledReportDto>.Error("Kullanıcı kimliği bulunamadı", "User not authenticated");
            }

            // Cron expression validasyonu
            if (!IsValidCronExpression(request.CronExpression))
            {
                return Result<ScheduledReportDto>.Error("Geçersiz cron ifadesi", $"Invalid cron expression: {request.CronExpression}");
            }

            // MessageId validasyonu
            if (!Guid.TryParse(request.MessageId, out var messageId))
            {
                return Result<ScheduledReportDto>.Error("Geçersiz mesaj ID'si", "Invalid message ID format");
            }

            // Mesajı getir
            var message = await _historyRepository.GetMessageAsync(messageId, cancellationToken);
            if (message == null)
            {
                return Result<ScheduledReportDto>.Error("Mesaj bulunamadı", $"Message not found: {request.MessageId}");
            }

            // SQL sorgusunu ve diğer bilgileri çıkar
            string? sqlQuery = null;
            string? originalPrompt = null;
            string? reportServiceType = null;
            string? databaseType = null;
            string? databaseServiceType = null;
            Guid? conversationId = message.ConversationId;

            // Önce mesaj içeriğinden SQL sorgusunu almayı dene
            // Rapor servisleri SQL sorgusunu Content olarak kaydediyor
            var content = message.Content;
            if (!string.IsNullOrEmpty(content))
            {
                // SQL sorgusu olup olmadığını kontrol et (SELECT ile başlıyor mu?)
                var trimmedContent = content.Trim();
                if (trimmedContent.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                    trimmedContent.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                {
                    sqlQuery = trimmedContent;
                }
            }

            // Metadata'dan ek bilgileri çıkar (reportServiceType, originalPrompt vb.)
            if (!string.IsNullOrEmpty(message.MetadataJson))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message.MetadataJson);
                    if (metadata != null)
                    {

                        if (metadata.TryGetValue("ReportServiceType", out var serviceElement))
                        {
                            reportServiceType = serviceElement.GetString();
                        }

                        if (metadata.TryGetValue("ReportDatabaseType", out var databaseElement))
                        {
                            databaseType = databaseElement.GetString();
                        }

                        if (metadata.TryGetValue("ReportDatabaseServiceType", out var databaseServiceElement))
                        {
                            databaseServiceType = databaseServiceElement.GetString();
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Mesaj metadata'sı parse edilemedi - MessageId: {MessageId}", messageId);
                }
            }

            // Zorunlu alanları kontrol et
            if (string.IsNullOrEmpty(sqlQuery))
            {
                return Result<ScheduledReportDto>.Error(
                    "Bu mesajda SQL sorgusu bulunamadı. Sadece rapor içeren mesajlar zamanlanabilir.",
                    "SQL query not found in message content or metadata");
            }

            // İsim uzunluğunu kontrol et ve gerekirse kısalt (max 255 karakter)
            var reportName = request.Name.Length > 250
                ? request.Name[..247] + "..."
                : request.Name;

            // İsim benzersizlik kontrolü
            if (!await _repository.IsNameUniqueAsync(userId, reportName, cancellationToken: cancellationToken))
            {
                return Result<ScheduledReportDto>.Error("Bu isimde bir zamanlanmış rapor zaten mevcut", "Duplicate name");
            }

            // E-posta adreslerini virgülle birleştir
            var notificationEmail = request.RecipientEmails != null && request.RecipientEmails.Count > 0
                ? string.Join(",", request.RecipientEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                : null;

            var report = ScheduledReport.Create(
                userId: userId,
                name: reportName,
                originalPrompt: originalPrompt ?? "Zamanlanmış rapor",
                sqlQuery: sqlQuery,
                cronExpression: request.CronExpression,
                reportServiceType: reportServiceType ?? "Unknown",
                reportDatabaseType: databaseType ?? "",
                reportDatabaseServiceType: databaseServiceType ?? "",
                originalMessageId: messageId,
                originalConversationId: conversationId,
                notificationEmail: notificationEmail,
                teamsWebhookUrl: request.SendToTeams ? "pending" : null // Teams entegrasyonu için placeholder
            );

            // Aktiflik durumunu ayarla
            if (!request.IsActive)
            {
                report.SetActive(false);
            }

            // İlk çalışma zamanını hesapla
            var nextRun = CalculateNextRun(request.CronExpression);
            if (nextRun.HasValue)
            {
                report.SetNextRunAt(nextRun);
            }

            await _repository.CreateAsync(report, cancellationToken);

            _logger.LogInformation("Mesajdan zamanlanmış rapor oluşturuldu - Id: {Id}, UserId: {UserId}, Name: {Name}, MessageId: {MessageId}",
                report.Id, userId, report.Name, messageId);

            return Result<ScheduledReportDto>.Success(MapToDto(report), "Zamanlanmış rapor başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            // Inner exception'ı da logla
            var innerMessage = ex.InnerException?.Message ?? "No inner exception";
            _logger.LogError(ex, "Mesajdan zamanlanmış rapor oluşturulurken hata - Name: {Name}, MessageId: {MessageId}, InnerException: {InnerException}",
                request.Name, request.MessageId, innerMessage);
            return Result<ScheduledReportDto>.Error($"Rapor kaydedilemedi: {innerMessage}", nameof(CreateFromMessageAsync));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ScheduledReportDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _repository.GetByIdAsync(id, cancellationToken);

            if (report == null)
            {
                return Result<ScheduledReportDto>.Error("Zamanlanmış rapor bulunamadı", $"Report not found: {id}");
            }

            // Yetki kontrolü
            if (!HasAccessToReport(report))
            {
                return Result<ScheduledReportDto>.Error("Bu rapora erişim yetkiniz yok", "Access denied");
            }

            return Result<ScheduledReportDto>.Success(MapToDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor getirilirken hata - Id: {Id}", id);
            return Result<ScheduledReportDto>.Error(ex, nameof(GetByIdAsync));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ScheduledReportDetailDto>> GetByIdWithLogsAsync(Guid id, int logLimit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _repository.GetByIdWithLogsAsync(id, logLimit, cancellationToken);

            if (report == null)
            {
                return Result<ScheduledReportDetailDto>.Error("Zamanlanmış rapor bulunamadı", $"Report not found: {id}");
            }

            // Yetki kontrolü
            if (!HasAccessToReport(report))
            {
                return Result<ScheduledReportDetailDto>.Error("Bu rapora erişim yetkiniz yok", "Access denied");
            }

            var dto = new ScheduledReportDetailDto
            {
                Report = MapToDto(report),
                RecentLogs = report.Logs?.Select(MapLogToDto).ToList() ?? []
            };

            return Result<ScheduledReportDetailDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor detayı getirilirken hata - Id: {Id}", id);
            return Result<ScheduledReportDetailDto>.Error(ex, nameof(GetByIdWithLogsAsync));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<ScheduledReportDto>>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reports = await _repository.GetByUserIdAsync(userId, cancellationToken);
            var dtos = reports.Select(MapToDto).ToList();

            return Result<List<ScheduledReportDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcının zamanlanmış raporları getirilirken hata - UserId: {UserId}", userId);
            return Result<List<ScheduledReportDto>>.Error(ex, nameof(GetByUserIdAsync));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<ScheduledReportDto>>> GetMyReportsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result<List<ScheduledReportDto>>.Error("Kullanıcı kimliği bulunamadı", "User not authenticated");
        }

        return await GetByUserIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ScheduledReportDto>> UpdateAsync(Guid id, UpdateScheduledReportDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _repository.GetByIdAsync(id, cancellationToken);

            if (report == null)
            {
                return Result<ScheduledReportDto>.Error("Zamanlanmış rapor bulunamadı", $"Report not found: {id}");
            }

            // Yetki kontrolü
            if (!HasAccessToReport(report))
            {
                return Result<ScheduledReportDto>.Error("Bu rapora erişim yetkiniz yok", "Access denied");
            }

            // İsim güncelleme
            if (!string.IsNullOrEmpty(request.Name) && request.Name != report.Name)
            {
                if (!await _repository.IsNameUniqueAsync(report.UserId, request.Name, id, cancellationToken))
                {
                    return Result<ScheduledReportDto>.Error("Bu isimde bir zamanlanmış rapor zaten mevcut", "Duplicate name");
                }
                report.UpdateName(request.Name);
            }

            // Cron güncelleme
            if (!string.IsNullOrEmpty(request.CronExpression) && request.CronExpression != report.CronExpression)
            {
                if (!IsValidCronExpression(request.CronExpression))
                {
                    return Result<ScheduledReportDto>.Error("Geçersiz cron ifadesi", $"Invalid cron expression: {request.CronExpression}");
                }
                report.UpdateSchedule(request.CronExpression);
            }

            // Bildirim ayarları güncelleme
            if (request.NotificationEmail != null || request.TeamsWebhookUrl != null)
            {
                report.UpdateNotificationSettings(
                    request.NotificationEmail ?? report.NotificationEmail,
                    request.TeamsWebhookUrl ?? report.TeamsWebhookUrl
                );
            }

            // SQL güncelleme
            if (!string.IsNullOrEmpty(request.SqlQuery))
            {
                report.UpdateSqlQuery(request.SqlQuery);
            }

            await _repository.UpdateAsync(report, cancellationToken);

            _logger.LogInformation("Zamanlanmış rapor güncellendi - Id: {Id}", id);

            return Result<ScheduledReportDto>.Success(MapToDto(report), "Zamanlanmış rapor başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor güncellenirken hata - Id: {Id}", id);
            return Result<ScheduledReportDto>.Error(ex, nameof(UpdateAsync));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _repository.GetByIdAsync(id, cancellationToken);

            if (report == null)
            {
                return Result<string>.Error("Zamanlanmış rapor bulunamadı", $"Report not found: {id}");
            }

            // Yetki kontrolü
            if (!HasAccessToReport(report))
            {
                return Result<string>.Error("Bu rapora erişim yetkiniz yok", "Access denied");
            }

            await _repository.DeleteAsync(id, cancellationToken);

            _logger.LogInformation("Zamanlanmış rapor silindi - Id: {Id}, Name: {Name}", id, report.Name);

            return Result<string>.Success("Silindi", "Zamanlanmış rapor başarıyla silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor silinirken hata - Id: {Id}", id);
            return Result<string>.Error(ex, nameof(DeleteAsync));
        }
    }

    #endregion

    #region Status Operations

    /// <inheritdoc />
    public async Task<Result<ScheduledReportDto>> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _repository.GetByIdAsync(id, cancellationToken);

            if (report == null)
            {
                return Result<ScheduledReportDto>.Error("Zamanlanmış rapor bulunamadı", $"Report not found: {id}");
            }

            // Yetki kontrolü
            if (!HasAccessToReport(report))
            {
                return Result<ScheduledReportDto>.Error("Bu rapora erişim yetkiniz yok", "Access denied");
            }

            report.SetActive(isActive);
            await _repository.UpdateAsync(report, cancellationToken);

            var status = isActive ? "aktif" : "pasif";
            _logger.LogInformation("Zamanlanmış rapor {Status} yapıldı - Id: {Id}", status, id);

            return Result<ScheduledReportDto>.Success(MapToDto(report), $"Rapor {status} yapıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor durumu değiştirilirken hata - Id: {Id}", id);
            return Result<ScheduledReportDto>.Error(ex, nameof(SetActiveAsync));
        }
    }

    /// <inheritdoc />
    public Task<Result<ScheduledReportDto>> PauseAsync(Guid id, CancellationToken cancellationToken = default)
        => SetActiveAsync(id, false, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ScheduledReportDto>> ResumeAsync(Guid id, CancellationToken cancellationToken = default)
        => SetActiveAsync(id, true, cancellationToken);

    #endregion

    #region Execution Operations

    /// <inheritdoc />
    public async Task<Result<ScheduledReportLogDto>> RunNowAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _repository.GetByIdAsync(id, cancellationToken);

            if (report == null)
            {
                return Result<ScheduledReportLogDto>.Error("Zamanlanmış rapor bulunamadı", $"Report not found: {id}");
            }

            // Yetki kontrolü
            if (!HasAccessToReport(report))
            {
                return Result<ScheduledReportLogDto>.Error("Bu rapora erişim yetkiniz yok", "Access denied");
            }

            // TODO: Hangfire ile raporu hemen çalıştır
            // BackgroundJob.Enqueue<IScheduledReportExecutor>(x => x.ExecuteAsync(id, CancellationToken.None));

            _logger.LogInformation("Zamanlanmış rapor manuel tetiklendi - Id: {Id}", id);

            // Şimdilik placeholder log döndür
            // Aggregate Root üzerinden log oluştur (DDD pattern)
            var log = report.AddLog();
            await _repository.CreateLogAsync(log, cancellationToken);

            return Result<ScheduledReportLogDto>.Success(MapLogToDto(log), "Rapor çalıştırılmak üzere kuyruğa alındı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor manuel çalıştırılırken hata - Id: {Id}", id);
            return Result<ScheduledReportLogDto>.Error(ex, nameof(RunNowAsync));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<ScheduledReportLogDto>>> GetLogsAsync(Guid reportId, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _repository.GetByIdAsync(reportId, cancellationToken);

            if (report == null)
            {
                return Result<List<ScheduledReportLogDto>>.Error("Zamanlanmış rapor bulunamadı", $"Report not found: {reportId}");
            }

            // Yetki kontrolü
            if (!HasAccessToReport(report))
            {
                return Result<List<ScheduledReportLogDto>>.Error("Bu rapora erişim yetkiniz yok", "Access denied");
            }

            var logs = await _repository.GetLogsByReportIdAsync(reportId, skip, take, cancellationToken);
            var dtos = logs.Select(MapLogToDto).ToList();

            return Result<List<ScheduledReportLogDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış rapor logları getirilirken hata - ReportId: {ReportId}", reportId);
            return Result<List<ScheduledReportLogDto>>.Error(ex, nameof(GetLogsAsync));
        }
    }

    #endregion

    #region Private Helper Methods

    private bool HasAccessToReport(ScheduledReport report)
    {
        var currentUserId = _currentUserService.UserId;
        // TODO: Admin rolü kontrolü eklenebilir
        return report.UserId == currentUserId;
    }

    private static bool IsValidCronExpression(string cronExpression)
    {
        try
        {
            CronExpression.Parse(cronExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime? CalculateNextRun(string cronExpression)
    {
        try
        {
            var cron = CronExpression.Parse(cronExpression);
            return cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }
        catch
        {
            return null;
        }
    }

    private static string GetCronDescription(string cronExpression)
    {
        // Basit açıklamalar - daha detaylı için CronExpressionDescriptor paketi kullanılabilir
        return cronExpression switch
        {
            "0 * * * *" => "Her saat başı",
            "0 0 * * *" => "Her gün gece yarısı",
            "0 9 * * *" => "Her gün saat 09:00",
            "0 9 * * 1" => "Her pazartesi saat 09:00",
            "0 9 * * 1-5" => "Hafta içi her gün saat 09:00",
            "0 0 1 * *" => "Her ayın 1'inde gece yarısı",
            _ => cronExpression
        };
    }

    private static ScheduledReportDto MapToDto(ScheduledReport report)
    {
        return new ScheduledReportDto
        {
            Id = report.Id,
            UserId = report.UserId,
            Name = report.Name,
            OriginalPrompt = report.OriginalPrompt,
            SqlQuery = report.SqlQuery,
            OriginalMessageId = report.OriginalMessageId,
            OriginalConversationId = report.OriginalConversationId,
            CronExpression = report.CronExpression,
            CronDescription = GetCronDescription(report.CronExpression),
            ReportServiceType = report.ReportServiceType,
            IsActive = report.IsActive,
            LastRunAt = report.LastRunAt,
            NextRunAt = report.NextRunAt,
            RunCount = report.RunCount,
            LastRunSuccess = report.LastRunSuccess,
            LastErrorMessage = report.LastErrorMessage,
            NotificationEmail = report.NotificationEmail,
            TeamsWebhookUrl = report.TeamsWebhookUrl,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    private static ScheduledReportLogDto MapLogToDto(ScheduledReportLog log)
    {
        return new ScheduledReportLogDto
        {
            Id = log.Id,
            ScheduledReportId = log.ScheduledReportId,
            StartedAt = log.StartedAt,
            CompletedAt = log.CompletedAt,
            DurationMs = log.DurationMs,
            DurationFormatted = FormatDuration(log.DurationMs),
            IsSuccess = log.IsSuccess,
            ErrorMessage = log.ErrorMessage,
            OutputFilePath = log.OutputFilePath,
            OutputUrl = log.OutputUrl,
            RecordCount = log.RecordCount,
            EmailSent = log.EmailSent,
            TeamsSent = log.TeamsSent
        };
    }

    private static string? FormatDuration(long? durationMs)
    {
        if (!durationMs.HasValue)
            return null;

        var duration = TimeSpan.FromMilliseconds(durationMs.Value);

        if (duration.TotalSeconds < 1)
            return $"{durationMs} ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F1} saniye";
        if (duration.TotalHours < 1)
            return $"{duration.TotalMinutes:F1} dakika";

        return $"{duration.TotalHours:F1} saat";
    }

    #endregion
}