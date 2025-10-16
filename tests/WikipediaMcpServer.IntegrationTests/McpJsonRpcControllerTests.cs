using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.IntegrationTests;

public class McpJsonRpcControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpJsonRpcControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent, _jsonOptions);

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
        serverInfo.GetProperty("name").GetString().Should().Be("wikipedia-mcp-server");
        serverInfo.GetProperty("version").GetString().Should().Be("6.0.0");
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent, _jsonOptions);

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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent, _jsonOptions);

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
        textValue.Should().ContainEquivalentOf("title");
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent, _jsonOptions);

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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent, _jsonOptions);

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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpErrorResponse>(responseContent, _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("6");
        mcpResponse.Error.Should().NotBeNull();
        mcpResponse.Error.Code.Should().Be(-32601);
        mcpResponse.Error.Message.Should().Contain("Method not found");
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpErrorResponse>(responseContent, _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("7");
        mcpResponse.Error.Should().NotBeNull();
        mcpResponse.Error.Code.Should().Be(-32603);
        mcpResponse.Error.Message.Should().Contain("Tool execution failed");
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpErrorResponse>(responseContent, _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("8");
        mcpResponse.Error.Should().NotBeNull();
        mcpResponse.Error.Code.Should().Be(-32602);
        mcpResponse.Error.Message.Should().Contain("Invalid params");
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpErrorResponse>(responseContent, _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.JsonRpc.Should().Be("2.0");
        mcpResponse.Id.Should().NotBeNull();
        mcpResponse.Id.ToString().Should().Be("9");
        mcpResponse.Error.Should().NotBeNull();
        mcpResponse.Error.Code.Should().Be(-32603);
        mcpResponse.Error.Message.Should().Contain("Tool execution failed");
    }

    [Fact]
    public async Task McpJsonRpc_InvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        var responseContent = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpErrorResponse>(responseContent, _jsonOptions);

        mcpResponse.Should().NotBeNull();
        mcpResponse!.Error.Should().NotBeNull();
        mcpResponse.Error.Code.Should().Be(-32603);
        mcpResponse.Error.Message.Should().Contain("Tool execution failed");
    }
}