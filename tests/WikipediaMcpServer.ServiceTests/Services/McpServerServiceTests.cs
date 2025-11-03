using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WikipediaMcpServer.Services;
using WikipediaMcpServer.Models;
using System.Text.Json;
using System.Reflection;

namespace WikipediaMcpServer.ServiceTests.Services;

/// <summary>
/// Tests for McpServerService - LEGACY custom MCP implementation
/// 
/// IMPORTANT: These tests validate the legacy McpServerService which is no longer
/// used in the current runtime. The application now uses Microsoft.ModelContextProtocol.Server
/// with WikipediaTools for MCP protocol handling.
/// 
/// These tests remain valuable for:
/// - Understanding MCP protocol requirements and JSON-RPC 2.0 compliance
/// - Validating alternative architecture patterns 
/// - Providing reference implementation testing approaches
/// - Maintaining knowledge of custom MCP protocol implementation
/// 
/// Current active MCP implementation: src/WikipediaMcpServer/Tools/WikipediaTools.cs
/// </summary>

public class McpServerServiceTests
{
    private readonly Mock<ILogger<McpServerService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IWikipediaService> _mockWikipediaService;
    private readonly McpServerService _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServerServiceTests()
    {
        _mockLogger = new Mock<ILogger<McpServerService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockWikipediaService = new Mock<IWikipediaService>();

        // Setup service provider chain - use a simpler approach
        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(x => x.GetService(typeof(IWikipediaService)))
            .Returns(_mockWikipediaService.Object);
        
        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(mockScopeServiceProvider.Object);
        
        _mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(_mockServiceScope.Object);
            
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);

        _service = new McpServerService(_mockLogger.Object, _mockServiceProvider.Object);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var service = new McpServerService(_mockLogger.Object, _mockServiceProvider.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequest_Initialize_ShouldReturnCorrectResponse()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-1",
            Method = "initialize",
            Params = new Dictionary<string, object>()
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-1");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpInitializeResponse>();
        
        var initResponse = response.Result as McpInitializeResponse;
        initResponse.Should().NotBeNull();
        initResponse!.ProtocolVersion.Should().Be("2024-11-05");
        initResponse.Capabilities.Should().NotBeNull();
        initResponse.Capabilities.Tools.Should().NotBeNull();
        initResponse.ServerInfo.Should().NotBeNull();
        initResponse.ServerInfo.Name.Should().Be("wikipedia-mcp-dotnet-server");
        initResponse.ServerInfo.Version.Should().Be("2.0.0-enhanced");
    }

    [Fact]
    public async Task HandleRequest_ToolsList_ShouldReturnAvailableTools()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-2",
            Method = "tools/list",
            Params = new Dictionary<string, object>()
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-2");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolsListResponse>();
        
        var toolsResponse = response.Result as McpToolsListResponse;
        toolsResponse.Should().NotBeNull();
        toolsResponse!.Tools.Should().HaveCount(3);
        
        var toolNames = toolsResponse.Tools.Select(t => t.Name).ToList();
        toolNames.Should().Contain("wikipedia_search");
        toolNames.Should().Contain("wikipedia_sections");
        toolNames.Should().Contain("wikipedia_section_content");

