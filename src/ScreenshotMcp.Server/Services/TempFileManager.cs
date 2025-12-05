using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenshotMcp.Server.Configuration;

namespace ScreenshotMcp.Server.Services;

public class TempFileManager : ITempFileManager
{
    private readonly IOptions<ScreenshotServerOptions> _options;
    private readonly ILogger<TempFileManager> _logger;

    public TempFileManager(
        IOptions<ScreenshotServerOptions> options,
        ILogger<TempFileManager> logger)
    {
        _options = options;
        _logger = logger;

        EnsureTempDirectoryExists();
    }

    private void EnsureTempDirectoryExists()
    {
        var tempDir = _options.Value.TempDirectory;
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
            _logger.LogInformation("Created temp directory: {TempDirectory}", tempDir);
        }
    }

    public async Task<string> CreateTempFileAsync(string htmlContent, CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid()}.html";
        var filePath = Path.Combine(_options.Value.TempDirectory, fileName);

        await File.WriteAllTextAsync(filePath, htmlContent, cancellationToken);
        _logger.LogDebug("Created temp file: {FilePath}", filePath);

        return filePath;
    }

    public Task DeleteTempFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted temp file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", filePath);
        }

        return Task.CompletedTask;
    }

    public Task CleanupOldFilesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var tempDir = _options.Value.TempDirectory;
        if (!Directory.Exists(tempDir))
            return Task.CompletedTask;

        var cutoff = DateTime.UtcNow - maxAge;
        var files = Directory.GetFiles(tempDir, "*.html");
        var deletedCount = 0;

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var creationTime = File.GetCreationTimeUtc(file);
                if (creationTime < cutoff)
                {
                    File.Delete(file);
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old temp file: {FilePath}", file);
            }
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old temp files", deletedCount);
        }

        return Task.CompletedTask;
    }
}
