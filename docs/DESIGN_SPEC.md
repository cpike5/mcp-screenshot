# MCP Screenshot Server - Design Specification

## Document Information

| Property | Value |
|----------|-------|
| Version | 1.0.0 |
| Status | Implementation Complete |
| Target Framework | .NET 8.0 |
| Protocol | Model Context Protocol (MCP) |
| Transport | stdio |

---

## 1. Project Overview

### 1.1 Purpose

The MCP Screenshot Server is a Model Context Protocol server that provides browser-based rendering and screenshot capabilities. It enables AI agents to generate HTML/CSS/JS prototypes, capture visual output, and iterate on designs through a feedback loop.

### 1.2 Core Value Proposition

- **Visual Feedback Loop**: AI agents can generate HTML prototypes and immediately see the rendered output
- **Responsive Testing**: Capture screenshots at multiple device viewports in a single operation
- **Format Flexibility**: Output in PNG (lossless) or JPEG (optimized for size) with configurable quality
- **Minimal Latency**: Browser pooling and connection reuse minimize screenshot capture time

### 1.3 Key Capabilities

| Capability | Description |
|------------|-------------|
| HTML Rendering | Render raw HTML content, local files, or remote URLs |
| Viewport Configuration | Custom dimensions or predefined device presets |
| Full Page Capture | Capture entire scrollable content, not just viewport |
| Wait Conditions | Wait for selectors or fixed delays before capture |
| Dark Mode Emulation | Test designs with `prefers-color-scheme: dark` |
| Multi-Viewport Capture | Capture at multiple resolutions in a single call |
| Image Optimization | Scale, compress, and format images for size reduction |
| Thumbnail Mode | Quick preview captures at reduced resolution |

---

## 2. System Architecture

### 2.1 High-Level Architecture

```
+----------------------------------------------------------+
|                      MCP Client                           |
|               (Claude Desktop / Claude API)               |
+---------------------------+------------------------------+
                            |
                            | stdio transport (JSON-RPC 2.0)
                            v
+----------------------------------------------------------+
|                   MCP Server Host                         |
|                  (.NET Generic Host)                      |
|  +------------------------------------------------------+ |
|  |              ModelContextProtocol SDK                | |
|  |             (Tool Router/Dispatcher)                 | |
|  +------------------------+-----------------------------+ |
|                           |                               |
|  +------------------------v-----------------------------+ |
|  |                  Tool Handlers                       | |
|  |   +----------------+  +----------------+             | |
|  |   | ScreenshotPage |  | ScreenshotMulti|             | |
|  |   +-------+--------+  +-------+--------+             | |
|  |           |                   |                      | |
|  |   +-------+-------------------+--------+             | |
|  |   |                                    |             | |
|  |   v                                    v             | |
|  |  +---------+  +----------+  +---------+  +--------+  | |
|  |  |Validator|  |Screenshot|  | Image   |  | Temp   |  | |
|  |  | Layer   |  | Service  |  |Processor|  | Files  |  | |
|  |  +---------+  +----+-----+  +---------+  +--------+  | |
|  +------------------------+-----------------------------+ |
|                           |                               |
|  +------------------------v-----------------------------+ |
|  |              Browser Pool Manager                    | |
|  |     (Semaphore-based concurrency control)            | |
|  +------------------------+-----------------------------+ |
|                           |                               |
|  +------------------------v-----------------------------+ |
|  |              Playwright Engine                       | |
|  |         (Chromium / Firefox / WebKit)                | |
|  +------------------------------------------------------+ |
+----------------------------------------------------------+
```

### 2.2 Component Layer Diagram

```
+------------------------------------------------------------------+
|                        PRESENTATION LAYER                         |
|  +-------------------------------------------------------------+  |
|  |                    MCP Tool Classes                         |  |
|  |  - ScreenshotPageTool                                       |  |
|  |  - ScreenshotMultiTool                                      |  |
|  |  - ListPresetsTool                                          |  |
|  +-------------------------------------------------------------+  |
+------------------------------------------------------------------+
                                |
+------------------------------------------------------------------+
|                        VALIDATION LAYER                           |
|  +-------------------------------------------------------------+  |
|  |  InputValidator              SecurityValidator              |  |
|  |  - Content source check      - File path whitelist          |  |
|  |  - Viewport limits           - URL validation               |  |
|  |  - Preset validation         - Blocked pattern check        |  |
|  +-------------------------------------------------------------+  |
+------------------------------------------------------------------+
                                |
+------------------------------------------------------------------+
|                        SERVICE LAYER                              |
|  +-------------------------------------------------------------+  |
|  |  ScreenshotService                                          |  |
|  |  - Page configuration                                       |  |
|  |  - Navigation and wait conditions                           |  |
|  |  - Screenshot capture orchestration                         |  |
|  +-------------------+------------------+----------------------+  |
|  | ImageProcessor    | TempFileManager  | TempFileCleanupSvc   |  |
|  | - Format convert  | - Create temp    | - Background cleanup |  |
|  | - Scale/resize    | - Delete temp    | - Orphan removal     |  |
|  | - Quality adjust  | - Track files    |                      |  |
|  +-------------------+------------------+----------------------+  |
+------------------------------------------------------------------+
                                |
+------------------------------------------------------------------+
|                      INFRASTRUCTURE LAYER                         |
|  +-------------------------------------------------------------+  |
|  |  BrowserPoolManager                                         |  |
|  |  - Lazy browser initialization                              |  |
|  |  - Page acquisition/release with semaphore                  |  |
|  |  - Lifecycle management (IAsyncDisposable)                  |  |
|  +-------------------------------------------------------------+  |
|  |  Configuration (IOptions<T>)                                |  |
|  |  - ScreenshotServerOptions                                  |  |
|  |  - BrowserOptions, DefaultsOptions, SecurityOptions         |  |
|  +-------------------------------------------------------------+  |
+------------------------------------------------------------------+
                                |
+------------------------------------------------------------------+
|                      EXTERNAL DEPENDENCIES                        |
|  +-------------------------------------------------------------+  |
|  |  Microsoft.Playwright       ModelContextProtocol            |  |
|  |  SkiaSharp (image proc)     Microsoft.Extensions.Hosting    |  |
|  +-------------------------------------------------------------+  |
+------------------------------------------------------------------+
```

