namespace AI.Domain.Common;

/// <summary>
/// Marker interface for entities that hold domain events.
/// Used by EF Core SaveChangesInterceptor to discover events before dispatch.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
