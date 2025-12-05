# Developer Documentation

## Table of Contents

- [Development Environment Setup](#development-environment-setup)
- [Project Structure](#project-structure)
- [Building and Running](#building-and-running)
- [Testing Guide](#testing-guide)
- [Code Conventions](#code-conventions)
- [Adding New Features](#adding-new-features)
- [Debugging Tips](#debugging-tips)
- [API Reference](#api-reference)
- [Contributing Guidelines](#contributing-guidelines)

---

## Development Environment Setup

### Prerequisites

| Requirement | Version | Purpose |
|-------------|---------|---------|
| .NET SDK | 8.0 or higher | Runtime and build tools |
| Git | Latest | Version control |
| IDE | VS Code, Rider, or Visual Studio | Development environment |
| Playwright CLI | Latest | Browser binary management |

### Installation Steps

#### 1. Install .NET 8.0 SDK

**Windows:**
```powershell
winget install Microsoft.DotNet.SDK.8
```

**macOS:**
```bash
brew install dotnet@8
```

**Linux (Ubuntu/Debian):**
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

Verify installation:
```bash
dotnet --version  # Should output 8.0.x
```

#### 2. Clone Repository

```bash
git clone <repository-url>
cd screenshot-mcp
```

#### 3. Restore Dependencies

```bash
dotnet restore
```

#### 4. Build Project

```bash
dotnet build
```

#### 5. Install Playwright Browsers

**Windows:**
```powershell
cd src/ScreenshotMcp.Server
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

**Linux/macOS:**
```bash
cd src/ScreenshotMcp.Server
./bin/Debug/net8.0/playwright.sh install chromium
```

**Alternative (Global CLI):**
```bash
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

#### 6. Verify Setup

```bash
dotnet run --project src/ScreenshotMcp.Server
```

The server should start and wait for stdio input. Press Ctrl+C to exit.

### IDE Configuration

#### Visual Studio Code

**Recommended Extensions:**
- C# Dev Kit
- .NET Extension Pack
- EditorConfig for VS Code

**Launch Configuration (.vscode/launch.json):**
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (console)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/ScreenshotMcp.Server/bin/Debug/net8.0/ScreenshotMcp.Server.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/ScreenshotMcp.Server",
      "console": "internalConsole",
      "stopAtEntry": false
    },
    {
      "name": ".NET Core Attach",
      "type": "coreclr",
      "request": "attach"
    }
  ]
}
```

**Tasks Configuration (.vscode/tasks.json):**
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/ScreenshotMcp.sln",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": [
        "test",
        "${workspaceFolder}/ScreenshotMcp.sln"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

#### JetBrains Rider

1. Open solution file: `ScreenshotMcp.sln`
2. Rider will automatically detect .NET 8.0 SDK
3. Set run configuration to `ScreenshotMcp.Server`
4. Enable automatic restore on build

#### Visual Studio

1. Open `ScreenshotMcp.sln`
2. Set `ScreenshotMcp.Server` as startup project
3. Configure launch profile in Properties > Debug

---

## Project Structure

```
/home/cpike/Workspace/screenshot-mcp/
│
├── Directory.Build.props              # Shared build properties
├── ScreenshotMcp.sln                  # Solution file
├── README.md                          # Project overview
├── .gitignore                         # Git ignore rules
│
├── docs/                              # Documentation
│   ├── DESIGN_SPEC.md                 # Design specification
│   ├── ARCHITECTURE.md                # Architecture documentation
│   ├── DEVELOPER.md                   # This file
│   └── USAGE.md                       # User guide
│
├── src/
│   └── ScreenshotMcp.Server/          # Main server project
│       ├── ScreenshotMcp.Server.csproj
│       ├── Program.cs                 # Entry point
│       ├── appsettings.json           # Configuration
│       │
│       ├── Configuration/             # Configuration models
│       │   ├── ScreenshotServerOptions.cs
│       │   ├── BrowserOptions.cs
│       │   ├── DefaultsOptions.cs
│       │   └── SecurityOptions.cs
│       │
│       ├── Models/                    # Domain models
│       │   ├── ContentSource.cs       # Enum
│       │   ├── ViewportConfig.cs      # Record
│       │   ├── DevicePreset.cs        # Record
│       │   ├── DevicePresets.cs       # Static catalog
│       │   ├── ImageOptions.cs        # Record with presets
│       │   ├── ScreenshotRequest.cs   # Request DTO
│       │   └── ScreenshotResult.cs    # Result DTO
│       │
│       ├── Services/                  # Business logic
│       │   ├── IBrowserPoolManager.cs
│       │   ├── BrowserPoolManager.cs
│       │   ├── IScreenshotService.cs
│       │   ├── ScreenshotService.cs
│       │   ├── ITempFileManager.cs
│       │   ├── TempFileManager.cs
│       │   ├── TempFileCleanupService.cs
│       │   ├── IImageProcessor.cs
│       │   └── ImageProcessor.cs
│       │
│       ├── Validation/                # Input and security validation
│       │   ├── IInputValidator.cs
│       │   ├── InputValidator.cs
│       │   ├── ISecurityValidator.cs
│       │   └── SecurityValidator.cs
│       │
│       ├── Tools/                     # MCP tool implementations
│       │   ├── ScreenshotPageTool.cs
│       │   ├── ScreenshotMultiTool.cs
│       │   └── ListPresetsTool.cs
│       │
│       └── Extensions/                # DI and utility extensions
│           └── ServiceCollectionExtensions.cs
│
└── tests/
    └── ScreenshotMcp.Server.Tests/    # Unit and integration tests
        ├── ScreenshotMcp.Server.Tests.csproj
        ├── Unit/
        │   ├── Models/
        │   │   └── DevicePresetsTests.cs
        │   └── Validation/
        │       ├── InputValidatorTests.cs
        │       └── SecurityValidatorTests.cs
        └── Fixtures/                  # Test HTML files
            ├── simple.html
            ├── responsive.html
            └── async-load.html
```

### Key Directories

| Directory | Purpose | Key Files |
|-----------|---------|-----------|
| `Configuration/` | Strongly-typed config models | Options classes bound to appsettings.json |
| `Models/` | Domain entities and DTOs | Records, enums, static catalogs |
| `Services/` | Core business logic | Browser pooling, screenshot capture, image processing |
| `Validation/` | Input and security validation | Guards against invalid/malicious input |
| `Tools/` | MCP tool endpoints | Direct interface to MCP clients |
| `Extensions/` | Service registration | DI container setup |

---

## Building and Running

### Build Commands

**Debug build:**
```bash
dotnet build
```

**Release build:**
```bash
dotnet build -c Release
```

**Clean build:**
```bash
dotnet clean
dotnet build
```

**Build specific project:**
```bash
dotnet build src/ScreenshotMcp.Server/ScreenshotMcp.Server.csproj
```

### Running the Server

**Development mode:**
```bash
dotnet run --project src/ScreenshotMcp.Server
```

**With specific configuration:**
```bash
dotnet run --project src/ScreenshotMcp.Server -- --environment=Development
```

**Release mode:**
```bash
dotnet run --project src/ScreenshotMcp.Server -c Release
```

**With custom appsettings:**
Create `appsettings.Development.json` in the project directory:
```json
{
  "ScreenshotServer": {
    "Browser": {
      "Headless": false  // Run with visible browser for debugging
    }
  }
}
```

### Publishing

**Self-contained deployment (includes .NET runtime):**
```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```

**Framework-dependent deployment (requires .NET 8 on target):**
```bash
dotnet publish -c Release
```

**Single-file deployment:**
```bash
dotnet publish -c Release -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

---

## Testing Guide

### Running Tests

**All tests:**
```bash
dotnet test
```

**Specific test project:**
```bash
dotnet test tests/ScreenshotMcp.Server.Tests/ScreenshotMcp.Server.Tests.csproj
```

**With detailed output:**
```bash
dotnet test --logger "console;verbosity=detailed"
```

**Run specific test:**
```bash
dotnet test --filter "FullyQualifiedName~InputValidatorTests.ValidateContentSource_ExactlyOne_Valid"
```

**Run tests in category:**
```bash
dotnet test --filter "Category=Unit"
```

### Code Coverage

**Install coverage tool:**
```bash
dotnet tool install --global dotnet-coverage
```

**Run tests with coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Generate HTML report:**
```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
```

### Writing Tests

#### Unit Test Example

**Location:** `tests/ScreenshotMcp.Server.Tests/Unit/Validation/InputValidatorTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.Options;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Validation;
using Xunit;

namespace ScreenshotMcp.Server.Tests.Unit.Validation;

public class InputValidatorTests
{
    private readonly IInputValidator _validator;

    public InputValidatorTests()
    {
        var options = Options.Create(new ScreenshotServerOptions
        {
            Security = new SecurityOptions
            {
                MaxViewportWidth = 4096,
                MaxViewportHeight = 4096,
                MaxWaitMs = 30000
            }
        });
        _validator = new InputValidator(options);
    }

    [Fact]
    public void ValidateContentSource_ExactlyOne_Valid()
    {
        // Act & Assert
        _validator.Invoking(v => v.ValidateContentSource(
            html: "<html></html>",
            filePath: null,
            url: null))
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateContentSource_Multiple_ThrowsException()
    {
        // Act & Assert
        _validator.Invoking(v => v.ValidateContentSource(
            html: "<html></html>",
            filePath: "/some/path.html",
            url: null))
            .Should().Throw<ArgumentException>()
            .WithMessage("*exactly one*");
    }
}
```

#### Integration Test Example

**Location:** `tests/ScreenshotMcp.Server.Tests/Integration/ScreenshotServiceTests.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using ScreenshotMcp.Server.Models;
using ScreenshotMcp.Server.Services;
using Xunit;

namespace ScreenshotMcp.Server.Tests.Integration;

public class ScreenshotServiceTests : IAsyncLifetime
{
    private ServiceProvider _services;
    private IScreenshotService _screenshotService;

    public async Task InitializeAsync()
    {
        // Setup DI container
        var services = new ServiceCollection();
        services.AddScreenshotServer(configuration);
        _services = services.BuildServiceProvider();
        _screenshotService = _services.GetRequiredService<IScreenshotService>();
    }

    [Fact]
    public async Task CaptureAsync_SimpleHtml_ReturnsScreenshot()
    {
        // Arrange
        var request = new ScreenshotRequest
        {
            Source = ContentSource.Html,
            Content = "<html><body><h1>Test</h1></body></html>",
            Viewport = new ViewportConfig(1280, 720),
            FullPage = false,
            WaitMs = 0,
            DarkMode = false,
            ImageOptions = ImageOptions.Default
        };

        // Act
        var result = await _screenshotService.CaptureAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Base64Data.Should().NotBeNullOrEmpty();
        result.MimeType.Should().Be("image/png");
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }
}
```

### Test Fixtures

Test HTML files are located in `tests/ScreenshotMcp.Server.Tests/Fixtures/`:

**simple.html:**
```html
<!DOCTYPE html>
<html>
<body>
  <h1>Simple Test Page</h1>
  <p>This is a basic static page.</p>
</body>
</html>
```

**async-load.html:**
```html
<!DOCTYPE html>
<html>
<body>
  <div id="initial">Loading...</div>
  <script>
    setTimeout(() => {
      document.getElementById('initial').innerHTML = '<h1 id="loaded">Content Loaded</h1>';
    }, 500);
  </script>
</body>
</html>
```

Use these fixtures in tests:
```csharp
var fixturePath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "Fixtures",
    "simple.html");
```

---

## Code Conventions

### C# Style Guidelines

The project follows standard C# conventions with some specific preferences:

#### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespaces | PascalCase | `ScreenshotMcp.Server.Services` |
| Classes | PascalCase | `ScreenshotService` |
| Interfaces | IPascalCase | `IScreenshotService` |
| Methods | PascalCase | `CaptureAsync` |
| Private fields | _camelCase | `_browser`, `_semaphore` |
| Parameters | camelCase | `request`, `cancellationToken` |
| Local variables | camelCase | `result`, `viewport` |
| Constants | PascalCase | `MaxConcurrentPages` |

#### File Organization

**One type per file:**
```
ScreenshotService.cs contains only ScreenshotService class
IScreenshotService.cs contains only IScreenshotService interface
```

**File naming matches type name:**
```
class ScreenshotService → ScreenshotService.cs
interface IScreenshotService → IScreenshotService.cs
record ViewportConfig → ViewportConfig.cs
```

#### Code Structure

**Using directives:**
- Place at top of file
- Sort alphabetically
- Group System.* first, then third-party, then project

**Namespace:**
```csharp
namespace ScreenshotMcp.Server.Services;  // File-scoped namespace
```

**Class structure:**
```csharp
public class ScreenshotService : IScreenshotService
{
    // 1. Private fields
    private readonly IBrowserPoolManager _browserPool;
    private readonly ITempFileManager _tempFileManager;

    // 2. Constructor
    public ScreenshotService(
        IBrowserPoolManager browserPool,
        ITempFileManager tempFileManager)
    {
        _browserPool = browserPool;
        _tempFileManager = tempFileManager;
    }

    // 3. Public methods
    public async Task<ScreenshotResult> CaptureAsync(...)
    {
        // Implementation
    }

    // 4. Private methods
    private async Task<string> ResolveContentUrlAsync(...)
    {
        // Implementation
    }
}
```

### Async/Await Conventions

**Always use async suffix:**
```csharp
public async Task<ScreenshotResult> CaptureAsync(...)
```

**Pass CancellationToken:**
```csharp
public async Task<T> MethodAsync(CancellationToken cancellationToken)
```

**Use ConfigureAwait(false) in library code:**
```csharp
var result = await DoWorkAsync().ConfigureAwait(false);
```

### Null Handling

**Enable nullable reference types (project-wide):**
```xml
<Nullable>enable</Nullable>
```

**Use nullable annotations:**
```csharp
public string? OptionalValue { get; set; }  // Can be null
public string RequiredValue { get; set; }   // Cannot be null
```

**Null checks:**
```csharp
ArgumentNullException.ThrowIfNull(parameter);
```

### Error Handling

**Use specific exceptions:**
```csharp
throw new ArgumentException("Invalid format", nameof(format));
throw new FileNotFoundException("HTML file not found", filePath);
throw new InvalidOperationException("Browser not initialized");
```

**Don't catch generic exceptions in libraries:**
```csharp
// BAD
try { ... }
catch (Exception ex) { ... }

// GOOD
try { ... }
catch (TimeoutException ex) { ... }
catch (PlaywrightException ex) { ... }
```

### Logging

**Use structured logging:**
```csharp
_logger.LogInformation(
    "Screenshot captured: {Width}x{Height}, Format: {Format}",
    width, height, format);
```

**Log levels:**
- `Trace`: Very detailed diagnostic info
- `Debug`: Debugging information
- `Information`: General flow (default)
- `Warning`: Unexpected but recoverable
- `Error`: Error in current operation
- `Critical`: Unrecoverable application error

---

## Adding New Features

### Adding a New Device Preset

**1. Open `/src/ScreenshotMcp.Server/Models/DevicePresets.cs`**

**2. Add entry to the `All` dictionary:**
```csharp
["iphone-15-pro"] = new DevicePreset(
    "iphone-15-pro",
    393,      // Width
    852,      // Height
    3.0f,     // Scale (device pixel ratio)
    "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"
),
```

**3. Test the preset:**
```bash
dotnet test --filter "FullyQualifiedName~DevicePresetsTests"
```

**4. Document in USAGE.md**

### Adding a New Image Format

**1. Update `/src/ScreenshotMcp.Server/Models/ImageOptions.cs`:**

Update `Normalize()` method to accept new format:
```csharp
public ImageOptions Normalize()
{
    var normalizedFormat = Format.ToLowerInvariant() switch
    {
        "png" => "png",
        "jpg" or "jpeg" => "jpeg",
        "webp" => "webp",  // NEW
        _ => "png"
    };
    // ...
}
```

**2. Update `/src/ScreenshotMcp.Server/Services/ImageProcessor.cs`:**

Add encoding logic:
```csharp
private (byte[] Bytes, string MimeType) EncodeImage(
    SKBitmap bitmap,
    ImageOptions options)
{
    using var image = SKImage.FromBitmap(bitmap);

    var (format, mimeType) = options.Format switch
    {
        "jpeg" => (SKEncodedImageFormat.Jpeg, "image/jpeg"),
        "webp" => (SKEncodedImageFormat.Webp, "image/webp"),  // NEW
        _ => (SKEncodedImageFormat.Png, "image/png")
    };

    using var data = image.Encode(format, options.Quality);
    return (data.ToArray(), mimeType);
}
```

**3. Add unit tests:**
```csharp
[Fact]
public async Task Process_WebPFormat_ReturnsWebPImage()
{
    // Arrange
    var pngBytes = ...; // Sample PNG
    var options = new ImageOptions { Format = "webp", Quality = 80 };

    // Act
    var result = await _processor.ProcessAsync(pngBytes, options);

    // Assert
    result.MimeType.Should().Be("image/webp");
}
```

### Adding a New MCP Tool

**1. Create tool class in `/src/ScreenshotMcp.Server/Tools/`:**

```csharp
using System.ComponentModel;
using ModelContextProtocol.SDK;

namespace ScreenshotMcp.Server.Tools;

[McpServerToolType]
public class ElementScreenshotTool
{
    private readonly IScreenshotService _screenshotService;
    private readonly ILogger<ElementScreenshotTool> _logger;

    public ElementScreenshotTool(
        IScreenshotService screenshotService,
        ILogger<ElementScreenshotTool> logger)
    {
        _screenshotService = screenshotService;
        _logger = logger;
    }

    [McpServerTool(Name = "screenshot_element")]
    [Description("Captures a screenshot of a specific element")]
    public async Task<object> ExecuteAsync(
        [Description("CSS selector of the element")] string selector,
        [Description("URL to capture")] string url,
        [Description("Viewport width")] int width = 1280,
        [Description("Viewport height")] int height = 720)
    {
        try
        {
            // Implementation
            _logger.LogInformation("Capturing element: {Selector}", selector);

            var result = await _screenshotService.CaptureElementAsync(
                url, selector, width, height);

            return new
            {
                type = "image",
                data = result.Base64Data,
                mimeType = result.MimeType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture element screenshot");
            return new { error = "CAPTURE_FAILED", message = ex.Message };
        }
    }
}
```

**2. Implement service method in `IScreenshotService`:**

```csharp
Task<ScreenshotResult> CaptureElementAsync(
    string url,
    string selector,
    int width,
    int height,
    CancellationToken cancellationToken = default);
```

**3. Tool is automatically discovered** via `WithToolsFromAssembly()` in `Program.cs`

**4. Add tests:**
```csharp
public class ElementScreenshotToolTests
{
    [Fact]
    public async Task ExecuteAsync_ValidSelector_ReturnsScreenshot()
    {
        // Test implementation
    }
}
```

### Adding a New Validator

**1. Create validator interface:**
```csharp
public interface ICustomValidator
{
    void Validate(string input);
}
```

**2. Implement validator:**
```csharp
public class CustomValidator : ICustomValidator
{
    private readonly CustomOptions _options;

    public CustomValidator(IOptions<CustomOptions> options)
    {
        _options = options.Value;
    }

    public void Validate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty");

        // Additional validation logic
    }
}
```

**3. Register in DI:**
```csharp
// In ServiceCollectionExtensions.cs
services.AddSingleton<ICustomValidator, CustomValidator>();
```

**4. Inject and use:**
```csharp
public class SomeTool
{
    private readonly ICustomValidator _validator;

    public SomeTool(ICustomValidator validator)
    {
        _validator = validator;
    }
}
```

---

## Debugging Tips

### Debugging the MCP Server

**1. Enable detailed logging:**

In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.Playwright": "Debug",
      "ScreenshotMcp.Server": "Trace"
    }
  }
}
```

**2. Run with visible browser:**

```json
{
  "ScreenshotServer": {
    "Browser": {
      "Headless": false
    }
  }
}
```

**3. Attach debugger:**

- Set breakpoints in your code
- Run with F5 in VS Code/Rider/Visual Studio
- Server will pause at breakpoints

**4. Inspect stdio communication:**

Since MCP uses stdio, you can't see the JSON-RPC messages directly. Use logging:

```csharp
_logger.LogDebug("Received request: {@Request}", request);
_logger.LogDebug("Sending response: {@Response}", response);
```

### Common Issues

#### Browser Not Launching

**Symptom:** `BrowserNotFound` exception

**Solution:**
```bash
# Reinstall browsers
playwright install chromium

# Check Playwright installation
playwright --version
```

#### Temp Files Accumulating

**Symptom:** `/tmp/mcp-screenshots/` filling up

**Solution:**
1. Check cleanup service is running
2. Verify cleanup interval in config
3. Manually delete orphaned files:
```bash
find /tmp/mcp-screenshots -type f -mtime +1 -delete
```

#### Memory Issues

**Symptom:** OutOfMemoryException or high RAM usage

**Solution:**
1. Reduce `MaxConcurrentPages`
2. Use `scale` parameter to reduce image sizes
3. Use JPEG format instead of PNG
4. Monitor with:
```bash
dotnet-counters monitor --process-id <pid>
```

#### Screenshot Timeouts

**Symptom:** `TimeoutException` during navigation

**Solution:**
1. Increase timeout in config
2. Check network connectivity (for URL captures)
3. Verify content isn't blocking (e.g., waiting for user input)

### Performance Profiling

**1. Use dotnet-trace:**
```bash
dotnet tool install --global dotnet-trace
dotnet-trace collect --process-id <pid> --duration 00:00:30
```

**2. Use dotnet-counters:**
```bash
dotnet tool install --global dotnet-counters
dotnet-counters monitor --process-id <pid> System.Runtime
```

**3. Memory profiling:**
```bash
dotnet tool install --global dotnet-gcdump
dotnet-gcdump collect --process-id <pid>
```

---

## API Reference

### Core Interfaces

#### IScreenshotService

**Purpose:** Core service for capturing screenshots

**Methods:**

```csharp
Task<ScreenshotResult> CaptureAsync(
    ScreenshotRequest request,
    CancellationToken cancellationToken = default);
```

Captures a single screenshot based on the request parameters.

**Parameters:**
- `request`: Screenshot configuration (source, viewport, options)
- `cancellationToken`: Cancellation token

**Returns:** `ScreenshotResult` with base64 image data

**Throws:**
- `ArgumentException`: Invalid request parameters
- `FileNotFoundException`: File not found (for FilePath source)
- `TimeoutException`: Navigation or wait timeout exceeded
- `InvalidOperationException`: Browser not available

---

```csharp
Task<IReadOnlyList<ScreenshotResult>> CaptureMultipleAsync(
    ContentSource source,
    string content,
    IEnumerable<ViewportConfig> viewports,
    string? waitForSelector,
    int waitMs,
    bool darkMode,
    ImageOptions? imageOptions,
    CancellationToken cancellationToken = default);
```

Captures screenshots at multiple viewports (single navigation).

**Parameters:**
- `source`: Content source type (Html/FilePath/Url)
- `content`: Content value (HTML string, file path, or URL)
- `viewports`: Collection of viewport configurations
- `waitForSelector`: Optional CSS selector to wait for
- `waitMs`: Additional wait time in milliseconds
- `darkMode`: Enable dark mode emulation
- `imageOptions`: Image processing options
- `cancellationToken`: Cancellation token

**Returns:** List of `ScreenshotResult`, one per viewport

#### IBrowserPoolManager

**Purpose:** Manages browser lifecycle and page pooling

**Methods:**

```csharp
Task<IPage> AcquirePageAsync(CancellationToken cancellationToken = default);
```

Acquires a page from the pool (blocks if at capacity).

**Returns:** Playwright `IPage` instance

---

```csharp
Task ReleasePageAsync(IPage page);
```

Releases page back to pool (closes page).

**Parameters:**
- `page`: Page to release

#### IImageProcessor

**Purpose:** Image format conversion and optimization

**Methods:**

```csharp
Task<(byte[] Bytes, string MimeType)> ProcessAsync(
    byte[] pngBytes,
    ImageOptions options);
```

Processes screenshot image (format conversion, scaling).

**Parameters:**
- `pngBytes`: PNG image data from Playwright
- `options`: Processing options (format, quality, scale)

**Returns:** Tuple of processed bytes and MIME type

#### IInputValidator

**Purpose:** Business rule validation

**Methods:**

```csharp
void ValidateContentSource(string? html, string? filePath, string? url);
void ValidateViewport(int width, int height);
void ValidateDevicePreset(string? preset);
void ValidateWaitTime(int waitMs);
```

#### ISecurityValidator

**Purpose:** Security policy enforcement

**Methods:**

```csharp
void ValidateFilePath(string filePath);
void ValidateUrl(string url);
```

### Configuration Models

#### ScreenshotServerOptions

```csharp
public class ScreenshotServerOptions
{
    public BrowserOptions Browser { get; set; }
    public DefaultsOptions Defaults { get; set; }
    public SecurityOptions Security { get; set; }
    public string TempDirectory { get; set; }
    public int CleanupIntervalMinutes { get; set; }
    public int MaxConcurrentPages { get; set; }
}
```

#### BrowserOptions

```csharp
public class BrowserOptions
{
    public string Type { get; set; }       // "Chromium", "Firefox", "Webkit"
    public bool Headless { get; set; }     // Default: true
    public string[] Args { get; set; }     // Browser launch arguments
}
```

#### SecurityOptions

```csharp
public class SecurityOptions
{
    public string[] AllowedBasePaths { get; set; }
    public string[] BlockedUrlPatterns { get; set; }
    public int MaxViewportWidth { get; set; }
    public int MaxViewportHeight { get; set; }
    public int MaxWaitMs { get; set; }
}
```

### Domain Models

#### ViewportConfig

```csharp
public record ViewportConfig(
    int Width,
    int Height,
    float Scale = 1.0f
);
```

#### ImageOptions

```csharp
public record ImageOptions
{
    public string Format { get; init; }     // "png" or "jpeg"
    public int Quality { get; init; }       // 1-100
    public float Scale { get; init; }       // 0.1-1.0
    public int MaxHeight { get; init; }     // 0 = unlimited
    public bool Thumbnail { get; init; }

    public ImageOptions Normalize();

    public static ImageOptions Default { get; }
    public static ImageOptions Optimized { get; }
    public static ImageOptions ThumbnailPreset { get; }
}
```

---

## Contributing Guidelines

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-new-feature`
3. Make your changes
4. Write tests for new functionality
5. Run tests: `dotnet test`
6. Commit with descriptive message
7. Push to your fork
8. Create a Pull Request

### Pull Request Process

1. **Update Documentation**: Update relevant docs (ARCHITECTURE.md, USAGE.md, etc.)
2. **Add Tests**: Ensure 80%+ code coverage for new code
3. **Follow Conventions**: Adhere to code style guidelines
4. **Update CHANGELOG**: Add entry describing changes
5. **Keep PRs Focused**: One feature/fix per PR
6. **Descriptive Commits**: Use clear commit messages

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Build process or tooling changes

**Example:**
```
feat(tools): Add PDF export capability

Implement new tool for exporting pages as PDF files.
Uses Playwright's PDF API with configurable options.

Closes #123
```

### Code Review Checklist

Before submitting PR, verify:

- [ ] All tests pass
- [ ] No compiler warnings
- [ ] Code follows style guidelines
- [ ] XML documentation comments for public APIs
- [ ] Security considerations addressed
- [ ] Performance impact considered
- [ ] Breaking changes documented
- [ ] Examples added to docs (if applicable)

### Reporting Issues

When reporting bugs, include:

1. **Environment**: OS, .NET version, browser type
2. **Steps to Reproduce**: Minimal reproduction steps
3. **Expected Behavior**: What should happen
4. **Actual Behavior**: What actually happened
5. **Logs**: Relevant log output
6. **Configuration**: Relevant config sections

**Example:**
```markdown
**Environment:**
- OS: Ubuntu 22.04
- .NET: 8.0.1
- Browser: Chromium

**Steps to Reproduce:**
1. Call screenshot_page with fullPage=true
2. Use URL: https://example.com/long-page

**Expected:** Screenshot of entire page
**Actual:** TimeoutException after 30 seconds

**Logs:**
```
[Error] Navigation timeout exceeded
```

**Configuration:**
```json
{
  "Defaults": {
    "Timeout": 30000
  }
}
```
```

### Development Workflow

**1. Pull latest changes:**
```bash
git checkout main
git pull upstream main
```

**2. Create feature branch:**
```bash
git checkout -b feature/new-tool
```

**3. Make changes and test:**
```bash
# Code changes
dotnet build
dotnet test
```

**4. Commit:**
```bash
git add .
git commit -m "feat(tools): Add new screenshot tool"
```

**5. Push and create PR:**
```bash
git push origin feature/new-tool
# Create PR on GitHub
```

---

## Resources

### Documentation

- [.NET 8.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [SkiaSharp Documentation](https://learn.microsoft.com/en-us/dotnet/api/skiasharp)
- [Model Context Protocol](https://github.com/modelcontextprotocol)

### Tools

- [.NET CLI Reference](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)

### Community

- GitHub Issues: Report bugs and request features
- GitHub Discussions: Ask questions and share ideas
- Pull Requests: Contribute code improvements

---

**Need Help?**

If you're stuck or have questions:
1. Check existing issues on GitHub
2. Review documentation in `/docs`
3. Ask in GitHub Discussions
4. Create a new issue with details

Happy coding!
