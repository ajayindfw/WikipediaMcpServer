using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace WikipediaMcpServer.StdioTests;

/// <summary>
/// Integration tests for stdio mode transport.
/// These tests spawn the actual server process with --mcp flag and communicate via stdin/stdout.
/// </summary>
public class StdioModeTests : IDisposable
{
    private readonly string _projectPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public StdioModeTests()
    {
        // Get the project path relative to test assembly
        var baseDir = AppContext.BaseDirectory;
        _projectPath = Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "WikipediaMcpServer", "WikipediaMcpServer.csproj"));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Helper to run the MCP server in stdio mode and send/receive JSON-RPC messages
    /// </summary>
    private async Task<string> RunStdioCommand(string jsonRpcRequest, int timeoutSeconds = 10)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_projectPath}\" -- --mcp",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(_projectPath)!
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var outputComplete = new TaskCompletionSource<bool>();

        var responseReceived = false;
        var jsonStarted = false;
        var braceCount = 0;
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                
                if (!responseReceived)
                {
                    // Track JSON braces to detect complete response
                    var line = e.Data.Trim();
                    
                    if (line.StartsWith("{"))
                    {
                        jsonStarted = true;
                    }
                    
                    if (jsonStarted)
                    {
                        braceCount += line.Count(c => c == '{');
                        braceCount -= line.Count(c => c == '}');
                        
                        // Response complete when braces are balanced
                        if (braceCount == 0 && line.Contains("}"))
                        {
                            responseReceived = true;
                            outputComplete.TrySetResult(true);
                        }
                    }
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait a bit for server to initialize
        await Task.Delay(1000);

        // Send the JSON-RPC request
        await process.StandardInput.WriteLineAsync(jsonRpcRequest);
        await process.StandardInput.FlushAsync();

        // Wait for response or timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
        var completedTask = await Task.WhenAny(outputComplete.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            process.Kill();
            throw new TimeoutException($"Stdio command timed out after {timeoutSeconds} seconds.\nStderr: {errorBuilder}");
        }

        // Give a moment for any additional output
        await Task.Delay(500);

        // Clean shutdown
        process.StandardInput.Close();
        
        // Wait for exit with timeout
        var exitTask = process.WaitForExitAsync();
        var timeoutExit = Task.Delay(2000);
        await Task.WhenAny(exitTask, timeoutExit);
        
        if (!process.HasExited)
        {
            process.Kill();
        }

        var output = outputBuilder.ToString();
        var stderr = errorBuilder.ToString();

        // Filter out build messages - extract only the JSON response
        // Look for lines starting with { (the JSON response)
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var jsonLines = new List<string>();
        var inJson = false;
        var braceDepth = 0;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("{") && !inJson)
            {
                inJson = true;
            }
            
            if (inJson)
            {
                jsonLines.Add(line);
                
                // Count braces to track nesting depth
                foreach (var ch in line)
                {
                    if (ch == '{') braceDepth++;
                    else if (ch == '}') braceDepth--;
                }
                
                // Complete JSON object when depth returns to zero
                if (braceDepth == 0)
                {
                    break;
                }
            }
        }
        
        return string.Join("\n", jsonLines);
    }

    [Fact]
    public async Task StdioMode_Initialize_ShouldReturnValidResponse()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // Act
        var response = await RunStdioCommand(requestJson);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().MatchRegex("\"id\"\\s*:\\s*1");
        response.Should().Contain("result");
        response.Should().Contain("protocolVersion");
        response.Should().Contain("serverInfo");
        response.Should().Contain("Wikipedia MCP Server");
    }

    [Fact]
    public async Task StdioMode_ToolsList_ShouldReturnWikipediaTools()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // Act
        var response = await RunStdioCommand(requestJson);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().MatchRegex("\"id\"\\s*:\\s*2");
        response.Should().Contain("result");
        response.Should().Contain("tools");
        response.Should().Contain("wikipedia_search");
        response.Should().Contain("wikipedia_sections");
        response.Should().Contain("wikipedia_section_content");
    }

    [Fact]
    public async Task StdioMode_WikipediaSearch_ShouldReturnResults()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "wikipedia_search",
                arguments = new
                {
                    query = "Python programming"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // Act
        var response = await RunStdioCommand(requestJson, timeoutSeconds: 15);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().MatchRegex("\"id\"\\s*:\\s*3");
        response.Should().Contain("result");
        response.Should().Contain("content");
        
        // Should contain Wikipedia search results
        response.Should().MatchRegex("(?i)python", "Response should contain information about Python");
    }

    [Fact]
    public async Task StdioMode_WikipediaSections_ShouldReturnSections()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            id = 4,
            method = "tools/call",
            @params = new
            {
                name = "wikipedia_sections",
                arguments = new
                {
                    topic = "Machine learning"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // Act
        var response = await RunStdioCommand(requestJson, timeoutSeconds: 15);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().MatchRegex("\"id\"\\s*:\\s*4");
        response.Should().Contain("result");
        response.Should().Contain("content");
        
        // Should contain section information
        response.Should().MatchRegex("(?i)(section|overview|history)", "Response should contain section information");
    }

    [Fact]
    public async Task StdioMode_WikipediaSectionContent_ShouldReturnContent()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            id = 5,
            method = "tools/call",
            @params = new
            {
                name = "wikipedia_section_content",
                arguments = new
                {
                    topic = "Artificial intelligence",
                    sectionTitle = "History"
                }
            }
        };

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // Act
        var response = await RunStdioCommand(requestJson, timeoutSeconds: 15);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().MatchRegex("\"id\"\\s*:\\s*5");
        response.Should().Contain("result");
        response.Should().Contain("content");
    }

    [Fact]
    public async Task StdioMode_InvalidMethod_ShouldReturnError()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            id = 6,
            method = "invalid/method",
            @params = new { }
        };

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // Act
        var response = await RunStdioCommand(requestJson);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().MatchRegex("\"id\"\\s*:\\s*6");
        response.Should().Contain("error");
        response.Should().Contain("-32601"); // Method not found error code
    }

    [Fact]
    public async Task StdioMode_MalformedJson_ShouldReturnParseError()
    {
        // Arrange
        var malformedRequest = "{invalid json";

        // Act
        var response = await RunStdioCommand(malformedRequest);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().Contain("error");
        response.Should().Contain("-32700"); // Parse error code
    }

    [Fact]
    public async Task StdioMode_MissingRequiredParameter_ShouldReturnError()
    {
        // Arrange - Call wikipedia_search without query parameter
        var request = new
        {
            jsonrpc = "2.0",
            id = 7,
            method = "tools/call",
            @params = new
            {
                name = "wikipedia_search",
                arguments = new { } // Missing 'query' parameter
            }
        };

        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // Act
        var response = await RunStdioCommand(requestJson);

        // Assert
        response.Should().NotBeNullOrWhiteSpace();
        response.Should().Contain("jsonrpc");
        response.Should().Contain("2.0");
        response.Should().MatchRegex("\"id\"\\s*:\\s*7");
        response.Should().Contain("error");
        
        // Should indicate an error (missing parameter causes dictionary key error or similar)
        response.Should().MatchRegex("(?i)(query|parameter|required|key|dictionary|missing)", "Error should indicate missing parameter");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
