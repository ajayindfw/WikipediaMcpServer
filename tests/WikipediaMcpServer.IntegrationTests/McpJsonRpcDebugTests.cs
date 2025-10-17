using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.IntegrationTests;

public class McpJsonRpcDebugTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpJsonRpcDebugTests(TestWebApplicationFactory<Program> factory)
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
            WriteIndented = true
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
    public async Task Debug_Initialize_ResponseFormat()
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
        Console.WriteLine($"Request JSON:\n{json}");
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response JSON:\n{responseContent}");

        // Try to deserialize
        try
        {
            var mcpResponse = JsonSerializer.Deserialize<McpResponse>(ExtractJsonFromSseResponse(responseContent), _jsonOptions);
            Console.WriteLine($"Deserialized successfully. Id: {mcpResponse?.Id}, JsonRpc: {mcpResponse?.JsonRpc}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization failed: {ex.Message}");
        }

        Assert.True(response.IsSuccessStatusCode);
    }
}