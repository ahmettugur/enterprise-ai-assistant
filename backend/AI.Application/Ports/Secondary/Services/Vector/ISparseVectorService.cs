using AI.Application.DTOs.SparseVector;

namespace AI.Application.Ports.Secondary.Services.Vector;


/// <summary>
/// Sparse vector generation servisi - Qdrant native sparse vector için
/// Lucene.Net Turkish Analyzer kullanarak BM25-style sparse vectors oluşturur
/// Deterministic hashing kullanarak vocabulary persistence gerektirmez
/// </summary>
public interface ISparseVectorService
{
    /// <summary>
    /// Metinden sparse vector oluşturur (Qdrant formatında)
    /// </summary>
    (uint[] indices, float[] values) GenerateSparseVector(string text);

    /// <summary>
    /// Metinden sparse vector oluşturur (named tuple olarak)
    /// </summary>
    SparseVectorResult GenerateSparseVectorResult(string text);
}