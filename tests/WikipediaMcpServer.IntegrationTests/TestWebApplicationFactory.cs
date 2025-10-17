using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WikipediaMcpServer.IntegrationTests;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set to Development environment to ensure proper configuration
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // The main issue is that WebApplicationFactory might not properly pass
            // command line args to the Program.cs, so the mcpMode check fails.
            // We need to ensure we don't run in MCP mode during tests.
            
            // Let's explicitly set a configuration value that can be checked in Program.cs
            builder.UseSetting("TestMode", "true");
        });
    }
}