### 2.3 Data Flow Diagrams

#### 2.3.1 Single Screenshot Capture Flow

```
Client Request (screenshot_page)
        |
        v
+------------------+
| ScreenshotPage   |
| Tool             |
+--------+---------+
         |
         | 1. Validate input parameters
         v
+------------------+
| InputValidator   |
| SecurityValidator|
+--------+---------+
         |
         | 2. Build ScreenshotRequest
         v
+------------------+
| ScreenshotService|
+--------+---------+
         |
         | 3. Acquire page from pool
         v
+------------------+
| BrowserPoolMgr   |
| (SemaphoreSlim)  |
+--------+---------+
         |
         | 4. Configure viewport, emulation
         | 5. Navigate to content
         | 6. Wait for conditions
         | 7. Capture screenshot (PNG bytes)
         v
+------------------+
| ImageProcessor   |
| (SkiaSharp)      |
+--------+---------+
         |
         | 8. Convert format, scale, compress
         | 9. Encode to base64
         v
+------------------+
| Release page     |
| Delete temp file |
+--------+---------+
         |
         | 10. Return MCP response
         v
    Client Response
    {type: "image", data: "<base64>", mimeType: "..."}
```

#### 2.3.2 Multi-Viewport Capture Flow

```
Client Request (screenshot_multi)
        |
        v
+-------------------+
| ScreenshotMulti   |
| Tool              |
+--------+----------+
         |
         | 1. Parse viewports JSON
         | 2. Validate input
         v
+-------------------+
| ScreenshotService |
| .CaptureMultiple  |
+--------+----------+
         |
         | 3. Acquire SINGLE page from pool
         v
+-------------------+
| BrowserPoolMgr    |
+--------+----------+
         |
         | 4. Navigate to content ONCE
         | 5. For each viewport:
         |    a. Resize viewport
         |    b. Wait for conditions
         |    c. Capture screenshot
         |    d. Process image
         v
+-------------------+
| Collect results   |
| with viewport     |
| annotations       |
+--------+----------+
         |
         | 6. Release page
         v
    Client Response
    {content: [{type: "image", ...}, ...]}
```

#### 2.3.3 Temp File Lifecycle

```
Raw HTML Input
      |
      v
+------------------+
| TempFileManager  |
| .CreateTempFile  |
+--------+---------+
         |
         | Write to /tmp/mcp-screenshots/{guid}.html
         v
+------------------+
| ScreenshotService|
| navigates to     |
| file:// URL      |
+--------+---------+
         |
         | Immediate deletion after capture
         v
+------------------+
| TempFileManager  |
| .DeleteTempFile  |
+------------------+

         +
         |
         | Background cleanup (every 60 min)
         v
+------------------+
| TempFileCleanup  |
| Service          |
| (removes files   |
|  older than 2hr) |
+------------------+
```

---

## 3. Component Breakdown

### 3.1 Tool Layer

#### 3.1.1 ScreenshotPageTool

| Responsibility | Description |
|----------------|-------------|
| MCP Interface | Exposes `screenshot_page` tool via MCP protocol |
| Parameter Handling | Accepts content source, viewport, wait conditions, image options |
| Error Translation | Converts exceptions to MCP error responses |

**Location**: `/src/ScreenshotMcp.Server/Tools/ScreenshotPageTool.cs`

**Key Parameters**:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| html | string? | null | Raw HTML content |
| filePath | string? | null | Absolute path to HTML file |
| url | string? | null | HTTP/HTTPS URL |
| width | int | 1280 | Viewport width (pixels) |
| height | int | 720 | Viewport height (pixels) |
| fullPage | bool | false | Capture full scrollable content |
| devicePreset | string? | null | Preset name (desktop, mobile, etc.) |
| waitForSelector | string? | null | CSS selector to wait for |
| waitMs | int | 0 | Additional wait time (ms) |
| darkMode | bool | false | Enable dark mode emulation |
| format | string | "png" | Output format (png/jpeg) |
| quality | int | 80 | JPEG quality (1-100) |
| scale | float | 1.0 | Scale factor (0.1-1.0) |
| maxHeight | int | 0 | Max height for full-page (0=unlimited) |
| thumbnail | bool | false | Quick preview mode |

#### 3.1.2 ScreenshotMultiTool

| Responsibility | Description |
|----------------|-------------|
| Multi-Viewport Capture | Captures at multiple viewports in single operation |
| Viewport Parsing | Parses JSON array of presets or custom dimensions |
| Response Aggregation | Returns array of images with viewport annotations |

**Location**: `/src/ScreenshotMcp.Server/Tools/ScreenshotMultiTool.cs`

