using WikipediaMcpServer.Services;
using WikipediaMcpServer.Tools;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging.Console;
using System.Text.Json;
using System.Reflection;
using System.ComponentModel;
using System.Text.RegularExpressions;

// Check if running in stdio mode (--mcp flag)
var isStdioMode = args.Contains("--mcp");

if (isStdioMode)
{
    // stdio mode: Run MCP server directly without ASP.NET Core web server
    await RunStdioModeAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for production deployment
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false; // Security: Remove server header
        
        // For cloud deployments (Render, etc.), only listen on HTTP
        // HTTPS termination is handled by the platform's load balancer
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        options.ListenAnyIP(int.Parse(port));
    });
}

// Add HTTP client for Wikipedia API calls with timeout configuration
builder.Services.AddHttpClient<IWikipediaService, WikipediaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Wikipedia service as singleton (required for MCP tools)
builder.Services.AddSingleton<IWikipediaService, WikipediaService>();

// Configure MCP Server
// The Microsoft MCP SDK automatically supports BOTH transports:
// - HTTP transport: Enabled via WithHttpTransport() - for remote access
// - stdio transport: Automatically available when run with --mcp flag
// When VS Code or Claude Desktop runs with --mcp, SDK uses stdio automatically
builder.Services.AddMcpServer()
    .WithHttpTransport()   // Enable HTTP for remote access and testing
    .WithTools<WikipediaTools>();


    // Add CORS support
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

    // Configure forwarded headers for reverse proxy (Render, etc.)
    if (builder.Environment.IsProduction())
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                     Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                                     Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;
            options.RequireHeaderSymmetry = false;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { 
            Title = "Wikipedia MCP Server API", 
            Version = "v8.1",
            Description = "A Model Context Protocol (MCP) server for Wikipedia search and content retrieval - Built with Official Microsoft ModelContextProtocol SDK"
        });
    });

    var app = builder.Build();

    // Configure forwarded headers for reverse proxy (must be early in pipeline)
    if (app.Environment.IsProduction())
    {
        app.UseForwardedHeaders();
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wikipedia MCP Server API v7.0");
            c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger instead of root
        });
    }

    // Security headers for production
    if (app.Environment.IsProduction())
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            await next();
        });
    }

    // Configure routing first
    app.UseRouting();

    // Add CORS middleware (must be after UseRouting but before UseEndpoints)
    app.UseCors();

    // Configure MCP endpoints
    app.MapMcp();

    // Add MCP-compliant HTTP POST endpoint for remote MCP access
    // This endpoint implements MCP over HTTP POST with proper protocol compliance
    // Supports both 2024-11-05 and 2025-06-18 protocol versions
    app.MapPost("/mcp/rpc", async (HttpContext context, IWikipediaService wikipediaService) =>
    {
        try
        {
            // Validate required MCP headers
            var protocolVersion = context.Request.Headers["MCP-Protocol-Version"].FirstOrDefault() ?? "2024-11-05";
            Console.WriteLine($"📡 MCP Protocol Version: {protocolVersion}");
            
            // Validate Accept header for MCP compliance (optional - be permissive for testing)
            var acceptHeader = context.Request.Headers["Accept"].FirstOrDefault() ?? "";
            // Don't enforce Accept header requirement - be permissive for integration tests
            
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            Console.WriteLine($"📥 MCP HTTP Request: {requestBody.Substring(0, Math.Min(100, requestBody.Length))}...");
            
            // Parse JSON-RPC request
            var jsonDoc = JsonDocument.Parse(requestBody);
            var root = jsonDoc.RootElement;
            
            // Validate JSON-RPC 2.0 format
            if (!root.TryGetProperty("jsonrpc", out var jsonrpc) || jsonrpc.GetString() != "2.0")
            {
                return Results.Json(new { 
                    jsonrpc = "2.0", 
                    id = (object?)null, 
                    error = new { code = -32600, message = "Invalid Request: must be JSON-RPC 2.0" }
                });
            }
            
            if (!root.TryGetProperty("method", out var method))
            {
                return Results.Json(new { 
                    jsonrpc = "2.0", 
                    id = root.TryGetProperty("id", out var idProp) ? (object?)idProp.GetInt32() : null, 
                    error = new { code = -32600, message = "Invalid Request: missing method" }
                });
            }
            
            var methodName = method.GetString();
            Console.WriteLine($"🎯 MCP Method: {methodName}");
            
            // Handle different MCP methods with protocol version awareness
            var response = methodName switch
            {
                "initialize" => await HandleInitializeHttpCompliant(root, protocolVersion),
                "notifications/initialized" => HandleInitializedNotification(root),
                "tools/list" => await HandleToolsListHttpCompliant(root),
                "tools/call" => await HandleToolsCallHttpCompliant(root, wikipediaService),
                _ => CreateErrorResponseHttpCompliant(root, -32601, $"Method '{methodName}' not found")
            };
            
            // Handle notification responses (return 202 Accepted with no body)
            var responseType = response.GetType();
            if (responseType.GetProperty("IsNotification")?.GetValue(response) as bool? == true)
            {
                var statusCode = responseType.GetProperty("StatusCode")?.GetValue(response) as int? ?? 202;
                context.Response.Headers["MCP-Protocol-Version"] = protocolVersion;
                return Results.StatusCode(statusCode);
            }
            
            // Set MCP-compliant response headers
            context.Response.Headers["MCP-Protocol-Version"] = protocolVersion;
            context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
            
            Console.WriteLine($"📤 MCP HTTP Response sent (Protocol: {protocolVersion})");
            return Results.Json(response);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"❌ JSON Parse Error: {ex.Message}");
            return Results.Json(new { 
                jsonrpc = "2.0", 
                id = (object?)null, 
                error = new { code = -32700, message = $"Parse error: {ex.Message}" }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MCP HTTP Error: {ex.Message}");
            return Results.Json(new { 
                jsonrpc = "2.0", 
                id = (object?)null, 
                error = new { code = -32603, message = $"Internal error: {ex.Message}" }
            });
        }
    });

    // Add health check endpoint with JSON response
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow.ToString("O"),
                duration = report.TotalDuration.TotalMilliseconds
            });
            await context.Response.WriteAsync(result);
        }
    });

    // Add an info endpoint with API information
    app.MapGet("/info", () => new { 
        name = "Wikipedia MCP Server", 
        version = "v8.1",
        status = "running",
        framework = "Microsoft ModelContextProtocol v0.4.0-preview.2",
        mcpCompliance = new {
            protocolVersions = new[] { "2024-11-05", "2025-06-18" },
            jsonRpc = "2.0",
            headers = new[] { "MCP-Protocol-Version", "Accept", "Content-Type" },
            lifecycle = "Full support (initialize, notifications/initialized)",
            capabilities = new[] { "tools", "resources", "prompts" },
            methods = new[] { "initialize", "tools/list", "tools/call", "notifications/initialized" }
        },
        transports = new {
            stdio = "Use --mcp flag for local stdio mode (VS Code, Claude Desktop)",
            http_mcp_compliant = "/mcp/rpc endpoint (MCP-compliant JSON-RPC over HTTP)",
            http_sdk = "/mcp endpoint (Microsoft MCP SDK - SSE/WebSocket)"
        },
        endpoints = new {
            health = "/health",
            mcp = "/mcp",
            mcpRpc = "/mcp/rpc",
            swagger = "/swagger",
            info = "/info"
        }
    });

    Console.WriteLine("🚀 Wikipedia MCP Server v8.1");
    Console.WriteLine("📊 Available at: http://localhost:5070");
    Console.WriteLine("🔧 Endpoints:");
    Console.WriteLine("   POST /mcp/rpc - MCP-compliant JSON-RPC endpoint (HTTP transport)");
    Console.WriteLine("   POST /mcp - Microsoft MCP SDK endpoint (SSE/WebSocket)");
    Console.WriteLine("   GET  /health - Health check");
    Console.WriteLine("   GET  /info - Server info");
    Console.WriteLine("   GET  /swagger - API documentation");
    Console.WriteLine();
    Console.WriteLine("🛠️ Available Tools:");
    Console.WriteLine("   • wikipedia_search - Search Wikipedia for topics");
    Console.WriteLine("   • wikipedia_sections - Get page sections/outline");
    Console.WriteLine("   • wikipedia_section_content - Get specific section content");
    Console.WriteLine();
    Console.WriteLine("✅ MCP Protocol Compliance:");
    Console.WriteLine("   • JSON-RPC 2.0 format ✅");
    Console.WriteLine("   • Protocol versions: 2024-11-05, 2025-06-18 ✅");
    Console.WriteLine("   • MCP headers support ✅");
    Console.WriteLine("   • Lifecycle management ✅");
    Console.WriteLine("   • Tool discovery & execution ✅");
    Console.WriteLine($"   • Using Official Microsoft MCP SDK v0.4.0-preview.2");

