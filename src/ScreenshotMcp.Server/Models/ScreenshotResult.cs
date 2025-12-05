namespace ScreenshotMcp.Server.Models;

public record ScreenshotResult(string Base64Data, string MimeType, ViewportConfig? Viewport = null);
