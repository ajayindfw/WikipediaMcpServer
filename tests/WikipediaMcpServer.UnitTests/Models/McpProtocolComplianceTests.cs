using FluentAssertions;
using System.Text.Json;
using WikipediaMcpServer.Models;
using Xunit;

namespace WikipediaMcpServer.UnitTests.Models;

/// <summary>
/// Additional unit tests specifically for MCP protocol compliance features
/// </summary>
public class McpProtocolComplianceTests
{
    [Theory]
    [InlineData("2024-11-05")]
    [InlineData("2025-06-18")]
    [InlineData("1.0.0")] // Test unsupported version
    public void McpInitializeRequest_ShouldSupportMultipleProtocolVersions(string protocolVersion)
    {
        // Arrange & Act
        var request = new McpInitializeRequest
        {
            ProtocolVersion = protocolVersion
        };

        // Assert
        request.ProtocolVersion.Should().Be(protocolVersion);
    }

    [Fact]
    public void McpInitializeResponse_ShouldDefaultToLegacyProtocolVersion()
    {
        // Arrange & Act
        var response = new McpInitializeResponse();

        // Assert
        response.ProtocolVersion.Should().Be("2024-11-05", "Should default to legacy version for backward compatibility");
    }

    [Fact]
    public void McpServerCapabilities_ShouldIncludeAllRequiredCapabilities()
    {
        // Arrange & Act
        var capabilities = new McpServerCapabilities();

        // Assert
        capabilities.Tools.Should().NotBeNull("Tools capability is required by MCP specification");
        capabilities.Resources.Should().NotBeNull("Resources capability should be declared");
        capabilities.Logging.Should().NotBeNull("Logging capability should be declared");
        
        // Verify default values
        capabilities.Tools.ListChanged.Should().BeFalse("Tools list should not indicate dynamic changes by default");
    }

    [Theory]
    [InlineData("2024-11-05", "wikipedia-mcp-dotnet-server")]
    [InlineData("2025-06-18", "wikipedia-mcp-dotnet-server")]
    public void McpInitializeResponse_ShouldIncludeServerInfoForAllProtocolVersions(string protocolVersion, string expectedServerName)
    {
        // Arrange & Act
        var response = new McpInitializeResponse
        {
            ProtocolVersion = protocolVersion
        };

        // Assert
        response.ServerInfo.Should().NotBeNull("Server info is required by MCP specification");
        response.ServerInfo.Name.Should().Be(expectedServerName, "Server name should match expected default");
        response.ServerInfo.Version.Should().NotBeNullOrEmpty("Server version is required");
    }

    [Fact]
    public void McpClientInfo_ShouldSupportOptionalFields()
    {
        // Arrange
        var clientInfo = new McpClientInfo
        {
            Name = "Test MCP Client",
            Version = "2.1.0"
        };

        // Act & Assert
        clientInfo.Name.Should().Be("Test MCP Client");
        clientInfo.Version.Should().Be("2.1.0");
    }

    [Fact]
    public void McpRequest_ShouldSerializeWithJsonRpcCompliance()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "initialize"
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<McpRequest>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JsonRpc.Should().Be("2.0", "Must comply with JSON-RPC 2.0 specification");
        // Handle JsonElement for object? Id property
        if (deserialized.Id is JsonElement idElement)
        {
            idElement.GetInt32().Should().Be(1);
        }
        else
        {
            deserialized.Id.Should().Be(1);
        }
        deserialized.Method.Should().Be("initialize");
    }

    [Fact]
    public void McpResponse_ShouldSerializeWithJsonRpcCompliance()
    {
        // Arrange
        var response = new McpResponse
        {
            JsonRpc = "2.0",
            Id = 1,
            Result = new { test = "value" }
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<McpResponse>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JsonRpc.Should().Be("2.0", "Must comply with JSON-RPC 2.0 specification");
        // Handle JsonElement for object? Id property
        if (deserialized.Id is JsonElement idElement)
        {
            idElement.GetInt32().Should().Be(1);
        }
        else
        {
            deserialized.Id.Should().Be(1);
        }
        deserialized.Result.Should().NotBeNull();
    }

    [Fact]
    public void McpErrorResponse_ShouldCompleteWithJsonRpcSpecification()
    {
        // Arrange
        var errorResponse = new McpErrorResponse
        {
            JsonRpc = "2.0",
            Id = 1,
            Error = new McpError
            {
                Code = -32601,
                Message = "Method not found"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(errorResponse);
        var deserialized = JsonSerializer.Deserialize<McpErrorResponse>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JsonRpc.Should().Be("2.0");
        // Handle JsonElement for object? Id property
        if (deserialized.Id is JsonElement idElement)
        {
            idElement.GetInt32().Should().Be(1);
        }
        else
        {
            deserialized.Id.Should().Be(1);
        }
        deserialized.Error.Should().NotBeNull();
        deserialized.Error.Code.Should().Be(-32601);
        deserialized.Error.Message.Should().Be("Method not found");
    }

    [Theory]
    [InlineData("initialize")]
    [InlineData("tools/list")]
    [InlineData("tools/call")]
    [InlineData("notifications/initialized")]
    public void McpRequest_ShouldSupportAllStandardMethods(string method)
    {
        // Arrange & Act
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = method
        };

        // Assert
        request.Method.Should().Be(method);
        request.JsonRpc.Should().Be("2.0");
    }

    [Fact]
    public void McpToolsCapability_ShouldSupportDynamicListChanges()
    {
        // Arrange & Act
        var capability = new McpToolsCapability
        {
            ListChanged = true
        };

        // Assert
        capability.ListChanged.Should().BeTrue("Should support dynamic tool list changes");
    }

    [Fact]
    public void McpResourcesCapability_ShouldBeInitializable()
    {
        // Arrange & Act
        var capability = new McpResourcesCapability();

        // Assert
        capability.Should().NotBeNull("Resources capability should be constructible");
    }

    [Fact]
    public void McpLoggingCapability_ShouldBeInitializable()
    {
        // Arrange & Act
        var capability = new McpLoggingCapability();

        // Assert
        capability.Should().NotBeNull("Logging capability should be constructible");
    }
}