using AI.Application.DTOs.Neo4j;

namespace AI.Application.Ports.Secondary.Services.Database;


/// <summary>
/// Markdown şema dosyasını parse eden servis interface'i
/// </summary>
public interface ISchemaParserService
{
    /// <summary>
    /// Markdown dosyasını parse eder
    /// </summary>
    /// <param name="markdownContent">Markdown içeriği</param>
    /// <returns>Parse sonucu</returns>
    SchemaParseResult ParseMarkdown(string markdownContent);
    
    /// <summary>
    /// Dosya yolundan parse eder
    /// </summary>
    /// <param name="filePath">Markdown dosya yolu</param>
    /// <returns>Parse sonucu</returns>
    Task<SchemaParseResult> ParseFromFileAsync(string filePath);
    
    /// <summary>
    /// Embedded resource'dan parse eder
    /// </summary>
    /// <param name="resourceName">Resource adı</param>
    /// <returns>Parse sonucu</returns>
    Task<SchemaParseResult> ParseFromResourceAsync(string resourceName = "adventurerworks_schema.md");
    
    /// <summary>
    /// Parse sonucunu Neo4j Cypher import script'ine dönüştürür
    /// </summary>
    /// <param name="parseResult">Parse sonucu</param>
    /// <returns>Cypher script</returns>
    string GenerateCypherImportScript(SchemaParseResult parseResult);
}
