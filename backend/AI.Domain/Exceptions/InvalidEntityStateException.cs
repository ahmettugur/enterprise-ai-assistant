namespace AI.Domain.Exceptions;

/// <summary>
/// Entity geçersiz durumda olduğunda veya zorunlu alanlar boş olduğunda fırlatılır.
/// ArgumentException ve InvalidOperationException yerine kullanılır.
/// </summary>
public sealed class InvalidEntityStateException : DomainException
{
    public string EntityName { get; }
    public string PropertyName { get; }

    public InvalidEntityStateException(string entityName, string propertyName, string reason)
        : base("INVALID_ENTITY_STATE",
               $"'{entityName}.{propertyName}' geçersiz: {reason}")
    {
        EntityName = entityName;
        PropertyName = propertyName;
    }

    /// <summary>
    /// Zorunlu alanın boş olduğu durum için factory method
    /// </summary>
    public static InvalidEntityStateException RequiredFieldEmpty(string entityName, string propertyName)
        => new(entityName, propertyName, "Bu alan boş olamaz.");

    /// <summary>
    /// Geçersiz ID durumu için factory method
    /// </summary>
    public static InvalidEntityStateException InvalidId(string entityName)
        => new(entityName, "Id", "Geçersiz kimlik değeri.");
}
