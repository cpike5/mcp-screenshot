using Microsoft.Extensions.Options;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Models;

namespace ScreenshotMcp.Server.Validation;

public class InputValidator : IInputValidator
{
    private readonly SecurityOptions _securityOptions;
    private readonly ISecurityValidator _securityValidator;

    public InputValidator(
        IOptions<ScreenshotServerOptions> options,
        ISecurityValidator securityValidator)
    {
        _securityOptions = options.Value.Security;
        _securityValidator = securityValidator;
    }

    public bool ValidateScreenshotInput(
        string? html,
        string? filePath,
        string? url,
        int width,
        int height,
        string? devicePreset,
        int waitMs,
        out string? error)
    {
        error = null;

        // Check content source mutual exclusivity
        var sourceCount = new[] { html, filePath, url }.Count(s => !string.IsNullOrEmpty(s));

        if (sourceCount == 0)
        {
            error = "One of 'html', 'filePath', or 'url' is required";
            return false;
        }

        if (sourceCount > 1)
        {
            error = "Only one of 'html', 'filePath', or 'url' can be specified";
            return false;
        }

        // Validate device preset if specified
        if (!string.IsNullOrEmpty(devicePreset))
        {
            if (DevicePresets.GetPreset(devicePreset) is null)
            {
                var validPresets = string.Join(", ", DevicePresets.All.Keys);
                error = $"Invalid device preset '{devicePreset}'. Valid presets: {validPresets}";
                return false;
            }
        }

        // Validate viewport dimensions
        if (width <= 0 || width > _securityOptions.MaxViewportWidth)
        {
            error = $"Width must be between 1 and {_securityOptions.MaxViewportWidth}";
            return false;
        }

        if (height <= 0 || height > _securityOptions.MaxViewportHeight)
        {
            error = $"Height must be between 1 and {_securityOptions.MaxViewportHeight}";
            return false;
        }

        // Validate wait time
        if (waitMs < 0 || waitMs > _securityOptions.MaxWaitMs)
        {
            error = $"WaitMs must be between 0 and {_securityOptions.MaxWaitMs}";
            return false;
        }

        // Validate file path if specified
        if (!string.IsNullOrEmpty(filePath))
        {
            if (!_securityValidator.ValidateFilePath(filePath, out error))
            {
                return false;
            }
        }

        // Validate URL if specified
        if (!string.IsNullOrEmpty(url))
        {
            if (!_securityValidator.ValidateUrl(url, out error))
            {
                return false;
            }
        }

        return true;
    }
}
