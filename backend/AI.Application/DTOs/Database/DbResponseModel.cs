using System.Dynamic;

namespace AI.Application.DTOs.Database;

/// <summary>
/// Veritabanı sorgu sonucu modeli
/// </summary>
public class DbResponseModel
{
    public List<ExpandoObject>? Data { get; set; }
    public int Count { get; set; }
}
