namespace AI.Domain.Exceptions;

/// <summary>
/// Domain katmanı için temel exception sınıfı.
/// Tüm domain-specific exception'lar bu sınıftan türetilmelidir.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Hata kodu (API response ve logging için)
    /// </summary>
    public string Code { get; }

    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    protected DomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
