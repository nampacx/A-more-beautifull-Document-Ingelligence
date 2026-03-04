namespace nampacx.docintel.Models;

public class EntryExtractionResultDto
{
    public List<InvoiceEntryDto> Entries { get; set; } = [];
}

public class InvoiceEntryDto
{
    public string PositionNumber { get; set; } = string.Empty;

    public string? ArticleCode { get; set; }

    public int? PageNumber { get; set; }

    public int StartIndex { get; set; }

    public int EndIndex { get; set; }

    public string? Quantity { get; set; }

    public string? Unit { get; set; }

    public string? UnitPrice { get; set; }

    public string? VatRate { get; set; }

    public string? PositionTotal { get; set; }

    public string? Description { get; set; }

    public string? DeliveryReference { get; set; }

    public string? BillingStartDate { get; set; }

    public string? DiscountAmount { get; set; }

    public string? DiscountVatRate { get; set; }

    public string? DiscountTotal { get; set; }

    public string? TotalPriceNet { get; set; }

    public string? TotalPriceGross { get; set; }

    public List<string> RawContent { get; set; } = [];
}
