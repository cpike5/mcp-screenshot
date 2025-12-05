using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using ScreenshotMcp.Server.Extensions;
using ScreenshotMcp.Server.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add Screenshot Server services
builder.Services.AddScreenshotServer(builder.Configuration);

// Add MCP Server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

// Initialize browser pool on startup
var browserPool = host.Services.GetRequiredService<IBrowserPoolManager>();
await browserPool.InitializeAsync();

await host.RunAsync();
