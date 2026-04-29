namespace AI.Domain.Common;

/// <summary>
/// Aggregate Root base class'ı
/// Domain event koleksiyonu yönetimi sağlar
/// Aggregate sınırlarını tanımlar — child entity'ler sadece bu root üzerinden oluşturulmalıdır
/// </summary>
/// <typeparam name="TId">Aggregate kimlik tipi</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Henüz dispatch edilmemiş domain event'leri
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Yeni domain event ekler
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Tüm domain event'leri temizler (dispatch sonrası çağrılır)
    /// </summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
