using Microsoft.Extensions.Options;
using ScreenshotMcp.Server.Configuration;

namespace ScreenshotMcp.Server.Validation;

public class SecurityValidator : ISecurityValidator
{
    private readonly SecurityOptions _securityOptions;

    public SecurityValidator(IOptions<ScreenshotServerOptions> options)
    {
        _securityOptions = options.Value.Security;
    }

    public bool ValidateFilePath(string path, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "File path cannot be empty";
            return false;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception)
        {
            error = "Invalid file path format";
            return false;
        }

        if (!File.Exists(fullPath))
        {
            error = $"File not found: {path}";
            return false;
        }

        // Check allowed base paths if configured
        if (_securityOptions.AllowedBasePaths.Length > 0)
        {
            var isAllowed = _securityOptions.AllowedBasePaths.Any(basePath =>
                fullPath.StartsWith(Path.GetFullPath(basePath), StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                error = "File path is not within allowed directories";
                return false;
            }
        }

        return true;
    }

    public bool ValidateUrl(string url, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL cannot be empty";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            error = "Invalid URL format";
            return false;
        }

        // Only allow http and https by default
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            error = $"URL scheme '{uri.Scheme}' is not allowed. Only http and https are supported.";
            return false;
        }

        // Check blocked patterns
        if (_securityOptions.BlockedUrlPatterns.Length > 0)
        {
            var isBlocked = _securityOptions.BlockedUrlPatterns.Any(pattern =>
                uri.Host.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                uri.AbsoluteUri.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (isBlocked)
            {
                error = "URL matches a blocked pattern";
                return false;
            }
        }

        return true;
    }
}
