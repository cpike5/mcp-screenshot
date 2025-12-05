# MCP Screenshot Server Specification

## Overview

A Model Context Protocol (MCP) server that provides browser-based rendering and screenshot capabilities. Enables AI agents to generate HTML/CSS/JS prototypes, capture visual output, and iterate on designs through a feedback loop.

## Goals

- Render HTML content or files in a headless browser
- Capture screenshots at configurable viewport sizes
- Return images in a format consumable by AI agents (base64)
- Support rapid iteration with minimal latency
- Provide device/viewport presets for responsive testing

---

## Tools

### `screenshot_page`

Renders HTML content and returns a screenshot.

#### Parameters

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `html` | string | No* | - | Raw HTML content to render |
| `filePath` | string | No* | - | Absolute path to HTML file |
| `url` | string | No* | - | URL to capture (local or remote) |
| `width` | integer | No | 1280 | Viewport width in pixels |
| `height` | integer | No | 720 | Viewport height in pixels |
| `fullPage` | boolean | No | false | Capture full scrollable page |
| `devicePreset` | string | No | - | Predefined device (see presets) |
| `waitForSelector` | string | No | - | CSS selector to wait for before capture |
| `waitMs` | integer | No | 0 | Additional wait time in milliseconds |
| `darkMode` | boolean | No | false | Emulate dark color scheme |

*One of `html`, `filePath`, or `url` is required.

#### Device Presets

| Preset | Width | Height | Scale | User Agent |
|--------|-------|--------|-------|------------|
| `desktop` | 1280 | 720 | 1 | Chrome Desktop |
| `desktop-hd` | 1920 | 1080 | 1 | Chrome Desktop |
| `tablet` | 768 | 1024 | 2 | iPad |
| `tablet-landscape` | 1024 | 768 | 2 | iPad |
| `mobile` | 375 | 667 | 2 | iPhone SE |
| `mobile-large` | 414 | 896 | 3 | iPhone 11 Pro Max |

#### Response

Returns MCP content with type `image`:

```json
{
  "content": [
    {
      "type": "image",
      "data": "<base64-encoded-png>",
      "mimeType": "image/png"
    }
  ]
}
```

#### Errors

| Code | Condition |
|------|-----------|
| `INVALID_INPUT` | No content source provided or multiple sources |
| `FILE_NOT_FOUND` | Specified file path does not exist |
| `RENDER_TIMEOUT` | Page failed to reach idle state within timeout |
| `SELECTOR_TIMEOUT` | `waitForSelector` element not found |

---

### `screenshot_multi` (Optional)

Captures screenshots at multiple viewport sizes in a single call.

#### Parameters

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `html` | string | No* | Raw HTML content |
| `filePath` | string | No* | Path to HTML file |
| `url` | string | No* | URL to capture |
| `viewports` | array | Yes | Array of viewport configs or preset names |

#### Response

Returns multiple images with viewport metadata:

```json
{
  "content": [
    {
      "type": "image",
      "data": "<base64>",
      "mimeType": "image/png",
      "annotations": {
        "viewport": "desktop",
        "width": 1280,
        "height": 720
      }
    },
    {
      "type": "image",
      "data": "<base64>",
      "mimeType": "image/png",
      "annotations": {
        "viewport": "mobile",
        "width": 375,
        "height": 667
      }
    }
  ]
}
```

---

### `list_presets` (Optional)

Returns available device presets.

#### Parameters

None.

#### Response

```json
{
  "content": [
    {
      "type": "text",
      "text": "{ \"presets\": [...] }"
    }
  ]
}
```

---

## Configuration

Server configuration via `appsettings.json` or environment variables.

```json
{
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
    "MaxConcurrentPages": 5
  }
}
```

| Setting | Env Variable | Description |
|---------|--------------|-------------|
| `Browser.Type` | `MCP_BROWSER_TYPE` | Chromium, Firefox, or Webkit |
| `Browser.Headless` | `MCP_BROWSER_HEADLESS` | Run without UI |
| `Defaults.Timeout` | `MCP_DEFAULT_TIMEOUT` | Max render time (ms) |
| `MaxConcurrentPages` | `MCP_MAX_PAGES` | Parallel page limit |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      MCP Client                         │
│                  (Claude Desktop/API)                   │
└─────────────────────┬───────────────────────────────────┘
                      │ stdio/SSE
