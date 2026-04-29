namespace AI.Application.DTOs.SparseVector;

/// <summary>
/// Sparse vector result
/// </summary>
public class SparseVectorResult
{
    public uint[] Indices { get; set; } = Array.Empty<uint>();
    public float[] Values { get; set; } = Array.Empty<float>();
    public int NonZeroCount => Indices.Length;
}