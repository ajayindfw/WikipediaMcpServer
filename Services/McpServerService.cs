using System.Text.Json;
using WikipediaMcpServer.Models;
using WikipediaMcpServer.Services;

namespace WikipediaMcpServer.Services;

public class McpServerService : BackgroundService
{
    private readonly ILogger<McpServerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServerService(ILogger<McpServerService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP Server started, listening on stdin/stdout");

        try
        {
            await using var stdin = Console.OpenStandardInput();
            using var reader = new StreamReader(stdin);

            while (!stoppingToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var request = JsonSerializer.Deserialize<McpRequest>(line, _jsonOptions);
                    if (request != null)
                    {
                        var response = await HandleRequest(request);
                        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                        await Console.Out.WriteLineAsync(responseJson);
                        await Console.Out.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MCP request: {Line}", line);
                    
                    var errorResponse = new McpResponse
                    {
                        Id = null,
                        Error = new McpError
                        {
                            Code = -32603,
                            Message = "Internal error"
                        }
                    };
                    
                    var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    await Console.Out.WriteLineAsync(errorJson);
                    await Console.Out.FlushAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in MCP server");
        }
    }

    private async Task<McpResponse> HandleRequest(McpRequest request)
    {
        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolsCall(request),
            _ => new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32601,
                    Message = $"Method not found: {request.Method}"
                }
            }
        };
    }

    private McpResponse HandleInitialize(McpRequest request)
    {
        return new McpResponse
        {
            Id = request.Id,
            Result = new McpInitializeResponse
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new McpServerCapabilities
                {
                    Tools = new McpToolsCapability { ListChanged = false }
                },
                ServerInfo = new McpServerInfo
                {
                    Name = "wikipedia-mcp-dotnet-server",
                    Version = "2.0.0-enhanced"
                }
            }
        };
    }

    private McpResponse HandleToolsList(McpRequest request)
    {
        var tools = new List<McpTool>
        {
            new()
            {
                Name = "wikipedia_search",
                Description = "Search Wikipedia for a topic and return detailed information about the best matching page.",
                InputSchema = new McpToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, McpProperty>
                    {
                        ["query"] = new() { Type = "string", Description = "The search query to look for on Wikipedia" }
                    },
                    Required = new List<string> { "query" }
                }
            },
            new()
            {
                Name = "wikipedia_sections",
                Description = "Get the sections/outline of a Wikipedia page for a given topic.",
                InputSchema = new McpToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, McpProperty>
                    {
                        ["topic"] = new() { Type = "string", Description = "The topic to get sections for" }
                    },
                    Required = new List<string> { "topic" }
                }
            },
            new()
            {
                Name = "wikipedia_section_content",
                Description = "Get the content of a specific section from a Wikipedia page.",
                InputSchema = new McpToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, McpProperty>
                    {
                        ["topic"] = new() { Type = "string", Description = "The Wikipedia topic/page title" },
                        ["section_title"] = new() { Type = "string", Description = "The title of the section to retrieve content for" }
                    },
                    Required = new List<string> { "topic", "section_title" }
                }
            }
        };

        return new McpResponse
        {
            Id = request.Id,
            Result = new McpToolsListResponse { Tools = tools }
        };
    }

    private async Task<McpResponse> HandleToolsCall(McpRequest request)
    {
        try
        {
            var callRequest = JsonSerializer.Deserialize<McpToolCallRequest>(
                JsonSerializer.Serialize(request.Params, _jsonOptions), _jsonOptions);

            if (callRequest?.Arguments == null)
            {
                return new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError { Code = -32602, Message = "Invalid parameters" }
                };
            }

            using var scope = _serviceProvider.CreateScope();
            var wikipediaService = scope.ServiceProvider.GetRequiredService<IWikipediaService>();

            string resultText = callRequest.Name switch
            {
                "wikipedia_search" => await HandleWikipediaSearch(wikipediaService, callRequest.Arguments),
                "wikipedia_sections" => await HandleWikipediaSections(wikipediaService, callRequest.Arguments),
                "wikipedia_section_content" => await HandleWikipediaSectionContent(wikipediaService, callRequest.Arguments),
                _ => $"Unknown tool: {callRequest.Name}"
            };

            return new McpResponse
            {
                Id = request.Id,
                Result = new McpToolCallResponse
                {
                    Content = new List<McpContent>
                    {
                        new() { Type = "text", Text = resultText }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling tool call");
            return new McpResponse
            {
                Id = request.Id,
                Error = new McpError { Code = -32603, Message = "Internal error" }
            };
        }
    }

    private async Task<string> HandleWikipediaSearch(IWikipediaService service, Dictionary<string, object> args)
    {
        if (!args.TryGetValue("query", out var queryObj) || queryObj?.ToString() is not string query)
        {
            return "Error: query parameter is required";
        }

        var result = await service.SearchAsync(query);
        if (result == null)
        {
            return $"No Wikipedia page found for query: {query}";
        }

        return JsonSerializer.Serialize(result, _jsonOptions);
    }

    private async Task<string> HandleWikipediaSections(IWikipediaService service, Dictionary<string, object> args)
    {
        if (!args.TryGetValue("topic", out var topicObj) || topicObj?.ToString() is not string topic)
        {
            return "Error: topic parameter is required";
        }

        var result = await service.GetSectionsAsync(topic);
        if (result == null)
        {
            return $"No Wikipedia page found for topic: {topic}";
        }

        return JsonSerializer.Serialize(result, _jsonOptions);
    }

    private async Task<string> HandleWikipediaSectionContent(IWikipediaService service, Dictionary<string, object> args)
    {
        if (!args.TryGetValue("topic", out var topicObj) || topicObj?.ToString() is not string topic)
        {
            return "Error: topic parameter is required";
        }

        if (!args.TryGetValue("section_title", out var sectionObj) || sectionObj?.ToString() is not string sectionTitle)
        {
            return "Error: section_title parameter is required";
        }

        var result = await service.GetSectionContentAsync(topic, sectionTitle);
        if (result == null)
        {
            return $"No content found for topic: {topic}, section: {sectionTitle}";
        }

        return JsonSerializer.Serialize(result, _jsonOptions);
    }
}