**Additional Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| viewports | string | JSON array of viewport configs or preset names |
| compact | bool | Compact mode: JPEG 70%, 0.75 scale |

#### 3.1.3 ListPresetsTool

| Responsibility | Description |
|----------------|-------------|
| Preset Discovery | Returns all available device presets |
| Documentation Aid | Helps AI agents discover valid preset names |

**Location**: `/src/ScreenshotMcp.Server/Tools/ListPresetsTool.cs`

### 3.2 Validation Layer

#### 3.2.1 InputValidator

**Location**: `/src/ScreenshotMcp.Server/Validation/InputValidator.cs`

**Interface**: `IInputValidator`

| Validation | Rule |
|------------|------|
| Content Source | Exactly one of html/filePath/url must be provided |
| Device Preset | Must be valid preset name (case-insensitive) |
| Viewport Width | 1 to MaxViewportWidth (default: 4096) |
| Viewport Height | 1 to MaxViewportHeight (default: 4096) |
| Wait Time | 0 to MaxWaitMs (default: 30000) |

#### 3.2.2 SecurityValidator

**Location**: `/src/ScreenshotMcp.Server/Validation/SecurityValidator.cs`

**Interface**: `ISecurityValidator`

| Validation | Rule |
|------------|------|
| File Path | Must exist, must be within AllowedBasePaths (if configured) |
| URL | Must be http or https scheme, must not match BlockedUrlPatterns |

### 3.3 Service Layer

#### 3.3.1 ScreenshotService

**Location**: `/src/ScreenshotMcp.Server/Services/ScreenshotService.cs`

**Interface**: `IScreenshotService`

**Responsibilities**:

1. **Content URL Resolution**: Converts html/filePath/url to navigable URL
2. **Page Configuration**: Sets viewport size, user agent, color scheme
3. **Navigation**: Loads content with NetworkIdle wait state
4. **Wait Conditions**: Waits for CSS selectors and/or fixed delays
5. **Capture**: Takes screenshot with fullPage or clip options
6. **Cleanup**: Releases page to pool, deletes temp files

**Key Methods**:

```csharp
Task<ScreenshotResult> CaptureAsync(ScreenshotRequest request, CancellationToken ct);

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

#### 3.3.2 ImageProcessor

**Location**: `/src/ScreenshotMcp.Server/Services/ImageProcessor.cs`

**Interface**: `IImageProcessor`

**Responsibilities**:

1. **Format Conversion**: PNG to JPEG with quality control
2. **Scaling**: Reduces image dimensions by scale factor
3. **Optimization**: Uses SkiaSharp for efficient image processing

**Processing Logic**:

```
Input: PNG bytes from Playwright

If format=png AND scale>=1.0:
    Return original (no processing)

Else:
    1. Decode PNG to SKBitmap
    2. If scale < 1.0:
       - Calculate new dimensions
       - Create scaled SKBitmap
       - Use SKFilterQuality.Medium for quality
    3. Encode to target format:
       - PNG: Quality 100 (lossless)
       - JPEG: User-specified quality (1-100)
    4. Return processed bytes + mime type
```

#### 3.3.3 TempFileManager

**Location**: `/src/ScreenshotMcp.Server/Services/TempFileManager.cs`

**Interface**: `ITempFileManager`

**Responsibilities**:

1. **File Creation**: Writes HTML content to unique temp file
2. **File Deletion**: Removes temp files after use
3. **Orphan Cleanup**: Removes old files based on age threshold

**File Naming**: `{GUID}.html` in configured TempDirectory

#### 3.3.4 TempFileCleanupService

**Location**: `/src/ScreenshotMcp.Server/Services/TempFileCleanupService.cs`

**Type**: `BackgroundService` (hosted service)

**Behavior**:

- Runs at configurable interval (default: 60 minutes)
- Removes files older than 2x the cleanup interval (default: 120 minutes)
- Logs cleanup operations
- Handles exceptions without crashing

### 3.4 Infrastructure Layer

#### 3.4.1 BrowserPoolManager

**Location**: `/src/ScreenshotMcp.Server/Services/BrowserPoolManager.cs`

**Interface**: `IBrowserPoolManager : IAsyncDisposable`

**Responsibilities**:

1. **Lazy Initialization**: Browser started on first page request
2. **Concurrency Control**: SemaphoreSlim limits concurrent pages
3. **Browser Selection**: Chromium, Firefox, or WebKit based on config
4. **Resource Cleanup**: Proper disposal of browser and Playwright instances

**Concurrency Model**:

```
SemaphoreSlim _semaphore(MaxConcurrentPages)

AcquirePageAsync():
    await _semaphore.WaitAsync()  // Block if at capacity
    try:
        await EnsureBrowserAsync()
        return await _browser.NewPageAsync()
    catch:
        _semaphore.Release()  // Release on failure
        throw

ReleasePageAsync(page):
    try:
        await page.CloseAsync()
    finally:
        _semaphore.Release()  // Always release
```

**Double-Check Locking for Initialization**:

```
private SemaphoreSlim _initLock = new(1, 1)

EnsureBrowserAsync():
    if _browser is not null: return

    await _initLock.WaitAsync()
    try:
        if _browser is not null: return  // Double-check

        _playwright = await Playwright.CreateAsync()
        _browser = await _playwright.{BrowserType}.LaunchAsync(...)
    finally:
        _initLock.Release()
