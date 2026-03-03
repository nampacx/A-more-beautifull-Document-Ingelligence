using System.Text.Json.Serialization;

namespace nampacx.docintel.Models;

public class DocumentTableDto
{
    [JsonPropertyName("rowCount")]
    public int RowCount { get; set; }

    [JsonPropertyName("columnCount")]
    public int ColumnCount { get; set; }

    [JsonPropertyName("cells")]
    public List<TableCellDto> Cells { get; set; } = [];

    [JsonPropertyName("boundingRegions")]
    public List<BoundingRegionDto> BoundingRegions { get; set; } = [];
}
