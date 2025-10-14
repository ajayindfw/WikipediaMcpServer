using Microsoft.AspNetCore.Mvc;
using WikipediaMcpServer.Models;
using WikipediaMcpServer.Services;
using System.ComponentModel.DataAnnotations;

namespace WikipediaMcpServer.Controllers;

[ApiController]
[Route("api/wikipedia")]
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
    public async Task<IActionResult> Search([FromQuery][Required][MinLength(1)] string? query)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _wikipediaService.SearchAsync(query!);
            
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
    /// Search Wikipedia for a topic using POST request with JSON body.
    /// </summary>
    /// <param name="request">The search request containing the query</param>
    /// <returns>Dictionary containing title, summary, and url for the Wikipedia page</returns>
    [HttpPost("search")]
    public async Task<IActionResult> SearchPost([FromBody] WikipediaSearchRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _wikipediaService.SearchAsync(request.Query);
            
            if (result == null)
            {
                return NotFound(new { error = $"No Wikipedia page found for query: {request.Query}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request for query: {Query}", request.Query);
            return StatusCode(500, new { error = "Internal server error occurred while searching Wikipedia" });
        }
    }

    /// <summary>
    /// Get the sections/outline of a Wikipedia page for a given topic.
    /// </summary>
    /// <param name="topic">The topic to get sections for</param>
    /// <returns>Dictionary containing sections list and page information</returns>
    [HttpGet("sections")]
    public async Task<IActionResult> GetSections([FromQuery][Required][MinLength(1)] string? topic)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _wikipediaService.GetSectionsAsync(topic!);
            
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
    /// Get the sections/outline of a Wikipedia page using POST request with JSON body.
    /// </summary>
    /// <param name="request">The sections request containing the topic</param>
    /// <returns>Dictionary containing sections list and page information</returns>
    [HttpPost("sections")]
    public async Task<IActionResult> GetSectionsPost([FromBody] WikipediaSectionsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _wikipediaService.GetSectionsAsync(request.Topic);
            
            if (result == null)
            {
                return NotFound(new { error = $"No Wikipedia page found for topic: {request.Topic}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sections request for topic: {Topic}", request.Topic);
            return StatusCode(500, new { error = "Internal server error occurred while getting sections" });
        }
    }

    /// <summary>
    /// Get the content of a specific section from a Wikipedia page.
    /// </summary>
    /// <param name="topic">The Wikipedia topic/page title</param>
    /// <param name="sectionTitle">The title of the section to retrieve content for</param>
    /// <returns>Dictionary containing section content and metadata</returns>
    [HttpGet("section-content")]
    public async Task<IActionResult> GetSectionContent([FromQuery][Required][MinLength(1)] string? topic, [FromQuery][Required][MinLength(1)] string? sectionTitle)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _wikipediaService.GetSectionContentAsync(topic!, sectionTitle!);
            
            if (result == null)
            {
                return NotFound(new { error = $"No section '{sectionTitle}' found for topic: {topic}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing section content request for topic: {Topic}, section: {SectionTitle}", topic, sectionTitle);
            return StatusCode(500, new { error = "Internal server error occurred while getting section content" });
        }
    }

    /// <summary>
    /// Get the content of a specific section from a Wikipedia page using POST request with JSON body.
    /// </summary>
    /// <param name="request">The section content request containing topic and section title</param>
    /// <returns>Dictionary containing the section content on success or error information on failure</returns>
    [HttpPost("section-content")]
    public async Task<IActionResult> GetSectionContentPost([FromBody] WikipediaSectionContentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _wikipediaService.GetSectionContentAsync(request.Topic, request.SectionTitle);
            
            if (result == null)
            {
                return NotFound(new { error = $"No content found for topic: {request.Topic}, section: {request.SectionTitle}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing section content request for topic: {Topic}, section: {Section}", 
                request.Topic, request.SectionTitle);
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