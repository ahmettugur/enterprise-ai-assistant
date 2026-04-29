using System.Security.Cryptography;
using AI.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Extensions;

/// <summary>
/// Veritabanı seed işlemleri için extension metodları
/// </summary>
public static class DatabaseSeederExtensions
{
    // Sabit ID'ler - Migration'larda tutarlılık için
    public static readonly string AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001").ToString();
    public static readonly string AdminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111").ToString();
    public static readonly string UserRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString();

    /// <summary>
    /// Varsayılan admin kullanıcısını ve rollerini seed eder
    /// </summary>
    public static async Task SeedDefaultUsersAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ChatDbContext>>();

        try
        {
            // Admin kullanıcısı var mı kontrol et
            var adminExists = await context.Users.AnyAsync(u => u.Id == AdminUserId);
            if (adminExists)
            {
                logger.LogInformation("Admin user already exists, skipping seed");
                return;
            }

            // Admin kullanıcısı oluştur
            const string defaultPassword = "Admin123!";
            var (passwordHash, passwordSalt) = HashPassword(defaultPassword);

            // Raw SQL ile insert (private setter'lar nedeniyle)
            await context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO identity.users (
                    id, username, email, display_name, 
                    password_hash, password_salt, authentication_source,
                    is_active, created_at, failed_login_attempts
                ) VALUES (
                    {AdminUserId}, 
                    'admin@system.local', 
                    'admin@system.local', 
                    'System Administrator',
                    {passwordHash},
                    {passwordSalt},
                    'Local',
                    true,
                    {DateTime.UtcNow},
                    0
                )
                ON CONFLICT (id) DO NOTHING");

            // Admin rolünü ata
            await context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO identity.user_roles (user_id, role_id, assigned_at)
                VALUES ({AdminUserId}, {AdminRoleId}, {DateTime.UtcNow})
                ON CONFLICT (user_id, role_id) DO NOTHING");

            logger.LogInformation("Default admin user seeded successfully. Email: admin@system.local, Password: Admin123!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding default users");
            throw;
        }
    }

    private static (string hash, string salt) HashPassword(string password)
    {
        var saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        
        var salt = Convert.ToBase64String(saltBytes);
        var hash = Convert.ToBase64String(
            Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100000, HashAlgorithmName.SHA256, 32)
        );
        
        return (hash, salt);
    }
}
