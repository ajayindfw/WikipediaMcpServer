using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace WikipediaMcpServer.IntegrationTests;

/// <summary>
/// Integration tests for MCP protocol version edge cases and compatibility
/// </summary>
public class McpProtocolVersionEdgeCaseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpProtocolVersionEdgeCaseTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Theory]
    [InlineData("1.0.0")]      // Too old
    [InlineData("2023-01-01")] // Invalid format
    [InlineData("3.0.0")]      // Too new
    [InlineData("")]           // Empty
    public async Task Initialize_WithUnsupportedProtocolVersion_ShouldReturnError(string unsupportedVersion)
    {
        // Arrange
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = unsupportedVersion,
                capabilities = new { tools = new { } },
                clientInfo = new
                {
                    name = "Test Client",
                    version = "1.0.0"
                }
            }
        };

        var json = JsonSerializer.Serialize(initRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp/rpc", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        // Should return an error for unsupported protocol version
        jsonResponse.RootElement.TryGetProperty("error", out var errorProperty).Should().BeTrue();
        
        var errorCode = errorProperty.GetProperty("code").GetInt32();
        errorCode.Should().Be(-32602); // Invalid params error code
    }

    [Theory]
    [InlineData("2024-11-05", "wikipedia-mcp-dotnet-server", "8.1.0")]
    [InlineData("2025-06-18", "wikipedia-mcp-dotnet-server", "8.1.0")]
    public async Task Initialize_WithSupportedProtocolVersion_ShouldReturnCorrectServerInfo(
        string protocolVersion, string expectedName, string expectedVersion)
    {
        // Arrange
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion,
                capabilities = new { tools = new { } },
                clientInfo = new
                {
                    name = "Test Client",
                    version = "1.0.0"
                }
            }
        };

        var json = JsonSerializer.Serialize(initRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Set MCP protocol version header
        content.Headers.Add("MCP-Protocol-Version", protocolVersion);

        // Act
        var response = await _client.PostAsync("/mcp/rpc", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        // Should return successful initialization
        jsonResponse.RootElement.TryGetProperty("result", out var resultProperty).Should().BeTrue();
        
        var serverInfo = resultProperty.GetProperty("serverInfo");
        serverInfo.GetProperty("name").GetString().Should().Be(expectedName);
        serverInfo.GetProperty("version").GetString().Should().Be(expectedVersion);
        
        var returnedProtocolVersion = resultProperty.GetProperty("protocolVersion").GetString();
        returnedProtocolVersion.Should().Be(protocolVersion);
    }

    [Fact]
    public async Task Initialize_WithMismatchedHeaderAndBodyProtocolVersion_ShouldUseBodyVersion()
    {
        // Arrange
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18", // Body says 2025-06-18
                capabilities = new { tools = new { } },
                clientInfo = new
                {
                    name = "Test Client",
                    version = "1.0.0"
                }
            }
        };

        var json = JsonSerializer.Serialize(initRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Header says 2024-11-05 (different from body)
        content.Headers.Add("MCP-Protocol-Version", "2024-11-05");

        // Act
        var response = await _client.PostAsync("/mcp/rpc", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        jsonResponse.RootElement.TryGetProperty("result", out var resultProperty).Should().BeTrue();
        var returnedProtocolVersion = resultProperty.GetProperty("protocolVersion").GetString();
        
        // Should use the version from the request body, not the header
        returnedProtocolVersion.Should().Be("2025-06-18");
    }

    [Fact]
    public async Task Initialize_WithoutMcpProtocolVersionHeader_ShouldStillWork()
    {
        // Arrange
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { tools = new { } },
                clientInfo = new
                {
                    name = "Test Client",
                    version = "1.0.0"
                }
            }
        };

        var json = JsonSerializer.Serialize(initRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Deliberately not setting MCP-Protocol-Version header

        // Act
        var response = await _client.PostAsync("/mcp/rpc", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        jsonResponse.RootElement.TryGetProperty("result", out var resultProperty).Should().BeTrue();
        var returnedProtocolVersion = resultProperty.GetProperty("protocolVersion").GetString();
        returnedProtocolVersion.Should().Be("2025-06-18");
    }

    [Theory]
    [InlineData("2024-11-05", false, false, false)] // Legacy version - basic capabilities
    [InlineData("2025-06-18", true, true, true)]    // Latest version - enhanced capabilities
    public async Task Initialize_ProtocolVersionShouldDetermineCapabilities(
        string protocolVersion, bool expectResources, bool expectLogging, bool expectListChanged)
    {
        // Arrange
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion,
                capabilities = new { tools = new { } },
                clientInfo = new
                {
                    name = "Test Client",
                    version = "1.0.0"
                }
            }
        };

        var json = JsonSerializer.Serialize(initRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("MCP-Protocol-Version", protocolVersion);

        // Act
        var response = await _client.PostAsync("/mcp/rpc", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        jsonResponse.RootElement.TryGetProperty("result", out var resultProperty).Should().BeTrue();
        var capabilities = resultProperty.GetProperty("capabilities");

        // Tools should always be present
        capabilities.TryGetProperty("tools", out _).Should().BeTrue();

        // Resources and logging depend on protocol version
        capabilities.TryGetProperty("resources", out _).Should().Be(expectResources);
        capabilities.TryGetProperty("logging", out _).Should().Be(expectLogging);

        if (expectListChanged)
        {
            var tools = capabilities.GetProperty("tools");
            tools.TryGetProperty("listChanged", out var listChangedProperty).Should().BeTrue();
            listChangedProperty.GetBoolean().Should().BeFalse();
        }
    }

    [Fact]
    public async Task ToolsList_AfterInitialization_ShouldRespectProtocolVersion()
    {
        // Arrange - First initialize with specific protocol version
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { tools = new { } },
                clientInfo = new
                {
                    name = "Test Client",
                    version = "1.0.0"
                }
            }
        };

        var initJson = JsonSerializer.Serialize(initRequest, _jsonOptions);
        var initContent = new StringContent(initJson, Encoding.UTF8, "application/json");
        initContent.Headers.Add("MCP-Protocol-Version", "2025-06-18");

        await _client.PostAsync("/mcp/rpc", initContent);

        // Act - Now call tools/list
        var toolsListRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };

        var toolsJson = JsonSerializer.Serialize(toolsListRequest, _jsonOptions);
        var toolsContent = new StringContent(toolsJson, Encoding.UTF8, "application/json");
        toolsContent.Headers.Add("MCP-Protocol-Version", "2025-06-18");

        var toolsResponse = await _client.PostAsync("/mcp/rpc", toolsContent);

        // Assert
        toolsResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        toolsResponse.Headers.Should().ContainKey("MCP-Protocol-Version");
        toolsResponse.Headers.GetValues("MCP-Protocol-Version").First().Should().Be("2025-06-18");

        var responseContent = await toolsResponse.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        jsonResponse.RootElement.TryGetProperty("result", out var resultProperty).Should().BeTrue();
        resultProperty.TryGetProperty("tools", out var toolsProperty).Should().BeTrue();
        
        // Should have Wikipedia tools
        var toolsArray = toolsProperty.EnumerateArray().ToList();
        toolsArray.Should().HaveCountGreaterThan(0);
        
        // Verify tool structure includes required MCP fields
        var firstTool = toolsArray.First();
        firstTool.TryGetProperty("name", out _).Should().BeTrue();
        firstTool.TryGetProperty("description", out _).Should().BeTrue();
        firstTool.TryGetProperty("inputSchema", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentProtocolVersionRequests_ShouldHandleCorrectly()
    {
        // Arrange - Multiple clients with different protocol versions
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < 5; i++)
        {
            var protocolVersion = i % 2 == 0 ? "2024-11-05" : "2025-06-18";
            
            var initRequest = new
            {
                jsonrpc = "2.0",
                id = i + 1,
                method = "initialize",
                @params = new
                {
                    protocolVersion,
                    capabilities = new { tools = new { } },
                    clientInfo = new
                    {
                        name = $"Test Client {i}",
                        version = "1.0.0"
                    }
                }
            };

            var json = JsonSerializer.Serialize(initRequest, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.Add("MCP-Protocol-Version", protocolVersion);

            tasks.Add(_client.PostAsync("/mcp/rpc", content));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(5);
        
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);
            
            jsonResponse.RootElement.TryGetProperty("result", out var resultProperty).Should().BeTrue();
            var protocolVersion = resultProperty.GetProperty("protocolVersion").GetString();
            protocolVersion.Should().BeOneOf("2024-11-05", "2025-06-18");
        }
    }
}