        // Verify tool details
        var searchTool = toolsResponse.Tools.First(t => t.Name == "wikipedia_search");
        searchTool.Description.Should().Contain("Search Wikipedia");
        searchTool.InputSchema.Should().NotBeNull();
        searchTool.InputSchema.Required.Should().Contain("query");
    }

    [Fact]
    public async Task HandleRequest_UnknownMethod_ShouldReturnMethodNotFoundError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-3",
            Method = "unknown/method",
            Params = new Dictionary<string, object>()
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-3");
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32601);
        response.Error.Message.Should().Contain("Method not found: unknown/method");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSearch_ShouldCallService()
    {
        // Arrange
        var searchResult = new WikipediaSearchResult
        {
            Title = "Test Topic",
            Summary = "Test summary",
            Url = "https://test.com"
        };

        _mockWikipediaService.Setup(x => x.SearchAsync("test query"))
            .ReturnsAsync(searchResult);

        var request = new McpRequest
        {
            Id = "test-4",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_search",
                Arguments = new Dictionary<string, object> { ["query"] = "test query" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-4");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolCallResponse>();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Type.Should().Be("text");
        callResponse.Content[0].Text.Should().Contain("Test Topic");

        _mockWikipediaService.Verify(x => x.SearchAsync("test query"), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSections_ShouldCallService()
    {
        // Arrange
        var sectionsResult = new WikipediaSectionsResult
        {
            Title = "Test Topic",
            Sections = new List<string> { "Introduction", "History" },
            Url = "https://test.com"
        };

        _mockWikipediaService.Setup(x => x.GetSectionsAsync("test topic"))
            .ReturnsAsync(sectionsResult);

        var request = new McpRequest
        {
            Id = "test-5",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_sections",
                Arguments = new Dictionary<string, object> { ["topic"] = "test topic" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-5");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolCallResponse>();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Type.Should().Be("text");
        callResponse.Content[0].Text.Should().Contain("Test Topic");

        _mockWikipediaService.Verify(x => x.GetSectionsAsync("test topic"), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSectionContent_ShouldCallService()
    {
        // Arrange
        var contentResult = new WikipediaSectionContentResult
        {
            SectionTitle = "History",
            Content = "This is the history content"
        };

        _mockWikipediaService.Setup(x => x.GetSectionContentAsync("test topic", "History"))
            .ReturnsAsync(contentResult);

        var request = new McpRequest
        {
            Id = "test-6",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_section_content",
                Arguments = new Dictionary<string, object> 
                { 
                    ["topic"] = "test topic",
                    ["section_title"] = "History"
                }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-6");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolCallResponse>();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Type.Should().Be("text");
        callResponse.Content[0].Text.Should().Contain("History");

        _mockWikipediaService.Verify(x => x.GetSectionContentAsync("test topic", "History"), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_UnknownTool_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-7",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "unknown_tool",
                Arguments = new Dictionary<string, object>()
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-7");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolCallResponse>();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Type.Should().Be("text");
        callResponse.Content[0].Text.Should().Contain("Unknown tool: unknown_tool");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_InvalidParams_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-8",
            Method = "tools/call",
            Params = null // Invalid params
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-8");
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32602);
        response.Error.Message.Should().Be("Invalid parameters");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_MissingQueryArgument_ShouldReturnErrorContent()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-9",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_search",
                Arguments = new Dictionary<string, object>() // Missing query
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-9");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolCallResponse>();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Type.Should().Be("text");
        callResponse.Content[0].Text.Should().Be("Error: query parameter is required");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_ServiceException_ShouldReturnInternalError()
    {
        // Arrange
        _mockWikipediaService.Setup(x => x.SearchAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Service failed"));

        var request = new McpRequest
        {
            Id = "test-10",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_search",
                Arguments = new Dictionary<string, object> { ["query"] = "test" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-10");
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32603);
        response.Error.Message.Should().Be("Internal error");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSearchReturnsNull_ShouldReturnNoResultsMessage()
    {
        // Arrange
        _mockWikipediaService.Setup(x => x.SearchAsync("nonexistent"))
            .ReturnsAsync((WikipediaSearchResult?)null);

        var request = new McpRequest
        {
            Id = "test-11",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_search",
                Arguments = new Dictionary<string, object> { ["query"] = "nonexistent" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-11");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolCallResponse>();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Type.Should().Be("text");
        callResponse.Content[0].Text.Should().Be("No Wikipedia page found for query: nonexistent");
    }

    /// <summary>
    /// Helper method to invoke the private HandleRequest method using reflection
    /// </summary>
    private async Task<McpResponse> InvokeHandleRequest(McpRequest request)
    {
        var method = typeof(McpServerService).GetMethod("HandleRequest", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        method.Should().NotBeNull("HandleRequest method should exist");
        
        var task = (Task<McpResponse>)method!.Invoke(_service, new object[] { request })!;
        return await task;
    }

    // PRIORITY 3: Additional comprehensive tests for missing coverage

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSections_ShouldCallWikipediaService()
    {
        // Arrange
        var sectionsResult = new WikipediaSectionsResult
        {
            Title = "Machine Learning",
            Sections = new List<string> { "Introduction", "History", "Applications" },
            Url = "https://en.wikipedia.org/wiki/Machine_Learning"
        };

        _mockWikipediaService.Setup(x => x.GetSectionsAsync("machine learning"))
            .ReturnsAsync(sectionsResult);

        var request = new McpRequest
        {
            Id = "test-sections",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_sections",
                Arguments = new Dictionary<string, object> { ["topic"] = "machine learning" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-sections");
        response.Error.Should().BeNull();
        response.Result.Should().BeOfType<McpToolCallResponse>();

        var callResponse = response.Result as McpToolCallResponse;
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Contain("Machine Learning");
        callResponse.Content[0].Text.Should().Contain("Introduction");
        
        _mockWikipediaService.Verify(x => x.GetSectionsAsync("machine learning"), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSections_NoResults_ShouldReturnNotFoundMessage()
    {
        // Arrange
        _mockWikipediaService.Setup(x => x.GetSectionsAsync("nonexistent"))
            .ReturnsAsync((WikipediaSectionsResult?)null);

        var request = new McpRequest
        {
            Id = "test-sections-null",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_sections",
                Arguments = new Dictionary<string, object> { ["topic"] = "nonexistent" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse!.Content[0].Text.Should().Be("No Wikipedia page found for topic: nonexistent");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSectionContent_ShouldCallWikipediaService()
    {
        // Arrange
        var sectionContent = new WikipediaSectionContentResult
        {
            SectionTitle = "History",
            Content = "This section contains historical information about the topic..."
        };

        _mockWikipediaService.Setup(x => x.GetSectionContentAsync("ai", "History"))
            .ReturnsAsync(sectionContent);

        var request = new McpRequest
        {
            Id = "test-section-content",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_section_content",
                Arguments = new Dictionary<string, object> 
                { 
                    ["topic"] = "ai",
                    ["section_title"] = "History"  // Fixed parameter name
                }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-section-content");
        response.Error.Should().BeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Contain("History");
        callResponse.Content[0].Text.Should().Contain("historical information");
        
        _mockWikipediaService.Verify(x => x.GetSectionContentAsync("ai", "History"), Times.Once);
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSectionContent_NoResults_ShouldReturnNotFoundMessage()
    {
        // Arrange
        _mockWikipediaService.Setup(x => x.GetSectionContentAsync("test", "NonExistent"))
            .ReturnsAsync((WikipediaSectionContentResult?)null);

        var request = new McpRequest
        {
            Id = "test-section-content-null",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_section_content",
                Arguments = new Dictionary<string, object> 
                { 
                    ["topic"] = "test",
                    ["sectionTitle"] = "NonExistent"
                }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse!.Content[0].Text.Should().Be("Error: section_title parameter is required");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_UnknownTool_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-unknown",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "unknown_tool",
                Arguments = new Dictionary<string, object> { ["param"] = "value" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-unknown");
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Be("Unknown tool: unknown_tool");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_MissingArguments_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-missing-args",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_search",
                Arguments = new Dictionary<string, object>() // Empty arguments
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-missing-args");
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Be("Error: query parameter is required");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSectionContentMissingTopic_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-missing-topic",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_section_content",
                Arguments = new Dictionary<string, object> { ["sectionTitle"] = "History" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Be("Error: topic parameter is required");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSectionContentMissingSectionTitle_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-missing-section",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_section_content",
                Arguments = new Dictionary<string, object> { ["topic"] = "ai" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Be("Error: section_title parameter is required");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_WikipediaSectionsMissingTopic_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-missing-topic-sections",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_sections",
                Arguments = new Dictionary<string, object>() // Empty arguments
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Be("Error: topic parameter is required");
    }

    [Fact]
    public async Task HandleRequest_ToolsCall_ServiceException_ShouldReturnError()
    {
        // Arrange
        _mockWikipediaService.Setup(x => x.SearchAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Wikipedia API error"));

        var request = new McpRequest
        {
            Id = "test-exception",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "wikipedia_search",
                Arguments = new Dictionary<string, object> { ["query"] = "test" }
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-exception");
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32603);
        response.Error.Message.Should().Be("Internal error");
    }

    [Fact]
    public async Task HandleRequest_UnknownMethodName_ShouldReturnMethodNotFoundError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-unknown-method",
            Method = "unknown/method",
            Params = null
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-unknown-method");
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32601);
        response.Error.Message.Should().Be("Method not found: unknown/method");
    }

    [Fact]
    public async Task HandleRequest_NullParams_ShouldReturnInvalidParamsError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-null-params",
            Method = "tools/call",
            Params = null
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be("test-null-params");
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32602);
        response.Error.Message.Should().Be("Invalid parameters");
    }

    [Fact]
    public async Task HandleRequest_EmptyToolName_ShouldReturnError()
    {
        // Arrange
        var request = new McpRequest
        {
            Id = "test-empty-tool",
            Method = "tools/call",
            Params = new McpToolCallRequest
            {
                Name = "",
                Arguments = new Dictionary<string, object>()
            }
        };

        // Act
        var response = await InvokeHandleRequest(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        
        var callResponse = response.Result as McpToolCallResponse;
        callResponse.Should().NotBeNull();
        callResponse!.Content.Should().HaveCount(1);
        callResponse.Content[0].Text.Should().Be("Unknown tool: ");
    }
}