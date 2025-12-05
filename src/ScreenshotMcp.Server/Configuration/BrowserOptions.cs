namespace ScreenshotMcp.Server.Configuration;

public class BrowserOptions
{
    public string Type { get; set; } = "Chromium";
    public bool Headless { get; set; } = true;
    public string[] Args { get; set; } = ["--disable-gpu", "--no-sandbox"];
}
