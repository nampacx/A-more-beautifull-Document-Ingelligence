using System.Text.Json;
using System.Text.Json.Nodes;
using nampacx.docintel.Models;

namespace nampacx.docintel.Services;

public class ContentTransformService : IContentTransformService
{
    public DocumentContentResultDto ExtractPageAndParagraphContent(string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
            throw new ArgumentException("JSON payload cannot be empty.", nameof(jsonPayload));

        JsonNode root;
        try
        {
            root = JsonNode.Parse(jsonPayload) ?? throw new JsonException("Invalid JSON payload.");
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to parse JSON payload.", ex);
        }

        var analyzeResult = root["analyzeResult"] as JsonObject
            ?? throw new JsonException("Missing 'analyzeResult' object in payload.");

        var result = new DocumentContentResultDto
        {
            Pages = ExtractPages(analyzeResult),
            Paragraphs = ExtractParagraphs(analyzeResult)
        };

        return result;
    }

    private static List<PageContentDto> ExtractPages(JsonObject analyzeResult)
    {
        var pages = new List<PageContentDto>();
        var pagesArray = analyzeResult["pages"] as JsonArray;

        if (pagesArray is null)
            return pages;

        foreach (var pageNode in pagesArray)
        {
            if (pageNode is not JsonObject pageObject)
                continue;

            int pageNumber = pageObject["pageNumber"]?.GetValue<int>() ?? 0;
            var contentLines = new List<string>();

            if (pageObject["lines"] is JsonArray linesArray)
            {
                foreach (var line in linesArray)
                {
                    string? lineContent = line?["content"]?.GetValue<string>()?.Trim();
                    if (!string.IsNullOrEmpty(lineContent))
                        contentLines.Add(lineContent);
                }
            }

            if (contentLines.Count == 0 && pageObject["words"] is JsonArray wordsArray)
            {
                foreach (var word in wordsArray)
                {
                    string? wordContent = word?["content"]?.GetValue<string>()?.Trim();
                    if (!string.IsNullOrEmpty(wordContent))
                        contentLines.Add(wordContent);
                }
            }

            pages.Add(new PageContentDto
            {
                PageNumber = pageNumber,
                Content = contentLines
            });
        }

        return pages;
    }

    private static List<ParagraphContentDto> ExtractParagraphs(JsonObject analyzeResult)
    {
        var paragraphs = new List<ParagraphContentDto>();
        var paragraphsArray = analyzeResult["paragraphs"] as JsonArray;

        if (paragraphsArray is null)
            return paragraphs;

        for (int index = 0; index < paragraphsArray.Count; index++)
        {
            if (paragraphsArray[index] is not JsonObject paragraphObject)
                continue;

            string content = paragraphObject["content"]?.GetValue<string>()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(content))
                continue;

            int? pageNumber = null;
            if (paragraphObject["boundingRegions"] is JsonArray boundingRegions
                && boundingRegions.Count > 0
                && boundingRegions[0] is JsonObject firstRegion)
            {
                pageNumber = firstRegion["pageNumber"]?.GetValue<int>();
            }

            paragraphs.Add(new ParagraphContentDto
            {
                Index = index,
                PageNumber = pageNumber,
                Content = content
            });
        }

        return paragraphs;
    }
}