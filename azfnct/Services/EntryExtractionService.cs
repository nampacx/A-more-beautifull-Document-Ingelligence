using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using nampacx.docintel.Models;

namespace nampacx.docintel.Services;

public partial class EntryExtractionService : IEntryExtractionService
{
    public EntryExtractionResultDto ExtractEntriesFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("JSON file was not found.", filePath);

        string jsonPayload = File.ReadAllText(filePath);
        return ExtractEntries(jsonPayload);
    }

    public EntryExtractionResultDto ExtractEntries(string jsonPayload)
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

        var paragraphs = ParseParagraphs(root);
        var entries = ExtractInvoiceEntries(paragraphs);

        return new EntryExtractionResultDto { Entries = entries };
    }

    private static List<InvoiceEntryDto> ExtractInvoiceEntries(List<ParagraphToken> paragraphs)
    {
        var entries = new List<InvoiceEntryDto>();

        for (int index = 0; index < paragraphs.Count; index++)
        {
            var startMatch = StartOfEntryRegex().Match(paragraphs[index].Content);
            if (!startMatch.Success)
                continue;

            int blockEnd = index + 1;
            while (blockEnd < paragraphs.Count && !StartOfEntryRegex().IsMatch(paragraphs[blockEnd].Content))
            {
                blockEnd++;
            }

            var block = paragraphs.GetRange(index, blockEnd - index);
            var parsed = ParseBlock(block, startMatch);
            if (parsed is not null)
            {
                entries.Add(parsed);
            }

            index = blockEnd - 1;
        }

        return entries;
    }

    private static InvoiceEntryDto? ParseBlock(List<ParagraphToken> block, Match startMatch)
    {
        if (block.Count == 0)
            return null;

        string position = startMatch.Groups["position"].Value;
        string? articleCode = startMatch.Groups["article"].Success
            ? startMatch.Groups["article"].Value.Trim()
            : null;

        int scanIndex = 1;

        if (string.IsNullOrWhiteSpace(articleCode) && scanIndex < block.Count)
        {
            string candidate = block[scanIndex].Content;
            if (LooksLikeArticleCode(candidate))
            {
                articleCode = candidate;
                scanIndex++;
            }
        }

        string? quantity = null;
        string? unit = null;
        string? unitPrice = null;
        string? vatRate = null;
        string? positionTotal = null;
        string? description = null;
        string? deliveryReference = null;
        string? billingStartDate = null;
        string? discountAmount = null;
        string? discountVatRate = null;
        string? discountTotal = null;
        string? totalPriceNet = null;
        string? totalPriceGross = null;

        var descriptionParts = new List<string>();
        var rawContent = block.Select(token => token.Content).ToList();

        bool inDiscount = false;
        bool inTotalPrice = false;
        int discountNumberCount = 0;
        int totalNumberCount = 0;

        for (int i = scanIndex; i < block.Count; i++)
        {
            string current = block[i].Content;

            if (QuantityUnitRegex().Match(current) is { Success: true } quantityMatch)
            {
                quantity ??= quantityMatch.Groups["quantity"].Value;
                unit ??= quantityMatch.Groups["unit"].Value;
                continue;
            }

            if (current.StartsWith("Abrechnungsbeginn", StringComparison.OrdinalIgnoreCase))
            {
                var dateMatch = DateRegex().Match(current);
                if (dateMatch.Success)
                {
                    billingStartDate = dateMatch.Value;
                }
                else if (i + 1 < block.Count && DateRegex().IsMatch(block[i + 1].Content))
                {
                    billingStartDate = DateRegex().Match(block[i + 1].Content).Value;
                }
                continue;
            }

            if (current.StartsWith("Positionsabschlag", StringComparison.OrdinalIgnoreCase))
            {
                inDiscount = true;
                inTotalPrice = false;
                continue;
            }

            if (current.StartsWith("Gesamtpreis", StringComparison.OrdinalIgnoreCase))
            {
                inTotalPrice = true;
                inDiscount = false;
                continue;
            }

            if (current.Contains("LfsNr", StringComparison.OrdinalIgnoreCase))
            {
                deliveryReference = string.IsNullOrWhiteSpace(deliveryReference)
                    ? current
                    : $"{deliveryReference} {current}";
                continue;
            }

            if (VatRegex().IsMatch(current))
            {
                if (inDiscount)
                {
                    discountVatRate ??= current;
                }
                else
                {
                    vatRate ??= current;
                }

                continue;
            }

            if (MoneyRegex().IsMatch(current))
            {
                if (inTotalPrice)
                {
                    if (totalNumberCount == 0)
                    {
                        totalPriceNet = current;
                    }
                    else if (totalNumberCount == 1)
                    {
                        totalPriceGross = current;
                    }

                    totalNumberCount++;
                    continue;
                }

                if (inDiscount)
                {
                    if (discountNumberCount == 0)
                    {
                        discountAmount = current;
                    }
                    else if (discountNumberCount == 1)
                    {
                        discountTotal = current;
                    }

                    discountNumberCount++;
                    continue;
                }

                if (unitPrice is null)
                {
                    unitPrice = current;
                }
                else if (positionTotal is null)
                {
                    positionTotal = current;
                }

                continue;
            }

            if (!IsFooterNoise(current))
            {
                descriptionParts.Add(current);
            }
        }

        if (descriptionParts.Count > 0)
        {
            description = string.Join(" ", descriptionParts);
        }

        return new InvoiceEntryDto
        {
            PositionNumber = position,
            ArticleCode = articleCode,
            PageNumber = block[0].PageNumber,
            StartIndex = block[0].Index,
            EndIndex = block[^1].Index,
            Quantity = quantity,
            Unit = unit,
            UnitPrice = unitPrice,
            VatRate = vatRate,
            PositionTotal = positionTotal,
            Description = description,
            DeliveryReference = deliveryReference,
            BillingStartDate = billingStartDate,
            DiscountAmount = discountAmount,
            DiscountVatRate = discountVatRate,
            DiscountTotal = discountTotal,
            TotalPriceNet = totalPriceNet,
            TotalPriceGross = totalPriceGross,
            RawContent = rawContent
        };
    }

    private static List<ParagraphToken> ParseParagraphs(JsonNode root)
    {
        JsonArray? paragraphsArray = null;

        if (root["paragraphs"] is JsonArray directParagraphs)
        {
            paragraphsArray = directParagraphs;
        }
        else if (root["analyzeResult"]?["paragraphs"] is JsonArray analyzeParagraphs)
        {
            paragraphsArray = analyzeParagraphs;
        }

        if (paragraphsArray is null)
            throw new JsonException("Missing 'paragraphs' array in payload.");

        var tokens = new List<ParagraphToken>();

        for (int i = 0; i < paragraphsArray.Count; i++)
        {
            if (paragraphsArray[i] is not JsonObject paragraph)
                continue;

            string content = paragraph["content"]?.GetValue<string>()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
                continue;

            int index = paragraph["index"]?.GetValue<int>() ?? i;

            int? pageNumber = paragraph["pageNumber"]?.GetValue<int>();
            if (pageNumber is null
                && paragraph["boundingRegions"] is JsonArray regions
                && regions.Count > 0
                && regions[0] is JsonObject region)
            {
                pageNumber = region["pageNumber"]?.GetValue<int>();
            }

            tokens.Add(new ParagraphToken(index, pageNumber, content));
        }

        return tokens
            .OrderBy(token => token.Index)
            .ToList();
    }

    private static bool LooksLikeArticleCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Contains(' '))
            return false;

        if (MoneyRegex().IsMatch(value) || VatRegex().IsMatch(value) || DateRegex().IsMatch(value))
            return false;

        return value.Length <= 20;
    }

    private static bool IsFooterNoise(string value)
    {
        return value.StartsWith("Dieses Dokument ist eine Kopie", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("Seite ", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct ParagraphToken(int Index, int? PageNumber, string Content);

    [GeneratedRegex("^(?<position>\\d{3})(?:\\s+(?<article>\\S+))?$")]
    private static partial Regex StartOfEntryRegex();

    [GeneratedRegex("^(?<quantity>\\d+[,.]\\d+|\\d+)\\s+(?<unit>[A-Za-z]+)$")]
    private static partial Regex QuantityUnitRegex();

    [GeneratedRegex("^\\d{1,2}%$")]
    private static partial Regex VatRegex();

    [GeneratedRegex("^\\d+[,.]\\d{2}$")]
    private static partial Regex MoneyRegex();

    [GeneratedRegex("\\b\\d{2}\\.\\d{2}\\.\\d{4}\\b")]
    private static partial Regex DateRegex();
}