```

---

## 4. Data Models

### 4.1 Configuration Models

#### ScreenshotServerOptions

**Location**: `/src/ScreenshotMcp.Server/Configuration/ScreenshotServerOptions.cs`

```csharp
public class ScreenshotServerOptions
{
    public const string SectionName = "ScreenshotServer";

    public BrowserOptions Browser { get; set; }
    public DefaultsOptions Defaults { get; set; }
    public SecurityOptions Security { get; set; }
    public string TempDirectory { get; set; }       // Default: "/tmp/mcp-screenshots"
    public int CleanupIntervalMinutes { get; set; } // Default: 60
    public int MaxConcurrentPages { get; set; }     // Default: 5
}
```

#### BrowserOptions

```csharp
public class BrowserOptions
{
    public string Type { get; set; }      // "Chromium", "Firefox", "Webkit"
    public bool Headless { get; set; }    // Default: true
    public string[] Args { get; set; }    // Default: ["--disable-gpu", "--no-sandbox"]
}
```

#### DefaultsOptions

```csharp
public class DefaultsOptions
{
    public int Width { get; set; }    // Default: 1280
    public int Height { get; set; }   // Default: 720
    public int Timeout { get; set; }  // Default: 30000 (ms)
    public int WaitMs { get; set; }   // Default: 100 (ms)
}
```

#### SecurityOptions

```csharp
public class SecurityOptions
{
    public string[] AllowedBasePaths { get; set; }    // Empty = allow all
    public string[] BlockedUrlPatterns { get; set; }  // Empty = allow all
    public int MaxViewportWidth { get; set; }         // Default: 4096
    public int MaxViewportHeight { get; set; }        // Default: 4096
    public int MaxWaitMs { get; set; }                // Default: 30000
}
```

### 4.2 Domain Models

#### ContentSource

**Location**: `/src/ScreenshotMcp.Server/Models/ContentSource.cs`

```csharp
public enum ContentSource
{
    Html,      // Raw HTML content
    FilePath,  // Local file path
    Url        // HTTP/HTTPS URL
}
```

#### ViewportConfig

**Location**: `/src/ScreenshotMcp.Server/Models/ViewportConfig.cs`

```csharp
public record ViewportConfig(
    int Width,
    int Height,
    float Scale = 1.0f
);
```

#### DevicePreset

**Location**: `/src/ScreenshotMcp.Server/Models/DevicePreset.cs`

```csharp
public record DevicePreset(
    string Name,
    int Width,
    int Height,
    float Scale,
    string UserAgent
);
```

#### DevicePresets (Static Catalog)

**Location**: `/src/ScreenshotMcp.Server/Models/DevicePresets.cs`

| Preset Name | Width | Height | Scale | User Agent |
|-------------|-------|--------|-------|------------|
| desktop | 1280 | 720 | 1 | Chrome Desktop (Windows) |
| desktop-hd | 1920 | 1080 | 1 | Chrome Desktop (Windows) |
| tablet | 768 | 1024 | 2 | iPad (iOS 17) |
| tablet-landscape | 1024 | 768 | 2 | iPad (iOS 17) |
| mobile | 375 | 667 | 2 | iPhone SE (iOS 17) |
| mobile-large | 414 | 896 | 3 | iPhone 11 Pro Max (iOS 17) |

#### ImageOptions

**Location**: `/src/ScreenshotMcp.Server/Models/ImageOptions.cs`

```csharp
public record ImageOptions
{
    public string Format { get; init; }     // "png" or "jpeg"
    public int Quality { get; init; }       // 1-100 (JPEG only)
    public float Scale { get; init; }       // 0.1-1.0
    public int MaxHeight { get; init; }     // 0 = unlimited
    public bool Thumbnail { get; init; }    // Quick preview mode

    public ImageOptions Normalize();        // Clamp values to valid ranges

