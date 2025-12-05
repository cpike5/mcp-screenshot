using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using ScreenshotMcp.Server.Models;
using ScreenshotMcp.Server.Services;
using ScreenshotMcp.Server.Validation;

namespace ScreenshotMcp.Server.Tools;

[McpServerToolType]
public class ScreenshotMultiTool
{
    private readonly IScreenshotService _screenshotService;
    private readonly IInputValidator _inputValidator;

    public ScreenshotMultiTool(
        IScreenshotService screenshotService,
        IInputValidator inputValidator)
    {
        _screenshotService = screenshotService;
        _inputValidator = inputValidator;
    }

    private const int MaxRecommendedViewports = 3;

    [McpServerTool(Name = "screenshot_multi")]
    [Description("Captures screenshots at multiple viewport sizes. WARNING: Each viewport adds to response size. Use 'compact' mode or limit to 2-3 viewports to avoid large responses.")]
    public async Task<object> ExecuteAsync(
        [Description("Raw HTML content to render")] string? html = null,
        [Description("Absolute path to an HTML file to render")] string? filePath = null,
        [Description("URL to capture (http/https only)")] string? url = null,
        [Description("JSON array of viewport configurations. Each item can be a preset name (string) or an object with width/height properties. Recommend max 3 viewports.")] string viewports = "[]",
        [Description("CSS selector to wait for before capturing")] string? waitForSelector = null,
        [Description("Additional wait time in milliseconds after page load")] int waitMs = 0,
        [Description("Emulate dark color scheme (prefers-color-scheme: dark)")] bool darkMode = false,
        [Description("Image format: 'png' (lossless, larger) or 'jpeg' (lossy, ~60-80% smaller). Default: png")] string format = "png",
        [Description("JPEG quality 1-100 (only applies when format is 'jpeg'). Lower = smaller file. Default: 80")] int quality = 80,
        [Description("Scale factor 0.1-1.0 for output images. 0.5 = half size (~75% smaller). Default: 1.0")] float scale = 1.0f,
        [Description("Compact mode: uses JPEG 70% quality and 0.75 scale for significantly smaller responses")] bool compact = false,
        CancellationToken cancellationToken = default)
    {
        // Parse viewports
        List<ViewportConfig> viewportConfigs;
        try
        {
            viewportConfigs = ParseViewports(viewports);
        }
        catch (Exception ex)
        {
            return new { error = "INVALID_INPUT", message = $"Invalid viewports format: {ex.Message}" };
        }

        if (viewportConfigs.Count == 0)
        {
            return new { error = "INVALID_INPUT", message = "At least one viewport is required" };
        }

        // Warn about too many viewports (but don't block)
        string? warning = null;
        if (viewportConfigs.Count > MaxRecommendedViewports)
        {
            warning = $"Using {viewportConfigs.Count} viewports may produce a large response. Consider using compact mode or limiting to {MaxRecommendedViewports} viewports.";
        }

        // Validate basic input (use first viewport for validation)
        var firstViewport = viewportConfigs[0];
        if (!_inputValidator.ValidateScreenshotInput(html, filePath, url, firstViewport.Width, firstViewport.Height, null, waitMs, out var error))
        {
            return new { error = "INVALID_INPUT", message = error };
        }

        // Build image options
        ImageOptions imageOptions;
        if (compact)
        {
            // Compact mode: JPEG 70% quality, 0.75 scale
            imageOptions = new ImageOptions
            {
                Format = "jpeg",
                Quality = 70,
                Scale = 0.75f
            };
        }
        else
        {
            imageOptions = new ImageOptions
            {
                Format = format,
                Quality = quality,
                Scale = scale
            };
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

        try
        {
            var results = await _screenshotService.CaptureMultipleAsync(
                source,
                content,
                viewportConfigs,
                waitForSelector,
                waitMs,
                darkMode,
                imageOptions,
                cancellationToken);

            var images = results.Select(r => new
            {
                type = "image",
                data = r.Base64Data,
                mimeType = r.MimeType,
                annotations = new
                {
                    width = r.Viewport?.Width ?? 0,
                    height = r.Viewport?.Height ?? 0
                }
            }).ToList();

            // Include warning if present
            if (warning != null)
            {
                return new { content = images, warning };
            }

            return new { content = images };
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

    private static List<ViewportConfig> ParseViewports(string viewportsJson)
    {
        var result = new List<ViewportConfig>();

        using var doc = JsonDocument.Parse(viewportsJson);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Viewports must be a JSON array");
        }

        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                // Preset name
                var presetName = element.GetString()!;
                var preset = DevicePresets.GetPreset(presetName);
                if (preset is null)
                {
                    throw new ArgumentException($"Unknown preset: {presetName}");
                }
                result.Add(new ViewportConfig(preset.Width, preset.Height, preset.Scale));
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                // Custom dimensions
                if (!element.TryGetProperty("width", out var widthProp) ||
                    !element.TryGetProperty("height", out var heightProp))
                {
                    throw new ArgumentException("Custom viewport must have 'width' and 'height' properties");
                }

                var width = widthProp.GetInt32();
                var height = heightProp.GetInt32();
                var scale = element.TryGetProperty("scale", out var scaleProp) ? scaleProp.GetSingle() : 1.0f;

                result.Add(new ViewportConfig(width, height, scale));
            }
            else
            {
                throw new ArgumentException("Each viewport must be a string (preset name) or an object with width/height");
            }
        }

        return result;
    }
}
