namespace ScreenshotMcp.Server.Validation;

public interface ISecurityValidator
{
    bool ValidateFilePath(string path, out string? error);
    bool ValidateUrl(string url, out string? error);
}
