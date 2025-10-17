using WikipediaMcpServer.Services;
using WikipediaMcpServer.Tools;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging.Console;
using System.Text.Json;

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
            Version = "v8.0",
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
        version = "v8.0",
        status = "running",
        framework = "Microsoft ModelContextProtocol v0.4.0-preview.2",
        endpoints = new {
            health = "/health",
            mcp = "/mcp",
            swagger = "/swagger",
            info = "/info"
        }
    });

    Console.WriteLine("🚀 Wikipedia MCP Server v8.0");
    Console.WriteLine("📊 Available at: http://localhost:5070");
    Console.WriteLine("🔧 Endpoints:");
    Console.WriteLine("   POST / - Main MCP JSON-RPC endpoint (via Microsoft SDK)");  
    Console.WriteLine("   GET  /health - Health check");
    Console.WriteLine("   GET  /info - Server info");
    Console.WriteLine("   GET  /swagger - API documentation");
    Console.WriteLine();
    Console.WriteLine("🛠️ Available Tools:");
    Console.WriteLine("   • wikipedia_search - Search Wikipedia for topics");
    Console.WriteLine("   • wikipedia_sections - Get page sections/outline");
    Console.WriteLine("   • wikipedia_section_content - Get specific section content");
    Console.WriteLine();
    Console.WriteLine("✅ Now using Official Microsoft ModelContextProtocol SDK v0.4.0-preview.2");

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
    
    var serviceProvider = services.BuildServiceProvider();
    
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
            
            Console.Error.WriteLine($"📥 Received: {line.Substring(0, Math.Min(100, line.Length))}...");
            
            try
            {
                // Parse JSON-RPC request
                var jsonDoc = JsonDocument.Parse(line);
                var root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("method", out var method))
                {
                    var methodName = method.GetString();
                    Console.Error.WriteLine($"🎯 Method: {methodName}");
                    
                    // Handle different MCP methods
                    string response = methodName switch
                    {
                        "initialize" => await HandleInitialize(root),
                        "tools/list" => await HandleToolsList(root),
                        "tools/call" => await HandleToolsCall(root, serviceProvider),
                        _ => CreateErrorResponse(root, -32601, "Method not found")
                    };
                    
                    await writer.WriteLineAsync(response);
                    Console.Error.WriteLine($"📤 Sent response");
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

static Task<string> HandleInitialize(JsonElement request)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    var response = $$"""
    {
        "jsonrpc": "2.0",
        "id": {{id}},
        "result": {
            "protocolVersion": "2024-11-05",
            "capabilities": {
                "tools": {}
            },
            "serverInfo": {
                "name": "Wikipedia MCP Server",
                "version": "8.0.0"
            }
        }
    }
    """;
    return Task.FromResult(response);
}

static Task<string> HandleToolsList(JsonElement request)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    var response = $$"""
    {
        "jsonrpc": "2.0",
        "id": {{id}},
        "result": {
            "tools": [
                {
                    "name": "wikipedia_search",
                    "description": "Search Wikipedia for topics and articles",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "query": {
                                "type": "string",
                                "description": "The search query to find Wikipedia articles"
                            }
                        },
                        "required": ["query"]
                    }
                },
                {
                    "name": "wikipedia_sections",
                    "description": "Get the sections/outline of a Wikipedia page",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "topic": {
                                "type": "string",
                                "description": "The topic/page title to get sections for"
                            }
                        },
                        "required": ["topic"]
                    }
                },
                {
                    "name": "wikipedia_section_content",
                    "description": "Get the content of a specific section from a Wikipedia page",
                    "inputSchema": {
                        "type": "object",
                        "properties": {
                            "topic": {
                                "type": "string",
                                "description": "The Wikipedia topic/page title"
                            },
                            "sectionTitle": {
                                "type": "string",
                                "description": "The title of the section to retrieve content for"
                            }
                        },
                        "required": ["topic", "sectionTitle"]
                    }
                }
            ]
        }
    }
    """;
    return Task.FromResult(response);
}

static async Task<string> HandleToolsCall(JsonElement request, IServiceProvider serviceProvider)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    
    try
    {
        var toolName = request.GetProperty("params").GetProperty("name").GetString();
        var arguments = request.GetProperty("params").GetProperty("arguments");
        
        var wikipediaService = serviceProvider.GetRequiredService<IWikipediaService>();
        
        string resultText = toolName switch
        {
            "wikipedia_search" => await WikipediaTools.SearchWikipedia(wikipediaService, arguments.GetProperty("query").GetString()!),
            "wikipedia_sections" => await WikipediaTools.GetWikipediaSections(wikipediaService, arguments.GetProperty("topic").GetString()!),
            "wikipedia_section_content" => await WikipediaTools.GetWikipediaSectionContent(
                wikipediaService,
                arguments.GetProperty("topic").GetString()!,
                arguments.GetProperty("sectionTitle").GetString()!),
            _ => $"Unknown tool: {toolName}"
        };
        
        var escapedText = JsonSerializer.Serialize(resultText);
        var response = $$"""
        {
            "jsonrpc": "2.0",
            "id": {{id}},
            "result": {
                "content": [
                    {
                        "type": "text",
                        "text": {{escapedText}}
                    }
                ]
            }
        }
        """;
        return response;
    }
    catch (Exception ex)
    {
        return CreateErrorResponse(request, -32603, $"Internal error: {ex.Message}");
    }
}

static string CreateErrorResponse(JsonElement request, int code, string message)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    return $$"""
    {
        "jsonrpc": "2.0",
        "id": {{id}},
        "error": {
            "code": {{code}},
            "message": "{{message}}"
        }
    }
    """;
}

// Make Program class accessible for integration tests
public partial class Program { }
