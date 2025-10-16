using WikipediaMcpServer.Services;
using Microsoft.Extensions.Logging.Console;

// Check if running in MCP mode (stdio) or HTTP mode
var mcpMode = args.Contains("--mcp") || args.Contains("--stdio");

if (mcpMode)
{
    // MCP stdio mode
    var builder = Host.CreateApplicationBuilder(args);
    
    // Configure logging to stderr only for MCP mode
    builder.Services.Configure<ConsoleLoggerOptions>(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
    
    // Add HTTP client for Wikipedia API calls
    builder.Services.AddHttpClient<IWikipediaService, WikipediaService>();
    
    // Register Wikipedia service
    builder.Services.AddScoped<IWikipediaService, WikipediaService>();
    
    // Register MCP server service
    builder.Services.AddHostedService<McpServerService>();
    
    var host = builder.Build();
    await host.RunAsync();
}
else
{
    // HTTP API mode
    var builder = WebApplication.CreateBuilder(args);

    // Configure Kestrel for production deployment
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false; // Security: Remove server header
        
        // For cloud deployments (Render, Railway, etc.), only listen on HTTP
        // HTTPS termination is handled by the platform's load balancer
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        options.ListenAnyIP(int.Parse(port));
    });

    // Add services to the container.
    builder.Services.AddControllers();

    // Add HTTP client for Wikipedia API calls with timeout configuration
    builder.Services.AddHttpClient<IWikipediaService, WikipediaService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Register Wikipedia service
    builder.Services.AddScoped<IWikipediaService, WikipediaService>();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

    // Configure CORS for production
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // Production CORS - restrict as needed
                policy.WithOrigins("*") // Configure specific origins in production
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        });
    });

    // Configure forwarded headers for reverse proxy (Render, Railway, etc.)
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
            Version = "v1",
            Description = "A Model Context Protocol (MCP) server for Wikipedia search and content retrieval"
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
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wikipedia MCP Server API v1");
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

    // Add CORS
    app.UseCors();

    // Add health check endpoint
    app.MapHealthChecks("/api/wikipedia/health");

    app.UseAuthorization();

    app.MapControllers();

    // Add a root endpoint with API information
    app.MapGet("/", () => new { 
        name = "Wikipedia MCP Server", 
        version = "v5.0",
        status = "running",
        endpoints = new {
            health = "/api/wikipedia/health",
            search = "/api/wikipedia/search",
            sections = "/api/wikipedia/sections",
            sectionContent = "/api/wikipedia/section-content",
            swagger = "/swagger"
        }
    });

    app.Run();
}

// Make Program class accessible for integration tests
public partial class Program { }
