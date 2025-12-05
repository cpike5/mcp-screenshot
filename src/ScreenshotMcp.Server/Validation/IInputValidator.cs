namespace ScreenshotMcp.Server.Validation;

public interface IInputValidator
{
    bool ValidateScreenshotInput(
        string? html,
        string? filePath,
        string? url,
        int width,
        int height,
        string? devicePreset,
        int waitMs,
        out string? error);
}
