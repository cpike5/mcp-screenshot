namespace ScreenshotMcp.Server.Models;

public static class DevicePresets
{
    private const string ChromeDesktopUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";

    private const string IPadUserAgent =
        "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

    private const string IPhoneSeUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

    private const string IPhone11ProMaxUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";

    public static readonly IReadOnlyDictionary<string, DevicePreset> All = new Dictionary<string, DevicePreset>(StringComparer.OrdinalIgnoreCase)
    {
        ["desktop"] = new DevicePreset("desktop", 1280, 720, 1, ChromeDesktopUserAgent),
        ["desktop-hd"] = new DevicePreset("desktop-hd", 1920, 1080, 1, ChromeDesktopUserAgent),
        ["tablet"] = new DevicePreset("tablet", 768, 1024, 2, IPadUserAgent),
        ["tablet-landscape"] = new DevicePreset("tablet-landscape", 1024, 768, 2, IPadUserAgent),
        ["mobile"] = new DevicePreset("mobile", 375, 667, 2, IPhoneSeUserAgent),
        ["mobile-large"] = new DevicePreset("mobile-large", 414, 896, 3, IPhone11ProMaxUserAgent)
    };

    public static DevicePreset? GetPreset(string name)
    {
        return All.TryGetValue(name, out var preset) ? preset : null;
    }

    public static IEnumerable<DevicePreset> GetAll() => All.Values;
}
