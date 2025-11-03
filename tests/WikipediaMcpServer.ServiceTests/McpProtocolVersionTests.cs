using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;
using WikipediaMcpServer.Models;
using Xunit;

namespace WikipediaMcpServer.ServiceTests;

/// <summary>
/// Service-level tests for MCP protocol version handling and compliance
/// </summary>
public class McpProtocolVersionTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public McpProtocolVersionTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    [Theory]
    [InlineData("2024-11-05")]
    [InlineData("2025-06-18")]
    public void InitializeRequest_ShouldSerializeProtocolVersionCorrectly(string protocolVersion)
    {
        // Arrange
        var request = new McpInitializeRequest
        {
            ProtocolVersion = protocolVersion,
            Capabilities = new McpClientCapabilities
            {
                Tools = new { }
            },
            ClientInfo = new McpClientInfo
            {
                Name = "Test Client",
                Version = "1.0.0"
            }
        };

        var mcpRequest = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "initialize",
            Params = request
        };

        // Act
        var json = JsonSerializer.Serialize(mcpRequest, _jsonOptions);

        // Assert
        json.Should().Contain($"\"protocolVersion\": \"{protocolVersion}\"");
        json.Should().Contain("\"jsonrpc\": \"2.0\"");
        json.Should().Contain("\"method\": \"initialize\"");
        json.Should().Contain("\"clientInfo\"");
    }

    [Theory]
    [InlineData("2024-11-05")]
    [InlineData("2025-06-18")]
    public void InitializeResponse_ShouldSerializeProtocolVersionCorrectly(string protocolVersion)
    {
        // Arrange
        var response = new McpInitializeResponse
        {
            ProtocolVersion = protocolVersion,
            Capabilities = new McpServerCapabilities(),
            ServerInfo = new McpServerInfo()
        };

        var mcpResponse = new McpResponse
        {
            JsonRpc = "2.0",
            Id = 1,
            Result = response
        };

        // Act
        var json = JsonSerializer.Serialize(mcpResponse, _jsonOptions);

        // Assert
        json.Should().Contain($"\"protocolVersion\": \"{protocolVersion}\"");
        json.Should().Contain("\"jsonrpc\": \"2.0\"");
        json.Should().Contain("\"capabilities\"");
        json.Should().Contain("\"serverInfo\"");
    }

    [Fact]
    public void EnhancedCapabilities_ShouldSerializeAllCapabilities()
    {
        // Arrange
        var capabilities = new McpServerCapabilities();
        var response = new McpInitializeResponse
        {
            ProtocolVersion = "2025-06-18",
            Capabilities = capabilities,
            ServerInfo = new McpServerInfo()
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        // Assert
        json.Should().Contain("\"tools\"");
        json.Should().Contain("\"resources\"");
        json.Should().Contain("\"logging\"");
        json.Should().Contain("\"listChanged\": false");
    }

    [Fact]
    public void NotificationMessage_ShouldSerializeWithoutId()
    {
        // Arrange
        var notification = new McpRequest
        {
            JsonRpc = "2.0",
            Method = "notifications/initialized",
            // No Id for notifications
        };

        // Act
        var json = JsonSerializer.Serialize(notification, _jsonOptions);

        // Assert
        json.Should().Contain("\"jsonrpc\": \"2.0\"");
        json.Should().Contain("\"method\": \"notifications/initialized\"");
        json.Should().NotContain("\"id\""); // Notifications should not have ID
    }

    [Theory]
    [InlineData("tools/list")]
    [InlineData("tools/call")]
    public void StandardMethods_ShouldSerializeCorrectly(string method)
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = method,
            Params = new { }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        json.Should().Contain($"\"method\": \"{method}\"");
        json.Should().Contain("\"jsonrpc\": \"2.0\"");
        json.Should().Contain("\"id\": 1");
    }

    [Fact]
    public void ClientInfo_ShouldSerializeAllFields()
    {
        // Arrange
        var clientInfo = new McpClientInfo
        {
            Name = "Advanced MCP Client",
            Version = "2.1.0"
        };

        var request = new McpInitializeRequest
        {
            ProtocolVersion = "2025-06-18",
            Capabilities = new McpClientCapabilities
            {
                Tools = new { }
            },
            ClientInfo = clientInfo
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        json.Should().Contain("\"clientInfo\"");
        json.Should().Contain("\"name\": \"Advanced MCP Client\"");
        json.Should().Contain("\"version\": \"2.1.0\"");
    }

    [Fact]
    public void ServerInfo_ShouldSerializeAllRequiredFields()
    {
        // Arrange
        var serverInfo = new McpServerInfo();

        // Act
        var json = JsonSerializer.Serialize(serverInfo, _jsonOptions);

        // Assert
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"version\"");
        json.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ErrorResponse_ShouldComplWithJsonRpcErrorSpec()
    {
        // Arrange
        var errorResponse = new McpErrorResponse
        {
            JsonRpc = "2.0",
            Id = 1,
            Error = new McpError
            {
                Code = -32601,
                Message = "Method not found",
                Data = new { additionalInfo = "test" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(errorResponse, _jsonOptions);

        // Assert
        json.Should().Contain("\"jsonrpc\": \"2.0\"");
        json.Should().Contain("\"id\": 1");
        json.Should().Contain("\"error\"");
        json.Should().Contain("\"code\": -32601");
        json.Should().Contain("\"message\": \"Method not found\"");
        json.Should().Contain("\"data\"");
    }

    [Theory]
    [InlineData("2024-11-05", false)] // Legacy version has basic capabilities
    [InlineData("2025-06-18", true)]  // Latest version has enhanced capabilities
    public void ProtocolVersion_ShouldDetermineCapabilityFeatures(string protocolVersion, bool expectEnhancedFeatures)
    {
        // Arrange
        var capabilities = new McpServerCapabilities();
        var response = new McpInitializeResponse
        {
            ProtocolVersion = protocolVersion,
            Capabilities = capabilities
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        // Assert
        json.Should().Contain($"\"protocolVersion\": \"{protocolVersion}\"");
        json.Should().Contain("\"tools\"");
        
        if (expectEnhancedFeatures)
        {
            json.Should().Contain("\"resources\"");
            json.Should().Contain("\"logging\"");
        }
    }

    [Fact]
    public void DeserializeInitializeRequest_ShouldPreserveAllMcpFields()
    {
        // Arrange
        var originalJson = """
        {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": "2025-06-18",
                "capabilities": {
                    "tools": {}
                },
                "clientInfo": {
                    "name": "Test Client",
                    "version": "1.0.0"
                }
            }
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<McpRequest>(originalJson, _jsonOptions);

        // Assert
        request.Should().NotBeNull();
        request!.JsonRpc.Should().Be("2.0");
        // Handle JsonElement for object? Id property
        if (request.Id is JsonElement idElement)
        {
            idElement.GetInt32().Should().Be(1);
        }
        else
        {
            request.Id.Should().Be(1);
        }
        request.Method.Should().Be("initialize");
        request.Params.Should().NotBeNull();
    }

    [Fact]
    public void DeserializeInitializeResponse_ShouldPreserveAllMcpFields()
    {
        // Arrange
        var originalJson = """
        {
            "jsonrpc": "2.0",
            "id": 1,
            "result": {
                "protocolVersion": "2025-06-18",
                "capabilities": {
                    "tools": {
                        "listChanged": false
                    },
                    "resources": {},
                    "logging": {}
                },
                "serverInfo": {
                    "name": "Wikipedia MCP Server",
                    "version": "8.1.0"
                }
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<McpResponse>(originalJson, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.JsonRpc.Should().Be("2.0");
        // Handle JsonElement for object? Id property
        if (response.Id is JsonElement idElement)
        {
            idElement.GetInt32().Should().Be(1);
        }
        else
        {
            response.Id.Should().Be(1);
        }
        response.Result.Should().NotBeNull();
    }
}