    public static ImageOptions Default { get; }
    public static ImageOptions Optimized { get; }
    public static ImageOptions ThumbnailPreset { get; }
}
```

#### ScreenshotRequest

**Location**: `/src/ScreenshotMcp.Server/Models/ScreenshotRequest.cs`

```csharp
public record ScreenshotRequest
{
    public required ContentSource Source { get; init; }
    public required string Content { get; init; }
    public required ViewportConfig Viewport { get; init; }
    public bool FullPage { get; init; }
    public string? WaitForSelector { get; init; }
    public int WaitMs { get; init; }
    public bool DarkMode { get; init; }
    public string? UserAgent { get; init; }
    public ImageOptions ImageOptions { get; init; }
}
```

#### ScreenshotResult

**Location**: `/src/ScreenshotMcp.Server/Models/ScreenshotResult.cs`

```csharp
public record ScreenshotResult(
    string Base64Data,
    string MimeType,
    ViewportConfig? Viewport = null
);
```

---

## 5. API/Protocol Specifications

### 5.1 MCP Protocol Compliance

The server implements the Model Context Protocol using the official `ModelContextProtocol` C# SDK (v0.4.1-preview.1).

**Transport**: stdio (JSON-RPC 2.0)

**MCP Features Used**:

- Tool registration via `[McpServerTool]` attribute
- Tool discovery via `WithToolsFromAssembly()`
- Dependency injection integration

### 5.2 Tool Specifications

#### screenshot_page

**Name**: `screenshot_page`

**Description**: Renders HTML content or a URL and returns a screenshot

**Input Schema** (MCP generates from parameters):

```json
{
  "type": "object",
  "properties": {
    "html": { "type": "string", "description": "Raw HTML content to render" },
    "filePath": { "type": "string", "description": "Absolute path to an HTML file" },
    "url": { "type": "string", "description": "URL to capture (http/https only)" },
    "width": { "type": "integer", "default": 1280 },
    "height": { "type": "integer", "default": 720 },
    "fullPage": { "type": "boolean", "default": false },
    "devicePreset": { "type": "string" },
    "waitForSelector": { "type": "string" },
    "waitMs": { "type": "integer", "default": 0 },
    "darkMode": { "type": "boolean", "default": false },
    "format": { "type": "string", "default": "png", "enum": ["png", "jpeg"] },
    "quality": { "type": "integer", "default": 80, "minimum": 1, "maximum": 100 },
    "scale": { "type": "number", "default": 1.0, "minimum": 0.1, "maximum": 1.0 },
    "maxHeight": { "type": "integer", "default": 0 },
    "thumbnail": { "type": "boolean", "default": false }
  }
}
```

**Success Response**:

```json
{
  "type": "image",
  "data": "<base64-encoded-image>",
  "mimeType": "image/png"
}
```

**Error Response**:

```json
{
  "error": "INVALID_INPUT|FILE_NOT_FOUND|RENDER_TIMEOUT|SELECTOR_TIMEOUT",
  "message": "Human-readable error description"
}
```

#### screenshot_multi

**Name**: `screenshot_multi`

**Description**: Captures screenshots at multiple viewport sizes

**Viewports Format**:

```json
[
  "desktop",
  "mobile",
  { "width": 800, "height": 600 },
  { "width": 1024, "height": 768, "scale": 2 }
]
```

**Success Response**:

```json
{
  "content": [
    {
      "type": "image",
      "data": "<base64>",
      "mimeType": "image/png",
      "annotations": { "width": 1280, "height": 720 }
    },
    {
      "type": "image",
      "data": "<base64>",
      "mimeType": "image/png",
      "annotations": { "width": 375, "height": 667 }
    }
  ]
}
```

#### list_presets

**Name**: `list_presets`

**Description**: Returns available device presets

**Response**:

```json
{
  "type": "text",
  "text": "{ \"presets\": [...] }"
}
```

### 5.3 Error Codes

| Code | HTTP Equivalent | Trigger |
|------|-----------------|---------|
| INVALID_INPUT | 400 | Missing content source, multiple sources, invalid preset, out-of-range values |
| FILE_NOT_FOUND | 404 | File path does not exist |
| RENDER_TIMEOUT | 408 | Page navigation exceeded timeout |
| SELECTOR_TIMEOUT | 408 | waitForSelector element not found within timeout |
| SECURITY_VIOLATION | 403 | File outside allowed paths, blocked URL pattern |

---

## 6. Dependencies and External Integrations

### 6.1 NuGet Package Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| ModelContextProtocol | 0.4.1-preview.1 | MCP SDK for .NET |
| Microsoft.Extensions.Hosting | 8.0.0 | Application host, DI, configuration |
| Microsoft.Extensions.Options.ConfigurationExtensions | 8.0.0 | Options pattern |
| Microsoft.Extensions.Logging.Console | 8.0.0 | Console logging |
| Microsoft.Playwright | 1.42.0 | Browser automation |
| SkiaSharp | 2.88.8 | Image processing |

### 6.2 Test Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 17.9.0 | Test framework |
| xunit | 2.7.0 | Test runner |
| xunit.runner.visualstudio | 2.5.7 | VS test adapter |
| coverlet.collector | 6.0.0 | Code coverage |
| Moq | 4.20.70 | Mocking framework |
| FluentAssertions | 6.12.0 | Assertion library |

### 6.3 External System Integrations

| System | Integration Type | Purpose |
|--------|-----------------|---------|
| Playwright Browsers | Native binaries | Chromium/Firefox/WebKit rendering |
| File System | Read/Write | Temp file management, file path capture |
| Network | HTTP/HTTPS | URL capture |

### 6.4 Browser Binary Requirements

Playwright browsers must be installed after build:

```bash
# Install Playwright CLI
dotnet tool install --global Microsoft.Playwright.CLI

# Install browsers (from project directory)
playwright install chromium

