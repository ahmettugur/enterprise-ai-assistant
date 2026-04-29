using System.Linq.Expressions;

namespace AI.Application.Ports.Secondary.Scheduling;

/// <summary>
/// Job scheduling abstraction - isolates scheduler from Hangfire implementation
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Schedules a recurring job
    /// </summary>
    void ScheduleRecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);

    /// <summary>
    /// Enqueues a one-time job
    /// </summary>
    void EnqueueJob<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Removes a job
    /// </summary>
    void RemoveJob(string jobId);

    /// <summary>
    /// Checks if a job exists
    /// </summary>
    bool JobExists(string jobId);
}
