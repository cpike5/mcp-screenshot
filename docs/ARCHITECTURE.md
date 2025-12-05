# Architecture Documentation

## Table of Contents

- [System Overview](#system-overview)
- [High-Level Architecture](#high-level-architecture)
- [Component Architecture](#component-architecture)
- [Data Flow](#data-flow)
- [Integration Points](#integration-points)
- [Design Patterns](#design-patterns)
- [Security Architecture](#security-architecture)
- [Performance Considerations](#performance-considerations)
- [Scalability and Extension Points](#scalability-and-extension-points)

---

## System Overview

The MCP Screenshot Server is a .NET 8.0 application that implements the Model Context Protocol (MCP) to provide browser-based rendering and screenshot capabilities. It enables AI agents to generate HTML/CSS/JS prototypes, capture visual output at various viewport sizes, and iterate on designs through a visual feedback loop.

### Core Design Principles

1. **Protocol-Driven**: Implements MCP as the primary interface, enabling integration with any MCP-compatible client
2. **Resource-Efficient**: Uses browser pooling and concurrency control to optimize resource usage
3. **Security-First**: Multi-layer validation and sandboxing to prevent malicious input exploitation
4. **Extensible**: Clean separation of concerns enables easy addition of new tools and features
5. **Observable**: Structured logging throughout the application for debugging and monitoring

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 8.0 |
| Protocol | Model Context Protocol (MCP) v0.4.1 |
| Transport | stdio (JSON-RPC 2.0) |
| Browser Engine | Playwright (Chromium/Firefox/WebKit) |
| Image Processing | SkiaSharp 2.88.8 |
| DI/Hosting | Microsoft.Extensions.Hosting |
| Configuration | Microsoft.Extensions.Configuration |

---

## High-Level Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      MCP Client Layer                         │
│               (Claude Desktop / Claude API)                   │
└────────────────────────┬─────────────────────────────────────┘
                         │
                         │ stdio transport (JSON-RPC 2.0)
                         │
┌────────────────────────▼─────────────────────────────────────┐
│                   .NET Generic Host                           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ModelContextProtocol SDK                 │   │
│  │             (Tool Router/Dispatcher)                  │   │
│  └────────────────────┬─────────────────────────────────┘   │
│                       │                                       │
│  ┌────────────────────▼─────────────────────────────────┐   │
│  │                  Tool Handlers                        │   │
│  │   ┌──────────────┬──────────────┬──────────────┐    │   │
│  │   │ Screenshot   │ Screenshot   │ List         │    │   │
│  │   │ Page Tool    │ Multi Tool   │ Presets Tool │    │   │
│  │   └──────┬───────┴──────┬───────┴──────────────┘    │   │
│  │          │               │                            │   │
│  │          └───────┬───────┘                            │   │
│  │                  │                                     │   │
│  │   ┌──────────────▼─────────────────────────────┐    │   │
│  │   │          Validation Layer                   │    │   │
│  │   │   ┌──────────────┬──────────────┐          │    │   │
│  │   │   │ Input        │ Security     │          │    │   │
│  │   │   │ Validator    │ Validator    │          │    │   │
│  │   │   └──────────────┴──────────────┘          │    │   │
│  │   └──────────────┬─────────────────────────────┘    │   │
│  │                  │                                     │   │
│  │   ┌──────────────▼─────────────────────────────┐    │   │
│  │   │          Service Layer                      │    │   │
│  │   │   ┌──────────────┬──────────────────┐      │    │   │
│  │   │   │ Screenshot   │ Image            │      │    │   │
│  │   │   │ Service      │ Processor        │      │    │   │
│  │   │   └──────┬───────┴──────────────────┘      │    │   │
│  │   │          │                                   │    │   │
│  │   │   ┌──────▼───────┬──────────────────┐      │    │   │
│  │   │   │ TempFile     │ TempFile         │      │    │   │
│  │   │   │ Manager      │ CleanupService   │      │    │   │
│  │   │   └──────────────┴──────────────────┘      │    │   │
│  │   └──────────────┬─────────────────────────────┘    │   │
│  │                  │                                     │   │
│  │   ┌──────────────▼─────────────────────────────┐    │   │
│  │   │      Browser Pool Manager                   │    │   │
│  │   │   (Concurrency Control & Lifecycle)         │    │   │
│  │   └──────────────┬─────────────────────────────┘    │   │
│  └──────────────────┼─────────────────────────────────┘   │
│                     │                                       │
│  ┌──────────────────▼─────────────────────────────────┐   │
│  │              Playwright Engine                      │   │
│  │         (Chromium / Firefox / WebKit)               │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

---

## Component Architecture

The application follows a layered architecture pattern with clear separation of concerns:

### Presentation Layer

**Responsibilities:**
- Expose MCP tools to clients
- Parameter binding and validation
- Error response formatting
- Result serialization

**Components:**
- `ScreenshotPageTool` - Single screenshot capture
- `ScreenshotMultiTool` - Multi-viewport capture
- `ListPresetsTool` - Device preset enumeration

**Location:** `/src/ScreenshotMcp.Server/Tools/`

**Key Design:**
- Each tool is decorated with `[McpServerToolType]` for automatic discovery
- Tool methods use `[McpServerTool(Name = "...")]` for MCP registration
- Parameters use `[Description("...")]` for automatic schema generation
- Tools delegate all business logic to service layer

### Validation Layer

**Responsibilities:**
- Input parameter validation
- Security policy enforcement
- File path and URL validation
- Viewport dimension limits

**Components:**

#### InputValidator
Validates business rules and constraints:
- Exactly one content source (html/filePath/url) is provided
- Device preset exists (case-insensitive lookup)
- Viewport dimensions within configured limits
- Wait times within security bounds

#### SecurityValidator
Enforces security policies:
- File paths exist and are within allowed directories
- URLs use http/https schemes only
- URLs don't match blocked patterns
- Path traversal prevention

**Location:** `/src/ScreenshotMcp.Server/Validation/`

**Key Design:**
- Interface-based for testability and extensibility
- Fail-fast validation with descriptive error messages
- Configured via `SecurityOptions` from appsettings

### Service Layer

**Responsibilities:**
- Core business logic
- Browser interaction orchestration
- Image processing pipeline
- Temporary file lifecycle management
- Background cleanup tasks

**Components:**

#### ScreenshotService
**Interface:** `IScreenshotService`

Core orchestration service that:
1. Resolves content sources (html/file/url) to navigable URLs
2. Acquires browser pages from the pool
3. Configures viewport, user agent, and color scheme
4. Navigates to content with timeout handling
5. Waits for selectors and fixed delays
6. Captures screenshots (viewport or full page)
7. Delegates image processing
8. Releases resources and cleans up temp files

**Key Methods:**
```csharp
Task<ScreenshotResult> CaptureAsync(
    ScreenshotRequest request,
    CancellationToken ct);

Task<IReadOnlyList<ScreenshotResult>> CaptureMultipleAsync(
    ContentSource source,
    string content,
    IEnumerable<ViewportConfig> viewports,
    string? waitForSelector,
    int waitMs,
    bool darkMode,
    ImageOptions? imageOptions,
    CancellationToken ct);
```

#### ImageProcessor
**Interface:** `IImageProcessor`

Handles image format conversion and optimization:
- PNG to JPEG conversion with quality control
- Image scaling (down-sampling only, 0.1 to 1.0)
- SkiaSharp-based encoding/decoding
- Pass-through optimization when no processing needed

**Processing Decision Tree:**
```
Input: PNG bytes from Playwright

format == "png" && scale >= 1.0
    ↓ NO
    └→ Return original bytes (pass-through)

    ↓ YES
    └→ Decode PNG to SKBitmap
       ↓
       scale < 1.0?
       ↓ YES
       └→ Calculate new dimensions
          Create scaled SKBitmap (Medium quality filter)
       ↓
       Encode to target format
       - PNG: Quality 100 (lossless)
       - JPEG: User-specified quality (1-100)
       ↓
       Return processed bytes + MIME type
```

#### TempFileManager
**Interface:** `ITempFileManager`

Manages temporary HTML file lifecycle:
- Creates uniquely-named files in configured temp directory
- Returns file:// URLs for browser navigation
- Deletes files immediately after use
- Provides orphan cleanup for background service

**Naming Strategy:** `{Guid.NewGuid()}.html`

#### TempFileCleanupService
**Type:** `BackgroundService`

Periodic cleanup of orphaned temp files:
- Runs at configured interval (default: 60 minutes)
- Removes files older than 2x interval (default: 120 minutes)
- Handles exceptions gracefully without crashing
- Logs cleanup operations for monitoring

**Location:** `/src/ScreenshotMcp.Server/Services/`

### Infrastructure Layer

**Responsibilities:**
- Browser lifecycle management
- Concurrency control
- Resource pooling
- Configuration management

**Components:**

#### BrowserPoolManager
**Interface:** `IBrowserPoolManager : IAsyncDisposable`

Central browser management with:

**Lazy Initialization:**
```csharp
private SemaphoreSlim _initLock = new(1, 1);

async Task EnsureBrowserAsync()
{
    if (_browser is not null) return; // Fast path

    await _initLock.WaitAsync();
    try
    {
        if (_browser is not null) return; // Double-check

        _playwright = await Playwright.CreateAsync();
        _browser = await LaunchBrowserAsync();
    }
    finally
    {
        _initLock.Release();
    }
}
```

**Concurrency Control:**
```csharp
private SemaphoreSlim _pageSemaphore;

async Task<IPage> AcquirePageAsync()
{
    await _pageSemaphore.WaitAsync(ct);
    try
    {
        await EnsureBrowserAsync();
        return await _browser.NewPageAsync();
    }
    catch
    {
        _pageSemaphore.Release(); // Release on failure
        throw;
    }
}

async Task ReleasePageAsync(IPage page)
{
    try
    {
        await page.CloseAsync();
    }
    finally
    {
        _pageSemaphore.Release(); // Always release
    }
}
```

**Lifecycle Management:**
- Implements `IAsyncDisposable` for proper cleanup
- Closes all pages before disposing browser
- Disposes Playwright instance on shutdown

**Location:** `/src/ScreenshotMcp.Server/Services/BrowserPoolManager.cs`

---

## Data Flow

### Single Screenshot Capture Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Client Request (screenshot_page)                         │
│    - html/filePath/url                                       │
│    - viewport config                                         │
│    - wait conditions                                         │
│    - image options                                           │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. ScreenshotPageTool                                        │
│    - Parse parameters                                        │
│    - Call validators                                         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. InputValidator                                            │
│    ✓ Exactly one content source                             │
│    ✓ Viewport dimensions valid                              │
│    ✓ Device preset exists (if provided)                     │
│    ✓ Wait times within limits                               │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. SecurityValidator                                         │
│    ✓ File path exists and within allowed directories        │
│    ✓ URL uses http/https scheme                             │
│    ✓ URL not in blocked patterns                            │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. ScreenshotService.CaptureAsync()                          │
│    - Build ScreenshotRequest                                │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. BrowserPoolManager.AcquirePageAsync()                     │
│    - Wait on semaphore (concurrency control)                │
│    - Ensure browser initialized (lazy)                      │
│    - Create new page                                         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 7. Configure Page                                            │
│    - Set viewport size                                       │
│    - Set user agent (if preset)                              │
│    - Set color scheme (dark mode)                            │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 8. Resolve Content Source                                    │
│    - html → TempFileManager.CreateTempFile()                │
│    - filePath → file:// URL                                 │
│    - url → direct URL                                        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 9. Navigate to Content                                       │
│    - page.GotoAsync(url, NetworkIdle)                       │
│    - Timeout: configured (default 30s)                       │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 10. Wait for Conditions                                      │
│     - waitForSelector (if specified)                         │
│     - fixed delay waitMs                                     │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 11. Capture Screenshot                                       │
│     - fullPage=true → entire scrollable content             │
│     - fullPage=false → viewport only                        │
│     - Returns PNG bytes                                      │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 12. ImageProcessor.Process()                                 │
│     - Format conversion (PNG→JPEG if needed)                │
│     - Scaling (if scale < 1.0)                              │
│     - Quality adjustment                                     │
│     - Base64 encoding                                        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 13. Cleanup                                                  │
│     - BrowserPoolManager.ReleasePageAsync()                 │
│     - TempFileManager.DeleteTempFile() (if html source)     │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 14. Return Result                                            │
│     {                                                        │
│       type: "image",                                         │
│       data: "<base64>",                                      │
│       mimeType: "image/png" | "image/jpeg"                   │
│     }                                                        │
└─────────────────────────────────────────────────────────────┘
```

### Multi-Viewport Capture Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Client Request (screenshot_multi)                         │
│    - viewports: ["desktop", "mobile", {...}]                │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Parse Viewports JSON                                      │
│    - Preset names → DevicePresets.All lookup                │
│    - Custom objects → ViewportConfig                         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Validation (same as single screenshot)                    │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. ScreenshotService.CaptureMultipleAsync()                  │
│    - Acquire SINGLE page from pool                          │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Navigate to Content ONCE                                  │
│    - Same as single screenshot                               │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. For Each Viewport:                                        │
│    ┌──────────────────────────────────────────────┐         │
│    │ a. Resize viewport (page.SetViewportSizeAsync)│         │
│    │ b. Wait for conditions                         │         │
│    │ c. Capture screenshot                          │         │
│    │ d. Process image                               │         │
│    │ e. Add to results with viewport annotation     │         │
│    └──────────────────────────────────────────────┘         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 7. Cleanup and Return                                        │
│    - Release page                                            │
│    - Delete temp file                                        │
│    - Return array of results                                 │
└─────────────────────────────────────────────────────────────┘
```

**Key Optimization:** Multi-viewport capture navigates only once and reuses the same page, resizing the viewport for each capture.

### Temp File Lifecycle

```
Raw HTML Input
      │
      ▼
┌──────────────────────────────────────┐
│ TempFileManager.CreateTempFile()     │
│ - Generate GUID                      │
│ - Write to temp directory            │
│ - Return file:// URL                 │
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│ Browser navigates to file:// URL     │
│ Screenshot captured                  │
└──────────────┬───────────────────────┘
               │
               ▼
┌──────────────────────────────────────┐
│ TempFileManager.DeleteTempFile()     │
│ - Immediate deletion after use       │
└──────────────────────────────────────┘

        Parallel Track:
               │
               ▼
┌──────────────────────────────────────┐
│ TempFileCleanupService               │
│ - Runs every 60 minutes (default)    │
│ - Scans temp directory               │
│ - Deletes files older than 120 min   │
│ - Catches orphaned files from crashes│
└──────────────────────────────────────┘
```

---

## Integration Points

### MCP Protocol Integration

**SDK:** `ModelContextProtocol` v0.4.1-preview.1

**Integration Method:** Attribute-based tool registration

```csharp
// Program.cs
builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly(typeof(Program).Assembly);
```

**Tool Discovery:**
- Assembly scanning finds all classes with `[McpServerToolType]`
- Methods with `[McpServerTool(Name = "...")]` are exposed as tools
- Parameter descriptions from `[Description("...")]` generate JSON schema
- Return types automatically serialized to MCP response format

**Transport:**
- stdio-based JSON-RPC 2.0
- Synchronous request/response pattern
- No session state required

### Playwright Integration

**Browser Selection:**
```csharp
var browser = browserType switch
{
    "Chromium" => await playwright.Chromium.LaunchAsync(options),
    "Firefox" => await playwright.Firefox.LaunchAsync(options),
    "Webkit" => await playwright.Webkit.LaunchAsync(options),
    _ => throw new InvalidOperationException()
};
```

**Page Configuration:**
```csharp
await page.SetViewportSizeAsync(width, height);
await page.EmulateMediaAsync(new()
{
    ColorScheme = darkMode ? ColorScheme.Dark : ColorScheme.Light
});
if (!string.IsNullOrEmpty(userAgent))
    await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
    {
        ["User-Agent"] = userAgent
    });
```

**Navigation:**
```csharp
await page.GotoAsync(url, new()
{
    WaitUntil = WaitUntilState.NetworkIdle,
    Timeout = timeout
});
```

**Screenshot:**
```csharp
var bytes = await page.ScreenshotAsync(new()
{
    FullPage = fullPage,
    Type = ScreenshotType.Png
});
```

### SkiaSharp Integration

**Image Decoding:**
```csharp
using var inputStream = new SKMemoryStream(pngBytes);
using var bitmap = SKBitmap.Decode(inputStream);
```

**Scaling:**
```csharp
var newWidth = (int)(bitmap.Width * scale);
var newHeight = (int)(bitmap.Height * scale);

var resizeInfo = new SKImageInfo(newWidth, newHeight);
using var scaledBitmap = bitmap.Resize(resizeInfo, SKFilterQuality.Medium);
```

**Encoding:**
```csharp
var format = targetFormat == "jpeg"
    ? SKEncodedImageFormat.Jpeg
    : SKEncodedImageFormat.Png;

using var image = SKImage.FromBitmap(bitmap);
using var data = image.Encode(format, quality);
return data.ToArray();
```

---

## Design Patterns

### Dependency Injection Pattern

**Implementation:** Microsoft.Extensions.DependencyInjection

All services registered in `ServiceCollectionExtensions`:
```csharp
services.AddSingleton<IBrowserPoolManager, BrowserPoolManager>();
services.AddScoped<IScreenshotService, ScreenshotService>();
services.AddScoped<IImageProcessor, ImageProcessor>();
services.AddScoped<ITempFileManager, TempFileManager>();
services.AddSingleton<IInputValidator, InputValidator>();
services.AddSingleton<ISecurityValidator, SecurityValidator>();
services.AddHostedService<TempFileCleanupService>();
```

**Lifetime Scopes:**
- **Singleton:** Browser pool, validators, configuration (shared state)
- **Scoped:** Services created per-request (isolated execution)
- **Hosted Service:** Background services (TempFileCleanupService)

### Options Pattern

**Configuration Binding:**
```csharp
services.Configure<ScreenshotServerOptions>(
    configuration.GetSection(ScreenshotServerOptions.SectionName));
```

**Consumption:**
```csharp
public class ScreenshotService
{
    private readonly ScreenshotServerOptions _options;

    public ScreenshotService(IOptions<ScreenshotServerOptions> options)
    {
        _options = options.Value;
    }
}
```

**Benefits:**
- Strongly-typed configuration
- Validation at startup
- Hot-reload support
- Environment variable overrides

### Repository Pattern (Implicit)

`DevicePresets.cs` acts as an in-memory repository:
```csharp
public static class DevicePresets
{
    public static readonly IReadOnlyDictionary<string, DevicePreset> All = ...;

    public static bool TryGet(string name, out DevicePreset preset)
    {
        return All.TryGetValue(name, out preset);
    }
}
```

### Strategy Pattern

**ImageProcessor** uses strategy for format handling:
```csharp
var (bytes, mimeType) = format switch
{
    "jpeg" => (EncodeJpeg(bitmap, quality), "image/jpeg"),
    "png" => (EncodePng(bitmap), "image/png"),
    _ => (originalBytes, "image/png")
};
```

### Template Method Pattern

**ScreenshotService.CaptureAsync()** defines the template:
1. Validate input
2. Acquire page
3. Configure page
4. Navigate
5. Wait
6. Capture
7. Process
8. Cleanup

Subclasses can override individual steps through DI replacement.

### Facade Pattern

**ScreenshotService** acts as a facade over:
- BrowserPoolManager
- TempFileManager
- ImageProcessor
- Playwright API

Simplifies the tool layer by providing a unified interface.

### Object Pool Pattern

**BrowserPoolManager** implements browser pooling:
- Single browser instance (singleton)
- Semaphore-controlled page allocation
- Explicit acquire/release lifecycle

### Double-Checked Locking Pattern

**Browser initialization:**
```csharp
if (_browser is not null) return; // First check (no lock)

await _initLock.WaitAsync();
try
{
    if (_browser is not null) return; // Second check (with lock)
    _browser = await LaunchBrowserAsync();
}
finally
{
    _initLock.Release();
}
```

---

## Security Architecture

### Defense in Depth

```
┌─────────────────────────────────────────────────────────────┐
│                       Layer 1: Input Validation              │
│ - Parameter type checking                                    │
│ - Range validation (dimensions, timeouts)                    │
│ - Content source mutual exclusivity                          │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                       Layer 2: Security Validation           │
│ - File path normalization (Path.GetFullPath)                │
│ - Whitelist enforcement (AllowedBasePaths)                  │
│ - URL scheme validation (http/https only)                   │
│ - Blocked pattern matching                                  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                       Layer 3: Resource Limits               │
│ - Concurrency control (MaxConcurrentPages)                  │
│ - Timeout enforcement (navigation, wait)                    │
│ - Dimension caps (MaxViewportWidth/Height)                  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│                       Layer 4: Browser Sandbox               │
│ - Playwright browser isolation                              │
│ - Headless mode (no UI interaction)                         │
│ - No persistent state between captures                      │
└─────────────────────────────────────────────────────────────┘
```

### Threat Mitigation Matrix

| Threat | Severity | Mitigation | Implementation |
|--------|----------|------------|----------------|
| Arbitrary File Read | Critical | Path whitelist | SecurityValidator checks AllowedBasePaths |
| Path Traversal | Critical | Path normalization | Path.GetFullPath() + whitelist check |
| SSRF | High | URL validation | http/https only + BlockedUrlPatterns |
| Resource Exhaustion (Memory) | Medium | Dimension limits | MaxViewportWidth/Height enforcement |
| Resource Exhaustion (CPU) | Medium | Concurrency limit | SemaphoreSlim in BrowserPoolManager |
| Resource Exhaustion (Time) | Medium | Timeout limits | MaxWaitMs cap + navigation timeout |
| Disk Exhaustion | Low | Temp file cleanup | Immediate deletion + background service |
| XSS via HTML Content | Low | Browser sandboxing | Isolated browser context, no persistence |

### File Path Security

**Normalization:**
```csharp
var fullPath = Path.GetFullPath(filePath);
```

**Whitelist Enforcement:**
```csharp
if (_options.AllowedBasePaths.Length > 0)
{
    var allowed = _options.AllowedBasePaths.Any(basePath =>
        fullPath.StartsWith(basePath,
            StringComparison.OrdinalIgnoreCase));

    if (!allowed)
        throw new SecurityException("File path not in allowed directories");
}
```

**Security Properties:**
- `../` sequences normalized away
- Symlinks resolved to real path
- Case-insensitive on Windows (NTFS)
- Empty whitelist = allow all (dev mode)

### URL Security

**Validation:**
```csharp
if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    throw new ArgumentException("Invalid URL");

if (uri.Scheme != "http" && uri.Scheme != "https")
    throw new SecurityException("Only HTTP/HTTPS allowed");

foreach (var pattern in _options.BlockedUrlPatterns)
{
    if (Regex.IsMatch(uri.AbsoluteUri, pattern))
        throw new SecurityException("URL matches blocked pattern");
}
```

**Blocked Schemes:**
- `file://` (local file access)
- `javascript:` (script execution)
- `data:` (data URIs)
- `ftp://`, `ssh://`, etc.

---

## Performance Considerations

### Latency Breakdown

| Operation | Typical Duration | Optimization |
|-----------|------------------|--------------|
| Browser Launch | 2-5 seconds | Lazy initialization (one-time) |
| Page Creation | 50-200 ms | Pooling (reuse browser) |
| Navigation (local) | 100-500 ms | NetworkIdle wait state |
| Navigation (remote) | 0.5-3 seconds | Network-dependent |
| Screenshot Capture | 50-200 ms | Scales with page complexity |
| Image Processing | 0-200 ms | Pass-through when possible |
| Base64 Encoding | 10-50 ms | Unavoidable for MCP transport |

**Total Request Time:**
- Simple HTML: 200-700 ms (after browser initialized)
- Remote URL: 1-4 seconds
- Multi-viewport (3): 600-2000 ms

### Memory Characteristics

| Component | Memory Usage | Notes |
|-----------|--------------|-------|
| Playwright Process | 100-200 MB | Chromium background |
| Per Page Instance | 50-150 MB | DOM + rendering |
| Screenshot Buffer | 1-50 MB | Full-page can be large |
| SkiaSharp Processing | 2x image size | Decode + encode buffers |
| Base64 String | 1.33x image size | Character encoding overhead |

**Memory Optimization Strategies:**
1. Use `scale` parameter to reduce dimensions
2. Use `maxHeight` to limit full-page captures
3. Use JPEG format for ~60-90% size reduction
4. Prompt page release after capture
5. Limit `MaxConcurrentPages` based on available RAM

### Throughput Analysis

With default config (`MaxConcurrentPages: 5`):

| Workload | Expected Throughput |
|----------|---------------------|
| Simple HTML (< 100KB) | 10-20 req/s |
| Complex pages (images, CSS) | 2-5 req/s |
| Remote URLs | 1-3 req/s |
| Full-page captures | 1-2 req/s |

**Bottlenecks:**
1. **Network I/O** for remote URLs (uncontrollable)
2. **Page rendering** for complex content (Chromium-dependent)
3. **Semaphore contention** when requests exceed MaxConcurrentPages

### Optimization Techniques

#### 1. Browser Pooling
**Impact:** Eliminates 2-5 second browser startup per request

**Implementation:**
```csharp
private IBrowser? _browser; // Shared instance
```

#### 2. Concurrency Control
**Impact:** Prevents memory exhaustion from unlimited parallel captures

**Implementation:**
```csharp
private SemaphoreSlim _semaphore = new(maxConcurrent, maxConcurrent);
```

#### 3. Lazy Initialization
**Impact:** Fast server startup, browser launched on-demand

**Trade-off:** First request slower by 2-5 seconds

#### 4. Image Pass-Through
**Impact:** 0-100ms saved when no processing needed

**Condition:** `format == "png" && scale >= 1.0`

#### 5. Multi-Viewport Reuse
**Impact:** Single navigation for multiple captures

**Savings:** (N-1) × navigation time for N viewports

---

## Scalability and Extension Points

### Horizontal Scaling

**Current Limitation:** Single browser instance per process

**Scaling Strategy:**
1. Run multiple server instances on different ports
2. Use load balancer (if MCP supports it) or client-side distribution
3. Each instance manages its own browser pool

**Future Enhancement:** Multi-browser pool with round-robin selection

### Vertical Scaling

**Configuration:**
```json
{
  "MaxConcurrentPages": 10  // Increase based on RAM
}
```

**Guidelines:**
- ~200 MB RAM per concurrent page
- Monitor memory usage and adjust
- Consider CPU cores (Chromium is multi-threaded)

### Adding New Device Presets

**Location:** `/src/ScreenshotMcp.Server/Models/DevicePresets.cs`

**Steps:**
1. Add entry to `All` dictionary
2. Provide name, dimensions, scale, and user agent
3. No code changes elsewhere needed (automatic lookup)

**Example:**
```csharp
["ipad-pro"] = new DevicePreset(
    "ipad-pro",
    1024,
    1366,
    2.0f,
    "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) ..."
),
```

### Adding New Image Formats

**Location:** `/src/ScreenshotMcp.Server/Services/ImageProcessor.cs`

**Steps:**
1. Update `ImageOptions.Normalize()` to accept new format
2. Add format case to `Process()` method
3. Use SkiaSharp encoder for format

**Example (WebP):**
```csharp
if (normalizedOptions.Format == "webp")
{
    using var image = SKImage.FromBitmap(scaledBitmap ?? bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
    return (data.ToArray(), "image/webp");
}
```

### Adding New Tools

**Steps:**
1. Create class in `/src/ScreenshotMcp.Server/Tools/`
2. Decorate class with `[McpServerToolType]`
3. Create method with `[McpServerTool(Name = "tool_name")]`
4. Use `[Description("...")]` for parameters
5. Inject required services via constructor

**Example:**
```csharp
[McpServerToolType]
public class PdfExportTool
{
    private readonly IScreenshotService _screenshotService;

    public PdfExportTool(IScreenshotService screenshotService)
    {
        _screenshotService = screenshotService;
    }

    [McpServerTool(Name = "export_pdf")]
    [Description("Exports a page as PDF")]
    public async Task<object> ExecuteAsync(
        [Description("URL to export")] string url)
    {
        // Implementation
    }
}
```

### Custom Validators

**Steps:**
1. Implement `IInputValidator` or `ISecurityValidator`
2. Add custom validation logic
3. Replace registration in `ServiceCollectionExtensions`

**Example (Custom Security Validator):**
```csharp
services.AddSingleton<ISecurityValidator, CustomSecurityValidator>();
```

### Browser Engine Selection

**Configuration:**
```json
{
  "Browser": {
    "Type": "Firefox"  // or "Webkit"
  }
}
```

**Installation:**
```bash
playwright install firefox
```

**Characteristics:**
- **Chromium:** Best compatibility, most tested
- **Firefox:** Better privacy, different rendering
- **WebKit:** Safari compatibility testing

### Extension Plugin Architecture (Future)

**Proposed Design:**
```csharp
public interface IScreenshotPlugin
{
    Task<byte[]> ProcessAsync(byte[] screenshot,
                               PluginContext context);
}
```

**Use Cases:**
- Watermarking
- OCR text extraction
- Accessibility analysis
- Visual diffing

**Registration:**
```csharp
services.AddScreenshotPlugin<WatermarkPlugin>();
services.AddScreenshotPlugin<OcrPlugin>();
```

---

## Summary

The MCP Screenshot Server architecture prioritizes:

1. **Modularity:** Clear layer separation enables independent testing and evolution
2. **Performance:** Browser pooling, concurrency control, and image optimization
3. **Security:** Multi-layer validation and sandboxing prevent exploitation
4. **Extensibility:** Plugin points for tools, formats, presets, and validators
5. **Reliability:** Proper resource lifecycle, timeout enforcement, and error handling

The design supports the core MCP use case (AI agent visual feedback) while remaining extensible for future capabilities like PDF export, element screenshots, and accessibility auditing.
