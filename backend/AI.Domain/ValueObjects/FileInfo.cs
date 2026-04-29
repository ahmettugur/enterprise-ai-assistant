using AI.Domain.Common;

namespace AI.Domain.ValueObjects;

/// <summary>
/// FileInfo Value Object — doküman dosya bilgilerini kapsüller
/// FileName, FileType, FileSize, FileHash birlikte anlamlıdır
/// </summary>
public sealed class FileInfo : ValueObject
{
    public string FileName { get; }
    public string FileType { get; }
    public long FileSize { get; }
    public string FileHash { get; }

    private FileInfo(string fileName, string fileType, long fileSize, string fileHash)
    {
        FileName = fileName;
        FileType = fileType;
        FileSize = fileSize;
        FileHash = fileHash;
    }

    /// <summary>
    /// Yeni FileInfo value object oluşturur
    /// </summary>
    public static FileInfo Create(string fileName, string fileType, long fileSize, string fileHash)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new Exceptions.InvalidEntityStateException("FileInfo", "FileName", "Dosya adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(fileType))
            throw new Exceptions.InvalidEntityStateException("FileInfo", "FileType", "Dosya tipi boş olamaz.");
        if (string.IsNullOrWhiteSpace(fileHash))
            throw new Exceptions.InvalidEntityStateException("FileInfo", "FileHash", "Dosya hash değeri boş olamaz.");
        if (fileSize < 0)
            throw new Exceptions.InvalidEntityStateException("FileInfo", "FileSize", "Dosya boyutu negatif olamaz.");

        return new FileInfo(fileName, fileType, fileSize, fileHash);
    }

    /// <summary>
    /// Dosya boyutunu okunabilir formatta döndürür
    /// </summary>
    public string GetReadableFileSize()
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return FileType;
        yield return FileSize;
        yield return FileHash;
    }

    public override string ToString() => $"{FileName} ({GetReadableFileSize()})";
}