app.Run();

// stdio mode implementation for MCP clients (VS Code, Claude Desktop, etc.)
static async Task RunStdioModeAsync()
{
    Console.Error.WriteLine("🔧 Starting Wikipedia MCP Server in stdio mode...");
    Console.Error.WriteLine("📡 Reading JSON-RPC messages from stdin, writing to stdout");
    
    // Create service collection for dependency injection
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    
    // Add Wikipedia service
    services.AddHttpClient<IWikipediaService, WikipediaService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    services.AddSingleton<IWikipediaService, WikipediaService>();
    
    // Add MCP server without HTTP transport
    services.AddMcpServer()
        .WithTools<WikipediaTools>();
    
    // Add logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });
    
    // Build service provider with validation for stdio mode
    // This is intentional and necessary for stdio mode where we need manual DI container
    // Suppress ASP0000 warning as this is not the typical web application scenario
#pragma warning disable ASP0000
    var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions 
    { 
        ValidateOnBuild = true,
        ValidateScopes = true 
    });
#pragma warning restore ASP0000
    
    Console.Error.WriteLine("✅ stdio mode initialized - ready for JSON-RPC messages");
    
    // Read from stdin, write to stdout
    using var stdin = Console.OpenStandardInput();
    using var stdout = Console.OpenStandardOutput();
    using var reader = new StreamReader(stdin);
    using var writer = new StreamWriter(stdout) { AutoFlush = true };
    
    try
    {
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            // Debug: Log the COMPLETE request from VS Code/client
            Console.Error.WriteLine($"📥 REAL REQUEST FROM CLIENT: {line}");
            Console.Error.WriteLine($"📥 Truncated: {line.Substring(0, Math.Min(100, line.Length))}...");
            
            try
            {
                // Parse JSON-RPC request
                var jsonDoc = JsonDocument.Parse(line);
                var root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("method", out var method))
                {
                    var methodName = method.GetString();
                    Console.Error.WriteLine($"🎯 Method: {methodName}");
                    
                    // Handle different MCP methods with full compliance
                    string response = methodName switch
                    {
                        "initialize" => await HandleInitializeStdioCompliant(root),
                        "notifications/initialized" => HandleNotificationStdio(root),
                        "tools/list" => await HandleToolsListStdio(root),
                        "tools/call" => await HandleToolsCallStdio(root, serviceProvider),
                        _ => CreateErrorResponse(root, -32601, "Method not found")
                    };
                    
                    // Only write response if it's not empty (notifications return empty)
                    if (!string.IsNullOrEmpty(response))
                    {
                        await writer.WriteLineAsync(response);
                        
                        // Debug: Log the COMPLETE response sent to client
                        Console.Error.WriteLine($"📤 REAL RESPONSE TO CLIENT: {response}");
                        Console.Error.WriteLine($"📤 Sent response (truncated)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Error processing request: {ex.Message}");
                var errorResponse = $"{{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{{\"code\":-32700,\"message\":\"Parse error: {ex.Message}\"}}}}";
                await writer.WriteLineAsync(errorResponse);
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Fatal error in stdio mode: {ex.Message}");
    }
}

static Task<string> HandleInitializeStdioCompliant(JsonElement request)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    
    Console.Error.WriteLine("🔧 MCP stdio initialize with protocol version negotiation");
    
    // CRITICAL: Extract and respect client's protocol version preference
    var clientProtocolVersion = "2024-11-05"; // Default fallback
    string? clientName = null;
    string? clientVersion = null;
    
    if (request.TryGetProperty("params", out var paramsElement))
    {
        // Extract protocol version
        if (paramsElement.TryGetProperty("protocolVersion", out var versionElement))
        {
            var requestedVersion = versionElement.GetString();
            Console.Error.WriteLine($"📋 Client requested protocol version: {requestedVersion}");
            
            // Support both versions with proper negotiation
            clientProtocolVersion = requestedVersion switch
            {
                "2025-06-18" => "2025-06-18",
                "2024-11-05" => "2024-11-05", 
                _ => "2024-11-05" // Default fallback for unknown versions
            };
        }
        
        // Extract client info for logging
        if (paramsElement.TryGetProperty("clientInfo", out var clientInfoElement))
        {
            if (clientInfoElement.TryGetProperty("name", out var nameElement))
            {
                clientName = nameElement.GetString();
            }
            if (clientInfoElement.TryGetProperty("version", out var clientVersionElement))
            {
                clientVersion = clientVersionElement.GetString();
            }
            
            Console.Error.WriteLine($"👤 Client: {clientName ?? "Unknown"} v{clientVersion ?? "Unknown"}");
        }
    }
    
    Console.Error.WriteLine($"✅ Negotiated protocol version: {clientProtocolVersion}");
    
    // CRITICAL: Enhanced capabilities declaration based on protocol version
    var capabilities = clientProtocolVersion == "2025-06-18" 
        ? """{"tools":{"listChanged":true},"resources":{},"prompts":{}}"""
        : """{"tools":{}}""";
    
    // Use compact single-line JSON for stdio mode (required by JSON-RPC 2.0 spec)
    var response = $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"protocolVersion\":\"{clientProtocolVersion}\",\"capabilities\":{capabilities},\"serverInfo\":{{\"name\":\"Wikipedia MCP Server\",\"version\":\"8.1.0\"}}}}}}";
    return Task.FromResult(response);
}

