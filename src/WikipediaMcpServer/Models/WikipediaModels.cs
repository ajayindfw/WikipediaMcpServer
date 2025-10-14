namespace WikipediaMcpServer.Models;

public class WikipediaSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class WikipediaSectionsResult
{
    public string Title { get; set; } = string.Empty;
    public List<string> Sections { get; set; } = new();
    public string Url { get; set; } = string.Empty;
}

public class WikipediaSectionContentResult
{
    public string SectionTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

// Wikipedia API Response DTOs
public class WikipediaApiSearchResponse
{
    public WikipediaApiQuery? Query { get; set; }
}

public class WikipediaApiQuery
{
    public List<WikipediaApiPage>? Pages { get; set; }
}

public class WikipediaApiPage
{
    public int PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Extract { get; set; }
    public List<WikipediaApiSection>? Sections { get; set; }
}

public class WikipediaApiSection
{
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Index { get; set; }
}

public class WikipediaApiSectionResponse
{
    public WikipediaApiParse? Parse { get; set; }
}

public class WikipediaApiParse
{
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string>? Text { get; set; }
    public List<WikipediaApiSectionDetail>? Sections { get; set; }
}

public class WikipediaApiSectionDetail
{
    public int TocLevel { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Index { get; set; } = string.Empty;
    public string FromTitle { get; set; } = string.Empty;
    public int ByteOffset { get; set; }
    public string Anchor { get; set; } = string.Empty;
    public string? LinkAnchor { get; set; }
}

public class WikipediaApiParseTextResponse
{
    public WikipediaApiParseContent? Parse { get; set; }
}

public class WikipediaApiParseContent
{
    public string Title { get; set; } = string.Empty;
    public int PageId { get; set; }
    public Dictionary<string, string>? Text { get; set; }
}