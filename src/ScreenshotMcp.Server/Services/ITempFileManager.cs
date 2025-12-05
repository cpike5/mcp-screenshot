namespace ScreenshotMcp.Server.Services;

public interface ITempFileManager
{
    Task<string> CreateTempFileAsync(string htmlContent, CancellationToken cancellationToken = default);
    Task DeleteTempFileAsync(string filePath);
    Task CleanupOldFilesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}