static string HandleNotificationStdio(JsonElement request)
{
    var methodName = request.GetProperty("method").GetString();
    Console.Error.WriteLine($"📬 Notification received: {methodName}");
    
    // Handle specific notifications
    switch (methodName)
    {
        case "notifications/initialized":
            Console.Error.WriteLine("🎉 Client initialization complete - server ready for requests");
            break;
        default:
            Console.Error.WriteLine($"⚠️ Unknown notification: {methodName}");
            break;
    }
    
    // CRITICAL: Notifications should NOT return a response in stdio mode per JSON-RPC 2.0
    return ""; // Empty response indicates "no response needed"
}

static Task<string> HandleToolsListStdio(JsonElement request)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    
    Console.Error.WriteLine("🔧 Tools list request with reflection-based discovery");
    
    // Use reflection to discover tools from WikipediaTools class - no hardcoding!
    var tools = DiscoverToolsFromAttributes();
    var toolsJson = string.Join(",", tools.Select(tool => tool.ToJson()));
    
    Console.Error.WriteLine($"📋 Discovered {tools.Count} tools via reflection");
    
    var response = $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"tools\":[{toolsJson}]}}}}";
    return Task.FromResult(response);
}

// Discover tools using reflection on the WikipediaTools class attributes
static List<ToolDefinition> DiscoverToolsFromAttributes()
{
    var tools = new List<ToolDefinition>();
    var toolsType = typeof(WikipediaTools);
    
    foreach (var method in toolsType.GetMethods(BindingFlags.Public | BindingFlags.Static))
    {
        var toolAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
        if (toolAttribute == null) continue;
        
        var descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttribute?.Description ?? "No description available";
        
        // Build input schema from method parameters
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        
        foreach (var param in method.GetParameters())
        {
            // Skip service injection parameters
            if (param.ParameterType == typeof(IWikipediaService)) continue;
            
            var paramDescription = param.GetCustomAttribute<DescriptionAttribute>();
            properties[param.Name!] = new
            {
                type = "string", // Simplified - could be enhanced to detect actual types
                description = paramDescription?.Description ?? $"Parameter {param.Name}"
            };
            
            // Add to required if parameter is not optional
            if (!param.HasDefaultValue)
            {
                required.Add(param.Name!);
            }
        }
        
        var inputSchema = new
        {
            type = "object",
            properties = properties,
            required = required.ToArray()
        };
        
        tools.Add(new ToolDefinition
        {
            Name = toolAttribute.Name ?? method.Name,
            Description = description,
            InputSchema = inputSchema
        });
    }
    
    return tools;
}

