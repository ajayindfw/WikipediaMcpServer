using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.IntegrationTests;

public class McpJsonRpcDebugTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpJsonRpcDebugTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
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
        var response = await _client.PostAsync("/api/wikipedia", content);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response JSON:\n{responseContent}");

        // Try to deserialize
        try
        {
            var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseContent, _jsonOptions);
            Console.WriteLine($"Deserialized successfully. Id: {mcpResponse?.Id}, JsonRpc: {mcpResponse?.JsonRpc}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization failed: {ex.Message}");
        }

        Assert.True(response.IsSuccessStatusCode);
    }
}