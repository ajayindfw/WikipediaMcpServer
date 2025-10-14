using Microsoft.AspNetCore.Mvc;
using WikipediaMcpServer.Models;
using WikipediaMcpServer.Services;

namespace WikipediaMcpServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WikipediaController : ControllerBase
{
    private readonly IWikipediaService _wikipediaService;
    private readonly ILogger<WikipediaController> _logger;

    public WikipediaController(IWikipediaService wikipediaService, ILogger<WikipediaController> logger)
    {
        _wikipediaService = wikipediaService;
        _logger = logger;
    }

    /// <summary>
    /// Search Wikipedia for a topic and return detailed information about the best matching page.
    /// </summary>
    /// <param name="query">The search query to look for on Wikipedia</param>
    /// <returns>Dictionary containing title, summary, and url for the Wikipedia page</returns>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        try
        {
            var result = await _wikipediaService.SearchAsync(query);
            
            if (result == null)
            {
                return NotFound(new { error = $"No Wikipedia page found for query: {query}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request for query: {Query}", query);
            return StatusCode(500, new { error = "Internal server error occurred while searching Wikipedia" });
        }
    }

    /// <summary>
    /// Get the sections/outline of a Wikipedia page for a given topic.
    /// </summary>
    /// <param name="topic">The topic to get sections for</param>
    /// <returns>Dictionary containing sections list and page information</returns>
    [HttpGet("sections")]
    public async Task<IActionResult> GetSections([FromQuery] string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest(new { error = "Topic parameter is required" });
        }

        try
        {
            var result = await _wikipediaService.GetSectionsAsync(topic);
            
            if (result == null)
            {
                return NotFound(new { error = $"No Wikipedia page found for topic: {topic}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sections request for topic: {Topic}", topic);
            return StatusCode(500, new { error = "Internal server error occurred while getting sections" });
        }
    }

    /// <summary>
    /// Get the content of a specific section from a Wikipedia page.
    /// </summary>
    /// <param name="topic">The Wikipedia topic/page title</param>
    /// <param name="sectionTitle">The title of the section to retrieve content for</param>
    /// <returns>Dictionary containing the section content on success or error information on failure</returns>
    [HttpGet("section-content")]
    public async Task<IActionResult> GetSectionContent([FromQuery] string topic, [FromQuery] string sectionTitle)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest(new { error = "Topic parameter is required" });
        }

        if (string.IsNullOrWhiteSpace(sectionTitle))
        {
            return BadRequest(new { error = "SectionTitle parameter is required" });
        }

        try
        {
            var result = await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);
            
            if (result == null)
            {
                return NotFound(new { error = $"No content found for topic: {topic}, section: {sectionTitle}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing section content request for topic: {Topic}, section: {Section}", 
                topic, sectionTitle);
            return StatusCode(500, new { error = "Internal server error occurred while getting section content" });
        }
    }

    /// <summary>
    /// Health check endpoint for the Wikipedia MCP server
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "Wikipedia MCP Server", timestamp = DateTime.UtcNow });
    }
}