using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Models;
using ScreenshotMcp.Server.Services;

Console.WriteLine("=== MCP Screenshot Server Test ===\n");

// Setup services
var options = Options.Create(new ScreenshotServerOptions());
var browserPool = new BrowserPoolManager(options, NullLogger<BrowserPoolManager>.Instance);
var tempFileManager = new TempFileManager(options, NullLogger<TempFileManager>.Instance);
var screenshotService = new ScreenshotService(browserPool, tempFileManager, options, NullLogger<ScreenshotService>.Instance);

try
{
    Console.WriteLine("Initializing browser...");
    await browserPool.InitializeAsync();
    Console.WriteLine("Browser initialized!\n");

    // Test 1: Desktop screenshot
    Console.WriteLine("Test 1: Capturing desktop screenshot of https://www.cpike.ca");
    var desktopRequest = new ScreenshotRequest
    {
        Source = ContentSource.Url,
        Content = "https://www.cpike.ca",
        Viewport = new ViewportConfig(1280, 720),
        FullPage = false,
        WaitMs = 2000,
        DarkMode = false
    };

    var desktopResult = await screenshotService.CaptureAsync(desktopRequest);
    var desktopBytes = Convert.FromBase64String(desktopResult.Base64Data);
    var desktopPath = Path.Combine(Directory.GetCurrentDirectory(), "cpike-desktop.png");
    await File.WriteAllBytesAsync(desktopPath, desktopBytes);
    Console.WriteLine($"  ✓ Saved: {desktopPath}");
    Console.WriteLine($"  ✓ Size: {desktopBytes.Length:N0} bytes\n");

    // Test 2: Mobile screenshot
    Console.WriteLine("Test 2: Capturing mobile screenshot of https://www.cpike.ca");
    var mobilePreset = DevicePresets.GetPreset("mobile")!;
    var mobileRequest = new ScreenshotRequest
    {
        Source = ContentSource.Url,
        Content = "https://www.cpike.ca",
        Viewport = new ViewportConfig(mobilePreset.Width, mobilePreset.Height, mobilePreset.Scale),
        FullPage = false,
        WaitMs = 2000,
        DarkMode = false,
        UserAgent = mobilePreset.UserAgent
    };

    var mobileResult = await screenshotService.CaptureAsync(mobileRequest);
    var mobileBytes = Convert.FromBase64String(mobileResult.Base64Data);
    var mobilePath = Path.Combine(Directory.GetCurrentDirectory(), "cpike-mobile.png");
    await File.WriteAllBytesAsync(mobilePath, mobileBytes);
    Console.WriteLine($"  ✓ Saved: {mobilePath}");
    Console.WriteLine($"  ✓ Size: {mobileBytes.Length:N0} bytes\n");

    // Test 3: Full page screenshot
    Console.WriteLine("Test 3: Capturing full page screenshot of https://www.cpike.ca");
    var fullPageRequest = new ScreenshotRequest
    {
        Source = ContentSource.Url,
        Content = "https://www.cpike.ca",
        Viewport = new ViewportConfig(1280, 720),
        FullPage = true,
        WaitMs = 2000,
        DarkMode = false
    };

    var fullPageResult = await screenshotService.CaptureAsync(fullPageRequest);
    var fullPageBytes = Convert.FromBase64String(fullPageResult.Base64Data);
    var fullPagePath = Path.Combine(Directory.GetCurrentDirectory(), "cpike-fullpage.png");
    await File.WriteAllBytesAsync(fullPagePath, fullPageBytes);
    Console.WriteLine($"  ✓ Saved: {fullPagePath}");
    Console.WriteLine($"  ✓ Size: {fullPageBytes.Length:N0} bytes\n");

    Console.WriteLine("=== All tests completed successfully! ===");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    Console.WriteLine("\nCleaning up...");
    await browserPool.DisposeAsync();
    Console.WriteLine("Done!");
}
