using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using System.Text;
using WikipediaMcpServer.Models;
using WikipediaMcpServer.Services;

namespace WikipediaMcpServer.IntegrationTests;

public class WikipediaControllerComprehensiveTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public WikipediaControllerComprehensiveTests(TestWebApplicationFactory<Program> factory)
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

    private static string ExtractJsonFromSseResponse(string sseResponse)
    {
        // Parse Server-Sent Events format
        var lines = sseResponse.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("data: "))
            {
                return line.Substring(6); // Remove "data: " prefix
            }
        }
        
        // If it's already JSON, return as-is
        return sseResponse.Trim();
    }

    [Fact]
    public async Task Health_ShouldReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        
        var healthResponse = JsonDocument.Parse(content);
        healthResponse.RootElement.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task McpInitialize_ShouldReturnCorrectCapabilities()
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
        
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task McpToolsList_ShouldReturnWikipediaTools()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "tools/list"
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        
        var tools = mcpResponse.RootElement.GetProperty("result").GetProperty("tools");
        tools.GetArrayLength().Should().BeGreaterThan(0);
        
        // Check for wikipedia tools
        var toolNames = new List<string>();
        foreach (var tool in tools.EnumerateArray())
        {
            toolNames.Add(tool.GetProperty("name").GetString()!);
        }
        
        toolNames.Should().Contain("wikipedia_search");
        toolNames.Should().Contain("wikipedia_sections");
        toolNames.Should().Contain("wikipedia_section_content");
    }

    [Fact]
    public async Task McpSearch_WithValidQuery_ShouldReturnWikipediaContent()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_search",
                arguments = new { query = "machine learning" }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        mcpResponse.RootElement.GetProperty("result").GetProperty("content").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task McpSections_WithValidTopic_ShouldReturnSectionsList()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_sections",
                arguments = new { topic = "Python" }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        mcpResponse.RootElement.GetProperty("result").GetProperty("content").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task McpSectionContent_WithValidParameters_ShouldReturnContent()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "tools/call",
            Params = new
            {
                name = "wikipedia_section_content",
                arguments = new { topic = "Python", section_title = "History" }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        mcpResponse.RootElement.GetProperty("result").GetProperty("content").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task McpInvalidMethod_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "invalid_method"
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        // MCP SDK returns HTTP 200 with JSON-RPC error for invalid methods
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var mcpResponse = JsonDocument.Parse(jsonResponse);
        mcpResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        mcpResponse.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        mcpResponse.RootElement.TryGetProperty("error", out var error).Should().BeTrue();
        error.GetProperty("code").GetInt32().Should().Be(-32601); // Method not found
    }

    [Fact]
    public async Task McpInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - MCP SDK returns HTTP 500 for JSON parsing errors
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void WikipediaService_ShouldBeRegistered()
    {
        // Act
        using var scope = _factory.Services.CreateScope();
        var wikipediaService = scope.ServiceProvider.GetService<IWikipediaService>();

        // Assert
        wikipediaService.Should().NotBeNull();
        wikipediaService.Should().BeOfType<WikipediaService>();
    }

    [Fact]
    public void HttpClientFactory_ShouldBeConfigured()
    {
        // Act
        using var scope = _factory.Services.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();

        // Assert
        httpClientFactory.Should().NotBeNull();
    }
}