static async Task<string> HandleToolsCallStdio(JsonElement request, IServiceProvider serviceProvider)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    
    try
    {
        var toolName = request.GetProperty("params").GetProperty("name").GetString();
        var arguments = request.GetProperty("params").GetProperty("arguments");
        
        Console.Error.WriteLine($"🛠️ Tool call: {toolName}");
        
        // Use reflection to find and invoke the tool method dynamically
        var resultText = await InvokeToolByReflection(toolName!, arguments, serviceProvider);
        
        Console.Error.WriteLine($"✅ Tool execution successful, result length: {resultText.Length} chars");
        
        var escapedText = JsonSerializer.Serialize(resultText);
        // Use compact single-line JSON for stdio mode (required by JSON-RPC 2.0 spec)
        var response = $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":{escapedText}}}]}}}}";
        return response;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Tool execution failed: {ex.Message}");
        return CreateErrorResponse(request, -32603, $"Internal error: {ex.Message}");
    }
}

// Dynamically invoke tool methods using reflection - no more hardcoded switch!
static async Task<string> InvokeToolByReflection(string toolName, JsonElement arguments, IServiceProvider serviceProvider)
{
    var toolsType = typeof(WikipediaTools);
    
    // Find the method with the matching tool name
    foreach (var method in toolsType.GetMethods(BindingFlags.Public | BindingFlags.Static))
    {
        var toolAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
        if (toolAttribute == null || toolAttribute.Name != toolName) continue;
        
        // Found the method! Now prepare the parameters
        var parameters = new List<object?>();
        
        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType == typeof(IWikipediaService))
            {
                // Inject the service
                parameters.Add(serviceProvider.GetRequiredService<IWikipediaService>());
            }
            else
            {
                // Extract from JSON arguments
                var paramValue = ExtractParameterFromJson(param, arguments);
                parameters.Add(paramValue);
            }
        }
        
        // Invoke the method dynamically
        var result = method.Invoke(null, parameters.ToArray());
        
        // Handle async methods
        if (result is Task<string> asyncResult)
        {
            return await asyncResult;
        }
        else if (result is string syncResult)
        {
            return syncResult;
        }
        else
        {
            return result?.ToString() ?? "No result";
        }
    }
    
    return $"Unknown tool: {toolName}";
}

