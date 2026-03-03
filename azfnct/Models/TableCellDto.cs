using System.Text.Json.Serialization;

namespace nampacx.docintel.Models;

public class TableCellDto
{
    [JsonPropertyName("rowIndex")]
    public int RowIndex { get; set; }

    [JsonPropertyName("columnIndex")]
    public int ColumnIndex { get; set; }

    [JsonPropertyName("columnSpan")]
    public int? ColumnSpan { get; set; }

    [JsonPropertyName("rowSpan")]
    public int? RowSpan { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }
}
