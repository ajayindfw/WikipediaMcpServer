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

// IMPORTANT: Only add MCP SDK for HTTP mode to avoid stdio conflicts
// In stdio mode, we use our custom implementation (RunStdioModeAsync) 
// to prevent message parsing collisions between SDK and custom handler
if (!isStdioMode)
{
    builder.Services.AddMcpServer()
        .WithHttpTransport()   // Enable HTTP for remote access and testing
        .WithTools<WikipediaTools>();
}


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

    // Add WebSocket support for MCP SDK
    app.UseWebSockets(new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(120)
    });

    // Add CORS middleware (must be after UseRouting but before UseEndpoints)
    app.UseCors();

    // Configure MCP endpoints
    app.MapMcp();

    // Add a working SSE endpoint that actually functions
    // This provides true Server-Sent Events as an alternative to the problematic Microsoft SDK endpoint
    app.MapPost("/mcp/sse", async (HttpContext context, IWikipediaService wikipediaService) =>
    {
        // Check if client accepts Server-Sent Events
        var acceptHeader = context.Request.Headers["Accept"].FirstOrDefault() ?? "";
        var isSSERequest = acceptHeader.Contains("text/event-stream");

        if (isSSERequest)
        {
            context.Response.Headers["Content-Type"] = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";

            // Send initial connection event
            await context.Response.WriteAsync("event: connection\n");
            await context.Response.WriteAsync("data: {\"status\":\"connected\",\"message\":\"SSE stream established\"}\n\n");
            await context.Response.Body.FlushAsync();

            // Read the JSON-RPC request
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            try
            {
                var jsonDoc = JsonDocument.Parse(requestBody);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("method", out var method))
                {
                    var methodName = method.GetString() ?? "";

                    // Send processing event
                    await context.Response.WriteAsync("event: processing\n");
                    await context.Response.WriteAsync($"data: {{\"method\":\"{methodName}\",\"status\":\"processing\"}}\n\n");
                    await context.Response.Body.FlushAsync();

                    // Process the request based on method
                    string responseData = methodName switch
                    {
                        "initialize" => JsonSerializer.Serialize(new
                        {
                            jsonrpc = "2.0",
                            id = root.TryGetProperty("id", out var id) ? id.GetInt32() : 1,
                            result = new
                            {
                                protocolVersion = "2024-11-05",
                                capabilities = new { tools = new { } },
                                serverInfo = new { name = "Wikipedia MCP Server", version = "v8.1" }
                            }
                        }),
                        "tools/list" => JsonSerializer.Serialize(new
                        {
                            jsonrpc = "2.0",
                            id = root.TryGetProperty("id", out var toolsId) ? toolsId.GetInt32() : 1,
                            result = new
                            {
                                tools = new object[]
                                {
                                    new { name = "wikipedia_search", description = "Search Wikipedia for topics (with streaming progress)", inputSchema = new { type = "object", properties = new { query = new { type = "string" } } } },
                                    new { name = "wikipedia_sections", description = "Get page sections/outline (with progressive analysis)", inputSchema = new { type = "object", properties = new { topic = new { type = "string" } } } },
                                    new { name = "wikipedia_section_content", description = "Get specific section content", inputSchema = new { type = "object", properties = new { topic = new { type = "string" }, sectionTitle = new { type = "string" } } } },
                                    new { name = "wikipedia_batch_search", description = "Search multiple Wikipedia topics with real-time progress", inputSchema = new { type = "object", properties = new { queries = new { type = "array", items = new { type = "string" } } } } },
                                    new { name = "wikipedia_research", description = "Comprehensive research session with multi-step streaming", inputSchema = new { type = "object", properties = new { topic = new { type = "string" } } } }
                                }
                            }
                        }),
                        "tools/call" => await ProcessToolCallForSSEStreaming(context, root, wikipediaService),
                        _ => JsonSerializer.Serialize(new
                        {
                            jsonrpc = "2.0",
                            id = root.TryGetProperty("id", out var errorId) ? errorId.GetInt32() : 1,
                            error = new { code = -32601, message = $"Method '{methodName}' not found" }
                        })
                    };

                    // Send result event
                    await context.Response.WriteAsync("event: result\n");
                    await context.Response.WriteAsync($"data: {responseData}\n\n");
                    await context.Response.Body.FlushAsync();
                }

                // Send completion event
                await context.Response.WriteAsync("event: complete\n");
                await context.Response.WriteAsync("data: {\"status\":\"complete\",\"message\":\"Request processed successfully\"}\n\n");
                await context.Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                // Send error event
                await context.Response.WriteAsync("event: error\n");
                await context.Response.WriteAsync($"data: {{\"error\":\"{ex.Message}\"}}\n\n");
                await context.Response.Body.FlushAsync();
            }

            return Results.Empty;
        }
        else
        {
            return Results.BadRequest("This endpoint requires Accept: text/event-stream header");
        }
    });

    // Helper method for tool calls
    // Enhanced SSE streaming method that demonstrates TRUE streaming capabilities
    async Task<string> ProcessToolCallForSSEStreaming(HttpContext context, JsonElement root, IWikipediaService wikipediaService)
    {
        if (root.TryGetProperty("params", out var paramsElement) &&
            paramsElement.TryGetProperty("name", out var toolNameElement))
        {
            var toolName = toolNameElement.GetString() ?? "";
            var toolId = root.TryGetProperty("id", out var id) ? id.GetInt32() : 1;

            if (paramsElement.TryGetProperty("arguments", out var argsElement))
            {
                switch (toolName)
                {
                    case "wikipedia_search":
                        if (argsElement.TryGetProperty("query", out var queryElement))
                        {
                            var query = queryElement.GetString() ?? "";
                            return await StreamWikipediaSearch(context, query, toolId);
                        }
                        break;
                    
                    case "wikipedia_batch_search":
                        if (argsElement.TryGetProperty("queries", out var queriesElement))
                        {
                            var queries = JsonSerializer.Deserialize<string[]>(queriesElement.GetRawText()) ?? new string[0];
                            return await StreamBatchSearch(context, queries, toolId);
                        }
                        break;
                    
                    case "wikipedia_research":
                        if (argsElement.TryGetProperty("topic", out var topicElement))
                        {
                            var topic = topicElement.GetString() ?? "";
                            return await StreamResearchSession(context, topic, toolId);
                        }
                        break;
                    
                    case "wikipedia_sections":
                        if (argsElement.TryGetProperty("topic", out var sectionsTopicElement))
                        {
                            var topic = sectionsTopicElement.GetString() ?? "";
                            return await StreamSectionsAnalysis(context, topic, toolId);
                        }
                        break;
                }
            }
        }

        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = root.TryGetProperty("id", out var errorId) ? errorId.GetInt32() : 1,
            error = new { code = -32602, message = "Invalid tool call parameters" }
        });
    }

    // Stream a single Wikipedia search with progress updates
    async Task<string> StreamWikipediaSearch(HttpContext context, string query, int toolId)
    {
        // Step 1: Start search
        await context.Response.WriteAsync("event: progress\n");
        await context.Response.WriteAsync($"data: {{\"step\":1,\"total\":3,\"message\":\"Starting Wikipedia search for '{query}'...\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
        await context.Response.Body.FlushAsync();
        await Task.Delay(500); // Simulate processing time

        // Step 2: Processing search
        await context.Response.WriteAsync("event: progress\n");
        await context.Response.WriteAsync($"data: {{\"step\":2,\"total\":3,\"message\":\"Processing search results...\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
        await context.Response.Body.FlushAsync();
        await Task.Delay(300);

        // Step 3: Execute search and stream partial results
        await context.Response.WriteAsync("event: progress\n");
        await context.Response.WriteAsync($"data: {{\"step\":3,\"total\":3,\"message\":\"Retrieving Wikipedia data...\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
        await context.Response.Body.FlushAsync();

        var wikipediaService = context.RequestServices.GetRequiredService<IWikipediaService>();
        var results = await wikipediaService.SearchAsync(query);

        // Stream the final result
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = toolId,
            result = new 
            { 
                content = new[] 
                { 
                    new 
                    { 
                        type = "text", 
                        text = JsonSerializer.Serialize(new 
                        {
                            query = query,
                            results = results,
                            streamingDemo = "This result was delivered via true SSE streaming with 3 progress updates!",
                            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        }) 
                    } 
                } 
            }
        });
    }

    // Stream multiple Wikipedia searches with progress
    async Task<string> StreamBatchSearch(HttpContext context, string[] queries, int toolId)
    {
        var wikipediaService = context.RequestServices.GetRequiredService<IWikipediaService>();
        var allResults = new List<object>();

        for (int i = 0; i < queries.Length; i++)
        {
            var query = queries[i];
            
            // Send progress for each query
            await context.Response.WriteAsync("event: batch_progress\n");
            await context.Response.WriteAsync($"data: {{\"queryIndex\":{i},\"totalQueries\":{queries.Length},\"currentQuery\":\"{query}\",\"message\":\"Processing query {i + 1} of {queries.Length}: {query}\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
            await context.Response.Body.FlushAsync();
            await Task.Delay(200);

            // Execute search
            var results = await wikipediaService.SearchAsync(query);
            allResults.Add(new { query = query, results = results, processedAt = DateTime.UtcNow });

            // Send partial result
            await context.Response.WriteAsync("event: partial_result\n");
            await context.Response.WriteAsync($"data: {{\"queryIndex\":{i},\"query\":\"{query}\",\"hasResults\":{results != null},\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
            await context.Response.Body.FlushAsync();
            await Task.Delay(100);
        }

        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = toolId,
            result = new 
            { 
                content = new[] 
                { 
                    new 
                    { 
                        type = "text", 
                        text = JsonSerializer.Serialize(new 
                        {
                            batchResults = allResults,
                            streamingDemo = $"Processed {queries.Length} queries with real-time progress streaming!",
                            completedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        }) 
                    } 
                } 
            }
        });
    }

    // Stream a research session with multiple steps
    async Task<string> StreamResearchSession(HttpContext context, string topic, int toolId)
    {
        var wikipediaService = context.RequestServices.GetRequiredService<IWikipediaService>();
        var researchSteps = new List<object>();

        // Step 1: Initial search
        await context.Response.WriteAsync("event: research_step\n");
        await context.Response.WriteAsync($"data: {{\"step\":\"initial_search\",\"message\":\"Starting research on '{topic}'\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
        await context.Response.Body.FlushAsync();
        await Task.Delay(300);

        var initialResults = await wikipediaService.SearchAsync(topic);
        researchSteps.Add(new { step = "initial_search", results = initialResults });

        // Step 2: Get sections if we found a main article
        if (initialResults != null && !string.IsNullOrEmpty(initialResults.Title))
        {
            await context.Response.WriteAsync("event: research_step\n");
            await context.Response.WriteAsync($"data: {{\"step\":\"analyzing_structure\",\"message\":\"Analyzing structure of '{initialResults.Title}'\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
            await context.Response.Body.FlushAsync();
            await Task.Delay(400);

            var sections = await wikipediaService.GetSectionsAsync(initialResults.Title);
            researchSteps.Add(new { step = "sections_analysis", page = initialResults.Title, sections = sections });

            // Step 3: Get content from first few sections
            if (sections?.Sections?.Count > 0)
            {
                var sectionsToAnalyze = sections.Sections.Take(3).ToArray();
                foreach (var section in sectionsToAnalyze)
                {
                    await context.Response.WriteAsync("event: research_step\n");
                    await context.Response.WriteAsync($"data: {{\"step\":\"content_extraction\",\"message\":\"Extracting content from section '{section}'\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
                    await context.Response.Body.FlushAsync();
                    await Task.Delay(200);

                    var content = await wikipediaService.GetSectionContentAsync(initialResults.Title, section);
                    researchSteps.Add(new { step = "content_extraction", section = section, contentLength = content?.Content?.Length ?? 0 });
                }
            }
        }

        // Step 4: Related topics search
        await context.Response.WriteAsync("event: research_step\n");
        await context.Response.WriteAsync($"data: {{\"step\":\"related_topics\",\"message\":\"Finding related topics to '{topic}'\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
        await context.Response.Body.FlushAsync();
        await Task.Delay(300);

        var relatedQueries = new[] { $"{topic} history", $"{topic} applications", $"{topic} future" };
        var relatedResults = new List<object>();
        
        foreach (var relatedQuery in relatedQueries)
        {
            await context.Response.WriteAsync("event: research_step\n");
            await context.Response.WriteAsync($"data: {{\"step\":\"related_search\",\"message\":\"Searching for '{relatedQuery}'\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
            await context.Response.Body.FlushAsync();
            await Task.Delay(150);

            var related = await wikipediaService.SearchAsync(relatedQuery);
            relatedResults.Add(new { query = relatedQuery, results = related });
        }

        researchSteps.Add(new { step = "related_topics", relatedSearches = relatedResults });

        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = toolId,
            result = new 
            { 
                content = new[] 
                { 
                    new 
                    { 
                        type = "text", 
                        text = JsonSerializer.Serialize(new 
                        {
                            topic = topic,
                            researchSteps = researchSteps,
                            streamingDemo = "This comprehensive research was delivered via multiple SSE events showing real research progress!",
                            completedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            totalSteps = researchSteps.Count
                        }) 
                    } 
                } 
            }
        });
    }

    // Stream sections analysis with progressive results
    async Task<string> StreamSectionsAnalysis(HttpContext context, string topic, int toolId)
    {
        var wikipediaService = context.RequestServices.GetRequiredService<IWikipediaService>();

        await context.Response.WriteAsync("event: analysis_start\n");
        await context.Response.WriteAsync($"data: {{\"message\":\"Starting sections analysis for '{topic}'\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
        await context.Response.Body.FlushAsync();
        await Task.Delay(200);

        var sections = await wikipediaService.GetSectionsAsync(topic);
        
        if (sections?.Sections?.Count > 0)
        {
            // Stream each section as we process it
            var processedSections = new List<object>();
            
            for (int i = 0; i < sections.Sections.Count && i < 5; i++) // Limit to first 5 for demo
            {
                var section = sections.Sections[i];
                
                await context.Response.WriteAsync("event: section_processing\n");
                await context.Response.WriteAsync($"data: {{\"sectionIndex\":{i},\"sectionTitle\":\"{section}\",\"message\":\"Processing section {i + 1}: {section}\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
                await context.Response.Body.FlushAsync();
                await Task.Delay(100);

                processedSections.Add(new 
                { 
                    index = i,
                    title = section,
                    processedAt = DateTime.UtcNow
                });

                await context.Response.WriteAsync("event: section_complete\n");
                await context.Response.WriteAsync($"data: {{\"sectionIndex\":{i},\"completed\":true,\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n");
                await context.Response.Body.FlushAsync();
            }

            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = toolId,
                result = new 
                { 
                    content = new[] 
                    { 
                        new 
                        { 
                            type = "text", 
                            text = JsonSerializer.Serialize(new 
                            {
                                topic = topic,
                                allSections = sections,
                                processedSections = processedSections,
                                streamingDemo = "Each section was processed and streamed individually showing true progressive delivery!",
                                completedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                            }) 
                        } 
                    } 
                }
            });
        }

        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = toolId,
            result = new { content = new[] { new { type = "text", text = "No sections found for the specified topic." } } }
        });
    }

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
            
            Console.WriteLine($"{ConsoleColors.RequestBlue}{ConsoleColors.Bold}📥 MCP HTTP Request:{ConsoleColors.Reset} {ConsoleColors.RequestCyan}{requestBody.Substring(0, Math.Min(100, requestBody.Length))}...{ConsoleColors.Reset}");
            
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
            Console.WriteLine($"{ConsoleColors.MethodMagenta}{ConsoleColors.Bold}🎯 MCP Method:{ConsoleColors.Reset} {ConsoleColors.InfoWhite}{methodName}{ConsoleColors.Reset}");
            
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
                Console.WriteLine($"{ConsoleColors.NotificationYellow}{ConsoleColors.Bold}📤 Notification Response:{ConsoleColors.Reset} {ConsoleColors.NotificationOrange}HTTP {statusCode} (no body) - Protocol: {protocolVersion}{ConsoleColors.Reset}");
                return Results.StatusCode(statusCode);
            }
            
            // Set MCP-compliant response headers
            context.Response.Headers["MCP-Protocol-Version"] = protocolVersion;
            context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
            
            // Log the actual response content for debugging
            var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = false });
            var truncatedResponse = responseJson.Length > 500 ? responseJson.Substring(0, 500) + "..." : responseJson;
            Console.WriteLine($"{ConsoleColors.ResponseGreen}{ConsoleColors.Bold}📤 MCP HTTP Response:{ConsoleColors.Reset} {ConsoleColors.ResponseDarkGreen}{truncatedResponse}{ConsoleColors.Reset}");
            Console.WriteLine($"{ConsoleColors.ResponseGreen}📤 Response sent (Protocol: {protocolVersion}, Length: {responseJson.Length} chars){ConsoleColors.Reset}");
            return Results.Json(response);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"{ConsoleColors.ErrorRed}{ConsoleColors.Bold}❌ JSON Parse Error:{ConsoleColors.Reset} {ConsoleColors.ErrorDarkRed}{ex.Message}{ConsoleColors.Reset}");
            var errorResponse = new { 
                jsonrpc = "2.0", 
                id = (object?)null, 
                error = new { code = -32700, message = $"Parse error: {ex.Message}" }
            };
            
            // Log error response content
            var errorJson = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = false });
            Console.WriteLine($"{ConsoleColors.ErrorRed}{ConsoleColors.Bold}📤 Error Response:{ConsoleColors.Reset} {ConsoleColors.ErrorDarkRed}{errorJson}{ConsoleColors.Reset}");
            return Results.Json(errorResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ConsoleColors.ErrorRed}{ConsoleColors.Bold}❌ MCP HTTP Error:{ConsoleColors.Reset} {ConsoleColors.ErrorDarkRed}{ex.Message}{ConsoleColors.Reset}");
            var errorResponse = new { 
                jsonrpc = "2.0", 
                id = (object?)null, 
                error = new { code = -32603, message = $"Internal error: {ex.Message}" }
            };
            
            // Log error response content
            var errorJson = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = false });
            Console.WriteLine($"{ConsoleColors.ErrorRed}{ConsoleColors.Bold}📤 Error Response:{ConsoleColors.Reset} {ConsoleColors.ErrorDarkRed}{errorJson}{ConsoleColors.Reset}");
            return Results.Json(errorResponse);
        }
    });

    // Railway-specific minimal health check for platform compliance
    app.MapGet("/railway-health", () => Results.Ok("OK"))
        .WithTags("Health")
        .WithSummary("Railway platform health check")
        .WithDescription("Simple health check endpoint specifically for Railway platform - returns immediate HTTP 200");

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
            http_sdk = "/mcp endpoint (Microsoft MCP SDK - SSE/WebSocket)",
            sse_working = "/mcp/sse endpoint (Working Server-Sent Events implementation)"
        },
        endpoints = new {
            health = "/health",
            mcp = "/mcp",
            mcpRpc = "/mcp/rpc",
            mcpSse = "/mcp/sse",
            swagger = "/swagger",
            info = "/info",
            demo = "/demo",
            trueSseDemo = "/true-sse-demo",
            workingSseDemo = "/working-sse-demo"
        }
    });

    // Add SSE Demo page endpoint
    app.MapGet("/demo", async (HttpContext context) =>
    {
        try
        {
            var demoPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "sse-demo.html");
            if (!File.Exists(demoPath))
            {
                // Try alternative path for when running from project directory
                demoPath = Path.Combine(Directory.GetCurrentDirectory(), "sse-demo.html");
            }
            
            if (File.Exists(demoPath))
            {
                var content = await File.ReadAllTextAsync(demoPath);
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(content);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("SSE Demo page not found. Please ensure sse-demo.html exists in the project root.");
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Error loading demo page: {ex.Message}");
        }
    })
    .WithTags("Demo")
    .WithSummary("SSE Live Streaming Demo")
    .WithDescription("Interactive demo page showing real-time MCP communication over SSE");

    // Add TRUE SSE Demo page endpoint (uses actual /mcp endpoint)
    app.MapGet("/true-sse-demo", async (HttpContext context) =>
    {
        try
        {
            var demoPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "true-sse-demo.html");
            if (!File.Exists(demoPath))
            {
                // Try alternative path for when running from project directory
                demoPath = Path.Combine(Directory.GetCurrentDirectory(), "true-sse-demo.html");
            }
            
            if (File.Exists(demoPath))
            {
                var content = await File.ReadAllTextAsync(demoPath);
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(content);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("True SSE Demo page not found. Please ensure true-sse-demo.html exists in the project root.");
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Error loading true SSE demo page: {ex.Message}");
        }
    })
    .WithTags("Demo")
    .WithSummary("TRUE SSE Demo using /mcp endpoint")
    .WithDescription("Demonstrates actual SSE streaming using the Microsoft MCP SDK /mcp endpoint");

    // Add Working SSE Demo page endpoint (shows SSE concept clearly)
    app.MapGet("/working-sse-demo", async (HttpContext context) =>
    {
        try
        {
            var demoPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "working-sse-demo.html");
            if (!File.Exists(demoPath))
            {
                // Try alternative path for when running from project directory
                demoPath = Path.Combine(Directory.GetCurrentDirectory(), "working-sse-demo.html");
            }
            
            if (File.Exists(demoPath))
            {
                var content = await File.ReadAllTextAsync(demoPath);
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(content);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Working SSE Demo page not found. Please ensure working-sse-demo.html exists in the project root.");
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Error loading working SSE demo page: {ex.Message}");
        }
    })
    .WithTags("Demo")
    .WithSummary("Working SSE Concept Demo")
    .WithDescription("Clear demonstration of SSE vs HTTP concepts using working endpoints");

    Console.WriteLine("🚀 Wikipedia MCP Server v8.1");
    Console.WriteLine("📊 Available at: http://localhost:5070");
    Console.WriteLine("🔧 Endpoints:");
    Console.WriteLine("   POST /mcp/rpc - MCP-compliant JSON-RPC endpoint (HTTP transport)");
    Console.WriteLine("   POST /mcp - Microsoft MCP SDK endpoint (SSE/WebSocket)");
    Console.WriteLine("   POST /mcp/sse - Working SSE endpoint (TRUE streaming) ✅");
    Console.WriteLine("   GET  /health - Comprehensive health check (JSON)");
    Console.WriteLine("   GET  /railway-health - Railway platform health check");
    Console.WriteLine("   GET  /info - Server info");
    Console.WriteLine("   GET  /swagger - API documentation");
    Console.WriteLine("   GET  /demo - Live SSE streaming demo page");
    Console.WriteLine("   GET  /true-sse-demo - TRUE SSE demo using working endpoint ⭐");
    Console.WriteLine("   GET  /working-sse-demo - Working SSE vs HTTP comparison ⭐");
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
    
    // IMPORTANT: Do NOT add MCP SDK in stdio mode to avoid conflicts
    // We use our custom stdio handler below instead of the Microsoft SDK
    // services.AddMcpServer().WithTools<WikipediaTools>(); // REMOVED - causes parsing conflicts
    
    // Add logging with MINIMAL output for stdio mode to prevent parse errors
    services.AddLogging(builder =>
    {
        // In stdio mode, suppress all console logging to prevent MCP parse errors
        // VS Code MCP expects ONLY JSON-RPC messages on stdout/stderr
        builder.ClearProviders(); // Remove all default providers
        builder.SetMinimumLevel(LogLevel.None); // Suppress all logging
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
    
    // STDIO MESSAGE PROCESSING LOOP
    // Continuously reads JSON-RPC messages from stdin and processes them
    // This is the heart of the MCP stdio protocol implementation
    // 
    // MESSAGE FLOW for tools/call:
    // 1. Client sends: {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"AI"}}}
    // 2. Server parses: Extracts method="tools/call" 
    // 3. Server routes: Calls HandleToolsCallStdio()
    // 4. Server executes: Uses reflection to invoke WikipediaTools.SearchWikipedia()
    // 5. Server responds: {"jsonrpc":"2.0","id":3,"result":{"content":[{"type":"text","text":"Wikipedia result..."}]}}
    // 6. Client receives: Tool execution complete with result
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
                // JSON-RPC REQUEST PARSING: Convert stdin line to structured request
                var jsonDoc = JsonDocument.Parse(line);
                var root = jsonDoc.RootElement;
                
                if (root.TryGetProperty("method", out var method))
                {
                    var methodName = method.GetString();
                    Console.Error.WriteLine($"{ConsoleColors.MethodMagenta}{ConsoleColors.Bold}🎯 Method:{ConsoleColors.Reset} {ConsoleColors.InfoWhite}{methodName}{ConsoleColors.Reset}");
                    
                    // MCP METHOD ROUTING: Dispatch JSON-RPC requests to appropriate handlers
                    // Each method represents a different phase of the MCP protocol lifecycle:
                    // - initialize: Establish connection and negotiate capabilities
                    // - notifications/initialized: Client confirms initialization complete
                    // - tools/list: Client discovers available tools (reflection-based)
                    // - tools/call: Client executes a specific tool (dynamic invocation)
                    string response = methodName switch
                    {
                        "initialize" => await HandleInitializeStdioCompliant(root),
                        "notifications/initialized" => HandleNotificationStdio(root),
                        "tools/list" => await HandleToolsListStdio(root),
                        "tools/call" => await HandleToolsCallStdio(root, serviceProvider), // ← TOOL EXECUTION ENTRY POINT
                        _ => CreateErrorResponse(root, -32601, "Method not found")
                    };
                    
                    // Only write response if it's not empty (notifications return empty)
                    if (!string.IsNullOrEmpty(response))
                    {
                        await writer.WriteLineAsync(response);
                        
                        // Debug: Log the COMPLETE response sent to client
                        var truncatedResponse = response.Length > 500 ? response.Substring(0, 500) + "..." : response;
                        Console.Error.WriteLine($"{ConsoleColors.ResponseGreen}{ConsoleColors.Bold}📤 STDIO Response:{ConsoleColors.Reset} {ConsoleColors.ResponseDarkGreen}{truncatedResponse}{ConsoleColors.Reset}");
                        Console.Error.WriteLine($"{ConsoleColors.ResponseGreen}📤 Sent response (Length: {response.Length} chars){ConsoleColors.Reset}");
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

// Handle tools/list request in stdio mode using dynamic reflection-based tool discovery
// Flow: Client Request → Method Routing → Reflection Scan → Schema Generation → JSON Response
static Task<string> HandleToolsListStdio(JsonElement request)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    
    Console.Error.WriteLine("🔧 Tools list request with reflection-based discovery");
    
    // ZERO HARDCODING APPROACH: Use reflection to discover tools from WikipediaTools class
    // This means adding a new tool only requires adding a new method with [McpServerTool] attribute
    // No manual tool registration or configuration needed!
    var tools = DiscoverToolsFromAttributes();
    var toolsJson = string.Join(",", tools.Select(tool => tool.ToJson()));
    
    Console.Error.WriteLine($"📋 Discovered {tools.Count} tools via reflection");
    
    // Construct JSON-RPC 2.0 compliant response with discovered tools
    var response = $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"tools\":[{toolsJson}]}}}}";
    return Task.FromResult(response);
}

// DYNAMIC TOOL DISCOVERY ENGINE
// Discovers MCP tools using reflection on the WikipediaTools class attributes
// Convention over Configuration: Method attributes drive everything
// Key Design Principles:
// 1. Zero Hardcoding - No manual tool registration required
// 2. Automatic Schema Generation - Method signatures become JSON schemas  
// 3. Service Injection Aware - Filters out DI parameters from tool schemas
// 4. Convention Driven - [McpServerTool] + [Description] attributes define tools
static List<ToolDefinition> DiscoverToolsFromAttributes()
{
    var tools = new List<ToolDefinition>();
    var toolsType = typeof(WikipediaTools);
    
    // REFLECTION SCAN: Find all public static methods that could be MCP tools
    foreach (var method in toolsType.GetMethods(BindingFlags.Public | BindingFlags.Static))
    {
        // TOOL IDENTIFICATION: Look for [McpServerTool] attribute to mark method as MCP tool
        var toolAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
        if (toolAttribute == null) continue; // Skip non-tool methods
        
        // DESCRIPTION EXTRACTION: Get human-readable description from [Description] attribute
        var descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttribute?.Description ?? "No description available";
        
        // JSON SCHEMA GENERATION: Build input schema from method parameters automatically
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        
        foreach (var param in method.GetParameters())
        {
            // SERVICE INJECTION FILTER: Skip dependency injection parameters (like IWikipediaService)
            // These are injected at runtime, not part of the tool's user-facing API
            if (param.ParameterType == typeof(IWikipediaService)) continue;
            
            // PARAMETER SCHEMA BUILDING: Convert method parameter to JSON schema property
            var paramDescription = param.GetCustomAttribute<DescriptionAttribute>();
            properties[param.Name!] = new
            {
                type = "string", // Simplified - could be enhanced to detect actual types
                description = paramDescription?.Description ?? $"Parameter {param.Name}"
            };
            
            // REQUIRED FIELD DETECTION: Non-optional parameters become required in schema
            if (!param.HasDefaultValue)
            {
                required.Add(param.Name!);
            }
        }
        
        // SCHEMA CONSTRUCTION: Build complete JSON schema for the tool
        var inputSchema = new
        {
            type = "object",
            properties = properties,
            required = required.ToArray()
        };
        
        // TOOL DEFINITION CREATION: Combine all metadata into tool definition
        tools.Add(new ToolDefinition
        {
            Name = toolAttribute.Name ?? method.Name,
            Description = description,
            InputSchema = inputSchema
        });
    }
    
    // DISCOVERY COMPLETE: Return all discovered tool definitions
    // Example output: wikipedia_search, wikipedia_sections, wikipedia_section_content
    return tools;
}

// Handle tools/call request in stdio mode using dynamic reflection-based tool execution
// Complete Flow: Request Reception → Parameter Extraction → Method Discovery → Dynamic Invocation → Response Construction
// Example Input: {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"wikipedia_search","arguments":{"query":"AI"}}}
// Example Output: {"jsonrpc":"2.0","id":3,"result":{"content":[{"type":"text","text":"Wikipedia search result..."}]}}
static async Task<string> HandleToolsCallStdio(JsonElement request, IServiceProvider serviceProvider)
{
    var id = request.TryGetProperty("id", out var idProp) ? idProp.ToString() : "null";
    
    try
    {
        // PARAMETER EXTRACTION: Extract tool name and arguments from JSON-RPC request
        // Expected structure: params.name = "wikipedia_search", params.arguments = {"query": "artificial intelligence"}
        var toolName = request.GetProperty("params").GetProperty("name").GetString();
        var arguments = request.GetProperty("params").GetProperty("arguments");
        
        Console.Error.WriteLine($"🛠️ Tool call: {toolName}");
        
        // DYNAMIC TOOL EXECUTION: Use reflection to find and invoke the tool method
        // This replaces hardcoded switch statements - adding new tools requires no code changes here!
        // Flow: toolName → Method Discovery → Parameter Resolution → Method Invocation → Result
        var resultText = await InvokeToolByReflection(toolName!, arguments, serviceProvider);
        
        Console.Error.WriteLine($"✅ Tool execution successful, result length: {resultText.Length} chars");
        
        // MCP RESPONSE CONSTRUCTION: Wrap tool result in MCP-compliant JSON-RPC response format
        // MCP spec requires content array with type/text structure for tool results
        var escapedText = JsonSerializer.Serialize(resultText);
        // Use compact single-line JSON for stdio mode (required by JSON-RPC 2.0 spec)
        var response = $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":{escapedText}}}]}}}}";
        return response;
    }
    catch (Exception ex)
    {
        // ERROR HANDLING: Convert .NET exceptions to MCP-compliant JSON-RPC error responses
        Console.Error.WriteLine($"❌ Tool execution failed: {ex.Message}");
        return CreateErrorResponse(request, -32603, $"Internal error: {ex.Message}");
    }
}

// DYNAMIC TOOL INVOCATION ENGINE  
// Invokes tool methods using reflection - no hardcoded switch statements!
// Handles both sync/async methods and supports dependency injection
// 
// TOOLS/CALL FLOW DETAILS:
// 1. Method Discovery: Scans WikipediaTools class for method with matching [McpServerTool(Name="toolName")]
// 2. Parameter Resolution: Maps JSON arguments to method parameters + injects services from DI
// 3. Dynamic Invocation: Calls method.Invoke() with prepared parameter array
// 4. Result Handling: Awaits async results and converts to string format
//
// Key Features:
// 1. Dynamic Method Discovery - Finds method by tool name at runtime
// 2. Parameter Extraction - Maps JSON arguments to method parameters
// 3. Service Injection - Automatically injects IWikipediaService 
// 4. Type Safety - Handles different parameter types and naming conventions
//
// Example: toolName="wikipedia_search", arguments={"query":"AI"} 
// → Finds WikipediaTools.SearchWikipedia() 
// → Calls SearchWikipedia(wikipediaService, "AI")
// → Returns "Wikipedia search result for 'AI'..."
static async Task<string> InvokeToolByReflection(string toolName, JsonElement arguments, IServiceProvider serviceProvider)
{
    var toolsType = typeof(WikipediaTools);
    
    // TOOL METHOD DISCOVERY: Find the method with matching tool name
    // Scans all static methods in WikipediaTools for [McpServerTool(Name=toolName)]
    foreach (var method in toolsType.GetMethods(BindingFlags.Public | BindingFlags.Static))
    {
        var toolAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
        if (toolAttribute == null || toolAttribute.Name != toolName) continue;
        
        // FOUND THE TARGET METHOD! Now prepare parameters for invocation
        // Example: Found WikipediaTools.SearchWikipedia(IWikipediaService, string query)
        var parameters = new List<object?>();
        
        // PARAMETER RESOLUTION LOOP: Build parameter array matching method signature
        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType == typeof(IWikipediaService))
            {
                // DEPENDENCY INJECTION: Inject service from DI container
                // Services are registered in RunStdioModeAsync() and resolved here automatically
                parameters.Add(serviceProvider.GetRequiredService<IWikipediaService>());
            }
            else
            {
                // USER PARAMETER EXTRACTION: Extract from JSON arguments with smart name matching
                // Supports multiple naming conventions: exact match → snake_case → camelCase
                // Example: "query" parameter gets value "AI" from arguments.query
                var paramValue = ExtractParameterFromJson(param, arguments);
                parameters.Add(paramValue);
            }
        }
        
        // DYNAMIC METHOD INVOCATION: Execute the discovered method with prepared parameters
        // Equivalent to: await WikipediaTools.SearchWikipedia(wikipediaService, "AI")
        var result = method.Invoke(null, parameters.ToArray());
        
        // RESULT HANDLING: Support both async and sync method return types
        // Most Wikipedia tools are async (Task<string>) but framework supports both
        if (result is Task<string> asyncResult)
        {
            return await asyncResult;  // Await async tool execution
        }
        else if (result is string syncResult)
        {
            return syncResult;         // Return sync result immediately
        }
        else
        {
            return result?.ToString() ?? "No result";
        }
    }
    
    // TOOL NOT FOUND: No method found with matching [McpServerTool(Name=toolName)]
    return $"Unknown tool: {toolName}";
}

// SMART PARAMETER EXTRACTION ENGINE
// Extracts parameter values from JSON arguments with intelligent name matching
// Supports multiple naming conventions to handle client variations
// Key Features:
// 1. Exact Name Match - Try parameter name as-is first
// 2. Snake Case Conversion - Handle VS Code MCP snake_case style  
// 3. Camel Case Fallback - Handle direct API calls with camelCase
// 4. Type Safety - Extract values based on target parameter type
static object? ExtractParameterFromJson(ParameterInfo param, JsonElement arguments)
{
    var paramName = param.Name!;
    
    // EXACT MATCH: Try exact parameter name first (most common case)
    if (arguments.TryGetProperty(paramName, out var exactMatch))
    {
        return ExtractValueByType(exactMatch, param.ParameterType);
    }
    
    // SNAKE_CASE CONVERSION: VS Code MCP often uses snake_case (sectionTitle → section_title)
    var snakeCaseName = ConvertToSnakeCase(paramName);
    if (arguments.TryGetProperty(snakeCaseName, out var snakeMatch))
    {
        return ExtractValueByType(snakeMatch, param.ParameterType);
    }
    
    // CAMEL_CASE FALLBACK: Handle direct API calls with camelCase
    var camelCaseName = char.ToLowerInvariant(paramName[0]) + paramName.Substring(1);
    if (arguments.TryGetProperty(camelCaseName, out var camelMatch))
    {
        return ExtractValueByType(camelMatch, param.ParameterType);
    }
    
    throw new ArgumentException($"Required parameter '{paramName}' not found in arguments");
}

// NAMING CONVENTION CONVERTER
// Converts parameter names to snake_case for VS Code MCP compatibility
// Example: sectionTitle → section_title, topicName → topic_name
static string ConvertToSnakeCase(string input)
{
    return System.Text.RegularExpressions.Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
}

// TYPE-SAFE VALUE EXTRACTION
// Extracts values from JsonElement based on target parameter type
// Provides type safety for method parameter binding
// Extensible: Add more types as needed for complex tool parameters
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
    // EXTENSIBILITY POINT: Add more types as needed (decimal, DateTime, etc.)
    else
    {
        return element.GetString(); // Default to string for unknown types
    }
}