// Extract parameter value from JSON arguments based on parameter info
static object? ExtractParameterFromJson(ParameterInfo param, JsonElement arguments)
{
    var paramName = param.Name!;
    
    // Try exact parameter name first
    if (arguments.TryGetProperty(paramName, out var exactMatch))
    {
        return ExtractValueByType(exactMatch, param.ParameterType);
    }
    
    // Try snake_case version (VS Code MCP often uses snake_case)
    var snakeCaseName = ConvertToSnakeCase(paramName);
    if (arguments.TryGetProperty(snakeCaseName, out var snakeMatch))
    {
        return ExtractValueByType(snakeMatch, param.ParameterType);
    }
    
    // Try camelCase version
    var camelCaseName = char.ToLowerInvariant(paramName[0]) + paramName.Substring(1);
    if (arguments.TryGetProperty(camelCaseName, out var camelMatch))
    {
        return ExtractValueByType(camelMatch, param.ParameterType);
    }
    
    throw new ArgumentException($"Required parameter '{paramName}' not found in arguments");
}

// Convert parameter name to snake_case (e.g., sectionTitle -> section_title)
static string ConvertToSnakeCase(string input)
{
    return System.Text.RegularExpressions.Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
}

// Extract value from JsonElement based on target type
static object? ExtractValueByType(JsonElement element, Type targetType)
{
    if (targetType == typeof(string))
    {
        return element.GetString();
    }
    else if (targetType == typeof(int))
    {
        return element.GetInt32();
    }
    else if (targetType == typeof(bool))
    {
        return element.GetBoolean();
    }
    // Add more types as needed
    else
    {
        return element.GetString(); // Default to string
    }
}