# Or install all browsers
playwright install
```

---

## 7. Configuration Reference

### 7.1 Configuration File Structure

**Location**: `/src/ScreenshotMcp.Server/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ScreenshotServer": {
    "Browser": {
      "Type": "Chromium",
      "Headless": true,
      "Args": ["--disable-gpu", "--no-sandbox"]
    },
    "Defaults": {
      "Width": 1280,
      "Height": 720,
      "Timeout": 30000,
      "WaitMs": 100
    },
    "TempDirectory": "/tmp/mcp-screenshots",
    "CleanupIntervalMinutes": 60,
    "MaxConcurrentPages": 5,
    "Security": {
      "AllowedBasePaths": [],
      "BlockedUrlPatterns": [],
      "MaxViewportWidth": 4096,
      "MaxViewportHeight": 4096,
      "MaxWaitMs": 30000
    }
  }
}
```

### 7.2 Environment Variable Overrides

Configuration can be overridden via environment variables using the standard .NET configuration provider naming convention:

| Environment Variable | Config Path |
|---------------------|-------------|
| `ScreenshotServer__Browser__Type` | ScreenshotServer:Browser:Type |
| `ScreenshotServer__Browser__Headless` | ScreenshotServer:Browser:Headless |
| `ScreenshotServer__MaxConcurrentPages` | ScreenshotServer:MaxConcurrentPages |
| `ScreenshotServer__Security__MaxWaitMs` | ScreenshotServer:Security:MaxWaitMs |

### 7.3 Configuration Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| Browser.Type | string | "Chromium" | Browser engine: Chromium, Firefox, Webkit |
| Browser.Headless | bool | true | Run without visible UI |
| Browser.Args | string[] | ["--disable-gpu", "--no-sandbox"] | Browser launch arguments |
| Defaults.Width | int | 1280 | Default viewport width |
| Defaults.Height | int | 720 | Default viewport height |
| Defaults.Timeout | int | 30000 | Navigation timeout (ms) |
| Defaults.WaitMs | int | 100 | Default post-load wait (ms) |
| TempDirectory | string | "/tmp/mcp-screenshots" | Temp file storage path |
| CleanupIntervalMinutes | int | 60 | Background cleanup interval |
| MaxConcurrentPages | int | 5 | Max simultaneous browser pages |
| Security.AllowedBasePaths | string[] | [] | Whitelist for file access (empty = all) |
| Security.BlockedUrlPatterns | string[] | [] | URL patterns to block |
| Security.MaxViewportWidth | int | 4096 | Max allowed viewport width |
| Security.MaxViewportHeight | int | 4096 | Max allowed viewport height |
| Security.MaxWaitMs | int | 30000 | Max allowed wait time |

---

## 8. Security Considerations

### 8.1 Threat Model

| Threat | Risk Level | Mitigation |
|--------|------------|------------|
| Arbitrary File Read | High | AllowedBasePaths whitelist |
| SSRF (Server-Side Request Forgery) | High | HTTP/HTTPS only, BlockedUrlPatterns |
| Resource Exhaustion (Memory) | Medium | MaxViewportWidth/Height limits, MaxConcurrentPages |
| Resource Exhaustion (Time) | Medium | Timeout limits, MaxWaitMs cap |
| Temp File Accumulation | Low | Immediate cleanup + background service |
| Path Traversal | High | Path.GetFullPath normalization, whitelist check |

### 8.2 Security Architecture

```
Request Input
     |
     v
+--------------------+
| Input Validation   |
| - Source check     |
| - Dimension limits |
| - Wait time limits |
+----------+---------+
           |
           v
+--------------------+
| Security Validation|
| - File path check  |
|   (AllowedBasePaths)|
| - URL validation   |
|   (scheme + blocks)|
+----------+---------+
           |
           v
+--------------------+
| Browser Sandbox    |
| (Playwright)       |
| - Isolated context |
| - No persistent    |
|   state            |
+--------------------+
```

### 8.3 File Path Security

**Validation Flow**:

1. Check for empty/whitespace path
2. Normalize path with `Path.GetFullPath()`
3. Check file existence
4. If `AllowedBasePaths` configured:
   - Ensure normalized path starts with an allowed base path
   - Use case-insensitive comparison on Windows

**Security Properties**:

- Path traversal attacks (../../../etc/passwd) are mitigated by normalization + whitelist
- Symlink attacks are partially mitigated (normalized path checked)
- Empty whitelist means all paths allowed (for development)

### 8.4 URL Security

**Validation Flow**:

1. Parse as absolute URI
2. Verify scheme is `http` or `https`
3. Check against BlockedUrlPatterns (host and full URI)

**Not Allowed**:

- `file://` URLs (prevents local file access via URL parameter)
- `javascript:` URLs
- `ftp://` and other schemes
- URLs matching blocked patterns

### 8.5 Resource Protection

| Resource | Limit | Enforcement |
|----------|-------|-------------|
| Viewport Size | 4096x4096 | InputValidator |
| Wait Time | 30 seconds | InputValidator |
| Concurrent Pages | 5 (configurable) | SemaphoreSlim in BrowserPoolManager |
| Navigation Timeout | 30 seconds | Playwright PageGotoOptions |

---

## 9. Performance Characteristics

### 9.1 Latency Analysis

| Operation | Typical Latency | Notes |
|-----------|-----------------|-------|
| Browser Launch | 2-5 seconds | One-time, lazy initialization |
| Page Creation | 50-200 ms | Reused from pool |
| Navigation (local HTML) | 100-500 ms | Depends on content complexity |
| Navigation (remote URL) | 500-3000 ms | Network dependent |
| Screenshot Capture | 50-200 ms | Depends on page size |
| Image Processing (PNG) | 0-10 ms | Pass-through if no scaling |
| Image Processing (JPEG + scale) | 50-200 ms | SkiaSharp encode/decode |
| Base64 Encoding | 10-50 ms | Linear with image size |

**Total Request Time (typical)**:

- Simple HTML: 200-500 ms
- Remote URL: 1-4 seconds
- Multi-viewport (3 viewports): 600-2000 ms (sequential viewport changes)

### 9.2 Memory Usage

| Component | Memory Usage | Notes |
|-----------|--------------|-------|
| Playwright Process | 100-200 MB | Chromium background process |
| Per Page | 50-150 MB | Depends on page content |
| Screenshot Buffer | 1-50 MB | Full-page can be large |
| SkiaSharp Bitmap | 2x screenshot size | Decode + encode buffers |

**Memory Optimization**:

- Use `scale` parameter to reduce image dimensions
- Use `maxHeight` to limit full-page capture size
- Use JPEG format for smaller buffers
- Pages are released promptly after capture

### 9.3 Throughput

With default configuration (`MaxConcurrentPages: 5`):

