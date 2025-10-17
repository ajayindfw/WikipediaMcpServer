using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using FluentAssertions;
using System.Net;

namespace WikipediaMcpServer.IntegrationTests;

public class SimpleCorsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SimpleCorsTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RootEndpoint_ShouldWork()
    {
        // Act
        var response = await _client.GetAsync("/info");

        // Assert
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error Response: {content}");
        }
        
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}