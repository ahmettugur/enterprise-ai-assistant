
using System.Diagnostics;
using AI.Application.Ports.Secondary.Services.Database;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.AI.Agents.SqlAgents;

/// <summary>
/// SQL Agent'ları koordine eden pipeline implementasyonu.
/// Validation ve Optimization agent'larını sırayla çalıştırır.
/// </summary>
public class SqlAgentPipeline : ISqlAgentPipeline
{
    private readonly ISqlValidationAgent _validationAgent;
    private readonly ISqlOptimizationAgent _optimizationAgent;
    private readonly ILogger<SqlAgentPipeline> _logger;

    public SqlAgentPipeline(
        ISqlValidationAgent validationAgent,
        ISqlOptimizationAgent optimizationAgent,
        ILogger<SqlAgentPipeline> logger)
    {
        _validationAgent = validationAgent ?? throw new ArgumentNullException(nameof(validationAgent));
        _optimizationAgent = optimizationAgent ?? throw new ArgumentNullException(nameof(optimizationAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SqlPipelineResult> ProcessAsync(
        string sql,
        string databaseType,
        SqlPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= SqlPipelineOptions.Default;
        var stages = new List<SqlPipelineStage>();
        var stopwatch = Stopwatch.StartNew();
        var currentSql = sql;

        _logger.LogInformation(
            "SQL Pipeline başlatıldı - DatabaseType: {DatabaseType}, Validation: {Validation}, Optimization: {Optimization}",
            databaseType, options.EnableValidation, options.EnableOptimization);

        SqlValidationResult? validationResult = null;
        SqlOptimizationResult? optimizationResult = null;

        try
        {
            // Stage 1: Validation
            if (options.EnableValidation)
            {
                var stageStopwatch = Stopwatch.StartNew();
                
                validationResult = await _validationAgent.ValidateAsync(
                    currentSql, 
                    databaseType, 
                    options.SchemaInfo,
                    cancellationToken).ConfigureAwait(false);

                stageStopwatch.Stop();

                var validationStage = new SqlPipelineStage
                {
                    Name = "Validation",
                    IsSuccess = validationResult.IsValid,
                    DurationMs = stageStopwatch.ElapsedMilliseconds,
                    InputSql = currentSql,
                    OutputSql = validationResult.CorrectedSql ?? currentSql,
                    Message = validationResult.Explanation
                };
                stages.Add(validationStage);

                // Validation başarısız ve kritik hatalar varsa
                if (!validationResult.IsValid)
                {
                    var hasAutoCorrection = options.EnableAutoCorrection && 
                                           !string.IsNullOrEmpty(validationResult.CorrectedSql);

                    if (hasAutoCorrection)
                    {
                        _logger.LogInformation("SQL validation hatası - otomatik düzeltme uygulanıyor");
                        currentSql = validationResult.CorrectedSql!;
                        
                        // Düzeltilmiş SQL'i tekrar validate et (retry)
                        if (options.MaxRetries > 0)
                        {
                            var retryResult = await RetryValidationAsync(
                                currentSql, 
                                databaseType, 
                                options, 
                                stages,
                                cancellationToken).ConfigureAwait(false);

                            if (!retryResult.IsSuccess)
                            {
                                stopwatch.Stop();
                                return SqlPipelineResult.Failure(
                                    sql,
                                    retryResult.ErrorMessage ?? "Validation retry başarısız",
                                    validationResult,
                                    stages,
                                    stopwatch.ElapsedMilliseconds);
                            }

                            validationResult = retryResult.ValidationResult;
                            currentSql = retryResult.FinalSql;
                        }
                    }
                    else
                    {
                        // Otomatik düzeltme yok, hata döndür
                        stopwatch.Stop();
                        var errorMessage = validationResult.Errors.Count > 0
                            ? string.Join("; ", validationResult.Errors.Select(e => e.Message))
                            : validationResult.Explanation ?? "SQL validation başarısız";

                        _logger.LogWarning("SQL Pipeline validation hatası: {Error}", errorMessage);

                        return SqlPipelineResult.Failure(
                            sql,
                            errorMessage,
                            validationResult,
                            stages,
                            stopwatch.ElapsedMilliseconds);
                    }
                }
                else if (!string.IsNullOrEmpty(validationResult.CorrectedSql))
                {
                    // Valid ama düzeltme var
                    currentSql = validationResult.CorrectedSql;
                    _logger.LogDebug("SQL küçük düzeltmeler uygulandı");
                }

                _logger.LogDebug("Validation tamamlandı - {DurationMs}ms", stageStopwatch.ElapsedMilliseconds);
            }

            // Stage 2: Optimization
            if (options.EnableOptimization)
            {
                var stageStopwatch = Stopwatch.StartNew();
                var sqlBeforeOptimization = currentSql;

                optimizationResult = await _optimizationAgent.OptimizeAsync(
                    currentSql,
                    databaseType,
                    options.SchemaInfo,
                    cancellationToken).ConfigureAwait(false);

                stageStopwatch.Stop();

                var optimizationStage = new SqlPipelineStage
                {
                    Name = "Optimization",
                    IsSuccess = true, // Optimization her zaman "başarılı" - sadece iyileştirme yapar veya yapmaz
                    DurationMs = stageStopwatch.ElapsedMilliseconds,
                    InputSql = currentSql,
                    OutputSql = optimizationResult.OptimizedSql,
                    Message = optimizationResult.Explanation
                };
                stages.Add(optimizationStage);

                if (optimizationResult.IsOptimized)
                {
                    currentSql = optimizationResult.OptimizedSql;
                    _logger.LogInformation(
                        "SQL optimize edildi - {OptimizationCount} iyileştirme, tahmini %{Improvement} performans artışı",
                        optimizationResult.Optimizations.Count,
                        optimizationResult.EstimatedImprovementPercent ?? 0);
                    
                    // Stage 3: Re-Validation (Optimization sonrası güvenlik kontrolü)
                    if (options.EnableValidation)
                    {
                        var revalidationResult = await RevalidateOptimizedSqlAsync(
                            sqlBeforeOptimization,
                            currentSql,
                            databaseType,
                            options,
                            stages,
                            cancellationToken).ConfigureAwait(false);

                        if (!revalidationResult.IsValid)
                        {
                            // Optimization SQL'i bozdu - orijinal SQL'e geri dön
                            _logger.LogWarning(
                                "Re-validation başarısız - Optimization geri alınıyor. Reason: {Reason}",
                                revalidationResult.Reason);
                            
                            currentSql = sqlBeforeOptimization;
                            
                            // Optimization stage'i güncelle - yeni instance oluştur
                            var updatedOptimizationStage = optimizationStage with 
                            { 
                                Message = $"Optimization geri alındı: {revalidationResult.Reason}",
                                OutputSql = sqlBeforeOptimization
                            };
                            stages[stages.IndexOf(optimizationStage)] = updatedOptimizationStage;
                        }
                    }
                }

                _logger.LogDebug("Optimization tamamlandı - {DurationMs}ms", stageStopwatch.ElapsedMilliseconds);
            }

            stopwatch.Stop();

            // Özet oluştur
            var summary = BuildSummary(validationResult, optimizationResult, stages);

            _logger.LogInformation(
                "SQL Pipeline tamamlandı - Toplam: {TotalMs}ms, Stages: {StageCount}",
                stopwatch.ElapsedMilliseconds, stages.Count);

            return SqlPipelineResult.Success(
                sql,
                currentSql,
                validationResult,
                optimizationResult,
                stages,
                stopwatch.ElapsedMilliseconds,
                summary);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "SQL Pipeline hatası");

            return SqlPipelineResult.Failure(
                sql,
                $"Pipeline hatası: {ex.Message}",
                validationResult,
                stages,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<RetryResult> RetryValidationAsync(
        string sql,
        string databaseType,
        SqlPipelineOptions options,
        List<SqlPipelineStage> stages,
        CancellationToken cancellationToken)
    {
        var currentSql = sql;
        SqlValidationResult? lastResult = null;

        for (int retry = 1; retry <= options.MaxRetries; retry++)
        {
            _logger.LogDebug("Validation retry {Retry}/{MaxRetries}", retry, options.MaxRetries);

            var stageStopwatch = Stopwatch.StartNew();

            lastResult = await _validationAgent.ValidateAsync(
                currentSql,
                databaseType,
                options.SchemaInfo,
                cancellationToken).ConfigureAwait(false);

            stageStopwatch.Stop();

            stages.Add(new SqlPipelineStage
            {
                Name = $"Validation Retry {retry}",
                IsSuccess = lastResult.IsValid,
                DurationMs = stageStopwatch.ElapsedMilliseconds,
                InputSql = currentSql,
                OutputSql = lastResult.CorrectedSql ?? currentSql,
                Message = lastResult.Explanation
            });

            if (lastResult.IsValid)
            {
                return new RetryResult
                {
                    IsSuccess = true,
                    FinalSql = lastResult.CorrectedSql ?? currentSql,
                    ValidationResult = lastResult
                };
            }

            if (!string.IsNullOrEmpty(lastResult.CorrectedSql))
            {
                currentSql = lastResult.CorrectedSql;
            }
            else
            {
                // Düzeltme önerisi yok, retry'ı durdur
                break;
            }
        }

        return new RetryResult
        {
            IsSuccess = false,
            FinalSql = currentSql,
            ValidationResult = lastResult,
            ErrorMessage = lastResult?.Errors.FirstOrDefault()?.Message ?? "Validation başarısız"
        };
    }

    private string BuildSummary(
        SqlValidationResult? validationResult,
        SqlOptimizationResult? optimizationResult,
        List<SqlPipelineStage> stages)
    {
        var parts = new List<string>();

        if (validationResult != null)
        {
            if (validationResult.IsValid)
            {
                parts.Add(validationResult.CorrectedSql != null 
                    ? "✓ Validation: Düzeltildi" 
                    : "✓ Validation: Geçerli");
            }
            else
            {
                parts.Add($"✗ Validation: {validationResult.Errors.Count} hata");
            }

            if (validationResult.Warnings.Count > 0)
            {
                parts.Add($"⚠ {validationResult.Warnings.Count} uyarı");
            }

            if (validationResult.SecurityIssues.Count > 0)
            {
                parts.Add($"🔒 {validationResult.SecurityIssues.Count} güvenlik sorunu");
            }
        }

        if (optimizationResult != null)
        {
            if (optimizationResult.IsOptimized)
            {
                parts.Add($"⚡ Optimization: {optimizationResult.Optimizations.Count} iyileştirme");
                if (optimizationResult.EstimatedImprovementPercent.HasValue)
                {
                    parts.Add($"~%{optimizationResult.EstimatedImprovementPercent} performans");
                }
            }
            else
            {
                parts.Add("✓ Optimization: Zaten optimize");
            }
        }

        return string.Join(" | ", parts);
    }

    private class RetryResult
    {
        public bool IsSuccess { get; init; }
        public string FinalSql { get; init; } = string.Empty;
        public SqlValidationResult? ValidationResult { get; init; }
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Optimize edilmiş SQL'i re-validate eder.
    /// Optimization'ın SQL'i bozup bozmadığını kontrol eder.
    /// </summary>
    private async Task<RevalidationResult> RevalidateOptimizedSqlAsync(
        string originalSql,
        string optimizedSql,
        string databaseType,
        SqlPipelineOptions options,
        List<SqlPipelineStage> stages,
        CancellationToken cancellationToken)
    {
        var stageStopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Hızlı yapısal karşılaştırma (LLM çağrısı öncesi)
            var structuralCheck = CompareStructure(originalSql, optimizedSql);
            if (!structuralCheck.IsValid)
            {
                stageStopwatch.Stop();
                stages.Add(new SqlPipelineStage
                {
                    Name = "Re-Validation",
                    IsSuccess = false,
                    DurationMs = stageStopwatch.ElapsedMilliseconds,
                    InputSql = optimizedSql,
                    OutputSql = originalSql,
                    Message = $"Yapısal değişiklik tespit edildi: {structuralCheck.Reason}"
                });

                return new RevalidationResult
                {
                    IsValid = false,
                    Reason = structuralCheck.Reason
                };
            }

            // 2. LLM ile syntax validation
            var validationResult = await _validationAgent.ValidateAsync(
                optimizedSql,
                databaseType,
                options.SchemaInfo,
                cancellationToken).ConfigureAwait(false);

            stageStopwatch.Stop();

            stages.Add(new SqlPipelineStage
            {
                Name = "Re-Validation",
                IsSuccess = validationResult.IsValid,
                DurationMs = stageStopwatch.ElapsedMilliseconds,
                InputSql = optimizedSql,
                OutputSql = validationResult.IsValid ? optimizedSql : originalSql,
                Message = validationResult.IsValid 
                    ? "Optimize edilmiş SQL geçerli" 
                    : $"Re-validation başarısız: {validationResult.Explanation}"
            });

            if (!validationResult.IsValid)
            {
                return new RevalidationResult
                {
                    IsValid = false,
                    Reason = validationResult.Explanation ?? "Re-validation başarısız"
                };
            }

            return new RevalidationResult { IsValid = true };
        }
        catch (Exception ex)
        {
            stageStopwatch.Stop();
            _logger.LogWarning(ex, "Re-validation sırasında hata - güvenli tarafa geçiliyor");

            stages.Add(new SqlPipelineStage
            {
                Name = "Re-Validation",
                IsSuccess = false,
                DurationMs = stageStopwatch.ElapsedMilliseconds,
                InputSql = optimizedSql,
                OutputSql = originalSql,
                Message = $"Re-validation hatası: {ex.Message}"
            });

            return new RevalidationResult
            {
                IsValid = false,
                Reason = $"Re-validation hatası: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Orijinal ve optimize edilmiş SQL'in yapısal bütünlüğünü kontrol eder.
    /// Kritik değişiklikleri hızlıca tespit eder (LLM çağrısı öncesi).
    /// </summary>
    private StructuralComparisonResult CompareStructure(string originalSql, string optimizedSql)
    {
        var originalNormalized = NormalizeSql(originalSql);
        var optimizedNormalized = NormalizeSql(optimizedSql);

        // 1. Tablo sayısı değişti mi?
        var originalTables = CountPattern(originalNormalized, @"\bFROM\s+[\w.]+|\bJOIN\s+[\w.]+");
        var optimizedTables = CountPattern(optimizedNormalized, @"\bFROM\s+[\w.]+|\bJOIN\s+[\w.]+");
        if (originalTables != optimizedTables)
        {
            return new StructuralComparisonResult
            {
                IsValid = false,
                Reason = $"Tablo sayısı değişti: {originalTables} → {optimizedTables}"
            };
        }

        // 2. WHERE koşulu kaldırıldı mı?
        var originalHasWhere = originalNormalized.Contains("WHERE", StringComparison.OrdinalIgnoreCase);
        var optimizedHasWhere = optimizedNormalized.Contains("WHERE", StringComparison.OrdinalIgnoreCase);
        if (originalHasWhere && !optimizedHasWhere)
        {
            return new StructuralComparisonResult
            {
                IsValid = false,
                Reason = "WHERE clause kaldırıldı"
            };
        }

        // 3. GROUP BY kaldırıldı mı?
        var originalHasGroupBy = originalNormalized.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase);
        var optimizedHasGroupBy = optimizedNormalized.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase);
        if (originalHasGroupBy && !optimizedHasGroupBy)
        {
            return new StructuralComparisonResult
            {
                IsValid = false,
                Reason = "GROUP BY clause kaldırıldı"
            };
        }

        // 4. DISTINCT kaldırıldı mı?
        var originalHasDistinct = System.Text.RegularExpressions.Regex.IsMatch(
            originalNormalized, @"\bDISTINCT\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var optimizedHasDistinct = System.Text.RegularExpressions.Regex.IsMatch(
            optimizedNormalized, @"\bDISTINCT\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (originalHasDistinct && !optimizedHasDistinct)
        {
            return new StructuralComparisonResult
            {
                IsValid = false,
                Reason = "DISTINCT kaldırıldı"
            };
        }

        // 5. Aggregate fonksiyon değişti mi?
        var originalAggregates = CountPattern(originalNormalized, @"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(");
        var optimizedAggregates = CountPattern(optimizedNormalized, @"\b(COUNT|SUM|AVG|MIN|MAX)\s*\(");
        if (originalAggregates != optimizedAggregates)
        {
            return new StructuralComparisonResult
            {
                IsValid = false,
                Reason = $"Aggregate fonksiyon sayısı değişti: {originalAggregates} → {optimizedAggregates}"
            };
        }

        // 6. LOWER/UPPER/TRANSLATE kaldırıldı mı?
        var originalStringFuncs = CountPattern(originalNormalized, @"\b(LOWER|UPPER|TRANSLATE)\s*\(");
        var optimizedStringFuncs = CountPattern(optimizedNormalized, @"\b(LOWER|UPPER|TRANSLATE)\s*\(");
        if (originalStringFuncs > optimizedStringFuncs)
        {
            return new StructuralComparisonResult
            {
                IsValid = false,
                Reason = "String fonksiyonları (LOWER/UPPER/TRANSLATE) kaldırıldı"
            };
        }

        return new StructuralComparisonResult { IsValid = true };
    }

    private static string NormalizeSql(string sql)
    {
        // Boşlukları normalize et
        return System.Text.RegularExpressions.Regex.Replace(sql, @"\s+", " ").Trim().ToUpperInvariant();
    }

    private static int CountPattern(string text, string pattern)
    {
        return System.Text.RegularExpressions.Regex.Matches(text, pattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
    }

    private class RevalidationResult
    {
        public bool IsValid { get; init; }
        public string? Reason { get; init; }
    }

    private class StructuralComparisonResult
    {
        public bool IsValid { get; init; }
        public string? Reason { get; init; }
    }
}
