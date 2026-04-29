namespace AI.Domain.Common;

/// <summary>
/// Domain event marker interface
/// Aggregate root'lar tarafından üretilen domain olaylarını temsil eder
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Olayın gerçekleştiği zaman
    /// </summary>
    DateTime OccurredOn { get; }
}
