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
    /// MCP JSON-RPC endpoint for remote MCP clients
    /// Handles initialize, tools/list, and tools/call methods
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> McpJsonRpc([FromBody] McpRequest request)
    {
        try
        {
            _logger.LogInformation("Received MCP request: {Method}", request.Method);

            return request.Method switch
            {
                "initialize" => Ok(new McpResponse
                {
                    JsonRpc = "2.0",
                    Id = request.Id,
                    Result = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new
                        {
                            tools = new { }
                        },
                        serverInfo = new
                        {
                            name = "wikipedia-mcp-server",
                            version = "6.0.0"
                        }
                    }
                }),

                "tools/list" => Ok(new McpResponse
                {
                    JsonRpc = "2.0",
                    Id = request.Id,
                    Result = new
                    {
                        tools = new object[]
                        {
                            new
                            {
                                name = "wikipedia_search",
                                description = "Search Wikipedia for a topic and return detailed information about the best matching page",
                                inputSchema = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        query = new { type = "string", description = "The search query to look for on Wikipedia" },
                                        limit = new { type = "number", description = "Maximum number of results to return (default: 10)" }
                                    },
                                    required = new[] { "query" }
                                }
                            },
                            new
                            {
                                name = "wikipedia_sections",
                                description = "Get the sections/outline of a Wikipedia page for a given topic",
                                inputSchema = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        topic = new { type = "string", description = "The topic to get sections for" }
                                    },
                                    required = new[] { "topic" }
                                }
                            },
                            new
                            {
                                name = "wikipedia_section_content",
                                description = "Get the content of a specific section from a Wikipedia page",
                                inputSchema = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        topic = new { type = "string", description = "The Wikipedia topic/page title" },
                                        section_title = new { type = "string", description = "The title of the section to retrieve content for" }
                                    },
                                    required = new[] { "topic", "section_title" }
                                }
                            }
                        }
                    }
                }),

                "tools/call" => await HandleToolCall(request),

                _ => BadRequest(new McpErrorResponse
                {
                    JsonRpc = "2.0",
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32601,
                        Message = $"Method not found: {request.Method}"
                    }
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP request: {Method}", request.Method);
            return StatusCode(500, new McpErrorResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal error"
                }
            });
        }
    }

    private async Task<IActionResult> HandleToolCall(McpRequest request)
    {
        // Parse the params as a tools/call request
        if (request.Params == null)
        {
            return BadRequest(new McpErrorResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32602,
                    Message = "Invalid params: missing parameters"
                }
            });
        }

        try
        {
            var paramsJson = System.Text.Json.JsonSerializer.Serialize(request.Params);
            var toolCallRequest = System.Text.Json.JsonSerializer.Deserialize<McpToolCallRequest>(paramsJson);
            
            if (toolCallRequest?.Arguments == null)
            {
                return BadRequest(new McpErrorResponse
                {
                    JsonRpc = "2.0",
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32602,
                        Message = "Invalid params: missing arguments"
                    }
                });
            }

            var toolName = toolCallRequest.Name;
            var arguments = toolCallRequest.Arguments;

            object? result = toolName switch
            {
                "wikipedia_search" => await HandleSearchTool(arguments),
                "wikipedia_sections" => await HandleSectionsTool(arguments),
                "wikipedia_section_content" => await HandleSectionContentTool(arguments),
                _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
            };

            return Ok(new McpResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool: {ToolName}", request.Params);
            return StatusCode(500, new McpErrorResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Tool execution failed: {ex.Message}"
                }
            });
        }
    }

    private async Task<object> HandleSearchTool(Dictionary<string, object> arguments)
    {
        var query = arguments.GetValueOrDefault("query")?.ToString();
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Missing required argument: query");
        }

        // Use the existing search method (it doesn't have a limit parameter)
        var result = await _wikipediaService.SearchAsync(query);
        if (result == null)
        {
            return new { error = "No results found" };
        }
        return result;
    }

    private async Task<object?> HandleSectionsTool(Dictionary<string, object> arguments)
    {
        var topic = arguments.GetValueOrDefault("topic")?.ToString();
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Missing required argument: topic");
        }

        return await _wikipediaService.GetSectionsAsync(topic);
    }

    private async Task<object?> HandleSectionContentTool(Dictionary<string, object> arguments)
    {
        var topic = arguments.GetValueOrDefault("topic")?.ToString();
        var sectionTitle = arguments.GetValueOrDefault("section_title")?.ToString();
        
        if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(sectionTitle))
        {
            throw new ArgumentException("Missing required arguments: topic and section_title");
        }

        return await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);
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