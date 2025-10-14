using FluentAssertions;
using WikipediaMcpServer.Models;
using Xunit;

namespace WikipediaMcpServer.UnitTests.Models;

public class WikipediaModelTests
{
    [Fact]
    public void WikipediaSearchResult_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new WikipediaSearchResult();

        // Assert
        result.Title.Should().BeEmpty();
        result.Summary.Should().BeEmpty();
        result.Url.Should().BeEmpty();
    }

    [Fact]
    public void WikipediaSearchResult_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var title = "Test Article";
        var summary = "Test summary content";
        var url = "https://en.wikipedia.org/wiki/Test_Article";

        // Act
        var result = new WikipediaSearchResult
        {
            Title = title,
            Summary = summary,
            Url = url
        };

        // Assert
        result.Title.Should().Be(title);
        result.Summary.Should().Be(summary);
        result.Url.Should().Be(url);
    }

    [Fact]
    public void WikipediaSectionsResult_ShouldInitializeWithEmptyList()
    {
        // Act
        var result = new WikipediaSectionsResult();

        // Assert
        result.Title.Should().BeEmpty();
        result.Sections.Should().NotBeNull();
        result.Sections.Should().BeEmpty();
        result.Url.Should().BeEmpty();
    }

    [Fact]
    public void WikipediaSectionsResult_ShouldAllowModifyingSections()
    {
        // Arrange
        var result = new WikipediaSectionsResult();
        var sections = new List<string> { "Introduction", "History", "Applications" };

        // Act
        result.Sections.AddRange(sections);

        // Assert
        result.Sections.Should().HaveCount(3);
        result.Sections.Should().BeEquivalentTo(sections);
    }

    [Fact]
    public void WikipediaSectionsResult_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var title = "Machine Learning";
        var sections = new List<string> { "Introduction", "History", "Applications" };
        var url = "https://en.wikipedia.org/wiki/Machine_Learning";

        // Act
        var result = new WikipediaSectionsResult
        {
            Title = title,
            Sections = sections,
            Url = url
        };

        // Assert
        result.Title.Should().Be(title);
        result.Sections.Should().BeEquivalentTo(sections);
        result.Url.Should().Be(url);
    }

    [Fact]
    public void WikipediaSectionContentResult_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new WikipediaSectionContentResult();

        // Assert
        result.SectionTitle.Should().BeEmpty();
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public void WikipediaSectionContentResult_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var sectionTitle = "History";
        var content = "This section contains historical information...";

        // Act
        var result = new WikipediaSectionContentResult
        {
            SectionTitle = sectionTitle,
            Content = content
        };

        // Assert
        result.SectionTitle.Should().Be(sectionTitle);
        result.Content.Should().Be(content);
    }

    // Wikipedia API DTO Tests

    [Fact]
    public void WikipediaApiSearchResponse_ShouldInitializeWithNullQuery()
    {
        // Act
        var response = new WikipediaApiSearchResponse();

        // Assert
        response.Query.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiSearchResponse_ShouldSetQueryCorrectly()
    {
        // Arrange
        var query = new WikipediaApiQuery();

        // Act
        var response = new WikipediaApiSearchResponse { Query = query };

        // Assert
        response.Query.Should().Be(query);
    }

    [Fact]
    public void WikipediaApiQuery_ShouldInitializeWithNullPages()
    {
        // Act
        var query = new WikipediaApiQuery();

        // Assert
        query.Pages.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiQuery_ShouldSetPagesCorrectly()
    {
        // Arrange
        var pages = new List<WikipediaApiPage>
        {
            new() { PageId = 1, Title = "Page 1" },
            new() { PageId = 2, Title = "Page 2" }
        };

        // Act
        var query = new WikipediaApiQuery { Pages = pages };

        // Assert
        query.Pages.Should().BeEquivalentTo(pages);
        query.Pages.Should().HaveCount(2);
    }

    [Fact]
    public void WikipediaApiPage_ShouldInitializeWithDefaultValues()
    {
        // Act
        var page = new WikipediaApiPage();

        // Assert
        page.PageId.Should().Be(0);
        page.Title.Should().BeEmpty();
        page.Extract.Should().BeNull();
        page.Sections.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiPage_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var pageId = 12345;
        var title = "Test Page";
        var extract = "This is a test page extract.";
        var sections = new List<WikipediaApiSection>
        {
            new() { Title = "Introduction", Level = 1, Index = 0 }
        };

        // Act
        var page = new WikipediaApiPage
        {
            PageId = pageId,
            Title = title,
            Extract = extract,
            Sections = sections
        };

        // Assert
        page.PageId.Should().Be(pageId);
        page.Title.Should().Be(title);
        page.Extract.Should().Be(extract);
        page.Sections.Should().BeEquivalentTo(sections);
    }

    [Fact]
    public void WikipediaApiSection_ShouldInitializeWithDefaultValues()
    {
        // Act
        var section = new WikipediaApiSection();

        // Assert
        section.Title.Should().BeEmpty();
        section.Level.Should().Be(0);
        section.Index.Should().Be(0);
    }

    [Fact]
    public void WikipediaApiSection_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var title = "Introduction";
        var level = 2;
        var index = 1;

        // Act
        var section = new WikipediaApiSection
        {
            Title = title,
            Level = level,
            Index = index
        };

        // Assert
        section.Title.Should().Be(title);
        section.Level.Should().Be(level);
        section.Index.Should().Be(index);
    }

    [Fact]
    public void WikipediaApiSectionResponse_ShouldInitializeWithNullParse()
    {
        // Act
        var response = new WikipediaApiSectionResponse();

        // Assert
        response.Parse.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiSectionResponse_ShouldSetParseCorrectly()
    {
        // Arrange
        var parse = new WikipediaApiParse { Title = "Test" };

        // Act
        var response = new WikipediaApiSectionResponse { Parse = parse };

        // Assert
        response.Parse.Should().Be(parse);
    }

    [Fact]
    public void WikipediaApiParse_ShouldInitializeWithDefaultValues()
    {
        // Act
        var parse = new WikipediaApiParse();

        // Assert
        parse.Title.Should().BeEmpty();
        parse.Text.Should().BeNull();
        parse.Sections.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiParse_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var title = "Test Article";
        var text = new Dictionary<string, string> { { "*", "<p>Content</p>" } };
        var sections = new List<WikipediaApiSectionDetail>
        {
            new() { Line = "Introduction", Index = "1", TocLevel = 1 }
        };

        // Act
        var parse = new WikipediaApiParse
        {
            Title = title,
            Text = text,
            Sections = sections
        };

        // Assert
        parse.Title.Should().Be(title);
        parse.Text.Should().BeEquivalentTo(text);
        parse.Sections.Should().BeEquivalentTo(sections);
    }

    [Fact]
    public void WikipediaApiSectionDetail_ShouldInitializeWithDefaultValues()
    {
        // Act
        var section = new WikipediaApiSectionDetail();

        // Assert
        section.TocLevel.Should().Be(0);
        section.Level.Should().BeEmpty();
        section.Line.Should().BeEmpty();
        section.Number.Should().BeEmpty();
        section.Index.Should().BeEmpty();
        section.FromTitle.Should().BeEmpty();
        section.ByteOffset.Should().Be(0);
        section.Anchor.Should().BeEmpty();
        section.LinkAnchor.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiSectionDetail_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var tocLevel = 2;
        var level = "2";
        var line = "History";
        var number = "1";
        var index = "2";
        var fromTitle = "Test_Article";
        var byteOffset = 1234;
        var anchor = "History";
        var linkAnchor = "History_section";

        // Act
        var section = new WikipediaApiSectionDetail
        {
            TocLevel = tocLevel,
            Level = level,
            Line = line,
            Number = number,
            Index = index,
            FromTitle = fromTitle,
            ByteOffset = byteOffset,
            Anchor = anchor,
            LinkAnchor = linkAnchor
        };

        // Assert
        section.TocLevel.Should().Be(tocLevel);
        section.Level.Should().Be(level);
        section.Line.Should().Be(line);
        section.Number.Should().Be(number);
        section.Index.Should().Be(index);
        section.FromTitle.Should().Be(fromTitle);
        section.ByteOffset.Should().Be(byteOffset);
        section.Anchor.Should().Be(anchor);
        section.LinkAnchor.Should().Be(linkAnchor);
    }

    [Fact]
    public void WikipediaApiParseTextResponse_ShouldInitializeWithNullParse()
    {
        // Act
        var response = new WikipediaApiParseTextResponse();

        // Assert
        response.Parse.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiParseTextResponse_ShouldSetParseCorrectly()
    {
        // Arrange
        var parse = new WikipediaApiParseContent { Title = "Test" };

        // Act
        var response = new WikipediaApiParseTextResponse { Parse = parse };

        // Assert
        response.Parse.Should().Be(parse);
    }

    [Fact]
    public void WikipediaApiParseContent_ShouldInitializeWithDefaultValues()
    {
        // Act
        var content = new WikipediaApiParseContent();

        // Assert
        content.Title.Should().BeEmpty();
        content.PageId.Should().Be(0);
        content.Text.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiParseContent_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var title = "Test Article";
        var pageId = 12345;
        var textDict = new Dictionary<string, string>
        {
            { "*", "<p>This is HTML content</p>" },
            { "contentmodel", "wikitext" }
        };

        // Act
        var content = new WikipediaApiParseContent
        {
            Title = title,
            PageId = pageId,
            Text = textDict
        };

        // Assert
        content.Title.Should().Be(title);
        content.PageId.Should().Be(pageId);
        content.Text.Should().BeEquivalentTo(textDict);
        content.Text!["*"].Should().Be("<p>This is HTML content</p>");
        content.Text["contentmodel"].Should().Be("wikitext");
    }

    // Edge cases and complex scenarios

    [Fact]
    public void WikipediaSectionsResult_ShouldHandleEmptyAndNullValues()
    {
        // Act
        var result = new WikipediaSectionsResult
        {
            Title = "",
            Sections = new List<string>(),
            Url = ""
        };

        // Assert
        result.Title.Should().BeEmpty();
        result.Sections.Should().BeEmpty();
        result.Url.Should().BeEmpty();
    }

    [Fact]
    public void WikipediaApiPage_ShouldHandleNullExtract()
    {
        // Act
        var page = new WikipediaApiPage
        {
            PageId = 123,
            Title = "Test",
            Extract = null
        };

        // Assert
        page.PageId.Should().Be(123);
        page.Title.Should().Be("Test");
        page.Extract.Should().BeNull();
    }

    [Fact]
    public void WikipediaApiParse_ShouldHandleEmptyTextDictionary()
    {
        // Arrange
        var emptyText = new Dictionary<string, string>();

        // Act
        var parse = new WikipediaApiParse { Text = emptyText };

        // Assert
        parse.Text.Should().BeEmpty();
        parse.Text.Should().NotBeNull();
    }

    [Fact]
    public void WikipediaApiSectionDetail_ShouldHandleNullLinkAnchor()
    {
        // Act
        var section = new WikipediaApiSectionDetail
        {
            Line = "Test Section",
            LinkAnchor = null
        };

        // Assert
        section.Line.Should().Be("Test Section");
        section.LinkAnchor.Should().BeNull();
    }
}