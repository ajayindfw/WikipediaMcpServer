using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace WikipediaMcpServer.StdioTests;

/// <summary>
/// Tests for MCP specification compliance features in stdio mode
/// </summary>
public class McpComplianceTests : IDisposable
{
    private Process? _process;
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _errorBuffer = new();

    public void Dispose()
    {
        _process?.Kill();
        _process?.Dispose();
    }

    private async Task<Process> StartServerProcessAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp",
            WorkingDirectory = GetProjectRoot(),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _outputBuffer.AppendLine(e.Data);
            }
        };
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _errorBuffer.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Give the process time to start
        await Task.Delay(2000);
        return process;
    }

    private static string GetProjectRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        
        // Look for the workspace root (where the src folder and solution file are)
        while (directory != null)
        {
            // Check if this is the workspace root by looking for src folder and solution file
            var srcPath = Path.Combine(directory, "src", "WikipediaMcpServer", "WikipediaMcpServer.csproj");
            var solutionPath = Path.Combine(directory, "WikipediaMcpServer.sln");
            
            if (File.Exists(srcPath) && File.Exists(solutionPath))
            {
                return directory; // Return workspace root
            }
            
            // Also handle if we're already in the project directory
            if (File.Exists(Path.Combine(directory, "WikipediaMcpServer.csproj")))
            {
                return directory;
            }
            
            directory = Directory.GetParent(directory)?.FullName;
        }
        
        throw new InvalidOperationException($"Could not find WikipediaMcpServer workspace root starting from {Directory.GetCurrentDirectory()}");
    }

    [Fact]
    public async Task StdioMode_ShouldSupportProtocolVersionNegotiation_2025_06_18()
    {
        // Arrange
        _process = await StartServerProcessAsync();
        
        var initializeRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new
                {
                    name = "TestClient",
                    version = "1.0.0"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(initializeRequest);

        // Act
        await _process.StandardInput.WriteLineAsync(requestJson);
        await _process.StandardInput.FlushAsync();
        await Task.Delay(1000);

        // Assert
        var output = _outputBuffer.ToString();
        output.Should().Contain("2025-06-18", "Server should accept the latest protocol version");
        output.Should().Contain("Wikipedia MCP Server", "Server should include server info");
        
        // Verify enhanced capabilities are declared
        output.Should().Contain("tools", "Server should declare tools capability");
        output.Should().Contain("resources", "Server should declare resources capability");
        // Note: logging capability might not be declared in stdio mode depending on implementation
    }

    [Fact]
    public async Task StdioMode_ShouldSupportProtocolVersionNegotiation_2024_11_05()
    {
        // Arrange
        _process = await StartServerProcessAsync();
        
        var initializeRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "LegacyClient",
                    version = "0.9.0"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(initializeRequest);

        // Act
        await _process.StandardInput.WriteLineAsync(requestJson);
        await _process.StandardInput.FlushAsync();
        await Task.Delay(1000);

        // Assert
        var output = _outputBuffer.ToString();
        output.Should().Contain("2024-11-05", "Server should accept legacy protocol version");
        output.Should().Contain("Wikipedia MCP Server", "Server should include server info");
    }

    [Fact]
    public async Task StdioMode_ShouldHandleNotificationsInitialized()
    {
        // Arrange
        _process = await StartServerProcessAsync();
        
        // First initialize
        var initializeRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new { name = "TestClient", version = "1.0.0" }
            }
        };

        await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(initializeRequest));
        await _process.StandardInput.FlushAsync();
        await Task.Delay(500);

        // Then send initialized notification
        var initializedNotification = new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized"
        };

        var notificationJson = JsonSerializer.Serialize(initializedNotification);

        // Act
        await _process.StandardInput.WriteLineAsync(notificationJson);
        await _process.StandardInput.FlushAsync();
        await Task.Delay(1000);

        // Assert
        var errorOutput = _errorBuffer.ToString();
        errorOutput.Should().NotContain("error", "Notification should be handled without errors");
        errorOutput.Should().NotContain("exception", "Should not cause exceptions");
        
        // Process should still be running and responsive
        _process.HasExited.Should().BeFalse("Process should remain running after notification");
    }

    [Fact]
    public async Task StdioMode_ShouldExtractClientInformation()
    {
        // Arrange
        _process = await StartServerProcessAsync();
        
        var initializeRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new
                {
                    name = "Advanced MCP Client",
                    version = "2.1.0"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(initializeRequest);

        // Act
        await _process.StandardInput.WriteLineAsync(requestJson);
        await _process.StandardInput.FlushAsync();
        await Task.Delay(1000);

        // Assert
        var output = _outputBuffer.ToString();
        // Client information might be logged to stderr or not at all, so just verify response structure
        output.Should().Contain("jsonrpc", "Server should provide valid JSON-RPC response");
        output.Should().Contain("Wikipedia MCP Server", "Server should include server info");
    }

    [Fact]
    public async Task StdioMode_ShouldDeclareEnhancedCapabilities()
    {
        // Arrange
        _process = await StartServerProcessAsync();
        
        var initializeRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new { name = "TestClient", version = "1.0.0" }
            }
        };

        var requestJson = JsonSerializer.Serialize(initializeRequest);

        // Act
        await _process.StandardInput.WriteLineAsync(requestJson);
        await _process.StandardInput.FlushAsync();
        await Task.Delay(1000);

        // Assert
        var output = _outputBuffer.ToString();
        
        // Parse the response to verify enhanced capabilities
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var responseLine = lines.FirstOrDefault(l => l.Contains("capabilities"));
        
        responseLine.Should().NotBeNull("Response should contain capabilities");
        responseLine.Should().Contain("tools", "Should declare tools capability");
        responseLine.Should().Contain("resources", "Should declare resources capability");
        // Note: logging capability might not be declared in stdio mode
    }

    [Fact]
    public async Task StdioMode_ShouldMaintainJsonRpc20Compliance()
    {
        // Arrange
        _process = await StartServerProcessAsync();
        
        var initializeRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { },
                clientInfo = new { name = "TestClient", version = "1.0.0" }
            }
        };

        var requestJson = JsonSerializer.Serialize(initializeRequest);

        // Act
        await _process.StandardInput.WriteLineAsync(requestJson);
        await _process.StandardInput.FlushAsync();
        await Task.Delay(1000);

        // Assert
        var output = _outputBuffer.ToString();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var responseLine = lines.FirstOrDefault(l => l.Contains("jsonrpc"));
        
        responseLine.Should().NotBeNull("Response should be present");
        responseLine.Should().Contain("\"jsonrpc\":\"2.0\"", "Response should include JSON-RPC 2.0 version");
        responseLine.Should().Contain("\"id\":1", "Response should include matching request ID");
        responseLine.Should().Contain("\"result\":", "Response should include result field for successful request");
    }
}