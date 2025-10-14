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

    // Add services to the container.
    builder.Services.AddControllers();

    // Add HTTP client for Wikipedia API calls
    builder.Services.AddHttpClient<IWikipediaService, WikipediaService>();

    // Register Wikipedia service
    builder.Services.AddScoped<IWikipediaService, WikipediaService>();

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

    // Add CORS for development
    app.UseCors(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    app.UseAuthorization();

    app.MapControllers();

    // Add a root endpoint
    app.MapGet("/", () => "Wikipedia MCP Server is running");

    app.Run();
}

// Make Program class accessible for integration tests
public partial class Program { }
