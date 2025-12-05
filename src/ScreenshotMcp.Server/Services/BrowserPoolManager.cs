using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ScreenshotMcp.Server.Configuration;

namespace ScreenshotMcp.Server.Services;

public class BrowserPoolManager : IBrowserPoolManager
{
    private readonly IOptions<ScreenshotServerOptions> _options;
    private readonly ILogger<BrowserPoolManager> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed;

    public BrowserPoolManager(
        IOptions<ScreenshotServerOptions> options,
        ILogger<BrowserPoolManager> logger)
    {
        _options = options;
        _logger = logger;
        _semaphore = new SemaphoreSlim(options.Value.MaxConcurrentPages, options.Value.MaxConcurrentPages);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureBrowserAsync(cancellationToken);
    }

    private async Task EnsureBrowserAsync(CancellationToken cancellationToken = default)
    {
        if (_browser is not null)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is not null)
                return;

            _logger.LogInformation("Initializing Playwright browser");

            _playwright = await Playwright.CreateAsync();

            var browserOptions = _options.Value.Browser;
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = browserOptions.Headless,
                Args = browserOptions.Args
            };

            _browser = browserOptions.Type.ToLowerInvariant() switch
            {
                "firefox" => await _playwright.Firefox.LaunchAsync(launchOptions),
                "webkit" => await _playwright.Webkit.LaunchAsync(launchOptions),
                _ => await _playwright.Chromium.LaunchAsync(launchOptions)
            };

            _logger.LogInformation("Browser initialized: {BrowserType}", browserOptions.Type);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IPage> AcquirePageAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await EnsureBrowserAsync(cancellationToken);
            var page = await _browser!.NewPageAsync();
            _logger.LogDebug("Page acquired, available slots: {Available}", _semaphore.CurrentCount);
            return page;
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    public async Task ReleasePageAsync(IPage page)
    {
        try
        {
            if (!page.IsClosed)
            {
                await page.CloseAsync();
            }
            _logger.LogDebug("Page released");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_browser is not null)
        {
            _logger.LogInformation("Closing browser");
            await _browser.CloseAsync();
            _browser = null;
        }

        if (_playwright is not null)
        {
            _playwright.Dispose();
            _playwright = null;
        }

        _semaphore.Dispose();
        _initLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
