// Simple test script to capture a screenshot
#r "src/ScreenshotMcp.Server/bin/Debug/net8.0/ScreenshotMcp.Server.dll"
#r "nuget: Microsoft.Playwright, 1.42.0"
#r "nuget: Microsoft.Extensions.Options, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging.Abstractions, 8.0.0"

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Models;
using ScreenshotMcp.Server.Services;

var options = Options.Create(new ScreenshotServerOptions());
var browserPool = new BrowserPoolManager(options, NullLogger<BrowserPoolManager>.Instance);
var tempFileManager = new TempFileManager(options, NullLogger<TempFileManager>.Instance);
var screenshotService = new ScreenshotService(browserPool, tempFileManager, options, NullLogger<ScreenshotService>.Instance);

await browserPool.InitializeAsync();

var request = new ScreenshotRequest
{
    Source = ContentSource.Url,
    Content = "https://www.cpike.ca",
    Viewport = new ViewportConfig(1280, 720),
    FullPage = false,
    WaitMs = 1000,
    DarkMode = false
};

Console.WriteLine("Capturing screenshot of https://www.cpike.ca...");
var result = await screenshotService.CaptureAsync(request);
Console.WriteLine($"Screenshot captured! Size: {result.Base64Data.Length} characters (base64)");

// Save to file
var bytes = Convert.FromBase64String(result.Base64Data);
File.WriteAllBytes("/home/cpike/Workspace/screenshot-mcp/cpike-screenshot.png", bytes);
Console.WriteLine("Saved to cpike-screenshot.png");

await browserPool.DisposeAsync();