┌─────────────────────▼───────────────────────────────────┐
│                   MCP Server Host                       │
│  ┌───────────────────────────────────────────────────┐  │
│  │              Tool Router/Dispatcher               │  │
│  └───────────────────────┬───────────────────────────┘  │
│                          │                              │
│  ┌───────────────────────▼───────────────────────────┐  │
│  │              Screenshot Service                   │  │
│  │  ┌─────────────────────────────────────────────┐  │  │
│  │  │           Browser Pool Manager              │  │  │
│  │  │  - Lifecycle management                     │  │  │
│  │  │  - Page pooling                             │  │  │
│  │  │  - Concurrency control                      │  │  │
│  │  └─────────────────────────────────────────────┘  │  │
│  └───────────────────────┬───────────────────────────┘  │
│                          │                              │
│  ┌───────────────────────▼───────────────────────────┐  │
│  │              Playwright Engine                    │  │
│  │           (Chromium/Firefox/Webkit)               │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Components

#### Browser Pool Manager

- Initializes browser instance on server startup
- Maintains pool of reusable page contexts
- Handles cleanup of crashed/stale pages
- Enforces concurrency limits

#### Screenshot Service

- Validates and normalizes input parameters
- Manages temp file creation/cleanup for raw HTML
- Applies viewport and device emulation settings
- Handles wait conditions and timeouts
- Encodes screenshots to base64

#### Tool Router

- Parses incoming MCP tool calls
- Routes to appropriate handler
- Formats responses per MCP spec

---

## Implementation Notes

### Dependencies

```xml
<PackageReference Include="Microsoft.Playwright" Version="1.42.0" />
<PackageReference Include="ModelContextProtocol" Version="0.1.0-preview" />
```

Note: Run `playwright install chromium` after build to download browser binaries.

### Browser Lifecycle

```csharp
public class BrowserPoolManager : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _pageSemaphore;
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = new[] { "--disable-gpu", "--no-sandbox" }
        });
    }
    
    public async Task<IPage> AcquirePageAsync()
    {
        await _pageSemaphore.WaitAsync();
        return await _browser!.NewPageAsync();
    }
    
    public void ReleasePage(IPage page)
    {
        _ = page.CloseAsync();
        _pageSemaphore.Release();
    }
}
```

### Temp File Handling

When rendering raw HTML:

1. Generate unique filename: `{guid}.html`
2. Write to configured temp directory
3. Render and capture
4. Delete immediately after capture
5. Background cleanup job handles orphaned files

### Error Handling

- Wrap all Playwright operations in try/catch
- Return structured MCP errors with actionable messages
- Log failures with context for debugging
- Implement retry logic for transient failures

---

## Security Considerations

- **File Path Validation**: Restrict `filePath` to allowed directories to prevent arbitrary file access
- **URL Restrictions**: Optionally whitelist allowed URL patterns/domains
- **Resource Limits**: Cap viewport dimensions to prevent memory exhaustion
- **Timeout Enforcement**: Hard limit on render time to prevent hangs
- **No Script Injection**: Raw HTML is rendered in isolated context

### Recommended Safeguards

```csharp
public class SecurityValidator
{
    private readonly string[] _allowedBasePaths;
    private readonly string[] _blockedUrlPatterns;
    
    public bool ValidateFilePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return _allowedBasePaths.Any(bp => fullPath.StartsWith(bp));
    }
    
    public bool ValidateUrl(string url)
    {
        var uri = new Uri(url);
        return uri.Scheme is "http" or "https" or "file"
            && !_blockedUrlPatterns.Any(p => uri.Host.Contains(p));
    }
}
```

---

## Testing Strategy

### Unit Tests

- Parameter validation
- Device preset resolution
- File path security validation
- Response formatting

### Integration Tests

- Render simple HTML, verify image output
- Test each device preset
- Verify `waitForSelector` behavior
- Test timeout handling
- Concurrent capture stress test

### Test Fixtures

Include sample HTML files:

- `simple.html` - Basic content
- `responsive.html` - Media queries
- `async-load.html` - Delayed content loading
- `long-page.html` - Scrollable content

---

## Future Enhancements

- **PDF Export**: Add `format` parameter for PDF output
- **Element Screenshot**: Capture specific element by selector
- **Diff Tool**: Compare two screenshots and highlight differences
- **Video Recording**: Capture interactions/animations
- **Accessibility Audit**: Return accessibility violations alongside screenshot
- **Performance Metrics**: Include page load timing data
- **Local Asset Server**: Built-in static file serving for CSS/JS/images

---

## MCP Manifest

```json
{
  "name": "screenshot-server",
  "version": "1.0.0",
  "description": "Browser rendering and screenshot capture for visual feedback loops",
  "tools": [
    {
      "name": "screenshot_page",
      "description": "Render HTML/URL and capture screenshot"
    },
    {
      "name": "screenshot_multi",
      "description": "Capture at multiple viewport sizes"
    },
    {
      "name": "list_presets",
      "description": "List available device presets"
    }
  ]
}
```