namespace ScreenshotMcp.Server.Models;

public record ScreenshotRequest
{
    public required ContentSource Source { get; init; }
    public required string Content { get; init; }
    public required ViewportConfig Viewport { get; init; }
    public bool FullPage { get; init; }
    public string? WaitForSelector { get; init; }
    public int WaitMs { get; init; }
    public bool DarkMode { get; init; }
    public string? UserAgent { get; init; }

    /// <summary>
    /// Image output options for format, quality, and size optimization.
    /// </summary>
    public ImageOptions ImageOptions { get; init; } = ImageOptions.Default;
}
