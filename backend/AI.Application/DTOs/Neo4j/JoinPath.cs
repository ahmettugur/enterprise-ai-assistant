namespace AI.Application.DTOs.Neo4j;

/// <summary>
/// İki tablo arasındaki JOIN yolu
/// </summary>
public class JoinPath
{
    /// <summary>
    /// Yol üzerindeki tablolar (sıralı)
    /// </summary>
    public List<string> Tables { get; set; } = new();
    
    /// <summary>
    /// JOIN bilgileri
    /// </summary>
    public List<JoinInfo> Joins { get; set; } = new();
    
    /// <summary>
    /// Toplam hop sayısı
    /// </summary>
    public int HopCount => Tables.Count > 0 ? Tables.Count - 1 : 0;
}

/// <summary>
/// Tek bir JOIN ilişkisi bilgisi
/// </summary>
public class JoinInfo
{
    /// <summary>
    /// Kaynak tablo (FROM)
    /// </summary>
    public string FromTable { get; set; } = string.Empty;
    
    /// <summary>
    /// Hedef tablo (TO)
    /// </summary>
    public string ToTable { get; set; } = string.Empty;
    
    /// <summary>
    /// JOIN kolonu (kaynak tablodaki FK)
    /// </summary>
    public string? Via { get; set; }
    
    /// <summary>
    /// Hedef tablodaki referans kolon
    /// </summary>
    public string? FkColumn { get; set; }
    
    /// <summary>
    /// SQL JOIN ifadesi oluşturur
    /// </summary>
    public string ToSqlJoin()
    {
        if (string.IsNullOrEmpty(Via) || string.IsNullOrEmpty(FkColumn))
            return string.Empty;
            
        return $"JOIN {ToTable} ON {FromTable}.{Via} = {ToTable}.{FkColumn}";
    }
}