| Scenario | Expected Throughput |
|----------|---------------------|
| Simple HTML captures | 10-20 requests/second |
| Complex pages | 2-5 requests/second |
| Remote URLs | 1-3 requests/second |

### 9.4 Optimization Strategies

| Strategy | Implementation | Benefit |
|----------|----------------|---------|
| Browser Pooling | Single browser instance | Eliminates browser startup |
| Page Concurrency | SemaphoreSlim control | Parallel captures up to limit |
| Lazy Init | Browser starts on first request | Fast server startup |
| Temp File Cleanup | Immediate + background | Prevents disk exhaustion |
| Image Compression | JPEG + scale options | Reduces response size 60-90% |

---

## 10. Extension Points and Customization

### 10.1 Adding New Device Presets

**Location**: `/src/ScreenshotMcp.Server/Models/DevicePresets.cs`

Add new entry to the `All` dictionary:

```csharp
public static readonly IReadOnlyDictionary<string, DevicePreset> All = new Dictionary<string, DevicePreset>
{
    // ... existing presets ...
    ["pixel-7"] = new DevicePreset("pixel-7", 412, 915, 2.625f,
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) ..."),
};
```

### 10.2 Adding New Image Formats

**Location**: `/src/ScreenshotMcp.Server/Services/ImageProcessor.cs`

Extend the `Process` method to handle new formats:

```csharp
if (normalizedOptions.Format == "webp")
{
    using var data = image.Encode(SKEncodedImageFormat.Webp, normalizedOptions.Quality);
    return (data.ToArray(), "image/webp");
}
```

Note: Update `ImageOptions.Normalize()` to accept the new format.

### 10.3 Adding New Tools

1. Create new tool class in `/src/ScreenshotMcp.Server/Tools/`
2. Apply `[McpServerToolType]` attribute to class
3. Apply `[McpServerTool(Name = "tool_name")]` to the execute method
4. Use `[Description("...")]` for parameter documentation
5. Tools are auto-discovered via `WithToolsFromAssembly()`

Example:

```csharp
[McpServerToolType]
public class NewTool
{
    [McpServerTool(Name = "new_tool")]
    [Description("Description for AI agents")]
    public async Task<object> ExecuteAsync(
        [Description("Parameter description")] string param)
    {
        // Implementation
    }
}
```

### 10.4 Custom Validators

1. Create validator class implementing `ISecurityValidator` or `IInputValidator`
2. Register in DI container (replace existing registration in `ServiceCollectionExtensions`)

### 10.5 Alternative Browser Engines

The server supports all Playwright browsers:

1. Change `Browser.Type` in configuration to "Firefox" or "Webkit"
2. Install the browser: `playwright install firefox` or `playwright install webkit`

### 10.6 Custom Image Processing Pipeline

To add custom image processing (watermarks, filters, etc.):

1. Create decorator for `IImageProcessor`
2. Register decorator in DI before base implementation
3. Call base implementation, then apply custom processing

---

## 11. Project Structure Reference

```
/home/cpike/Workspace/screenshot-mcp/
|
+-- Directory.Build.props              # Shared build properties (net8.0, nullable, etc.)
+-- ScreenshotMcp.sln                  # Solution file
+-- README.md                          # Project overview
+-- IMPLEMENTATION_PLAN.md             # Development plan
+-- spec.md                            # Original specification
+-- .gitignore                         # Git ignore rules
|
+-- src/
|   +-- ScreenshotMcp.Server/
|       +-- ScreenshotMcp.Server.csproj
|       +-- Program.cs                 # Entry point, host configuration
|       +-- appsettings.json           # Configuration file
|       |
|       +-- Configuration/
|       |   +-- ScreenshotServerOptions.cs
|       |   +-- BrowserOptions.cs
|       |   +-- DefaultsOptions.cs
|       |   +-- SecurityOptions.cs
|       |
|       +-- Models/
|       |   +-- ContentSource.cs       # Enum
|       |   +-- ViewportConfig.cs      # Record
|       |   +-- DevicePreset.cs        # Record
|       |   +-- DevicePresets.cs       # Static catalog
|       |   +-- ImageOptions.cs        # Record with presets
|       |   +-- ScreenshotRequest.cs   # Request DTO
|       |   +-- ScreenshotResult.cs    # Result DTO
|       |
|       +-- Services/
|       |   +-- IBrowserPoolManager.cs
|       |   +-- BrowserPoolManager.cs
|       |   +-- IScreenshotService.cs
|       |   +-- ScreenshotService.cs
|       |   +-- ITempFileManager.cs
|       |   +-- TempFileManager.cs
|       |   +-- TempFileCleanupService.cs
|       |   +-- ImageProcessor.cs      # + IImageProcessor
|       |
|       +-- Validation/
|       |   +-- IInputValidator.cs
|       |   +-- InputValidator.cs
|       |   +-- ISecurityValidator.cs
|       |   +-- SecurityValidator.cs
|       |
|       +-- Tools/
|       |   +-- ScreenshotPageTool.cs
|       |   +-- ScreenshotMultiTool.cs
|       |   +-- ListPresetsTool.cs
|       |
|       +-- Extensions/
|           +-- ServiceCollectionExtensions.cs
|
+-- tests/
|   +-- ScreenshotMcp.Server.Tests/
|       +-- ScreenshotMcp.Server.Tests.csproj
|       +-- Unit/
|       |   +-- Models/
|       |   |   +-- DevicePresetsTests.cs
|       |   +-- Validation/
|       |       +-- InputValidatorTests.cs
|       |       +-- SecurityValidatorTests.cs
|       +-- Fixtures/
|           +-- (HTML test files)
|
+-- docs/
    +-- DESIGN_SPEC.md                 # This document
```

