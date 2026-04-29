using AI.Domain.Common;

namespace AI.Domain.Documents;

/// <summary>
/// Dokümanların parçalanmış hallerini tutan entity
/// </summary>
public sealed class DocumentChunk : Entity<Guid>
{

    /// <summary>
    /// Bu chunk'ın ait olduğu doküman ID'si
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Chunk sıra numarası
    /// </summary>
    public int ChunkIndex { get; private set; }

    /// <summary>
    /// Chunk içeriği
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Chunk'ın karakter sayısı
    /// </summary>
    public int ContentLength { get; private set; }

    /// <summary>
    /// Orijinal dokümandaki başlangıç pozisyonu
    /// </summary>
    public int StartPosition { get; private set; }

    /// <summary>
    /// Orijinal dokümandaki bitiş pozisyonu
    /// </summary>
    public int EndPosition { get; private set; }

    /// <summary>
    /// Chunk'ın oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Qdrant'ta saklanan vector ID'si
    /// </summary>
    public Guid? VectorId { get; private set; }

    /// <summary>
    /// Qdrant'ta saklanan vector ID'si (string format)
    /// </summary>
    public string? QdrantVectorId { get; private set; }

    /// <summary>
    /// Embedding oluşturulma tarihi
    /// </summary>
    public DateTime? EmbeddingCreatedAt { get; private set; }

    /// <summary>
    /// Chunk'ın metadata bilgileri (JSON format)
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Sparse vector indices (Qdrant native sparse vector için)
    /// BM25-style term indices
    /// </summary>
    public uint[]? SparseIndices { get; private set; }

    /// <summary>
    /// Sparse vector values (Qdrant native sparse vector için)
    /// Term weights (BM25 scores)
    /// </summary>
    public float[]? SparseValues { get; private set; }

    /// <summary>
    /// Navigation property - İlişkili doküman
    /// </summary>
    public DocumentMetadata? Document { get; private set; }

    // EF Core / deserialization constructor
    private DocumentChunk() { }

    /// <summary>
    /// Factory method - yeni chunk oluşturur
    /// </summary>
    public static DocumentChunk Create(
        Guid documentId,
        int chunkIndex,
        string content,
        int startPosition,
        int endPosition,
        string? metadata = null)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId cannot be empty", nameof(documentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), "ChunkIndex cannot be negative");
        if (startPosition < 0)
            throw new ArgumentOutOfRangeException(nameof(startPosition), "StartPosition cannot be negative");
        if (endPosition < startPosition)
            throw new ArgumentException("EndPosition cannot be less than StartPosition", nameof(endPosition));

        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkIndex = chunkIndex,
            Content = content,
            ContentLength = content.Length,
            StartPosition = startPosition,
            EndPosition = endPosition,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// İçeriği günceller (encoding düzeltmesi vb.)
    /// </summary>
    public void UpdateContent(string newContent)
    {
        Content = newContent;
        ContentLength = newContent.Length;
    }

    /// <summary>
    /// Metadata JSON'ını ayarlar
    /// </summary>
    public void SetMetadata(string metadataJson)
    {
        Metadata = metadataJson;
    }

    /// <summary>
    /// Embedding bilgilerini ayarlar
    /// </summary>
    public void SetEmbedding(Guid vectorId, string? qdrantVectorId = null)
    {
        VectorId = vectorId;
        QdrantVectorId = qdrantVectorId;
        EmbeddingCreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sparse vector'ları ayarlar
    /// </summary>
    public void SetSparseVector(uint[] indices, float[] values)
    {
        if (indices.Length != values.Length)
            throw new ArgumentException("SparseIndices and SparseValues must have the same length");

        SparseIndices = indices;
        SparseValues = values;
    }

    /// <summary>
    /// Sparse vector'ün geçerli olup olmadığını kontrol eder
    /// </summary>
    public bool HasValidSparseVector()
    {
        return SparseIndices != null &&
               SparseValues != null &&
               SparseIndices.Length > 0 &&
               SparseIndices.Length == SparseValues.Length;
    }

    /// <summary>
    /// Sparse vector'ün non-zero term sayısını döndürür
    /// </summary>
    public int GetSparseVectorSize()
    {
        return SparseIndices?.Length ?? 0;
    }

    /// <summary>
    /// Chunk'ın embedding'e sahip olup olmadığını kontrol eder
    /// </summary>
    public bool HasEmbedding()
    {
        return VectorId.HasValue && EmbeddingCreatedAt.HasValue;
    }
}