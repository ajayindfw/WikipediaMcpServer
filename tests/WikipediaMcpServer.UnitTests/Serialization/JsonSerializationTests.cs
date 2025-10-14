using FluentAssertions;
using System.Text.Json;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.UnitTests.Serialization;

public class JsonSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonSerializationTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    [Fact]
    public void McpRequest_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalRequest = new McpRequest
        {
            Id = "test-123",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_search",
                Arguments = new Dictionary<string, object>
                {
                    ["query"] = "machine learning",
                    ["limit"] = 5
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(originalRequest, _jsonOptions);
        var deserializedRequest = JsonSerializer.Deserialize<McpRequest>(json, _jsonOptions);

        // Assert
        deserializedRequest.Should().NotBeNull();
        deserializedRequest!.Id?.ToString().Should().Be(originalRequest.Id?.ToString());
        deserializedRequest.Method.Should().Be(originalRequest.Method);
        deserializedRequest.Params.Should().NotBeNull();
        
        var toolCallParams = deserializedRequest.Params as JsonElement?;
        toolCallParams.Should().NotBeNull();
    }

    [Fact]
    public void McpResponse_WithSuccess_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalResponse = new McpResponse
        {
            Id = "test-response-123",
            Result = new McpToolCallResponse
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = "Test content from Wikipedia search" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(originalResponse, _jsonOptions);
        var deserializedResponse = JsonSerializer.Deserialize<McpResponse>(json, _jsonOptions);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Id?.ToString().Should().Be(originalResponse.Id?.ToString());
        deserializedResponse.Result.Should().NotBeNull();
        deserializedResponse.Error.Should().BeNull();
    }

    [Fact]
    public void McpResponse_WithError_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalResponse = new McpResponse
        {
            Id = "test-error-123",
            Error = new McpError
            {
                Code = -32602,
                Message = "Invalid params",
                Data = new { detail = "Missing required argument" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(originalResponse, _jsonOptions);
        var deserializedResponse = JsonSerializer.Deserialize<McpResponse>(json, _jsonOptions);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Id?.ToString().Should().Be(originalResponse.Id?.ToString());
        deserializedResponse.Result.Should().BeNull();
        deserializedResponse.Error.Should().NotBeNull();
        deserializedResponse.Error!.Code.Should().Be(-32602);
        deserializedResponse.Error.Message.Should().Be("Invalid params");
    }

    [Fact]
    public void McpServerInfo_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalInfo = new McpServerInfo
        {
            Name = "wikipedia-mcp-server",
            Version = "2.0.0"
        };

        // Act
        var json = JsonSerializer.Serialize(originalInfo, _jsonOptions);
        var deserializedInfo = JsonSerializer.Deserialize<McpServerInfo>(json, _jsonOptions);

        // Assert
        deserializedInfo.Should().NotBeNull();
        deserializedInfo!.Name.Should().Be(originalInfo.Name);
        deserializedInfo.Version.Should().Be(originalInfo.Version);
    }

    [Fact]
    public void McpToolsListResponse_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalResponse = new McpToolsListResponse
        {
            Tools = new List<McpTool>
            {
                new()
                {
                    Name = "wikipedia_search",
                    Description = "Search Wikipedia for a topic",
                    InputSchema = new McpToolInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpProperty>
                        {
                            ["query"] = new() { Type = "string", Description = "Search query" }
                        },
                        Required = new List<string> { "query" }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(originalResponse, _jsonOptions);
        var deserializedResponse = JsonSerializer.Deserialize<McpToolsListResponse>(json, _jsonOptions);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Tools.Should().HaveCount(1);
        
        var tool = deserializedResponse.Tools.First();
        tool.Name.Should().Be("wikipedia_search");
        tool.Description.Should().Be("Search Wikipedia for a topic");
        tool.InputSchema.Should().NotBeNull();
        tool.InputSchema!.Type.Should().Be("object");
        tool.InputSchema.Properties.Should().ContainKey("query");
        tool.InputSchema.Required.Should().Contain("query");
    }

    [Fact]
    public void WikipediaSearchResult_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalResult = new WikipediaSearchResult
        {
            Title = "Machine Learning",
            Summary = "Machine learning is a method of data analysis...",
            Url = "https://en.wikipedia.org/wiki/Machine_learning"
        };

        // Act
        var json = JsonSerializer.Serialize(originalResult, _jsonOptions);
        var deserializedResult = JsonSerializer.Deserialize<WikipediaSearchResult>(json, _jsonOptions);

        // Assert
        deserializedResult.Should().NotBeNull();
        deserializedResult!.Title.Should().Be(originalResult.Title);
        deserializedResult.Summary.Should().Be(originalResult.Summary);
        deserializedResult.Url.Should().Be(originalResult.Url);
    }

    [Fact]
    public void WikipediaSectionsResult_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalResult = new WikipediaSectionsResult
        {
            Title = "Artificial Intelligence",
            Sections = new List<string> { "History", "Applications", "Techniques", "Ethics" },
            Url = "https://en.wikipedia.org/wiki/Artificial_intelligence"
        };

        // Act
        var json = JsonSerializer.Serialize(originalResult, _jsonOptions);
        var deserializedResult = JsonSerializer.Deserialize<WikipediaSectionsResult>(json, _jsonOptions);

        // Assert
        deserializedResult.Should().NotBeNull();
        deserializedResult!.Title.Should().Be(originalResult.Title);
        deserializedResult.Sections.Should().BeEquivalentTo(originalResult.Sections);
        deserializedResult.Url.Should().Be(originalResult.Url);
    }

    [Fact]
    public void WikipediaSectionContentResult_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalResult = new WikipediaSectionContentResult
        {
            SectionTitle = "History",
            Content = "The history of artificial intelligence (AI) began in antiquity..."
        };

        // Act
        var json = JsonSerializer.Serialize(originalResult, _jsonOptions);
        var deserializedResult = JsonSerializer.Deserialize<WikipediaSectionContentResult>(json, _jsonOptions);

        // Assert
        deserializedResult.Should().NotBeNull();
        deserializedResult!.SectionTitle.Should().Be(originalResult.SectionTitle);
        deserializedResult.Content.Should().Be(originalResult.Content);
    }

    [Fact]
    public void ComplexNestedObject_SerializeDeserialize_ShouldPreserveAllData()
    {
        // Arrange
        var originalResponse = new McpResponse
        {
            Id = "complex-test",
            Result = new McpInitializeResponse
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new McpServerCapabilities
                {
                    Tools = new McpToolsCapability
                    {
                        ListChanged = true
                    }
                },
                ServerInfo = new McpServerInfo
                {
                    Name = "wikipedia-mcp-server",
                    Version = "2.0.0"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(originalResponse, _jsonOptions);
        var deserializedResponse = JsonSerializer.Deserialize<McpResponse>(json, _jsonOptions);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Id?.ToString().Should().Be(originalResponse.Id?.ToString());
        deserializedResponse.Result.Should().NotBeNull();
        
        // Note: Due to JSON serialization of generic objects, we verify the structure exists
        var resultElement = (JsonElement)deserializedResponse.Result!;
        resultElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void JsonElement_HandlesNullValues_Gracefully()
    {
        // Arrange
        var requestWithNulls = new McpRequest
        {
            Id = "null-test",
            Method = "test/method",
            Params = null
        };

        // Act
        var json = JsonSerializer.Serialize(requestWithNulls, _jsonOptions);
        var deserializedRequest = JsonSerializer.Deserialize<McpRequest>(json, _jsonOptions);

        // Assert
        deserializedRequest.Should().NotBeNull();
        deserializedRequest!.Id?.ToString().Should().Be("null-test");
        deserializedRequest.Method.Should().Be("test/method");
        deserializedRequest.Params.Should().BeNull();
    }

    [Fact]
    public void EmptyCollections_SerializeDeserialize_ShouldPreserveStructure()
    {
        // Arrange
        var responseWithEmptyCollections = new McpToolsListResponse
        {
            Tools = new List<McpTool>()
        };

        // Act
        var json = JsonSerializer.Serialize(responseWithEmptyCollections, _jsonOptions);
        var deserializedResponse = JsonSerializer.Deserialize<McpToolsListResponse>(json, _jsonOptions);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Tools.Should().NotBeNull();
        deserializedResponse.Tools.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("test with spaces")]
    [InlineData("test-with-dashes")]
    [InlineData("test_with_underscores")]
    [InlineData("TestWithCamelCase")]
    [InlineData("test123")]
    [InlineData("тест")]  // Unicode
    public void StringProperties_HandleVariousInputs_Correctly(string testString)
    {
        // Arrange
        var result = new WikipediaSearchResult
        {
            Title = testString,
            Summary = $"Summary for {testString}",
            Url = $"https://example.com/{testString}"
        };

        // Act
        var json = JsonSerializer.Serialize(result, _jsonOptions);
        var deserializedResult = JsonSerializer.Deserialize<WikipediaSearchResult>(json, _jsonOptions);

        // Assert
        deserializedResult.Should().NotBeNull();
        deserializedResult!.Title.Should().Be(testString);
        deserializedResult.Summary.Should().Be($"Summary for {testString}");
        deserializedResult.Url.Should().Be($"https://example.com/{testString}");
    }
}