using System.ComponentModel;
using ModelContextProtocol.Server;
using ScreenshotMcp.Server.Models;
using ScreenshotMcp.Server.Services;
using ScreenshotMcp.Server.Validation;

namespace ScreenshotMcp.Server.Tools;

[McpServerToolType]
public class ScreenshotPageTool
{
    private readonly IScreenshotService _screenshotService;
    private readonly IInputValidator _inputValidator;

    public ScreenshotPageTool(
        IScreenshotService screenshotService,
        IInputValidator inputValidator)
    {
        _screenshotService = screenshotService;
        _inputValidator = inputValidator;
    }

    [McpServerTool(Name = "screenshot_page")]
    [Description("Renders HTML content or a URL and returns a screenshot. Supports PNG (lossless) or JPEG (smaller files). Use 'optimized' mode or JPEG format to reduce response size.")]
    public async Task<object> ExecuteAsync(
        [Description("Raw HTML content to render")] string? html = null,
        [Description("Absolute path to an HTML file to render")] string? filePath = null,
        [Description("URL to capture (http/https only)")] string? url = null,
        [Description("Viewport width in pixels")] int width = 1280,
        [Description("Viewport height in pixels")] int height = 720,
        [Description("Capture the full scrollable page instead of just the viewport")] bool fullPage = false,
        [Description("Device preset name (desktop, desktop-hd, tablet, tablet-landscape, mobile, mobile-large)")] string? devicePreset = null,
        [Description("CSS selector to wait for before capturing")] string? waitForSelector = null,
        [Description("Additional wait time in milliseconds after page load")] int waitMs = 0,
        [Description("Emulate dark color scheme (prefers-color-scheme: dark)")] bool darkMode = false,
        [Description("Image format: 'png' (lossless, larger) or 'jpeg' (lossy, ~60-80% smaller). Default: png")] string format = "png",
        [Description("JPEG quality 1-100 (only applies when format is 'jpeg'). Lower = smaller file. Default: 80")] int quality = 80,
        [Description("Scale factor 0.1-1.0 for output image. 0.5 = half size (~75% smaller). Default: 1.0")] float scale = 1.0f,
        [Description("Maximum height in pixels for full-page captures. Prevents huge screenshots. 0 = no limit. Default: 0")] int maxHeight = 0,
        [Description("Thumbnail mode: captures at 640x360 with JPEG 60% quality for quick previews")] bool thumbnail = false,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (!_inputValidator.ValidateScreenshotInput(html, filePath, url, width, height, devicePreset, waitMs, out var error))
        {
            return new { error = "INVALID_INPUT", message = error };
        }

        // Determine content source
        ContentSource source;
        string content;

        if (!string.IsNullOrEmpty(html))
        {
            source = ContentSource.Html;
            content = html;
        }
        else if (!string.IsNullOrEmpty(filePath))
        {
            source = ContentSource.FilePath;
            content = filePath;
        }
        else
        {
            source = ContentSource.Url;
            content = url!;
        }

        // Resolve viewport from device preset or custom dimensions
        ViewportConfig viewport;
        string? userAgent = null;

        if (!string.IsNullOrEmpty(devicePreset))
        {
            var preset = DevicePresets.GetPreset(devicePreset)!;
            viewport = new ViewportConfig(preset.Width, preset.Height, preset.Scale);
            userAgent = preset.UserAgent;
        }
        else
        {
            viewport = new ViewportConfig(width, height);
        }

        // Build image options
        var imageOptions = new ImageOptions
        {
            Format = format,
            Quality = quality,
            Scale = scale,
            MaxHeight = maxHeight,
            Thumbnail = thumbnail
        };

        // Build request
        var request = new ScreenshotRequest
        {
            Source = source,
            Content = content,
            Viewport = viewport,
            FullPage = fullPage,
            WaitForSelector = waitForSelector,
            WaitMs = waitMs,
            DarkMode = darkMode,
            UserAgent = userAgent,
            ImageOptions = imageOptions
        };

        try
        {
            var result = await _screenshotService.CaptureAsync(request, cancellationToken);

            return new
            {
                type = "image",
                data = result.Base64Data,
                mimeType = result.MimeType
            };
        }
        catch (TimeoutException ex)
        {
            return new { error = "RENDER_TIMEOUT", message = ex.Message };
        }
        catch (FileNotFoundException ex)
        {
            return new { error = "FILE_NOT_FOUND", message = ex.Message };
        }
        catch (Exception ex) when (ex.Message.Contains("selector"))
        {
            return new { error = "SELECTOR_TIMEOUT", message = ex.Message };
        }
    }
}
