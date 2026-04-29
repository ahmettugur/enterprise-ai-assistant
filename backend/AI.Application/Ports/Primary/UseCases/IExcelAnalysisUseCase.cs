using AI.Application.Results;
using AI.Application.DTOs.Chat;

namespace AI.Application.Ports.Primary.UseCases;

/// <summary>
/// Excel/CSV dosya analizi Use Case arayüzü — Primary Port
/// DuckDB ile Excel/CSV analizi, LLM ile analiz planı ve yorumlama
/// </summary>
public interface IExcelAnalysisUseCase
{
    /// <summary>
    /// Excel/CSV dosyasını DuckDB ile analiz eder ve kullanıcı sorusuna SQL ile cevap verir.
    /// Spesifik sorularda tek SQL, genel analiz isteklerinde çoklu SQL üretir.
    /// </summary>
    /// <param name="request">Chat isteği (FileBase64, FileName, Prompt içerir)</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    /// <returns>Analiz sonucu</returns>
    Task<Result<LLmResponseModel>> ProcessExcelQueryAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