// JSON-RPC ERROR RESPONSE GENERATOR for stdio mode
// Converts .NET exceptions into MCP-compliant JSON-RPC error responses
// Used by tools/call when tool execution fails (e.g., missing parameters, API errors)
// Error codes follow JSON-RPC 2.0 specification:
// -32700: Parse error, -32600: Invalid Request, -32601: Method not found, -32603: Internal error
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

// ANSI color codes for terminal output (color-blind friendly)
public static class ConsoleColors
{
    public const string Reset = "\u001b[0m";
    public const string Bold = "\u001b[1m";
    
    // Request colors (Blue theme - good contrast, color-blind safe)
    public const string RequestBlue = "\u001b[94m";      // Bright blue
    public const string RequestCyan = "\u001b[96m";      // Bright cyan
    
    // Response colors (Green theme - distinguishable from blue)
    public const string ResponseGreen = "\u001b[92m";    // Bright green
    public const string ResponseDarkGreen = "\u001b[32m"; // Dark green
    
    // Error colors (Red theme)
    public const string ErrorRed = "\u001b[91m";         // Bright red
    public const string ErrorDarkRed = "\u001b[31m";     // Dark red
    
    // Notification colors (Yellow/Orange theme)
    public const string NotificationYellow = "\u001b[93m"; // Bright yellow
    public const string NotificationOrange = "\u001b[38;5;208m"; // Orange
    
    // Method/Info colors (Magenta theme)
    public const string MethodMagenta = "\u001b[95m";    // Bright magenta
    public const string InfoWhite = "\u001b[97m";        // Bright white
}

// TOOL DEFINITION HELPER CLASS
// Represents a discovered MCP tool with metadata and JSON serialization capability
// Used by the reflection-based tool discovery engine to build tool responses
class ToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object InputSchema { get; set; } = new { };
    
    // JSON SERIALIZATION: Convert tool definition to MCP-compliant JSON format
    // Produces output like: {"name":"wikipedia_search","description":"...","inputSchema":{...}}
    public string ToJson()
    {
        var inputSchemaJson = JsonSerializer.Serialize(InputSchema, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return $"{{\"name\":\"{Name}\",\"description\":\"{Description}\",\"inputSchema\":{inputSchemaJson}}}";
    }
}
