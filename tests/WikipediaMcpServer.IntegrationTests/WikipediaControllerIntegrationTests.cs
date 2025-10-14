using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using WikipediaMcpServer.Services;

namespace WikipediaMcpServer.IntegrationTests;

public class WikipediaControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WikipediaControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetSearch_WithValidQuery_ShouldReturnSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/search?query=artificial intelligence");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSearch_WithEmptyQuery_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/search?query=");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSections_WithValidTopic_ShouldReturnSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/sections?topic=machine learning");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSections_WithEmptyTopic_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/sections?topic=");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSectionContent_WithValidParameters_ShouldReturnSuccessResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/section-content?topic=python&sectionTitle=History");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSectionContent_WithEmptyTopic_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/section-content?topic=&sectionTitle=History");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSectionContent_WithEmptySectionTitle_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/wikipedia/section-content?topic=python&sectionTitle=");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
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

    private void Dispose()
    {
        _client?.Dispose();
    }
}