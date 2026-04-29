using AI.Domain.Common;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.Persistence;

/// <summary>
/// Domain event dispatcher implementation.
/// Domain event'leri loglar. Handler kayıt edildiğinde genişletilebilir.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(ILogger<DomainEventDispatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            _logger.LogDebug("Domain event dispatched: {EventType} at {OccurredOn}",
                eventType.Name, domainEvent.OccurredOn);
        }

        await Task.CompletedTask;
    }
}
