namespace ScreenshotMcp.Server.Models;

public record DevicePreset(
    string Name,
    int Width,
    int Height,
    float Scale,
    string UserAgent
);