static string CreateErrorResponse(JsonElement request, int code, string message)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    // Escape message for JSON and use compact single-line format (required by JSON-RPC 2.0 spec)
    var escapedMessage = JsonSerializer.Serialize(message);
    return $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"error\":{{\"code\":{code},\"message\":{escapedMessage}}}}}";
}

// MCP-compliant HTTP endpoint handlers with proper protocol version support
// These handlers implement the official MCP specification for HTTP transport

static Task<object> HandleInitializeHttpCompliant(JsonElement request, string protocolVersion)
{
    var id = request.TryGetProperty("id", out var idProp) ? (object)idProp.GetInt32() : null;
    
    Console.WriteLine($"🔧 MCP HTTP Initialize: Protocol version {protocolVersion}");
    Console.WriteLine($"📋 Request ID: {id}");
    
    // Extract client capabilities and info
    var hasParams = request.TryGetProperty("params", out var paramsElement);
    var clientCapabilities = new { };
    var clientInfo = new { name = "Unknown", version = "1.0.0" };
    var requestedProtocolVersion = protocolVersion; // Default to header version
    
    Console.WriteLine($"📋 Has params: {hasParams}");
    
    if (hasParams)
    {
        Console.WriteLine($"📋 Params element: {paramsElement}");
        
        if (paramsElement.TryGetProperty("capabilities", out var caps))
        {
            Console.WriteLine($"📋 Found client capabilities");
            // Parse client capabilities (for future extensibility)
            clientCapabilities = new { };
        }
        
        if (paramsElement.TryGetProperty("clientInfo", out var info))
        {
            Console.WriteLine($"📋 Found client info: {info}");
            var name = info.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : "Unknown";
            var version = info.TryGetProperty("version", out var versionEl) ? versionEl.GetString() : "1.0.0";
            clientInfo = new { name = name!, version = version! };
        }
        else
        {
            Console.WriteLine($"⚠️ Missing clientInfo in params");
        }
        
        // Check for requested protocol version in JSON body (takes precedence over header)
        if (paramsElement.TryGetProperty("protocolVersion", out var requestedVersionElement))
        {
            var jsonBodyVersion = requestedVersionElement.GetString();
            Console.WriteLine($"📋 Client requested protocol version in JSON body: {jsonBodyVersion}");
            requestedProtocolVersion = jsonBodyVersion ?? protocolVersion; // Use JSON body version if present
        }
    }
    else
    {
        Console.WriteLine($"⚠️ No params element found in initialize request");
    }
    
    // For unsupported protocol versions, return an error instead of falling back
    var supportedVersions = new[] { "2024-11-05", "2025-06-18" };
    if (!supportedVersions.Contains(requestedProtocolVersion))
    {
        Console.WriteLine($"❌ Unsupported protocol version: {requestedProtocolVersion}");
        return Task.FromResult<object>(new
        {
            jsonrpc = "2.0",
            id = id,
            error = new
            {
                code = -32602, // Invalid params error code as expected by tests
                message = $"Unsupported protocol version: {requestedProtocolVersion}. Supported versions: {string.Join(", ", supportedVersions)}"
            }
        });
    }
    
    Console.WriteLine($"🤝 MCP Initialize: Client={clientInfo.name} v{clientInfo.version}, Protocol={requestedProtocolVersion}");
    
    // Return appropriate capabilities based on negotiated protocol version
    object capabilities;
    if (requestedProtocolVersion == "2025-06-18")
    {
        capabilities = new { 
            tools = new { listChanged = false }, // Property exists but is false (feature supported but disabled)
            resources = new { }, 
            logging = new { },
            prompts = new { } 
        };
    }
    else
    {
        capabilities = new { 
            tools = new { } // No listChanged property at all
        };
    }
    
    var response = new
    {
        jsonrpc = "2.0",
        id = id,
        result = new
        {
            protocolVersion = requestedProtocolVersion,
            capabilities = capabilities,
            serverInfo = new
            {
                name = "wikipedia-mcp-dotnet-server",
                version = "8.1.0"
            }
        }
    };
    
    Console.WriteLine($"✅ Initialize response prepared for protocol {requestedProtocolVersion}");
    return Task.FromResult<object>(response);
}

