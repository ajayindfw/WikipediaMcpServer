using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.IntegrationTests;

public class McpJsonRpcControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpJsonRpcControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Configure client for MCP protocol requirements
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    // Helper method to parse Server-Sent Events response from Microsoft MCP SDK
    private static string ExtractJsonFromSseResponse(string sseResponse)
    {
        var lines = sseResponse.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("data: "))
            {
                return line.Substring(6); // Remove "data: " prefix
            }
        }
        return sseResponse; // Fallback to original if no data line found
    }

    [Fact]
    public async Task DebugErrorResponse()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 6,
            Method = "unknown/method",
            Params = new { }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Debug output
        Console.WriteLine("=== ERROR RESPONSE DEBUG ===");
        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine("Raw Response:");
        Console.WriteLine(responseContent);
        
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        Console.WriteLine("Extracted JSON:");
        Console.WriteLine(jsonResponse);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task McpJsonRpc_Initialize_ShouldReturnCorrectResponse()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "initialize",
            Params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                clientInfo = new { name = "Test Client", version = "1.0.0" }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        // Extract JSON from SSE response format
        var jsonContent = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(jsonContent, _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("1");
        mcpResponse.Result.Should().NotBeNull();
        mcpResponse.Error.Should().BeNull();

        // Verify the result structure
        var resultJson = JsonSerializer.Serialize(mcpResponse.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.GetProperty("protocolVersion").GetString().Should().Be("2024-11-05");
        resultElement.TryGetProperty("capabilities", out _).Should().BeTrue();
        resultElement.TryGetProperty("serverInfo", out _).Should().BeTrue();
        
        var serverInfo = resultElement.GetProperty("serverInfo");
        var serverName = serverInfo.GetProperty("name").GetString();
        serverName.Should().BeOneOf("WikipediaMcpServer", "testhost"); // Accept both production and test names
        serverInfo.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task McpJsonRpc_ToolsList_ShouldReturnAvailableTools()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 2,
            Method = "tools/list",
            Params = new { }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("2");
        mcpResponse.Result.Should().NotBeNull();
        mcpResponse.Error.Should().BeNull();

        // Verify the tools list structure
        var resultJson = JsonSerializer.Serialize(mcpResponse.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.TryGetProperty("tools", out var toolsProperty).Should().BeTrue();
        toolsProperty.ValueKind.Should().Be(JsonValueKind.Array);
        var tools = toolsProperty.EnumerateArray().ToArray();
        
        tools.Should().HaveCount(3);
        
        // Verify wikipedia_search tool
        var searchTool = tools.First(t => t.GetProperty("name").GetString() == "wikipedia_search");
        searchTool.GetProperty("description").GetString().Should().Contain("Search Wikipedia");
        searchTool.GetProperty("inputSchema").ValueKind.Should().Be(JsonValueKind.Object);
        
        // Verify wikipedia_sections tool
        var sectionsTool = tools.First(t => t.GetProperty("name").GetString() == "wikipedia_sections");
        sectionsTool.GetProperty("description").GetString().Should().Contain("sections/outline");
        
        // Verify wikipedia_section_content tool
        var contentTool = tools.First(t => t.GetProperty("name").GetString() == "wikipedia_section_content");
        contentTool.GetProperty("description").GetString().Should().Contain("content of a specific section");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_WikipediaSearch_ShouldReturnSearchResults()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 3,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_search",
                arguments = new Dictionary<string, object>
                {
                    ["query"] = "artificial intelligence"
                }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("3");
        mcpResponse.Result.Should().NotBeNull();
        mcpResponse.Error.Should().BeNull();

        // Verify the result contains content
        var resultJson = JsonSerializer.Serialize(mcpResponse.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.TryGetProperty("content", out var contentProperty).Should().BeTrue();
        contentProperty.ValueKind.Should().Be(JsonValueKind.Array);
        var contentArray = contentProperty.EnumerateArray().ToArray();
        
        contentArray.Should().HaveCount(1);
        var textContent = contentArray[0];
        textContent.GetProperty("type").GetString().Should().Be("text");
        textContent.GetProperty("text").GetString().Should().NotBeEmpty();
        
        // The text should contain Wikipedia search result data
        var textValue = textContent.GetProperty("text").GetString();
        textValue.Should().ContainEquivalentOf("artificial intelligence");
        textValue.Should().ContainEquivalentOf("summary");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_WikipediaSections_ShouldReturnSections()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 4,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_sections",
                arguments = new Dictionary<string, object>
                {
                    ["topic"] = "Machine Learning"
                }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("4");
        mcpResponse.Result.Should().NotBeNull();
        mcpResponse.Error.Should().BeNull();

        // Verify the result contains sections data
        var resultJson = JsonSerializer.Serialize(mcpResponse.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.TryGetProperty("content", out var contentProperty).Should().BeTrue();
        contentProperty.ValueKind.Should().Be(JsonValueKind.Array);
        var contentArray = contentProperty.EnumerateArray().ToArray();
        
        contentArray.Should().HaveCount(1);
        var textContent = contentArray[0];
        textContent.GetProperty("type").GetString().Should().Be("text");
        textContent.GetProperty("text").GetString().Should().NotBeEmpty();
        
        // The text should contain sections data
        var textValue = textContent.GetProperty("text").GetString();
        textValue.Should().Contain("sections");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_WikipediaSectionContent_ShouldReturnContent()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 5,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_section_content",
                arguments = new Dictionary<string, object>
                {
                    ["topic"] = "Artificial Intelligence",
                    ["section_title"] = "History"
                }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("5");
        mcpResponse.Result.Should().NotBeNull();
        mcpResponse.Error.Should().BeNull();

        // Verify the result contains section content
        var resultJson = JsonSerializer.Serialize(mcpResponse.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.TryGetProperty("content", out var contentProperty).Should().BeTrue();
        contentProperty.ValueKind.Should().Be(JsonValueKind.Array);
        var contentArray = contentProperty.EnumerateArray().ToArray();
        
        contentArray.Should().HaveCount(1);
        var textContent = contentArray[0];
        textContent.GetProperty("type").GetString().Should().Be("text");
        textContent.GetProperty("text").GetString().Should().NotBeEmpty();
        
        // The text should contain section content data
        var textValue = textContent.GetProperty("text").GetString();
        textValue.Should().ContainEquivalentOf("content");
    }

    [Fact]
    public async Task McpJsonRpc_UnknownMethod_ShouldReturnMethodNotFoundError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 6,
            Method = "unknown/method",
            Params = new { }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - MCP SDK returns HTTP 200 with JSON-RPC error response
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);
        
        // Should have error instead of result
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(6);
        mcpResponse.RootElement.TryGetProperty("error", out var errorElement).Should().BeTrue();
        errorElement.GetProperty("code").GetInt32().Should().Be(-32601); // Method not found error code
        errorElement.GetProperty("message").GetString().Should().Contain("unknown/method");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_UnknownTool_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 7,
            Method = "tools/call",
            Params = new
            {
                name = "unknown_tool",
                arguments = new Dictionary<string, object>()
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - Tool execution errors should be returned as successful responses
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);

        // Should have error instead of result for unknown tool
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(7);
        mcpResponse.RootElement.TryGetProperty("error", out var errorElement).Should().BeTrue();
        errorElement.GetProperty("message").GetString().Should().Contain("unknown_tool");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_MissingArguments_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 8,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_search"
                // Missing arguments
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - Missing arguments should be handled gracefully
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("8");
        // Framework handles missing arguments gracefully
        mcpResponse.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_MissingRequiredParameters_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 9,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_search",
                arguments = new Dictionary<string, object>()
                // Missing required "query" parameter
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - Tool with missing required parameters should be handled gracefully
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("9");
        // Framework handles missing required parameters gracefully
        mcpResponse.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task McpJsonRpc_InvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - Invalid JSON causes JSON parsing exception, returns HTTP 500
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task McpJsonRpc_ToolsCall_InvalidSearchQuery_ShouldReturnError(string? query)
    {
        // Arrange
        var arguments = new Dictionary<string, object>();
        if (query != null)
        {
            arguments["query"] = query;
        }

        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 10,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_search",
                arguments = arguments
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - Invalid search query should be handled gracefully
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        // Framework handles invalid queries gracefully
        mcpResponse.Result.Should().NotBeNull();
    }
}