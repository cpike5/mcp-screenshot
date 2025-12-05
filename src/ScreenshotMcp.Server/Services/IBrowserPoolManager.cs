using Microsoft.Playwright;

namespace ScreenshotMcp.Server.Services;

public interface IBrowserPoolManager : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IPage> AcquirePageAsync(CancellationToken cancellationToken = default);
    Task ReleasePageAsync(IPage page);
}
