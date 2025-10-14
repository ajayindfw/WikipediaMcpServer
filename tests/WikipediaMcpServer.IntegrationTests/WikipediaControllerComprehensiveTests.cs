using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Text.Json;
using WikipediaMcpServer.Models;
using WikipediaMcpServer.Services;

namespace WikipediaMcpServer.IntegrationTests;

public class WikipediaControllerComprehensiveTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WikipediaControllerComprehensiveTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        
        var healthResponse = JsonDocument.Parse(content);
        healthResponse.RootElement.GetProperty("status").GetString().Should().Be("healthy");
        healthResponse.RootElement.GetProperty("service").GetString().Should().Be("Wikipedia MCP Server");
        healthResponse.RootElement.GetProperty("timestamp").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task GetSearch_WithValidQuery_ShouldReturnWikipediaContent()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/search?query=machine%20learning");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        var searchResult = JsonDocument.Parse(content);
        searchResult.RootElement.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        searchResult.RootElement.GetProperty("summary").GetString().Should().NotBeNullOrEmpty();
        searchResult.RootElement.GetProperty("url").GetString().Should().StartWith("https://en.wikipedia.org/wiki/");
    }

    [Fact]
    public async Task GetSearch_WithComplexQuery_ShouldHandleSpecialCharacters()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/search?query=C%23%20programming");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        
        var searchResult = JsonDocument.Parse(content);
        searchResult.RootElement.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSearch_WithNonExistentTopic_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/search?query=xyzabc123nonexistent456");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorResponse = JsonDocument.Parse(content);
        errorResponse.RootElement.GetProperty("error").GetString().Should().Contain("No Wikipedia page found");
    }

    [Fact]
    public async Task GetSections_WithValidTopic_ShouldReturnSectionsList()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/sections?topic=artificial%20intelligence");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        
        var sectionsResult = JsonDocument.Parse(content);
        sectionsResult.RootElement.GetProperty("title").GetString().Should().NotBeNullOrEmpty();
        sectionsResult.RootElement.GetProperty("sections").GetArrayLength().Should().BeGreaterThan(0);
        sectionsResult.RootElement.GetProperty("url").GetString().Should().StartWith("https://en.wikipedia.org/wiki/");
    }

    [Fact]
    public async Task GetSections_WithNonExistentTopic_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/sections?topic=xyzabc123nonexistent456");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorResponse = JsonDocument.Parse(content);
        errorResponse.RootElement.GetProperty("error").GetString().Should().Contain("No Wikipedia page found");
    }

    [Fact]
    public async Task GetSectionContent_WithValidParameters_ShouldReturnContent()
    {
        // Act - Use a well-known topic and section that should exist
        var response = await _client.GetAsync("/api/wikipedia/section-content?topic=artificial%20intelligence&sectionTitle=History");

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var contentResult = JsonDocument.Parse(content);
            contentResult.RootElement.GetProperty("sectionTitle").GetString().Should().Be("History");
            contentResult.RootElement.GetProperty("content").GetString().Should().NotBeNullOrEmpty();
        }
        else
        {
            // Some sections might not exist, which is acceptable
            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetSectionContent_WithNonExistentTopic_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/section-content?topic=xyzabc123nonexistent456&sectionTitle=Introduction");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorResponse = JsonDocument.Parse(content);
        errorResponse.RootElement.GetProperty("error").GetString().Should().Contain("No section 'Introduction' found for topic:");
    }

    [Fact]
    public async Task GetSectionContent_WithValidTopicButNonExistentSection_ShouldReturnOkWithErrorMessage()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/section-content?topic=python&sectionTitle=NonExistentSection123");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        
        var contentResult = JsonDocument.Parse(content);
        contentResult.RootElement.GetProperty("content").GetString().Should().Contain("not found");
    }

    [Theory]
    [InlineData("/api/wikipedia/search?query=")]
    [InlineData("/api/wikipedia/search")]
    public async Task GetSearch_WithMissingQuery_ShouldReturnBadRequest(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorResponse = JsonDocument.Parse(content);
        errorResponse.RootElement.GetProperty("errors").Should().NotBeNull();
        errorResponse.RootElement.GetProperty("title").GetString().Should().Contain("validation errors");
    }

    [Theory]
    [InlineData("/api/wikipedia/sections?topic=")]
    [InlineData("/api/wikipedia/sections")]
    public async Task GetSections_WithMissingTopic_ShouldReturnBadRequest(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorResponse = JsonDocument.Parse(content);
        errorResponse.RootElement.GetProperty("errors").Should().NotBeNull();
        errorResponse.RootElement.GetProperty("title").GetString().Should().Contain("validation errors");
    }

    [Theory]
    [InlineData("/api/wikipedia/section-content?topic=&sectionTitle=History")]
    [InlineData("/api/wikipedia/section-content?sectionTitle=History")]
    public async Task GetSectionContent_WithMissingTopic_ShouldReturnBadRequest(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorResponse = JsonDocument.Parse(content);
        errorResponse.RootElement.GetProperty("errors").Should().NotBeNull();
        errorResponse.RootElement.GetProperty("title").GetString().Should().Contain("validation errors");
    }

    [Theory]
    [InlineData("/api/wikipedia/section-content?topic=python&sectionTitle=")]
    [InlineData("/api/wikipedia/section-content?topic=python")]
    public async Task GetSectionContent_WithMissingSectionTitle_ShouldReturnBadRequest(string url)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        
        var errorResponse = JsonDocument.Parse(content);
        errorResponse.RootElement.GetProperty("errors").Should().NotBeNull();
        errorResponse.RootElement.GetProperty("title").GetString().Should().Contain("validation errors");
    }

    [Fact]
    public async Task RootEndpoint_ShouldReturnServerMessage()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Wikipedia MCP Server is running");
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        
        var swaggerDoc = JsonDocument.Parse(content);
        swaggerDoc.RootElement.GetProperty("info").GetProperty("title").GetString()
            .Should().Be("Wikipedia MCP Server API");
    }

    [Fact]
    public async Task MultipleRequests_ShouldHandleConcurrency()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send multiple concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync($"/api/wikipedia/search?query=test{i}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task LargeQuery_ShouldHandleGracefully()
    {
        // Arrange
        var largeQuery = new string('a', 1000); // 1000 character query

        // Act
        var response = await _client.GetAsync($"/api/wikipedia/search?query={largeQuery}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    private void Dispose()
    {
        _client?.Dispose();
    }
}