---

## 12. Testing Strategy

### 12.1 Test Categories

| Category | Location | Scope |
|----------|----------|-------|
| Unit Tests | `tests/.../Unit/` | Validators, Models, isolated logic |
| Integration Tests | `tests/.../Integration/` | Service layer with real browser |
| E2E Tests | (Planned) | Full MCP protocol round-trip |

### 12.2 Current Test Coverage

| Component | Test Class | Coverage Focus |
|-----------|------------|----------------|
| InputValidator | InputValidatorTests | Content source validation, viewport limits, preset validation |
| SecurityValidator | SecurityValidatorTests | File path validation, URL validation, blocked patterns |
| DevicePresets | DevicePresetsTests | Preset lookup, case insensitivity, all presets defined |

### 12.3 Test Fixtures

**Location**: `tests/ScreenshotMcp.Server.Tests/Fixtures/`

| Fixture | Purpose |
|---------|---------|
| simple.html | Basic static content validation |
| responsive.html | Media query behavior |
| async-load.html | waitForSelector testing |
| long-page.html | fullPage capture testing |

### 12.4 Testing Recommendations

1. **Integration Tests**: Add tests for `ScreenshotService` with actual Playwright browser
2. **E2E Tests**: Add MCP protocol round-trip tests using MCP client library
3. **Performance Tests**: Add benchmarks for capture latency and throughput
4. **Security Tests**: Add penetration testing for path traversal and SSRF

---

## 13. Deployment Considerations

### 13.1 Prerequisites

- .NET 8.0 Runtime
- Playwright browser binaries (Chromium by default)
- Write access to temp directory
- Sufficient memory for concurrent page captures

### 13.2 Claude Desktop Integration

```json
{
  "mcpServers": {
    "screenshot": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/screenshot-mcp/src/ScreenshotMcp.Server"]
    }
  }
}
```

### 13.3 Docker Deployment (Future)

Considerations for Docker image:

- Use multi-stage build for smaller image
- Include Playwright dependencies (browser binaries)
- Configure headless mode
- Set appropriate browser Args for containerized environment
- Mount volume for temp files (or use in-memory storage)

### 13.4 Production Recommendations

| Aspect | Recommendation |
|--------|----------------|
| Logging | Set to Warning or Error in production |
| Security | Configure AllowedBasePaths to restrict file access |
| Memory | Monitor and limit MaxConcurrentPages based on available RAM |
| Cleanup | Set CleanupIntervalMinutes appropriate to load |
| Monitoring | Add health checks and metrics (future enhancement) |

---

## 14. Known Limitations

### 14.1 Current Limitations

| Limitation | Description | Workaround |
|------------|-------------|------------|
| Single Browser Type | Only one browser type per server instance | Configure at startup |
| No PDF Export | Only PNG/JPEG image output | Future enhancement |
| No Element Screenshot | Captures full page/viewport only | Use CSS to isolate element |
| No Video Recording | Static screenshots only | Future enhancement |
| No Accessibility Audit | No a11y integration | External tools |
| Sequential Multi-Viewport | Viewports captured sequentially | Use parallel server instances |

### 14.2 Technical Debt

| Item | Priority | Description |
|------|----------|-------------|
| Page Pooling | Medium | Currently creates new page per request; could pool pages |
| Retry Logic | Medium | No automatic retry on transient Playwright failures |
| Health Endpoint | Low | No built-in health check (outside MCP protocol) |
| Metrics | Low | No performance metrics collection |

---

## 15. Future Enhancements

### 15.1 Planned Features

| Feature | Priority | Description |
|---------|----------|-------------|
| PDF Export | High | Add format=pdf option |
| Element Screenshot | High | Capture specific element by selector |
| Retry Logic | Medium | Auto-retry transient browser failures |
| Page Caching | Medium | Cache rendered pages for repeated captures |
| Video Recording | Low | Capture animations/interactions |
| Diff Tool | Low | Compare two screenshots |
| Accessibility Audit | Low | Return a11y violations |

### 15.2 Architecture Improvements

| Improvement | Description |
|-------------|-------------|
| Page Pool | Pre-create and reuse page instances |
| Parallel Multi-Viewport | Capture viewports in parallel with multiple pages |
| Streaming Response | Stream large screenshots instead of base64 |
| Docker Image | Official Docker image with browsers |

---

## 16. Document Handoff Notes

### 16.1 For Documentation Writer

This specification is intended to be the source of truth for creating:

1. **Architectural Documentation**: Use sections 2-4 for component diagrams and data flows
2. **Developer Documentation**: Use sections 6-7 for setup and configuration
3. **Usage Guide**: Use section 5 for tool specifications and examples
4. **README**: Condense key points from all sections

### 16.2 Key Files to Reference

| Document Type | Source Files |
|--------------|--------------|
| Tool API | Tools/*.cs |
| Configuration | appsettings.json, Configuration/*.cs |
| Models | Models/*.cs |
| Setup | README.md, IMPLEMENTATION_PLAN.md |

### 16.3 Diagram Suggestions

- Convert ASCII architecture diagrams to proper diagrams (Mermaid, PlantUML, etc.)
- Add sequence diagrams for each tool's request flow
- Add class diagrams for the service layer
- Add deployment diagram for Claude Desktop integration

---

*End of Design Specification*
