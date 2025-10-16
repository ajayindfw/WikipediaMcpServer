using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using System.Text.Json;
using WikipediaMcpServer.Controllers;
using WikipediaMcpServer.Models;
using WikipediaMcpServer.Services;

namespace WikipediaMcpServer.UnitTests;

public class WikipediaControllerMcpJsonRpcTests
{
    private readonly Mock<IWikipediaService> _mockWikipediaService;
    private readonly Mock<ILogger<WikipediaController>> _mockLogger;
    private readonly WikipediaController _controller;
    private readonly JsonSerializerOptions _jsonOptions;

    public WikipediaControllerMcpJsonRpcTests()
    {
        _mockWikipediaService = new Mock<IWikipediaService>();
        _mockLogger = new Mock<ILogger<WikipediaController>>();
        _controller = new WikipediaController(_mockWikipediaService.Object, _mockLogger.Object);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task McpJsonRpc_Initialize_ShouldReturnOkWithCorrectResponse()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "initialize",
            Params = JsonSerializer.SerializeToElement(new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                clientInfo = new { name = "Test Client", version = "1.0.0" }
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<McpResponse>().Subject;

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        // Verify the result structure
        var resultJson = JsonSerializer.Serialize(response.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.GetProperty("protocolVersion").GetString().Should().Be("2024-11-05");
        resultElement.GetProperty("capabilities").ValueKind.Should().Be(JsonValueKind.Object);
        resultElement.GetProperty("serverInfo").ValueKind.Should().Be(JsonValueKind.Object);
        
        var serverInfo = resultElement.GetProperty("serverInfo");
        serverInfo.GetProperty("name").GetString().Should().Be("wikipedia-mcp-server");
        serverInfo.GetProperty("version").GetString().Should().Be("6.0.0");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsList_ShouldReturnOkWithToolsArray()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 2,
            Method = "tools/list",
            Params = JsonSerializer.SerializeToElement(new { }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<McpResponse>().Subject;

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        // Verify the tools list structure
        var resultJson = JsonSerializer.Serialize(response.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.GetProperty("tools").ValueKind.Should().Be(JsonValueKind.Array);
        var tools = resultElement.GetProperty("tools").EnumerateArray().ToArray();
        
        tools.Should().HaveCount(3);
        
        // Verify all expected tools are present
        var toolNames = tools.Select(t => t.GetProperty("name").GetString()).ToArray();
        toolNames.Should().Contain("wikipedia_search");
        toolNames.Should().Contain("wikipedia_sections");
        toolNames.Should().Contain("wikipedia_section_content");
        
        // Verify each tool has required properties
        foreach (var tool in tools)
        {
            tool.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
            tool.GetProperty("description").GetString().Should().NotBeNullOrEmpty();
            tool.GetProperty("inputSchema").ValueKind.Should().Be(JsonValueKind.Object);
            
            var inputSchema = tool.GetProperty("inputSchema");
            inputSchema.GetProperty("type").GetString().Should().Be("object");
            inputSchema.GetProperty("properties").ValueKind.Should().Be(JsonValueKind.Object);
            inputSchema.GetProperty("required").ValueKind.Should().Be(JsonValueKind.Array);
        }
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_WikipediaSearch_ShouldReturnSearchResults()
    {
        // Arrange
        var searchResult = new WikipediaSearchResult
        {
            Title = "Artificial intelligence",
            Summary = "AI is intelligence demonstrated by machines...",
            Url = "https://en.wikipedia.org/wiki/Artificial_intelligence"
        };

        _mockWikipediaService
            .Setup(s => s.SearchAsync("artificial intelligence"))
            .ReturnsAsync(searchResult);

        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 3,
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new
            {
                name = "wikipedia_search",
                arguments = new Dictionary<string, object>
                {
                    ["query"] = "artificial intelligence"
                }
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<McpResponse>().Subject;

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        // Verify the result contains the search data
        var resultJson = JsonSerializer.Serialize(response.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.GetProperty("content").ValueKind.Should().Be(JsonValueKind.Array);
        var contentArray = resultElement.GetProperty("content").EnumerateArray().ToArray();
        
        contentArray.Should().HaveCount(1);
        var textContent = contentArray[0];
        textContent.GetProperty("type").GetString().Should().Be("text");
        
        var textValue = textContent.GetProperty("text").GetString();
        textValue.Should().Contain("Artificial intelligence");
        textValue.Should().Contain("AI is intelligence demonstrated by machines");
        
        _mockWikipediaService.Verify(s => s.SearchAsync("artificial intelligence"), Times.Once);
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_WikipediaSections_ShouldReturnSections()
    {
        // Arrange
        var sectionsResult = new WikipediaSectionsResult
        {
            Title = "Machine Learning",
            Sections = new List<string> { "History", "Applications", "Machine Learning" },
            Url = "https://en.wikipedia.org/wiki/Machine_Learning"
        };

        _mockWikipediaService
            .Setup(s => s.GetSectionsAsync("Machine Learning"))
            .ReturnsAsync(sectionsResult);

        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 4,
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new
            {
                name = "wikipedia_sections",
                arguments = new Dictionary<string, object>
                {
                    ["topic"] = "Machine Learning"
                }
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<McpResponse>().Subject;

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        // Verify the result contains sections data
        var resultJson = JsonSerializer.Serialize(response.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.GetProperty("content").ValueKind.Should().Be(JsonValueKind.Array);
        var contentArray = resultElement.GetProperty("content").EnumerateArray().ToArray();
        
        contentArray.Should().HaveCount(1);
        var textContent = contentArray[0];
        textContent.GetProperty("type").GetString().Should().Be("text");
        
        var textValue = textContent.GetProperty("text").GetString();
        textValue.Should().Contain("History");
        textValue.Should().Contain("Applications");
        textValue.Should().Contain("Machine Learning");
        
        _mockWikipediaService.Verify(s => s.GetSectionsAsync("Machine Learning"), Times.Once);
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_WikipediaSectionContent_ShouldReturnContent()
    {
        // Arrange
        var sectionContent = new WikipediaSectionContentResult
        {
            SectionTitle = "History",
            Content = "The history of artificial intelligence began in antiquity..."
        };

        _mockWikipediaService
            .Setup(s => s.GetSectionContentAsync("Artificial Intelligence", "History"))
            .ReturnsAsync(sectionContent);

        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 5,
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new
            {
                name = "wikipedia_section_content",
                arguments = new Dictionary<string, object>
                {
                    ["topic"] = "Artificial Intelligence",
                    ["section_title"] = "History"
                }
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value.Should().BeOfType<McpResponse>().Subject;

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();

        // Verify the result contains section content
        var resultJson = JsonSerializer.Serialize(response.Result, _jsonOptions);
        var resultElement = JsonDocument.Parse(resultJson).RootElement;
        
        resultElement.GetProperty("content").ValueKind.Should().Be(JsonValueKind.Array);
        var contentArray = resultElement.GetProperty("content").EnumerateArray().ToArray();
        
        contentArray.Should().HaveCount(1);
        var textContent = contentArray[0];
        textContent.GetProperty("type").GetString().Should().Be("text");
        
        var textValue = textContent.GetProperty("text").GetString();
        textValue.Should().Contain("History");
        textValue.Should().Contain("The history of artificial intelligence began in antiquity");
        
        _mockWikipediaService.Verify(s => s.GetSectionContentAsync("Artificial Intelligence", "History"), Times.Once);
    }

    [Fact]
    public async Task McpJsonRpc_UnknownMethod_ShouldReturnBadRequestWithError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 6,
            Method = "unknown/method",
            Params = JsonSerializer.SerializeToElement(new { }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        var response = badRequestResult.Value.Should().BeOfType<McpErrorResponse>().Subject;

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32601);
        response.Error.Message.Should().Be("Method not found: unknown/method");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_UnknownTool_ShouldReturnInternalServerErrorWithError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 7,
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new
            {
                name = "unknown_tool",
                arguments = new Dictionary<string, object>()
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        
        var response = objectResult.Value.Should().BeOfType<McpErrorResponse>().Subject;
        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32603);
        response.Error.Message.Should().Be("Tool execution failed: Unknown tool: unknown_tool");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_MissingArguments_ShouldReturnBadRequestWithError()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 8,
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new
            {
                name = "wikipedia_search"
                // Missing arguments
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        var response = badRequestResult.Value.Should().BeOfType<McpErrorResponse>().Subject;

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32602);
        response.Error.Message.Should().Be("Invalid params: missing arguments");
    }

    [Fact]
    public async Task McpJsonRpc_ToolsCall_ServiceException_ShouldReturnInternalServerErrorWithError()
    {
        // Arrange
        _mockWikipediaService
            .Setup(s => s.SearchAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Wikipedia API is down"));

        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 9,
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new
            {
                name = "wikipedia_search",
                arguments = new Dictionary<string, object>
                {
                    ["query"] = "test"
                }
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        
        var response = objectResult.Value.Should().BeOfType<McpErrorResponse>().Subject;
        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32603);
        response.Error.Message.Should().Contain("Tool execution failed");
        response.Error.Message.Should().Contain("Wikipedia API is down");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task McpJsonRpc_ToolsCall_EmptyQueryParameter_ShouldReturnError(string emptyQuery)
    {
        // Arrange
        _mockWikipediaService
            .Setup(s => s.SearchAsync(emptyQuery))
            .ThrowsAsync(new ArgumentException("Query cannot be empty"));

        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 10,
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(new
            {
                name = "wikipedia_search",
                arguments = new Dictionary<string, object>
                {
                    ["query"] = emptyQuery
                }
            }, _jsonOptions)
        };

        // Act
        var result = await _controller.McpJsonRpc(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(500);
        
        var response = objectResult.Value.Should().BeOfType<McpErrorResponse>().Subject;
        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32603);
        response.Error.Message.Should().Contain("Tool execution failed");
        // Empty string gets "Missing required argument: query", whitespace gets "Query cannot be empty"
        response.Error.Message.Should().Match(msg => 
            msg.Contains("Missing required argument: query") || 
            msg.Contains("Query cannot be empty"));
    }
}