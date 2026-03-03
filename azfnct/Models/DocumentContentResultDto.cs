namespace nampacx.docintel.Models;

public class DocumentContentResultDto
{
    public List<PageContentDto> Pages { get; set; } = [];

    public List<ParagraphContentDto> Paragraphs { get; set; } = [];
}

public class PageContentDto
{
    public int PageNumber { get; set; }

    public List<string> Content { get; set; } = [];
}

public class ParagraphContentDto
{
    public int Index { get; set; }

    public int? PageNumber { get; set; }

    public string Content { get; set; } = string.Empty;
}