# Building Production-Ready MCP Servers: A Comprehensive Guide with Real-World Wikipedia Integration

*Learn how to develop, test, and deploy Model Context Protocol (MCP) servers using .NET 8, featuring dual transport modes (stdio + HTTP), comprehensive testing strategies, and production deployment on Render.*

## Introduction

The Model Context Protocol (MCP) is revolutionizing how AI applications interact with external data sources and tools. Whether you're building for local AI clients like VS Code or Claude Desktop, or deploying enterprise-grade solutions accessible remotely, understanding MCP's transport modes and testing strategies is crucial for successful implementation.

In this comprehensive guide, we'll explore building a production-ready MCP server using a real-world example: the Wikipedia MCP Server. This project demonstrates professional .NET development practices, dual transport architecture, and enterprise-level testing approaches that you can apply to any MCP server project.

## Table of Contents

1. [Understanding MCP Transport Modes](#understanding-mcp-transport-modes)
2. [Project Architecture Overview](#project-architecture-overview)
3. [Setting Up Dual Transport Support](#setting-up-dual-transport-support)
4. [Implementing Tools with Reflection-Based Architecture](#implementing-tools-with-reflection-based-architecture)
5. [Comprehensive Testing Strategy](#comprehensive-testing-strategy)
6. [Remote Deployment and Access](#remote-deployment-and-access)
7. [Production Considerations](#production-considerations)
8. [Advanced Topics](#advanced-topics)

## Understanding MCP Transport Modes

### The Two Worlds of MCP Communication

MCP servers can communicate through two primary transport modes, each serving distinct use cases:

#### 1. stdio Transport (Local AI Integration)

**Purpose**: Direct integration with local AI clients
**Protocol**: JSON-RPC 2.0 over stdin/stdout
**Use Cases**: VS Code extensions, Claude Desktop, local development

```bash
# Running in stdio mode
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj -- --mcp
```

**Characteristics**:
- ✅ **Secure**: No network exposure
- ✅ **Fast**: Direct process communication
- ✅ **Simple**: Clean protocol separation
- ✅ **Debuggable**: stderr for logs, stdout for protocol

#### 2. HTTP Transport (Remote Access)

**Purpose**: Remote deployment and team collaboration
**Protocol**: JSON-RPC 2.0 over HTTP POST or SSE/WebSocket
**Use Cases**: Cloud deployment, team sharing, integration testing

```bash
# Running in HTTP mode (default)
dotnet run --project src/WikipediaMcpServer/WikipediaMcpServer.csproj
# Available at http://localhost:5070
```

**Characteristics**:
- 🌐 **Accessible**: Remote team access
- 🔧 **Testable**: Postman/curl integration
- 📊 **Observable**: Health checks and monitoring
- 🚀 **Scalable**: Cloud deployment ready

### Why Both Modes Matter

Many MCP implementations focus on only one transport mode, but real-world applications need both:

```mermaid
graph TB
    subgraph "Local Development"
        A[VS Code MCP] --> B[stdio mode]
        C[Claude Desktop] --> B
    end
    
    subgraph "Remote Access"
        D[Team Members] --> E[HTTP mode]
        F[CI/CD Testing] --> E
        G[Integration Tools] --> E
    end
    
    B --> H[Wikipedia MCP Server]
    E --> H
    H --> I[Wikipedia API]
```

## Project Architecture Overview

### Professional .NET Project Structure

Our Wikipedia MCP Server follows enterprise .NET patterns:

```
WikipediaMcpServer/
├── src/WikipediaMcpServer/              # Main application
│   ├── Controllers/WikipediaController.cs
│   ├── Models/McpModels.cs
│   ├── Services/WikipediaService.cs
│   ├── Tools/WikipediaTools.cs
│   └── Program.cs                       # Dual-mode entry point
├── tests/                               # Comprehensive test suite
│   ├── WikipediaMcpServer.UnitTests/    # 77 unit tests
│   ├── WikipediaMcpServer.ServiceTests/ # 70 service tests
│   ├── WikipediaMcpServer.IntegrationTests/ # 51 integration tests
│   └── WikipediaMcpServer.StdioTests/   # 8 stdio-specific tests
├── docs/                                # Documentation
└── render.yaml                          # Production deployment
```

### Key Technologies and Packages

```xml
<PackageReference Include="Microsoft.ModelContextProtocol.Server" Version="0.4.0-preview.2" />
<PackageReference Include="Microsoft.ModelContextProtocol.AspNetCore" Version="0.4.0-preview.2" />
```

The project leverages Microsoft's official MCP SDK, ensuring:
- ✅ Protocol compliance
- ✅ Future-proof updates
- ✅ Enterprise support
- ✅ Performance optimization

## Setting Up Dual Transport Support

### Entry Point Architecture

The magic happens in `Program.cs` with intelligent transport detection:

```csharp
// Transport mode detection
var isStdioMode = args.Contains("--mcp");

if (isStdioMode)
{
    // stdio mode: Direct JSON-RPC over stdin/stdout
    await RunStdioModeAsync();
    return;
}

// HTTP mode: Full ASP.NET Core web server
var builder = WebApplication.CreateBuilder(args);
// ... configure HTTP services
```

### stdio Mode Implementation

The stdio mode provides a clean, focused implementation for local AI clients:

```csharp
static async Task RunStdioModeAsync()
{
    Console.Error.WriteLine("🔧 Starting Wikipedia MCP Server in stdio mode...");
    
    // Setup dependency injection
    var services = new ServiceCollection();
    services.AddHttpClient<IWikipediaService, WikipediaService>();
    services.AddSingleton<IWikipediaService, WikipediaService>();
    services.AddMcpServer().WithTools<WikipediaTools>();
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Process JSON-RPC messages
    using var reader = new StreamReader(Console.OpenStandardInput());
    using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
    
    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(line)) continue;
        
        var response = await ProcessJsonRpcRequest(line, serviceProvider);
        await writer.WriteLineAsync(response);
    }
}
```

**Key Features**:
- **Clean Logging**: stderr for diagnostics, stdout for protocol
- **DI Integration**: Full dependency injection support
- **Error Handling**: Robust JSON-RPC error responses
- **Performance**: Direct I/O without HTTP overhead

### HTTP Mode Implementation

HTTP mode provides comprehensive web server capabilities:

```csharp
// Configure services for HTTP mode
builder.Services.AddMcpServer()
    .WithHttpTransport()           // Microsoft SDK transport
    .WithTools<WikipediaTools>();

// Custom JSON-RPC endpoint for remote access
app.MapPost("/mcp/rpc", async (JsonDocument request, IWikipediaService service) =>
{
    var root = request.RootElement;
    var method = root.GetProperty("method").GetString();
    
    var response = method switch
    {
        "initialize" => await HandleInitializeHttp(root),
        "tools/list" => await HandleToolsListHttp(root),
        "tools/call" => await HandleToolsCallHttp(root, service),
        _ => CreateErrorResponseHttp(root, -32601, "Method not found")
    };
    
    return Results.Json(response);
});
```

**Endpoints Provided**:
- `POST /mcp/rpc` - Custom JSON-RPC endpoint
- `POST /mcp` - Microsoft SDK endpoint (SSE/WebSocket)
- `GET /health` - Health monitoring
- `GET /info` - Server information
- `GET /swagger` - API documentation

## Implementing Tools with Reflection-Based Architecture

### Tool Definition Using Attributes

The Wikipedia tools leverage .NET attributes for clean, declarative definitions:

```csharp
[McpServerToolType]
public sealed class WikipediaTools
{
    [McpServerTool(Name = "wikipedia_search")]
    [Description("Search Wikipedia for topics and articles")]
    public static async Task<string> SearchWikipedia(
        IWikipediaService wikipediaService,
        [Description("The search query to find Wikipedia articles")] string query)
    {
        var result = await wikipediaService.SearchAsync(query);
        return FormatSearchResult(result, query);
    }

    [McpServerTool(Name = "wikipedia_sections")]
    [Description("Get the sections/outline of a Wikipedia page")]
    public static async Task<string> GetWikipediaSections(
        IWikipediaService wikipediaService,
        [Description("The topic/page title to get sections for")] string topic)
    {
        var result = await wikipediaService.GetSectionsAsync(topic);
        return FormatSectionsResult(result, topic);
    }

    [McpServerTool(Name = "wikipedia_section_content")]
    [Description("Get the content of a specific section from a Wikipedia page")]
    public static async Task<string> GetWikipediaSectionContent(
        IWikipediaService wikipediaService,
        [Description("The Wikipedia topic/page title")] string topic,
        [Description("The title of the section to retrieve content for")] string sectionTitle)
    {
        var result = await wikipediaService.GetSectionContentAsync(topic, sectionTitle);
        return FormatSectionContentResult(result, topic, sectionTitle);
    }
}
```

### Dynamic Tool Discovery and Execution

The server uses reflection to automatically discover and execute tools:

```csharp
static async Task<string> InvokeToolByReflection(string toolName, JsonElement arguments, IServiceProvider serviceProvider)
{
    var toolsType = typeof(WikipediaTools);
    
    // Find the method with matching tool name
    foreach (var method in toolsType.GetMethods(BindingFlags.Public | BindingFlags.Static))
    {
        var toolAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
        if (toolAttribute?.Name != toolName) continue;
        
        // Prepare parameters with dependency injection
        var parameters = new List<object?>();
        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType == typeof(IWikipediaService))
            {
                parameters.Add(serviceProvider.GetRequiredService<IWikipediaService>());
            }
            else
            {
                parameters.Add(ExtractParameterFromJson(param, arguments));
            }
        }
        
        // Dynamic invocation with async support
        var result = method.Invoke(null, parameters.ToArray());
        return result is Task<string> asyncResult ? await asyncResult : result?.ToString() ?? "";
    }
    
    throw new InvalidOperationException($"Tool not found: {toolName}");
}
```

**Benefits of This Approach**:
- ✅ **Type Safety**: Compile-time parameter validation
- ✅ **Maintainability**: No hardcoded tool lists
- ✅ **Extensibility**: Add tools by adding methods
- ✅ **DI Integration**: Automatic service injection

## Comprehensive Testing Strategy

### Four-Layer Testing Approach

Our testing strategy covers all aspects of MCP server functionality with 150 total tests across four specialized test projects:

#### 1. Unit Tests (77 tests)
**Focus**: Individual component testing and business logic validation

```csharp
[Test]
public void WikipediaSearchResult_Properties_SetCorrectly()
{
    // Arrange
    var result = new WikipediaSearchResult
    {
        Title = "Artificial Intelligence",
        Url = "https://en.wikipedia.org/wiki/Artificial_Intelligence",
        Summary = "AI is the simulation of human intelligence..."
    };

    // Assert
    result.Title.Should().Be("Artificial Intelligence");
    result.Url.Should().Be("https://en.wikipedia.org/wiki/Artificial_Intelligence");
    result.Summary.Should().StartWith("AI is the simulation");
}

[Test]
public async Task WikipediaService_SearchAsync_ValidQuery_ReturnsResult()
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var service = new WikipediaService(mockHttpClient.Object);

    // Act
    var result = await service.SearchAsync("test query");

    // Assert
    result.Should().NotBeNull();
    result.Title.Should().NotBeNullOrEmpty();
}
```

#### 2. Service Tests (31 tests)
**Focus**: Wikipedia API integration and service layer validation

```csharp
[Test]
public async Task WikipediaService_SearchAsync_RealApi_ReturnsValidResult()
{
    // Arrange
    using var httpClient = new HttpClient();
    var service = new WikipediaService(httpClient);

    // Act
    var result = await service.SearchAsync("Artificial Intelligence");

    // Assert
    result.Should().NotBeNull();
    result.Title.Should().Be("Artificial intelligence");
    result.Url.Should().Contain("wikipedia.org");
    result.Summary.Should().NotBeNullOrEmpty();
}

[Test]
public async Task WikipediaService_GetSectionsAsync_RealApi_ReturnsValidSections()
{
    // Arrange
    using var httpClient = new HttpClient();
    var service = new WikipediaService(httpClient);

    // Act
    var result = await service.GetSectionsAsync("Python (programming language)");

    // Assert
    result.Should().NotBeNull();
    result.Sections.Should().NotBeEmpty();
    result.Sections.Should().Contain(s => s.Contains("History"));
}
```

#### 3. Integration Tests (51 tests)
**Focus**: End-to-end HTTP API testing with real server instances

```csharp
public class WikipediaControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WikipediaControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task McpRpc_Initialize_ReturnsValidResponse()
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
                capabilities = new { },
                clientInfo = new { name = "test", version = "1.0" }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), 
                                      Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/mcp/rpc", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);
        
        jsonResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        jsonResponse.RootElement.GetProperty("result").GetProperty("protocolVersion").GetString()
                   .Should().Be("2024-11-05");
    }
}
```

#### 4. stdio Tests (8 tests)
**Focus**: Process-level validation of stdio transport mode

This is where our testing approach gets truly sophisticated. The stdio tests spawn real processes and validate JSON-RPC communication:

```csharp
[Test]
public async Task StdioMode_Initialize_Success()
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
            capabilities = new { },
            clientInfo = new { name = "test", version = "1.0" }
        }
    };

    // Act
    var response = await RunStdioCommand(JsonSerializer.Serialize(request));

    // Assert
    response.Should().NotBeNullOrEmpty();
    
    var jsonResponse = JsonDocument.Parse(response);
    jsonResponse.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
    jsonResponse.RootElement.GetProperty("id").GetInt32().Should().Be(1);
    
    var result = jsonResponse.RootElement.GetProperty("result");
    result.GetProperty("protocolVersion").GetString().Should().Be("2024-11-05");
    result.GetProperty("serverInfo").GetProperty("name").GetString()
          .Should().Be("Wikipedia MCP Server");
}

private async Task<string> RunStdioCommand(string input)
{
    var projectPath = Path.GetFullPath("../../../src/WikipediaMcpServer");
    
    var processInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project {projectPath} -- --mcp",
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    using var process = Process.Start(processInfo);
    if (process == null) throw new InvalidOperationException("Failed to start process");

    // Send input
    await process.StandardInput.WriteLineAsync(input);
    await process.StandardInput.FlushAsync();
    process.StandardInput.Close();

    // Read response with intelligent brace counting for complete JSON
    return await ReadCompleteJsonResponse(process.StandardOutput);
}

private async Task<string> ReadCompleteJsonResponse(StreamReader reader)
{
    var response = new StringBuilder();
    var braceCount = 0;
    var inString = false;
    var escaped = false;

    while (!reader.EndOfStream)
    {
        var ch = (char)reader.Read();
        response.Append(ch);

        if (escaped)
        {
            escaped = false;
            continue;
        }

        if (ch == '\\' && inString)
        {
            escaped = true;
            continue;
        }

        if (ch == '"')
        {
            inString = !inString;
        }
        else if (!inString)
        {
            if (ch == '{') braceCount++;
            else if (ch == '}') braceCount--;
        }

        // Complete JSON object received
        if (braceCount == 0 && response.Length > 0 && !inString)
        {
            break;
        }
    }

    return response.ToString().Trim();
}
```

**What Makes stdio Tests Special**:
- 🔧 **Real Process Execution**: Tests actual `dotnet run --mcp` command
- 📡 **True Protocol Testing**: Validates stdin/stdout JSON-RPC communication
- ⏱️ **Timeout Handling**: Prevents hanging on malformed responses
- 🧠 **Smart JSON Parsing**: Handles streaming JSON with brace counting
- 🌐 **Live API Integration**: Tests against real Wikipedia API

### Running the Test Suite

```bash
# Run all 206 tests
dotnet test

# Run specific test categories
dotnet test --filter "FullyQualifiedName~UnitTests"        # 77 tests
dotnet test --filter "FullyQualifiedName~ServiceTests"    # 70 tests
dotnet test --filter "FullyQualifiedName~IntegrationTests" # 51 tests
dotnet test --filter "FullyQualifiedName~StdioTests"      # 8 tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Expected Results** (from our CI pipeline):
```
Test summary: total: 150, failed: 0, succeeded: 150, skipped: 0
Duration: ~12s
```

## Remote Deployment and Access

### Production Architecture on Render

Our Wikipedia MCP Server is deployed on Render, demonstrating real-world cloud deployment:

**Live Server**: `https://wikipediamcpserver.onrender.com`

#### Deployment Configuration (`render.yaml`)

```yaml
services:
  - type: web
    runtime: docker
    healthCheckPath: /health
    startCommand: dotnet ./publish/WikipediaMcpServer.dll
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: MCP_MODE
        value: "false"  # HTTP mode for remote access
```

#### Production Optimizations

```csharp
// Configure Kestrel for cloud deployment
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false; // Security enhancement
        
        // Cloud platform port binding
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        options.ListenAnyIP(int.Parse(port));
    });
    
    // Configure forwarded headers for reverse proxy
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                 ForwardedHeaders.XForwardedProto |
                                 ForwardedHeaders.XForwardedHost;
        options.RequireHeaderSymmetry = false;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}
```

### Enabling Remote Access for Local AI Clients

The challenge: VS Code and Claude Desktop expect stdio communication, but our server is deployed remotely via HTTP. The solution: **mcp-http-bridge.js**

#### Bridge Architecture

```javascript
#!/usr/bin/env node

const { spawn } = require('child_process');
const { stdin, stdout, stderr } = process;

// Configuration
const REMOTE_SERVER_URL = process.env.REMOTE_SERVER_URL || 
                         'https://wikipediamcpserver.onrender.com/mcp/rpc';

// Read JSON-RPC from stdin (from VS Code)
stdin.on('data', async (data) => {
    const request = data.toString().trim();
    
    try {
        // Forward to remote HTTP server
        const response = await fetch(REMOTE_SERVER_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: request
        });
        
        const result = await response.text();
        
        // Return to stdout (to VS Code)
        stdout.write(result + '\n');
        
    } catch (error) {
        stderr.write(`Bridge error: ${error.message}\n`);
        
        // Send JSON-RPC error response
        const errorResponse = {
            jsonrpc: "2.0",
            id: null,
            error: { code: -32603, message: `Bridge error: ${error.message}` }
        };
        stdout.write(JSON.stringify(errorResponse) + '\n');
    }
});
```

#### VS Code Configuration for Remote Access

```json
{
  "servers": {
    "wikipedia-remote": {
      "command": "node",
      "args": ["/path/to/WikipediaMcpServer/mcp-http-bridge.js"],
      "description": "Remote Wikipedia MCP Server on Render",
      "env": {
        "NODE_ENV": "production"
      }
    }
  }
}
```

#### Communication Flow

```mermaid
sequenceDiagram
    participant VS as VS Code
    participant Bridge as mcp-http-bridge.js
    participant Server as Render Server
    participant API as Wikipedia API

    VS->>Bridge: JSON-RPC via stdin
    Bridge->>Server: HTTP POST /mcp/rpc
    Server->>API: REST API call
    API-->>Server: Wikipedia data
    Server-->>Bridge: JSON-RPC response
    Bridge-->>VS: JSON-RPC via stdout
```

### Testing Remote Deployment

#### Direct HTTP Testing

```bash
# Test initialization
curl -X POST https://wikipediamcpserver.onrender.com/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'

# Test tool discovery
curl -X POST https://wikipediamcpserver.onrender.com/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'

# Test Wikipedia search
curl -X POST https://wikipediamcpserver.onrender.com/mcp/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"Machine Learning"}}}'
```

#### Bridge Testing

```bash
# Test with local server
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | \
  REMOTE_SERVER_URL=http://localhost:5070/mcp/rpc \
  MCP_DEBUG=true \
  node mcp-http-bridge.js

# Test with remote server
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | \
  MCP_DEBUG=true \
  node mcp-http-bridge.js
```

## Production Considerations

### Security and Performance

#### Authentication and Authorization
While our Wikipedia server is read-only and publicly accessible, production MCP servers often require authentication:

```csharp
// Example: API key authentication
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mcp/rpc"))
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!IsValidApiKey(apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
    }
    await next();
});
```

#### Rate Limiting

```csharp
// Using AspNetCoreRateLimit
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*/mcp/rpc",
            Period = "1m",
            Limit = 60,
        }
    };
});
```

#### Monitoring and Observability

```csharp
// Health checks with detailed information
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddCheck("wikipedia_api", async () =>
    {
        using var client = new HttpClient();
        var response = await client.GetAsync("https://en.wikipedia.org/api/rest_v1/");
        return response.IsSuccessStatusCode 
            ? HealthCheckResult.Healthy("Wikipedia API is accessible")
            : HealthCheckResult.Unhealthy("Wikipedia API is down");
    });

// Structured logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddApplicationInsights(); // For Azure
    // Or builder.AddSerilog(); for Serilog
});
```

### Scaling Considerations

#### Horizontal Scaling
MCP servers are typically stateless and scale well horizontally:

```yaml
# Docker Compose scaling example
version: '3.8'
services:
  wikipedia-mcp:
    image: wikipedia-mcp-server
    ports:
      - "5070-5079:5070"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      replicas: 5
  
  nginx:
    image: nginx
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - wikipedia-mcp
```

#### Caching Strategy

```csharp
// Redis caching for Wikipedia responses
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connectionString;
});

// In WikipediaService
public async Task<WikipediaSearchResult?> SearchAsync(string query)
{
    var cacheKey = $"wikipedia:search:{query.ToLowerInvariant()}";
    var cached = await _cache.GetStringAsync(cacheKey);
    
    if (cached != null)
    {
        return JsonSerializer.Deserialize<WikipediaSearchResult>(cached);
    }
    
    var result = await FetchFromWikipediaApi(query);
    
    if (result != null)
    {
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
                                   new DistributedCacheEntryOptions
                                   {
                                       AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                                   });
    }
    
    return result;
}
```

## Advanced Topics

### Error Handling and Resilience

#### Comprehensive Error Handling

```csharp
// Global exception handling middleware
public class McpExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpExceptionMiddleware> _logger;

    public McpExceptionMiddleware(RequestDelegate next, ILogger<McpExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = (object?)null,
            error = new
            {
                code = -32603,
                message = "Internal server error",
                data = new { type = exception.GetType().Name }
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

#### Retry Policies with Polly

```csharp
// Configure Polly for resilient HTTP calls
services.AddHttpClient<IWikipediaService, WikipediaService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}
```

### Parameter Validation and Type Safety

#### Advanced Parameter Handling

```csharp
// Support for complex parameter types
static object? ExtractParameterFromJson(ParameterInfo param, JsonElement arguments)
{
    var paramName = param.Name!;
    
    // Try different naming conventions
    var possibleNames = new[]
    {
        paramName,                              // exact match
        ConvertToSnakeCase(paramName),          // snake_case
        ConvertToCamelCase(paramName),          // camelCase
        ConvertToKebabCase(paramName)           // kebab-case
    };
    
    foreach (var name in possibleNames)
    {
        if (arguments.TryGetProperty(name, out var element))
        {
            return ConvertJsonElementToType(element, param.ParameterType);
        }
    }
    
    // Check if parameter has default value
    if (param.HasDefaultValue)
    {
        return param.DefaultValue;
    }
    
    throw new ArgumentException($"Required parameter '{paramName}' not found in arguments");
}

static object? ConvertJsonElementToType(JsonElement element, Type targetType)
{
    if (targetType == typeof(string))
        return element.GetString();
    
    if (targetType == typeof(int) || targetType == typeof(int?))
        return element.GetInt32();
    
    if (targetType == typeof(bool) || targetType == typeof(bool?))
        return element.GetBoolean();
    
    if (targetType == typeof(double) || targetType == typeof(double?))
        return element.GetDouble();
        
    if (targetType.IsArray && targetType.GetElementType() == typeof(string))
    {
        return element.EnumerateArray()
                     .Select(e => e.GetString())
                     .ToArray();
    }
    
    // Handle custom objects via JSON deserialization
    if (targetType.IsClass && targetType != typeof(string))
    {
        return JsonSerializer.Deserialize(element.GetRawText(), targetType);
    }
    
    return element.GetString(); // Fallback to string
}
```

### Custom Tool Attributes

```csharp
// Custom attributes for enhanced tool metadata
[AttributeUsage(AttributeTargets.Parameter)]
public class McpParameterAttribute : Attribute
{
    public string? Pattern { get; set; }
    public string? Format { get; set; }
    public object? Minimum { get; set; }
    public object? Maximum { get; set; }
    public string[]? Enum { get; set; }
}

// Usage in tool definition
[McpServerTool(Name = "advanced_search")]
public static async Task<string> AdvancedSearch(
    IWikipediaService service,
    [Description("Search query")]
    [McpParameter(Pattern = @"^[a-zA-Z0-9\s]+$")]
    string query,
    
    [Description("Maximum results")]
    [McpParameter(Minimum = 1, Maximum = 50)]
    int maxResults = 10,
    
    [Description("Search language")]
    [McpParameter(Enum = new[] { "en", "es", "fr", "de", "it" })]
    string language = "en")
{
    // Tool implementation with validated parameters
}
```

## Conclusion

Building production-ready MCP servers requires careful consideration of transport modes, comprehensive testing strategies, and robust deployment practices. The Wikipedia MCP Server demonstrates how to:

### Key Takeaways

1. **Dual Transport Architecture**: Support both stdio (local AI clients) and HTTP (remote access) modes for maximum flexibility

2. **Comprehensive Testing**: Implement four-layer testing (Unit, Service, Integration, stdio) with 206+ tests covering all scenarios

3. **Professional .NET Patterns**: Use dependency injection, reflection-based tool discovery, and enterprise-grade error handling

4. **Production Deployment**: Deploy to cloud platforms with proper health checks, monitoring, and security considerations

5. **Remote Access Bridge**: Enable local AI clients to access remote MCP servers through intelligent protocol bridging

### Best Practices Summary

- ✅ **Start with stdio mode** for local development and AI client integration
- ✅ **Add HTTP mode** for testing, team collaboration, and production deployment
- ✅ **Test comprehensively** with real process execution and live API integration
- ✅ **Use reflection** for maintainable tool discovery and execution
- ✅ **Plan for scale** with proper caching, rate limiting, and monitoring
- ✅ **Document thoroughly** with clear setup guides for both modes

### Next Steps

The Wikipedia MCP Server provides a solid foundation for your own MCP implementations. Whether you're building tools for database access, API integration, or custom business logic, the patterns demonstrated here will help you create maintainable, testable, and production-ready MCP servers.

**Repository**: The complete source code, including all 206 tests and deployment configurations, is available at the Wikipedia MCP Server repository.

**Live Demo**: Test the production deployment at `https://wikipediamcpserver.onrender.com`

Ready to build your own MCP server? Start with our architecture, adapt the patterns to your domain, and follow the testing strategies to ensure enterprise-grade quality.

---

*This article demonstrates real-world MCP development practices using a production-deployed Wikipedia MCP Server. The project showcases dual transport modes, comprehensive testing with 206 automated tests, and enterprise deployment on Render.*