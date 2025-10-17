using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;
using WikipediaMcpServer.Services;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.IntegrationTests;

public class WikipediaControllerIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public WikipediaControllerIntegrationTests(TestWebApplicationFactory<Program> factory)
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
        content.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.Should().NotBeNull();
        jsonDoc.RootElement.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task McpSearch_WithValidQuery_ShouldReturnSuccessResponse()
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
                arguments = new { query = "artificial intelligence" }
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
        
        // Verify it's valid JSON-RPC response
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var jsonDoc = JsonDocument.Parse(jsonResponse);
        jsonDoc.Should().NotBeNull();
        jsonDoc.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        jsonDoc.RootElement.GetProperty("id").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task McpSections_WithValidTopic_ShouldReturnSuccessResponse()
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
                arguments = new { topic = "machine learning" }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", requestContent);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON-RPC response
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var jsonDoc = JsonDocument.Parse(jsonResponse);
        jsonDoc.Should().NotBeNull();
        jsonDoc.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        jsonDoc.RootElement.GetProperty("id").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task McpSectionContent_WithValidParameters_ShouldReturnSuccessResponse()
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
                arguments = new { topic = "python", section_title = "History" }
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
        
        // Verify it's valid JSON-RPC response
        var jsonResponse = ExtractJsonFromSseResponse(responseContent);
        var jsonDoc = JsonDocument.Parse(jsonResponse);
        jsonDoc.Should().NotBeNull();
        jsonDoc.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        jsonDoc.RootElement.GetProperty("id").GetInt32().Should().Be(1);
    }

    [Fact]
    public void WikipediaService_ShouldBeRegisteredInDI()
    {
        // Act
        using var scope = _factory.Services.CreateScope();
        var wikipediaService = scope.ServiceProvider.GetService<IWikipediaService>();

        // Assert
        wikipediaService.Should().NotBeNull();
        wikipediaService.Should().BeOfType<WikipediaService>();
    }

    [Fact]
    public void HttpClient_ShouldBeConfiguredForWikipediaService()
    {
        // Act
        using var scope = _factory.Services.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();

        // Assert
        httpClientFactory.Should().NotBeNull();
        
        var httpClient = httpClientFactory!.CreateClient();
        httpClient.Should().NotBeNull();
    }


}