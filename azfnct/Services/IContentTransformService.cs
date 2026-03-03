using nampacx.docintel.Models;

namespace nampacx.docintel.Services;

public interface IContentTransformService
{
    /// <summary>
    /// Reads a Document Intelligence JSON payload and extracts
    /// page and paragraph information using content-focused fields.
    /// </summary>
    DocumentContentResultDto ExtractPageAndParagraphContent(string jsonPayload);

    /// <summary>
    /// Reads a Document Intelligence JSON file and extracts
    /// page and paragraph information using content-focused fields.
    /// </summary>
    DocumentContentResultDto ExtractPageAndParagraphContentFromFile(string filePath);
}