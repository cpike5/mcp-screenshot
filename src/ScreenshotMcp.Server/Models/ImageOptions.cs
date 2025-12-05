namespace ScreenshotMcp.Server.Models;

/// <summary>
/// Options for image output format, quality, and size optimization.
/// </summary>
public record ImageOptions
{
    /// <summary>
    /// Output format: "png" (lossless, larger) or "jpeg" (lossy, smaller).
    /// Default: "png"
    /// </summary>
    public string Format { get; init; } = "png";

    /// <summary>
    /// JPEG quality (1-100). Only applies when Format is "jpeg".
    /// Higher values = better quality but larger files.
    /// Default: 80
    /// </summary>
    public int Quality { get; init; } = 80;

    /// <summary>
    /// Scale factor for the output image (0.1 to 1.0).
    /// 0.5 = half size (75% smaller file), 1.0 = full size.
    /// Default: 1.0
    /// </summary>
    public float Scale { get; init; } = 1.0f;

    /// <summary>
    /// Maximum height in pixels for full-page captures.
    /// Prevents extremely tall screenshots. 0 = no limit.
    /// Default: 0
    /// </summary>
    public int MaxHeight { get; init; } = 0;

    /// <summary>
    /// Thumbnail mode: captures at reduced resolution for quick previews.
    /// When enabled, uses 640x360 viewport and JPEG at 60% quality.
    /// </summary>
    public bool Thumbnail { get; init; } = false;

    /// <summary>
    /// Default options (PNG, full quality, full size)
    /// </summary>
    public static ImageOptions Default => new();

    /// <summary>
    /// Optimized preset for smaller file sizes (JPEG 75%, half scale)
    /// </summary>
    public static ImageOptions Optimized => new()
    {
        Format = "jpeg",
        Quality = 75,
        Scale = 0.5f
    };

    /// <summary>
    /// Thumbnail preset for quick previews
    /// </summary>
    public static ImageOptions ThumbnailPreset => new()
    {
        Format = "jpeg",
        Quality = 60,
        Scale = 1.0f,
        Thumbnail = true
    };

    /// <summary>
    /// Validates and normalizes the options
    /// </summary>
    public ImageOptions Normalize()
    {
        var format = Format?.ToLowerInvariant() ?? "png";
        if (format != "png" && format != "jpeg")
        {
            format = "png";
        }

        return this with
        {
            Format = format,
            Quality = Math.Clamp(Quality, 1, 100),
            Scale = Math.Clamp(Scale, 0.1f, 1.0f),
            MaxHeight = Math.Max(0, MaxHeight)
        };
    }
}
