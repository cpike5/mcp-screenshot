using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Models;

namespace ScreenshotMcp.Server.Services;

public class ScreenshotService : IScreenshotService
{
    private readonly IBrowserPoolManager _browserPool;
    private readonly ITempFileManager _tempFileManager;
    private readonly IImageProcessor _imageProcessor;
    private readonly IOptions<ScreenshotServerOptions> _options;
    private readonly ILogger<ScreenshotService> _logger;

    public ScreenshotService(
        IBrowserPoolManager browserPool,
        ITempFileManager tempFileManager,
        IImageProcessor imageProcessor,
        IOptions<ScreenshotServerOptions> options,
        ILogger<ScreenshotService> logger)
    {
        _browserPool = browserPool;
        _tempFileManager = tempFileManager;
        _imageProcessor = imageProcessor;
        _options = options;
        _logger = logger;
    }

    public async Task<ScreenshotResult> CaptureAsync(ScreenshotRequest request, CancellationToken cancellationToken = default)
    {
        var page = await _browserPool.AcquirePageAsync(cancellationToken);
        string? tempFilePath = null;

        // Apply thumbnail mode overrides
        var imageOptions = request.ImageOptions.Normalize();
        var viewport = request.Viewport;

        if (imageOptions.Thumbnail)
        {
            // Thumbnail mode uses smaller viewport and JPEG
            viewport = new ViewportConfig(640, 360);
            imageOptions = imageOptions with { Format = "jpeg", Quality = 60 };
        }

        try
        {
            // Configure viewport
            await page.SetViewportSizeAsync(viewport.Width, viewport.Height);

            // Configure user agent if specified
            if (!string.IsNullOrEmpty(request.UserAgent))
            {
                await page.Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
                {
                    ["User-Agent"] = request.UserAgent
                });
            }

            // Configure dark mode if requested
            if (request.DarkMode)
            {
                await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
            }

            // Navigate to content
            var url = await ResolveContentUrlAsync(request, cancellationToken);
            if (request.Source == ContentSource.Html)
            {
                tempFilePath = url.Replace("file://", "");
            }

            var timeout = _options.Value.Defaults.Timeout;
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = timeout
            });

            // Wait for selector if specified
            if (!string.IsNullOrEmpty(request.WaitForSelector))
            {
                await page.WaitForSelectorAsync(request.WaitForSelector, new PageWaitForSelectorOptions
                {
                    Timeout = timeout
                });
            }

            // Additional wait time
            if (request.WaitMs > 0)
            {
                await Task.Delay(request.WaitMs, cancellationToken);
            }

            // Build screenshot options
            var screenshotOptions = new PageScreenshotOptions
            {
                FullPage = request.FullPage,
                Type = ScreenshotType.Png // Always capture as PNG, convert later
            };

            // Apply maxHeight clip if specified and using fullPage
            if (request.FullPage && imageOptions.MaxHeight > 0)
            {
                // Get the actual page height
                var pageHeight = await page.EvaluateAsync<int>("() => document.documentElement.scrollHeight");
                if (pageHeight > imageOptions.MaxHeight)
                {
                    // Use clip instead of fullPage to limit height
                    screenshotOptions.FullPage = false;
                    screenshotOptions.Clip = new Clip
                    {
                        X = 0,
                        Y = 0,
                        Width = viewport.Width,
                        Height = imageOptions.MaxHeight
                    };
                }
            }

            // Capture screenshot
            var rawBytes = await page.ScreenshotAsync(screenshotOptions);

            // Process image (format conversion, scaling)
            var (processedBytes, mimeType) = _imageProcessor.Process(rawBytes, imageOptions);

            var base64 = Convert.ToBase64String(processedBytes);
            _logger.LogDebug(
                "Screenshot captured: {Width}x{Height}, FullPage: {FullPage}, Format: {Format}, Scale: {Scale}, RawSize: {RawSize} bytes, ProcessedSize: {ProcessedSize} bytes",
                viewport.Width,
                viewport.Height,
                request.FullPage,
                imageOptions.Format,
                imageOptions.Scale,
                rawBytes.Length,
                processedBytes.Length);

            return new ScreenshotResult(base64, mimeType, viewport);
        }
        finally
        {
            await _browserPool.ReleasePageAsync(page);

            if (tempFilePath is not null)
            {
                await _tempFileManager.DeleteTempFileAsync(tempFilePath);
            }
        }
    }

    public async Task<IReadOnlyList<ScreenshotResult>> CaptureMultipleAsync(
        ContentSource source,
        string content,
        IEnumerable<ViewportConfig> viewports,
        string? waitForSelector = null,
        int waitMs = 0,
        bool darkMode = false,
        ImageOptions? imageOptions = null,
        CancellationToken cancellationToken = default)
    {
        var viewportList = viewports.ToList();
        var results = new List<ScreenshotResult>(viewportList.Count);
        var normalizedOptions = (imageOptions ?? ImageOptions.Default).Normalize();

        var page = await _browserPool.AcquirePageAsync(cancellationToken);
        string? tempFilePath = null;

        try
        {
            // Configure dark mode if requested
            if (darkMode)
            {
                await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
            }

            // Resolve content URL once
            var request = new ScreenshotRequest
            {
                Source = source,
                Content = content,
                Viewport = viewportList[0],
                WaitForSelector = waitForSelector,
                WaitMs = waitMs,
                DarkMode = darkMode
            };

            var url = await ResolveContentUrlAsync(request, cancellationToken);
            if (source == ContentSource.Html)
            {
                tempFilePath = url.Replace("file://", "");
            }

            // Navigate once
            var timeout = _options.Value.Defaults.Timeout;
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = timeout
            });

            foreach (var viewport in viewportList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Configure viewport
                await page.SetViewportSizeAsync(viewport.Width, viewport.Height);

                // Wait for selector if specified (on first viewport or if content might change)
                if (!string.IsNullOrEmpty(waitForSelector))
                {
                    await page.WaitForSelectorAsync(waitForSelector, new PageWaitForSelectorOptions
                    {
                        Timeout = timeout
                    });
                }

                // Additional wait time
                if (waitMs > 0)
                {
                    await Task.Delay(waitMs, cancellationToken);
                }

                // Capture screenshot
                var rawBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Type = ScreenshotType.Png
                });

                // Process image (format conversion, scaling)
                var (processedBytes, mimeType) = _imageProcessor.Process(rawBytes, normalizedOptions);

                var base64 = Convert.ToBase64String(processedBytes);
                results.Add(new ScreenshotResult(base64, mimeType, viewport));
            }

            return results;
        }
        finally
        {
            await _browserPool.ReleasePageAsync(page);

            if (tempFilePath is not null)
            {
                await _tempFileManager.DeleteTempFileAsync(tempFilePath);
            }
        }
    }

    private async Task<string> ResolveContentUrlAsync(ScreenshotRequest request, CancellationToken cancellationToken)
    {
        return request.Source switch
        {
            ContentSource.Url => request.Content,
            ContentSource.FilePath => $"file://{Path.GetFullPath(request.Content)}",
            ContentSource.Html => $"file://{await _tempFileManager.CreateTempFileAsync(request.Content, cancellationToken)}",
            _ => throw new ArgumentOutOfRangeException(nameof(request.Source))
        };
    }
}
