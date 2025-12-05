using ScreenshotMcp.Server.Models;

namespace ScreenshotMcp.Server.Services;

public interface IScreenshotService
{
    Task<ScreenshotResult> CaptureAsync(ScreenshotRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScreenshotResult>> CaptureMultipleAsync(
        ContentSource source,
        string content,
        IEnumerable<ViewportConfig> viewports,
        string? waitForSelector = null,
        int waitMs = 0,
        bool darkMode = false,
        ImageOptions? imageOptions = null,
        CancellationToken cancellationToken = default);
}
