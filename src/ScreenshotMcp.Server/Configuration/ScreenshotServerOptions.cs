namespace ScreenshotMcp.Server.Configuration;

public class ScreenshotServerOptions
{
    public const string SectionName = "ScreenshotServer";

    public BrowserOptions Browser { get; set; } = new();
    public DefaultsOptions Defaults { get; set; } = new();
    public SecurityOptions Security { get; set; } = new();
    public string TempDirectory { get; set; } = "/tmp/mcp-screenshots";
    public int CleanupIntervalMinutes { get; set; } = 60;
    public int MaxConcurrentPages { get; set; } = 5;
}
