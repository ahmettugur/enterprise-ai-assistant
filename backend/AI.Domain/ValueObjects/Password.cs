using System.Security.Cryptography;
using AI.Domain.Common;

namespace AI.Domain.ValueObjects;

/// <summary>
/// Password Value Object — hash + salt kapsülleme, immutable
/// Şifre oluşturma ve doğrulama lojiğini domain'de tutar
/// </summary>
public sealed class Password : ValueObject
{
    public string Hash { get; }
    public string Salt { get; }

    private Password(string hash, string salt)
    {
        Hash = hash;
        Salt = salt;
    }

    /// <summary>
    /// Plain text şifreden yeni Password value object oluşturur
    /// </summary>
    public static Password Create(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new Exceptions.InvalidEntityStateException("Password", "Value", "Şifre boş olamaz.");

        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var saltString = Convert.ToBase64String(salt);
        var hash = HashPassword(plainTextPassword, salt);

        return new Password(hash, saltString);
    }

    /// <summary>
    /// Mevcut hash ve salt değerlerinden Password value object oluşturur (DB'den yükleme)
    /// </summary>
    public static Password FromExisting(string hash, string salt)
    {
        return new Password(hash, salt);
    }

    /// <summary>
    /// Şifre doğrulaması
    /// </summary>
    public bool Verify(string plainTextPassword)
    {
        if (string.IsNullOrEmpty(Hash) || string.IsNullOrEmpty(Salt))
            return false;

        var salt = Convert.FromBase64String(Salt);
        var hash = HashPassword(plainTextPassword, salt);
        return hash == Hash;
    }

    private static string HashPassword(string password, byte[] salt)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
        yield return Salt;
    }
}
