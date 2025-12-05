using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Services;
using ScreenshotMcp.Server.Validation;

namespace ScreenshotMcp.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenshotServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<ScreenshotServerOptions>(
            configuration.GetSection(ScreenshotServerOptions.SectionName));

        // Services
        services.AddSingleton<IBrowserPoolManager, BrowserPoolManager>();
        services.AddSingleton<ITempFileManager, TempFileManager>();
        services.AddSingleton<IImageProcessor, ImageProcessor>();
        services.AddScoped<IScreenshotService, ScreenshotService>();

        // Validation
        services.AddSingleton<ISecurityValidator, SecurityValidator>();
        services.AddSingleton<IInputValidator, InputValidator>();

        // Background services
        services.AddHostedService<TempFileCleanupService>();

        return services;
    }
}
