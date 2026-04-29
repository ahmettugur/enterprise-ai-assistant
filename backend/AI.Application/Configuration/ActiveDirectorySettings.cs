namespace AI.Application.Configuration;

/// <summary>
/// Active Directory yapılandırma ayarları
/// </summary>
public sealed class ActiveDirectorySettings
{
    public const string SectionName = "ActiveDirectory";
    
    /// <summary>
    /// AD entegrasyonu aktif mi
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// AD Domain adı (örn: MYCOMPANY)
    /// </summary>
    public string Domain { get; set; } = null!;
    
    /// <summary>
    /// LDAP sunucu adresi (örn: ldap://dc.mycompany.com)
    /// </summary>
    public string? LdapServer { get; set; }
    
    /// <summary>
    /// LDAP arama base DN (örn: DC=mycompany,DC=com)
    /// </summary>
    public string? BaseDn { get; set; }
    
    /// <summary>
    /// Windows Authentication scheme'i için varsayılan rol
    /// </summary>
    public string DefaultRole { get; set; } = "User";
    
    /// <summary>
    /// AD grup - Uygulama rol eşleştirmeleri
    /// Key: AD Group Name, Value: Application Role Name
    /// </summary>
    public Dictionary<string, string> GroupRoleMappings { get; set; } = new();
    
    /// <summary>
    /// Otomatik kullanıcı oluşturma aktif mi
    /// AD'de doğrulanan kullanıcı veritabanında yoksa otomatik oluşturulsun mu
    /// </summary>
    public bool AutoCreateUsers { get; set; } = true;
}
