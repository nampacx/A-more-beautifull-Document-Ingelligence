using System.Text.Json.Serialization;

namespace nampacx.docintel.Models;

public class BoundingRegionDto
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }
}