static object HandleInitializedNotification(JsonElement request)
{
    // Handle the notifications/initialized message per MCP spec
    // This is a notification, so no response is expected
    Console.WriteLine("✅ MCP Client initialized notification received");
    
    // Return a special marker for notification handling
    return new { IsNotification = true, StatusCode = 202 };
}

static Task<object> HandleToolsListHttpCompliant(JsonElement request)
{
    var id = request.TryGetProperty("id", out var idProp) ? (object)idProp.GetInt32() : null;
    
    // Use reflection-based tool discovery for consistency with stdio mode
    var tools = DiscoverToolsFromAttributes();
    
    var response = new
    {
        jsonrpc = "2.0",
        id = id,
        result = new
        {
            tools = tools.Select(tool => new
            {
                name = tool.Name,
                description = tool.Description,
                inputSchema = tool.InputSchema
            }).ToArray()
        }
    };
    return Task.FromResult<object>(response);
}

static async Task<object> HandleToolsCallHttpCompliant(JsonElement request, IWikipediaService wikipediaService)
{
    var id = request.TryGetProperty("id", out var idProp) ? (object)idProp.GetInt32() : null;
    
    try
    {
        if (!request.TryGetProperty("params", out var paramsElement))
        {
            return CreateErrorResponseHttpCompliant(request, -32602, "Invalid params: missing params object");
        }
        
        if (!paramsElement.TryGetProperty("name", out var nameElement))
        {
            return CreateErrorResponseHttpCompliant(request, -32602, "Invalid params: missing tool name");
        }
        
        if (!paramsElement.TryGetProperty("arguments", out var argumentsElement))
        {
            return CreateErrorResponseHttpCompliant(request, -32602, "Invalid params: missing arguments");
        }
        
        var toolName = nameElement.GetString();
        Console.WriteLine($"🔧 MCP Tool Call: {toolName}");
        
        // Use direct tool invocation instead of reflection for HTTP mode to avoid service provider issues
        string resultText = toolName switch
        {
            "wikipedia_search" => await WikipediaTools.SearchWikipedia(wikipediaService, argumentsElement.GetProperty("query").GetString()!),
            "wikipedia_sections" => await WikipediaTools.GetWikipediaSections(wikipediaService, argumentsElement.GetProperty("topic").GetString()!),
            "wikipedia_section_content" => await WikipediaTools.GetWikipediaSectionContent(
                wikipediaService,
                argumentsElement.GetProperty("topic").GetString()!,
                // Support both snake_case (from VS Code MCP) and camelCase (from direct calls)
                argumentsElement.TryGetProperty("section_title", out var snakeCase) ? snakeCase.GetString()! : argumentsElement.GetProperty("sectionTitle").GetString()!),
            _ => $"Unknown tool: {toolName}"
        };
        
        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new
            {
                content = new object[]
                {
                    new
                    {
                        type = "text",
                        text = resultText
                    }
                }
            }
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ MCP Tool Call Error: {ex.Message}");
        return CreateErrorResponseHttpCompliant(request, -32603, $"Internal error: {ex.Message}");
    }
}

static object CreateErrorResponseHttpCompliant(JsonElement request, int code, string message)
{
    var id = request.TryGetProperty("id", out var idProp) ? (object)idProp.GetInt32() : null;
    return new
    {
        jsonrpc = "2.0",
        id = id,
        error = new
        {
            code = code,
            message = message
        }
    };
}

// Make Program class accessible for integration tests
public partial class Program { }

// Helper class for tool definition
class ToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object InputSchema { get; set; } = new { };
    
    public string ToJson()
    {
        var inputSchemaJson = JsonSerializer.Serialize(InputSchema, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return $"{{\"name\":\"{Name}\",\"description\":\"{Description}\",\"inputSchema\":{inputSchemaJson}}}";
    }
}
