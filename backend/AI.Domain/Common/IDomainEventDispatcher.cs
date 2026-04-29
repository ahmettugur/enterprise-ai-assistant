namespace AI.Domain.Common;

/// <summary>
/// Domain event dispatcher interface.
/// SaveChanges sırasında aggregate root'lardan toplanan event'leri dispatch eder.
/// Implementation Infrastructure layer'da yaşar.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
