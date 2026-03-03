using System.Text.Json.Nodes;
using nampacx.docintel.Models;

namespace nampacx.docintel.Services;

public interface ITableTransformService
{
    /// <summary>
    /// Transforms raw Document Intelligence tables into a generic
    /// normalized schema that is easy to process downstream.
    /// </summary>
    JsonArray BuildGenericTables(IReadOnlyList<DocumentTableDto> tables);
}
