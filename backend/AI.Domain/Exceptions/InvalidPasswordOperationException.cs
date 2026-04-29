namespace AI.Domain.Exceptions;

/// <summary>
/// Active Directory kullanıcısının şifresini değiştirme gibi geçersiz şifre işlemlerinde fırlatılır.
/// </summary>
public sealed class InvalidPasswordOperationException : DomainException
{
    public InvalidPasswordOperationException(string userId)
        : base("INVALID_PASSWORD_OPERATION",
               $"AD kullanıcısı '{userId}' için şifre işlemi yapılamaz. Şifre yönetimi Active Directory üzerinden yapılmalıdır.")
    {
    }
}
