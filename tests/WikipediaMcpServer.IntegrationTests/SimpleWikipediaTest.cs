using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.IntegrationTests;

public class SimpleWikipediaTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SimpleWikipediaTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Configure client for MCP protocol requirements
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
    }

    [Fact]
    public async Task WikipediaController_JsonRpc_ShouldNotHaveCorsError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = "1",
            Method = "initialize",
            Params = new { protocolVersion = "2024-11-05" }
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/", content);

        // Assert - we should not get a CORS error (500), but may get other errors (400, etc.)
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error Response: {responseContent}");
            
            // The CORS error specifically mentions "CORS metadata, but a middleware was not found"
            responseContent.Should().NotContain("CORS metadata, but a middleware was not found");
        }
        
        // We expect either success or a different kind of error, but not CORS error
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}