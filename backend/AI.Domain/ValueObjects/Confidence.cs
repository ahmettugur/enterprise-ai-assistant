using AI.Domain.Common;

namespace AI.Domain.ValueObjects;

/// <summary>
/// Confidence Value Object — 0.0 ile 1.0 arasında güvenilirlik skoru
/// Bound kontrolü ve domain lojiği kapsüller
/// </summary>
public sealed class Confidence : ValueObject
{
    public float Value { get; }

    private Confidence(float value) => Value = value;

    /// <summary>
    /// Yeni Confidence value object oluşturur (0.0-1.0 arasına sınırlar)
    /// </summary>
    public static Confidence Create(float value)
        => new(Math.Clamp(value, 0f, 1f));

    /// <summary>
    /// Varsayılan tam güven (1.0)
    /// </summary>
    public static Confidence Full => new(1.0f);

    /// <summary>
    /// Güven skorunu artırır
    /// </summary>
    public Confidence Boost(float amount = 0.1f)
        => new(Math.Clamp(Value + amount, 0f, 1f));

    /// <summary>
    /// Yüksek güven mi? (>= 0.7)
    /// </summary>
    public bool IsHigh => Value >= 0.7f;

    /// <summary>
    /// Düşük güven mi? (< 0.3)
    /// </summary>
    public bool IsLow => Value < 0.3f;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value:P0}";

    public static implicit operator float(Confidence confidence) => confidence.Value;
}
