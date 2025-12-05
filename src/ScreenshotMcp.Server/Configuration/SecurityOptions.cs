namespace ScreenshotMcp.Server.Configuration;

public class SecurityOptions
{
    public string[] AllowedBasePaths { get; set; } = [];
    public string[] BlockedUrlPatterns { get; set; } = [];
    public int MaxViewportWidth { get; set; } = 4096;
    public int MaxViewportHeight { get; set; } = 4096;
    public int MaxWaitMs { get; set; } = 30000;
}
