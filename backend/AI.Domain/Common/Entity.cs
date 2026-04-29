namespace AI.Domain.Common;

/// <summary>
/// Tüm entity'lerin base class'ı
/// Kimlik (Id) bazlı eşitlik sağlar
/// </summary>
/// <typeparam name="TId">Entity kimlik tipi (Guid, string vb.)</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// EF Core parameteresiz constructor
    /// </summary>
    protected Entity() { }

    /// <summary>
    /// Id ile entity oluşturma
    /// </summary>
    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
        => obj is Entity<TId> entity && Id.Equals(entity.Id);

    public bool Equals(Entity<TId>? other)
        => other is not null && Id.Equals(other.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !Equals(left, right);
}
