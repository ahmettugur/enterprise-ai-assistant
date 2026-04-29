using AI.Domain.Common;

namespace AI.Domain.ValueObjects;

/// <summary>
/// Email Value Object — immutable, küçük harfe normalize, format validasyonu
/// </summary>
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Yeni Email value object oluşturur
    /// </summary>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new Exceptions.InvalidEntityStateException("Email", "Value", "Email adresi boş olamaz.");

        var normalized = email.Trim().ToLowerInvariant();

        if (!normalized.Contains('@') || !normalized.Contains('.'))
            throw new Exceptions.InvalidEntityStateException("Email", "Value", $"Geçersiz email formatı: '{email}'");

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    /// <summary>
    /// Implicit string conversion (backward compatibility)
    /// </summary>
    public static implicit operator string(Email email) => email.Value;
}
