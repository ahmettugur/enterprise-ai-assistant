namespace AI.Domain.Enums;

/// <summary>
/// Represents the type of document for different processing strategies
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Standard document (PDF, TXT, DOCX, etc.)
    /// </summary>
    Document = 0,
    
    /// <summary>
    /// Question-Answer format document (JSON with Q&A pairs)
    /// </summary>
    QuestionAnswer = 1
}
