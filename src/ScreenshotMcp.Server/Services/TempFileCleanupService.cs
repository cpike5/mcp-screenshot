using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScreenshotMcp.Server.Configuration;

namespace ScreenshotMcp.Server.Services;

public class TempFileCleanupService : BackgroundService
{
    private readonly ITempFileManager _tempFileManager;
    private readonly IOptions<ScreenshotServerOptions> _options;
    private readonly ILogger<TempFileCleanupService> _logger;

    public TempFileCleanupService(
        ITempFileManager tempFileManager,
        IOptions<ScreenshotServerOptions> options,
        ILogger<TempFileCleanupService> logger)
    {
        _tempFileManager = tempFileManager;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _options.Value.CleanupIntervalMinutes;
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        var maxAge = TimeSpan.FromMinutes(intervalMinutes * 2);

        _logger.LogInformation(
            "Temp file cleanup service started. Interval: {Interval} minutes, Max age: {MaxAge} minutes",
            intervalMinutes,
            maxAge.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await _tempFileManager.CleanupOldFilesAsync(maxAge, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temp file cleanup");
            }
        }
    }
}
