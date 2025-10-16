using FluentAssertions;
using System.Text.Json;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.ServiceTests;

public class McpProtocolSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public McpProtocolSerializationTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public void McpRequest_ShouldSerializeWithCorrectJsonRpcFields()
    {
        // Arrange
        var request = new McpRequest
        {
            JsonRpc = "2.0",
            Id = 123,
            Method = "test/method",
            Params = new { testParam = "value" }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        jsonDocument.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        jsonDocument.RootElement.GetProperty("id").GetInt32().Should().Be(123);
        jsonDocument.RootElement.GetProperty("method").GetString().Should().Be("test/method");
        jsonDocument.RootElement.GetProperty("params").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void McpResponse_ShouldSerializeWithCorrectJsonRpcFields()
    {
        // Arrange
        var response = new McpResponse
        {
            JsonRpc = "2.0",
            Id = 456,
            Result = new { success = true },
            Error = null
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        jsonDocument.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        jsonDocument.RootElement.GetProperty("id").GetInt32().Should().Be(456);
        jsonDocument.RootElement.GetProperty("result").ValueKind.Should().Be(JsonValueKind.Object);
        jsonDocument.RootElement.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void McpErrorResponse_ShouldSerializeWithCorrectErrorStructure()
    {
        // Arrange
        var errorResponse = new McpErrorResponse
        {
            JsonRpc = "2.0",
            Id = 789,
            Error = new McpError
            {
                Code = -32601,
                Message = "Method not found",
                Data = new { additionalInfo = "test" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        jsonDocument.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        jsonDocument.RootElement.GetProperty("id").GetInt32().Should().Be(789);
        jsonDocument.RootElement.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Object);

        var error = jsonDocument.RootElement.GetProperty("error");
        error.GetProperty("code").GetInt32().Should().Be(-32601);
        error.GetProperty("message").GetString().Should().Be("Method not found");
        error.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void McpInitializeResponse_ShouldSerializeWithMcpProtocolCompliance()
    {
        // Arrange
        var initResponse = new McpInitializeResponse
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new McpServerCapabilities
            {
                Tools = new McpToolsCapability
                {
                    ListChanged = false
                }
            },
            ServerInfo = new McpServerInfo
            {
                Name = "wikipedia-mcp-server",
                Version = "6.0.0"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(initResponse, _jsonOptions);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        jsonDocument.RootElement.GetProperty("protocolVersion").GetString().Should().Be("2024-11-05");
        jsonDocument.RootElement.GetProperty("capabilities").ValueKind.Should().Be(JsonValueKind.Object);
        jsonDocument.RootElement.GetProperty("serverInfo").ValueKind.Should().Be(JsonValueKind.Object);

        var capabilities = jsonDocument.RootElement.GetProperty("capabilities");
        capabilities.GetProperty("tools").ValueKind.Should().Be(JsonValueKind.Object);
        capabilities.GetProperty("tools").GetProperty("listChanged").GetBoolean().Should().BeFalse();

        var serverInfo = jsonDocument.RootElement.GetProperty("serverInfo");
        serverInfo.GetProperty("name").GetString().Should().Be("wikipedia-mcp-server");
        serverInfo.GetProperty("version").GetString().Should().Be("6.0.0");
    }

    [Fact]
    public void McpToolsListResponse_ShouldSerializeWithCorrectToolStructure()
    {
        // Arrange
        var toolsResponse = new McpToolsListResponse
        {
            Tools = new List<McpTool>
            {
                new McpTool
                {
                    Name = "test_tool",
                    Description = "A test tool",
                    InputSchema = new McpToolInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, McpProperty>
                        {
                            ["param1"] = new McpProperty
                            {
                                Type = "string",
                                Description = "Test parameter"
                            }
                        },
                        Required = new List<string> { "param1" }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(toolsResponse, _jsonOptions);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        jsonDocument.RootElement.GetProperty("tools").ValueKind.Should().Be(JsonValueKind.Array);
        var tools = jsonDocument.RootElement.GetProperty("tools").EnumerateArray().ToArray();
        
        tools.Should().HaveCount(1);
        var tool = tools[0];
        
        tool.GetProperty("name").GetString().Should().Be("test_tool");
        tool.GetProperty("description").GetString().Should().Be("A test tool");
        tool.GetProperty("inputSchema").ValueKind.Should().Be(JsonValueKind.Object);

        var inputSchema = tool.GetProperty("inputSchema");
        inputSchema.GetProperty("type").GetString().Should().Be("object");
        inputSchema.GetProperty("properties").ValueKind.Should().Be(JsonValueKind.Object);
        inputSchema.GetProperty("required").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void McpToolCallResponse_ShouldSerializeWithCorrectContentStructure()
    {
        // Arrange
        var callResponse = new McpToolCallResponse
        {
            Content = new List<McpContent>
            {
                new McpContent
                {
                    Type = "text",
                    Text = "Test response content"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(callResponse, _jsonOptions);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        jsonDocument.RootElement.GetProperty("content").ValueKind.Should().Be(JsonValueKind.Array);
        var content = jsonDocument.RootElement.GetProperty("content").EnumerateArray().ToArray();
        
        content.Should().HaveCount(1);
        var textContent = content[0];
        
        textContent.GetProperty("type").GetString().Should().Be("text");
        textContent.GetProperty("text").GetString().Should().Be("Test response content");
    }

    [Theory]
    [InlineData(-32700, "Parse error")]
    [InlineData(-32600, "Invalid Request")]
    [InlineData(-32601, "Method not found")]
    [InlineData(-32602, "Invalid params")]
    [InlineData(-32603, "Internal error")]
    public void McpError_ShouldSupportStandardJsonRpcErrorCodes(int code, string message)
    {
        // Arrange
        var error = new McpError
        {
            Code = code,
            Message = message,
            Data = null
        };

        // Act
        var json = JsonSerializer.Serialize(error, _jsonOptions);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        jsonDocument.RootElement.GetProperty("code").GetInt32().Should().Be(code);
        jsonDocument.RootElement.GetProperty("message").GetString().Should().Be(message);
        jsonDocument.RootElement.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void McpRequest_ShouldDeserializeFromValidJsonRpc()
    {
        // Arrange
        var json = """
        {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": "2024-11-05",
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
        var request = JsonSerializer.Deserialize<McpRequest>(json, _jsonOptions);

        // Assert
        request.Should().NotBeNull();
        request!.JsonRpc.Should().Be("2.0");
        request.Id.Should().NotBeNull();
        request.Method.Should().Be("initialize");
        request.Params.Should().NotBeNull();
    }

    [Fact]
    public void McpResponse_ShouldDeserializeFromValidJsonRpc()
    {
        // Arrange
        var json = """
        {
            "jsonrpc": "2.0",
            "id": 1,
            "result": {
                "protocolVersion": "2024-11-05",
                "capabilities": {
                    "tools": {
                        "listChanged": false
                    }
                },
                "serverInfo": {
                    "name": "wikipedia-mcp-server",
                    "version": "6.0.0"
                }
            },
            "error": null
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<McpResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Result.Should().NotBeNull();
        response.Error.Should().BeNull();
    }

    [Fact]
    public void JsonElement_IdField_ShouldPreserveTypeInformation()
    {
        // Arrange - Test with different ID types
        var requestWithIntId = new McpRequest { JsonRpc = "2.0", Id = 123, Method = "test" };
        var requestWithStringId = new McpRequest { JsonRpc = "2.0", Id = "test-id", Method = "test" };
        var requestWithNullId = new McpRequest { JsonRpc = "2.0", Id = null, Method = "test" };

        // Act
        var intJson = JsonSerializer.Serialize(requestWithIntId, _jsonOptions);
        var stringJson = JsonSerializer.Serialize(requestWithStringId, _jsonOptions);
        var nullJson = JsonSerializer.Serialize(requestWithNullId, _jsonOptions);

        var intDeserialized = JsonSerializer.Deserialize<McpRequest>(intJson, _jsonOptions);
        var stringDeserialized = JsonSerializer.Deserialize<McpRequest>(stringJson, _jsonOptions);
        var nullDeserialized = JsonSerializer.Deserialize<McpRequest>(nullJson, _jsonOptions);

        // Assert
        intDeserialized!.Id.Should().NotBeNull();
        var intElement = (JsonElement)intDeserialized.Id!;
        intElement.ValueKind.Should().Be(JsonValueKind.Number);
        intElement.GetInt32().Should().Be(123);

        stringDeserialized!.Id.Should().NotBeNull();
        var stringElement = (JsonElement)stringDeserialized.Id!;
        stringElement.ValueKind.Should().Be(JsonValueKind.String);
        stringElement.GetString().Should().Be("test-id");

        nullDeserialized!.Id.Should().BeNull();
    }
}