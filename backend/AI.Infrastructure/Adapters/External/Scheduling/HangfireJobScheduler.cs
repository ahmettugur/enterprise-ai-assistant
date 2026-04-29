using System.Linq.Expressions;
using AI.Application.Ports.Secondary.Scheduling;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.External.Scheduling;

/// <summary>
/// Hangfire adapter for IJobScheduler interface
/// </summary>
public sealed class HangfireJobScheduler : IJobScheduler
{
    private readonly IRecurringJobManager _recurringJobs;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<HangfireJobScheduler> _logger;

    public HangfireJobScheduler(
        IRecurringJobManager recurringJobs,
        IBackgroundJobClient backgroundJobs,
        ILogger<HangfireJobScheduler> logger)
    {
        _recurringJobs = recurringJobs ?? throw new ArgumentNullException(nameof(recurringJobs));
        _backgroundJobs = backgroundJobs ?? throw new ArgumentNullException(nameof(backgroundJobs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ScheduleRecurringJob<T>(
        string jobId,
        Expression<Func<T, Task>> methodCall,
        string cronExpression)
    {
        try
        {
            _logger.LogInformation("Scheduling recurring job: {JobId} with cron: {Cron}", jobId, cronExpression);
            _recurringJobs.AddOrUpdate<T>(jobId, methodCall, cronExpression);
            _logger.LogInformation("Successfully scheduled recurring job: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling recurring job: {JobId}", jobId);
            throw;
        }
    }

    public void EnqueueJob<T>(Expression<Func<T, Task>> methodCall)
    {
        var jobType = typeof(T);
        try
        {
            _logger.LogInformation("Enqueueing job: {JobType}", jobType.Name);
            _backgroundJobs.Enqueue(methodCall);
            _logger.LogInformation("Successfully enqueued job: {JobType}", jobType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing job: {JobType}", jobType.Name);
            throw;
        }
    }

    public void RemoveJob(string jobId)
    {
        try
        {
            _logger.LogInformation("Removing job: {JobId}", jobId);
            _recurringJobs.RemoveIfExists(jobId);
            _logger.LogInformation("Successfully removed job: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing job: {JobId}", jobId);
            throw;
        }
    }

    public bool JobExists(string jobId)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var job = connection.GetRecurringJobs()
                .FirstOrDefault(j => j.Id == jobId);
            
            var exists = job != null;
            _logger.LogDebug("Job {JobId} exists: {Exists}", jobId, exists);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if job exists: {JobId}", jobId);
            throw;
        }
    }
}
