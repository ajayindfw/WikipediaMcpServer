using FluentAssertions;
using WikipediaMcpServer.Models;
using Xunit;

namespace WikipediaMcpServer.UnitTests.Models;

public class McpModelsTests
{
    [Fact]
    public void McpRequest_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var request = new McpRequest();

        // Assert
        request.JsonRpc.Should().Be("2.0");
        request.Id.Should().BeNull();
        request.Method.Should().Be(string.Empty);
        request.Params.Should().BeNull();
    }

    [Fact]
    public void McpResponse_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var response = new McpResponse();

        // Assert
        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().BeNull();
        response.Result.Should().BeNull();
        response.Error.Should().BeNull();
    }

    [Fact]
    public void McpError_ShouldSetCodeAndMessage()
    {
        // Arrange
        const int code = -32600;
        const string message = "Invalid Request";

        // Act
        var error = new McpError
        {
            Code = code,
            Message = message
        };

        // Assert
        error.Code.Should().Be(code);
        error.Message.Should().Be(message);
        error.Data.Should().BeNull();
    }

    [Fact]
    public void McpInitializeRequest_ShouldSetProtocolVersion()
    {
        // Arrange
        const string version = "2024-11-05";

        // Act
        var request = new McpInitializeRequest
        {
            ProtocolVersion = version
        };

        // Assert
        request.ProtocolVersion.Should().Be(version);
        request.Capabilities.Should().BeNull();
        request.ClientInfo.Should().BeNull();
    }

    [Fact]
    public void McpInitializeResponse_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var response = new McpInitializeResponse();

        // Assert
        response.ProtocolVersion.Should().Be("2024-11-05");
        response.Capabilities.Should().NotBeNull();
        response.ServerInfo.Should().NotBeNull();
        response.ServerInfo.Name.Should().Be("wikipedia-mcp-dotnet-server");
    }

    [Fact]
    public void McpTool_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var tool = new McpTool();

        // Assert
        tool.Name.Should().Be(string.Empty);
        tool.Description.Should().Be(string.Empty);
        tool.InputSchema.Should().NotBeNull();
        tool.InputSchema.Type.Should().Be("object");
    }

    [Fact]
    public void McpToolCallRequest_ShouldSetNameAndArguments()
    {
        // Arrange
        const string toolName = "wikipedia_search";
        var arguments = new Dictionary<string, object> { { "query", "test" } };

        // Act
        var request = new McpToolCallRequest
        {
            Name = toolName,
            Arguments = arguments
        };

        // Assert
        request.Name.Should().Be(toolName);
        request.Arguments.Should().NotBeNull();
        request.Arguments.Should().ContainKey("query");
        request.Arguments!["query"].Should().Be("test");
    }

    [Fact]
    public void McpContent_ShouldSetTypeAndText()
    {
        // Arrange
        const string type = "text";
        const string text = "Test content";

        // Act
        var content = new McpContent
        {
            Type = type,
            Text = text
        };

        // Assert
        content.Type.Should().Be(type);
        content.Text.Should().Be(text);
    }

    [Fact]
    public void McpToolCallResponse_ShouldInitializeContentList()
    {
        // Arrange & Act
        var response = new McpToolCallResponse();

        // Assert
        response.Content.Should().NotBeNull();
        response.Content.Should().BeEmpty();
    }

    [Fact]
    public void McpToolCallResponse_ShouldAllowAddingContent()
    {
        // Arrange
        var response = new McpToolCallResponse();
        var content = new McpContent { Type = "text", Text = "Test" };

        // Act
        response.Content.Add(content);

        // Assert
        response.Content.Should().HaveCount(1);
        response.Content.First().Should().Be(content);
    }

    // PRIORITY 3: Missing Model Coverage Tests

    [Fact]
    public void McpClientCapabilities_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var capabilities = new McpClientCapabilities();

        // Assert
        capabilities.Tools.Should().BeNull();
    }

    [Fact]
    public void McpClientCapabilities_ShouldSetTools()
    {
        // Arrange
        var toolsConfig = new { listSupported = true };

        // Act
        var capabilities = new McpClientCapabilities
        {
            Tools = toolsConfig
        };

        // Assert
        capabilities.Tools.Should().NotBeNull();
        capabilities.Tools.Should().Be(toolsConfig);
    }

    [Fact]
    public void McpClientInfo_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var clientInfo = new McpClientInfo();

        // Assert
        clientInfo.Name.Should().Be(string.Empty);
        clientInfo.Version.Should().Be(string.Empty);
    }

    [Fact]
    public void McpClientInfo_ShouldSetNameAndVersion()
    {
        // Arrange
        const string name = "Test Client";
        const string version = "1.0.0";

        // Act
        var clientInfo = new McpClientInfo
        {
            Name = name,
            Version = version
        };

        // Assert
        clientInfo.Name.Should().Be(name);
        clientInfo.Version.Should().Be(version);
    }

    [Fact]
    public void McpServerCapabilities_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var capabilities = new McpServerCapabilities();

        // Assert
        capabilities.Tools.Should().NotBeNull();
        capabilities.Tools.ListChanged.Should().BeFalse();
    }

    [Fact]
    public void McpToolsCapability_ShouldSetListChanged()
    {
        // Arrange & Act
        var capability = new McpToolsCapability
        {
            ListChanged = true
        };

        // Assert
        capability.ListChanged.Should().BeTrue();
    }

    [Fact]
    public void McpServerInfo_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var serverInfo = new McpServerInfo();

        // Assert
        serverInfo.Name.Should().Be("wikipedia-mcp-dotnet-server");
        serverInfo.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void McpServerInfo_ShouldAllowCustomValues()
    {
        // Arrange
        const string customName = "Custom Server";
        const string customVersion = "2.0.0";

        // Act
        var serverInfo = new McpServerInfo
        {
            Name = customName,
            Version = customVersion
        };

        // Assert
        serverInfo.Name.Should().Be(customName);
        serverInfo.Version.Should().Be(customVersion);
    }

    [Fact]
    public void McpToolsListResponse_ShouldInitializeWithEmptyList()
    {
        // Arrange & Act
        var response = new McpToolsListResponse();

        // Assert
        response.Tools.Should().NotBeNull();
        response.Tools.Should().BeEmpty();
    }

    [Fact]
    public void McpToolsListResponse_ShouldAllowAddingTools()
    {
        // Arrange
        var response = new McpToolsListResponse();
        var tool = new McpTool { Name = "test_tool", Description = "Test tool" };

        // Act
        response.Tools.Add(tool);

        // Assert
        response.Tools.Should().HaveCount(1);
        response.Tools.First().Should().Be(tool);
    }

    [Fact]
    public void McpToolInputSchema_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var schema = new McpToolInputSchema();

        // Assert
        schema.Type.Should().Be("object");
        schema.Properties.Should().NotBeNull();
        schema.Properties.Should().BeEmpty();
        schema.Required.Should().NotBeNull();
        schema.Required.Should().BeEmpty();
    }

    [Fact]
    public void McpToolInputSchema_ShouldAllowAddingProperties()
    {
        // Arrange
        var schema = new McpToolInputSchema();
        var property = new McpProperty { Type = "string", Description = "Test property" };

        // Act
        schema.Properties.Add("testProp", property);
        schema.Required.Add("testProp");

        // Assert
        schema.Properties.Should().ContainKey("testProp");
        schema.Properties["testProp"].Should().Be(property);
        schema.Required.Should().Contain("testProp");
    }

    [Fact]
    public void McpProperty_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var property = new McpProperty();

        // Assert
        property.Type.Should().Be(string.Empty);
        property.Description.Should().Be(string.Empty);
    }

    [Fact]
    public void McpProperty_ShouldSetTypeAndDescription()
    {
        // Arrange
        const string type = "string";
        const string description = "A test property";

        // Act
        var property = new McpProperty
        {
            Type = type,
            Description = description
        };

        // Assert
        property.Type.Should().Be(type);
        property.Description.Should().Be(description);
    }

    // Additional edge cases and complex scenarios

    [Fact]
    public void McpRequest_ShouldAllowComplexParams()
    {
        // Arrange
        var complexParams = new
        {
            name = "test",
            arguments = new Dictionary<string, object>
            {
                { "query", "artificial intelligence" },
                { "options", new { maxResults = 10 } }
            }
        };

        // Act
        var request = new McpRequest
        {
            Id = 123,
            Method = "tools/call",
            Params = complexParams
        };

        // Assert
        request.Id.Should().Be(123);
        request.Method.Should().Be("tools/call");
        request.Params.Should().Be(complexParams);
    }

    [Fact]
    public void McpResponse_ShouldAllowComplexResult()
    {
        // Arrange
        var complexResult = new
        {
            content = new[]
            {
                new { type = "text", text = "Result 1" },
                new { type = "text", text = "Result 2" }
            }
        };

        // Act
        var response = new McpResponse
        {
            Id = "test-id",
            Result = complexResult
        };

        // Assert
        response.Id.Should().Be("test-id");
        response.Result.Should().Be(complexResult);
        response.Error.Should().BeNull();
    }

    [Fact]
    public void McpResponse_ShouldAllowErrorWithData()
    {
        // Arrange
        var errorData = new { details = "Invalid tool name", code = "TOOL_NOT_FOUND" };
        var error = new McpError
        {
            Code = -32602,
            Message = "Invalid params",
            Data = errorData
        };

        // Act
        var response = new McpResponse
        {
            Id = 456,
            Error = error
        };

        // Assert
        response.Id.Should().Be(456);
        response.Result.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error.Code.Should().Be(-32602);
        response.Error.Message.Should().Be("Invalid params");
        response.Error.Data.Should().Be(errorData);
    }

    [Fact]
    public void McpInitializeRequest_ShouldAllowCompleteConfiguration()
    {
        // Arrange
        var capabilities = new McpClientCapabilities
        {
            Tools = new { listSupported = true, callSupported = true }
        };
        var clientInfo = new McpClientInfo
        {
            Name = "Test MCP Client",
            Version = "1.5.0"
        };

        // Act
        var request = new McpInitializeRequest
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = capabilities,
            ClientInfo = clientInfo
        };

        // Assert
        request.ProtocolVersion.Should().Be("2024-11-05");
        request.Capabilities.Should().Be(capabilities);
        request.ClientInfo.Should().Be(clientInfo);
        request.ClientInfo.Name.Should().Be("Test MCP Client");
        request.ClientInfo.Version.Should().Be("1.5.0");
    }

    [Fact]
    public void McpTool_ShouldAllowComplexInputSchema()
    {
        // Arrange
        var queryProperty = new McpProperty
        {
            Type = "string",
            Description = "The search query"
        };
        var limitProperty = new McpProperty
        {
            Type = "integer",
            Description = "Maximum number of results"
        };

        // Act
        var tool = new McpTool
        {
            Name = "complex_search",
            Description = "A complex search tool with multiple parameters",
            InputSchema = new McpToolInputSchema
            {
                Type = "object",
                Properties = new Dictionary<string, McpProperty>
                {
                    { "query", queryProperty },
                    { "limit", limitProperty }
                },
                Required = new List<string> { "query" }
            }
        };

        // Assert
        tool.Name.Should().Be("complex_search");
        tool.Description.Should().Be("A complex search tool with multiple parameters");
        tool.InputSchema.Properties.Should().HaveCount(2);
        tool.InputSchema.Properties["query"].Should().Be(queryProperty);
        tool.InputSchema.Properties["limit"].Should().Be(limitProperty);
        tool.InputSchema.Required.Should().Contain("query");
        tool.InputSchema.Required.Should().NotContain("limit");
    }
}