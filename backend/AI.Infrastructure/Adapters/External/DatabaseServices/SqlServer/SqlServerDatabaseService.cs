
using System.Data;
using System.Dynamic;
using AI.Application.Ports.Secondary.Services.Database;
using AI.Application.DTOs.Database;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Adapters.External.DatabaseServices.SqlServer;

/// <summary>
/// SQL Server veritabanı işlemleri için servis sınıfı
/// Best practices ve SOLID prensiplere uygun olarak optimize edilmiştir
/// </summary>
public class SqlServerDatabaseService : IDatabaseService
{
    #region Fields

    private readonly ISqlServerConnectionFactory _sqlConnectionFactory;
    private readonly ILogger<SqlServerDatabaseService> _logger;

    #endregion

    #region Constructor

    public SqlServerDatabaseService(
        ISqlServerConnectionFactory sqlConnectionFactory, 
        ILogger<SqlServerDatabaseService> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory ?? throw new ArgumentNullException(nameof(sqlConnectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// SQL komutunu asenkron olarak çalıştırır
    /// </summary>
    /// <param name="query">Çalıştırılacak SQL komutu</param>
    /// <param name="userConnectionId">Kullanıcı bağlantı kimliği</param>
    public async Task ExecuteCommandAsync(string query, string userConnectionId)
    {
        ValidateParameters(query, userConnectionId);

        try
        {
            _logger.LogInformation("SQL komutu çalıştırılmaya başlandı - ConnectionId: {ConnectionId}, Query: {Query}", 
                userConnectionId, query);

            using var connection = await _sqlConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            using var command = CreateCommand(connection, query);
            
            command.ExecuteNonQuery();

            _logger.LogInformation("SQL komutu başarıyla tamamlandı - ConnectionId: {ConnectionId}", userConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL komutu çalıştırılırken hata oluştu - ConnectionId: {ConnectionId}, Query: {Query}", 
                userConnectionId, query);
            throw new InvalidOperationException($"SQL komutu çalıştırılırken hata oluştu: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// SQL sorgusunu çalıştırır ve sonuçları ExpandoObject listesi olarak döner
    /// </summary>
    /// <param name="query">Çalıştırılacak SQL sorgusu</param>
    /// <param name="userConnectionId">Kullanıcı bağlantı kimliği</param>
    /// <returns>Veritabanı yanıt modeli</returns>
    public async Task<DbResponseModel> GetDataTableWithExpandoObjectAsync(string query, string userConnectionId)
    {
        ValidateParameters(query, userConnectionId);

        try
        {
            _logger.LogInformation("SQL sorgusu çalıştırılmaya başlandı - ConnectionId: {ConnectionId}, Query: {Query}", 
                userConnectionId, query);

            var rows = new List<ExpandoObject>();

            using var connection = await _sqlConnectionFactory.CreateConnectionAsync().ConfigureAwait(false);
            using var command = CreateCommand(connection, query);
            using var reader = command.ExecuteReader();

            rows = ProcessDataReader(reader);

            var dbResponseModel = new DbResponseModel
            {
                Data = rows,
                Count = rows.Count
            };

            _logger.LogInformation("SQL sorgusu başarıyla tamamlandı - ConnectionId: {ConnectionId}, RowCount: {RowCount}", 
                userConnectionId, rows.Count);

            return dbResponseModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL sorgusu çalıştırılırken hata oluştu - ConnectionId: {ConnectionId}, Query: {Query}", 
                userConnectionId, query);
            
            // Hata durumunda boş sonuç döndür
            return new DbResponseModel
            {
                Data = new List<ExpandoObject>(),
                Count = 0
            };
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Giriş parametrelerini validate eder
    /// </summary>
    /// <param name="query">SQL sorgusu</param>
    /// <param name="userConnectionId">Kullanıcı bağlantı kimliği</param>
    private static void ValidateParameters(string query, string userConnectionId)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("SQL sorgusu boş olamaz", nameof(query));

        if (string.IsNullOrWhiteSpace(userConnectionId))
            throw new ArgumentException("Kullanıcı bağlantı kimliği boş olamaz", nameof(userConnectionId));
    }

    /// <summary>
    /// Veritabanı komutu oluşturur
    /// </summary>
    /// <param name="connection">Veritabanı bağlantısı</param>
    /// <param name="query">SQL sorgusu</param>
    /// <returns>Yapılandırılmış veritabanı komutu</returns>
    private static IDbCommand CreateCommand(IDbConnection connection, string query)
    {
        var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = 30; // 30 saniye timeout
        return command;
    }

    /// <summary>
    /// DataReader'dan verileri işler
    /// </summary>
    /// <param name="reader">Veri okuyucu</param>
    /// <returns>ExpandoObject listesi</returns>
    private static List<ExpandoObject> ProcessDataReader(IDataReader reader)
    {
        var rows = new List<ExpandoObject>();

        while (reader.Read())
        {
            var row = CreateExpandoObjectFromReader(reader);
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// DataReader'dan ExpandoObject oluşturur
    /// </summary>
    /// <param name="reader">Veri okuyucu</param>
    /// <returns>ExpandoObject</returns>
    private static ExpandoObject CreateExpandoObjectFromReader(IDataReader reader)
    {
        dynamic row = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)row;

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var fieldName = reader.GetName(i);
            var fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
            dictionary[fieldName] = fieldValue;
        }

        return row;
    }

    #endregion
}