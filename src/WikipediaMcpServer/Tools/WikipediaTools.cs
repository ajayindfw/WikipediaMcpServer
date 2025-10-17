using ModelContextProtocol.Server;
using System.ComponentModel;
using WikipediaMcpServer.Services;

namespace WikipediaMcpServer.Tools;

[McpServerToolType]
public sealed class WikipediaTools
{
    [McpServerTool(Name = "wikipedia_search"), Description("Search Wikipedia for topics and articles")]
    public static async Task<string> SearchWikipedia(
        IWikipediaService wikipediaService,
        [Description("The search query to find Wikipedia articles")] string query)
    {
        var result = await wikipediaService.SearchAsync(query);
        
        if (result == null)
        {
            return $"No Wikipedia articles found for query: {query}";
        }

        var response = $"Wikipedia search result for '{query}':\n\n";
        response += $"**{result.Title}**\n";
        response += $"URL: {result.Url}\n";
        if (!string.IsNullOrEmpty(result.Summary))
        {
            response += $"Summary: {result.Summary}\n";
        }

        return response.TrimEnd();
    }

    [McpServerTool(Name = "wikipedia_sections"), Description("Get the sections/outline of a Wikipedia page")]
    public static async Task<string> GetWikipediaSections(
        IWikipediaService wikipediaService,
        [Description("The topic/page title to get sections for")] string topic)
    {
        var result = await wikipediaService.GetSectionsAsync(topic);
        
        if (result == null || !result.Sections.Any())
        {
            return $"No sections found for Wikipedia topic: {topic}";
        }

        var response = $"Wikipedia page sections for '{result.Title}':\n\n";
        
        foreach (var section in result.Sections)
        {
            response += $"{section}\n";
        }

        return response.TrimEnd();
    }

    [McpServerTool(Name = "wikipedia_section_content"), Description("Get the content of a specific section from a Wikipedia page")]
    public static async Task<string> GetWikipediaSectionContent(
        IWikipediaService wikipediaService,
        [Description("The Wikipedia topic/page title")] string topic,
        [Description("The title of the section to retrieve content for")] string sectionTitle)
    {
        var result = await wikipediaService.GetSectionContentAsync(topic, sectionTitle);
        
        if (result == null || string.IsNullOrEmpty(result.Content))
        {
            return $"No content found for section '{sectionTitle}' in Wikipedia topic '{topic}'";
        }

        var response = $"Content from section '{result.SectionTitle}' in Wikipedia page '{topic}':\n\n";
        response += result.Content;

        return response;
    }
}