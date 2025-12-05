# Usage Guide

## Table of Contents

- [Quick Start](#quick-start)
- [Installation](#installation)
- [Configuration](#configuration)
- [Available Tools](#available-tools)
- [Device Presets](#device-presets)
- [Common Use Cases](#common-use-cases)
- [Image Optimization](#image-optimization)
- [Troubleshooting](#troubleshooting)
- [Tips and Best Practices](#tips-and-best-practices)

---

## Quick Start

### 1. Prerequisites

- .NET 8.0 SDK or runtime
- Playwright browsers (Chromium by default)

### 2. Install and Run

```bash
# Clone repository
git clone <repository-url>
cd screenshot-mcp

# Build project
dotnet build

# Install Playwright browsers
cd src/ScreenshotMcp.Server
./bin/Debug/net8.0/playwright.sh install chromium  # Linux/macOS
# OR
pwsh bin/Debug/net8.0/playwright.ps1 install chromium  # Windows

# Run server
dotnet run --project src/ScreenshotMcp.Server
```

### 3. Configure Claude Desktop

Add to your Claude Desktop configuration file:

**macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "screenshot": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/screenshot-mcp/src/ScreenshotMcp.Server"
      ]
    }
  }
}
```

### 4. Use in Claude

Once configured, Claude can use the screenshot tools:

**Example conversation:**
```
You: Create a simple landing page for a coffee shop

Claude: I'll create a landing page and show you what it looks like.

[Claude generates HTML and uses screenshot_page tool to capture it]

Here's the landing page design...
```

---

## Installation

### System Requirements

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows, macOS, or Linux |
| .NET Runtime | 8.0 or higher |
| RAM | 2 GB minimum (4 GB recommended) |
| Disk Space | 500 MB for browsers |

### Installation Methods

#### Method 1: From Source

```bash
# Clone repository
git clone <repository-url>
cd screenshot-mcp

# Restore dependencies
dotnet restore

# Build release version
dotnet build -c Release

# Install browsers
cd src/ScreenshotMcp.Server/bin/Release/net8.0
./playwright.sh install chromium
```

#### Method 2: Published Binary

```bash
# Publish self-contained executable
dotnet publish -c Release -r linux-x64 --self-contained

# Output in: src/ScreenshotMcp.Server/bin/Release/net8.0/linux-x64/publish/

# Install browsers
cd src/ScreenshotMcp.Server/bin/Release/net8.0/linux-x64/publish
./playwright.sh install chromium
```

### Browser Installation

**Install specific browser:**
```bash
playwright install chromium    # Recommended
playwright install firefox     # Alternative
playwright install webkit      # Alternative
```

**Install all browsers:**
```bash
playwright install
```

**Verify installation:**
```bash
playwright --version
```

---

## Configuration

Configuration is managed via `appsettings.json` in the server project directory.

### Configuration File Location

`src/ScreenshotMcp.Server/appsettings.json`

### Full Configuration Example

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
      "Args": [
        "--disable-gpu",
        "--no-sandbox"
      ]
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

### Configuration Options

#### Browser Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Type` | string | "Chromium" | Browser engine: Chromium, Firefox, or Webkit |
| `Headless` | boolean | true | Run browser without UI |
| `Args` | string[] | ["--disable-gpu", "--no-sandbox"] | Browser launch arguments |

**Example - Use Firefox:**
```json
{
  "Browser": {
    "Type": "Firefox",
    "Headless": true
  }
}
```

**Example - Visible browser for debugging:**
```json
{
  "Browser": {
    "Headless": false
  }
}
```

#### Default Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Width` | integer | 1280 | Default viewport width (pixels) |
| `Height` | integer | 720 | Default viewport height (pixels) |
| `Timeout` | integer | 30000 | Navigation timeout (milliseconds) |
| `WaitMs` | integer | 100 | Default post-load wait (milliseconds) |

**Example - Higher resolution defaults:**
```json
{
  "Defaults": {
    "Width": 1920,
    "Height": 1080,
    "Timeout": 60000
  }
}
```

#### Performance Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxConcurrentPages` | integer | 5 | Maximum simultaneous browser pages |
| `TempDirectory` | string | "/tmp/mcp-screenshots" | Temp file storage path |
| `CleanupIntervalMinutes` | integer | 60 | Background cleanup frequency |

**Example - High throughput configuration:**
```json
{
  "MaxConcurrentPages": 10,
  "CleanupIntervalMinutes": 30
}
```

#### Security Settings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AllowedBasePaths` | string[] | [] | Whitelist of allowed file directories |
| `BlockedUrlPatterns` | string[] | [] | Regex patterns to block URLs |
| `MaxViewportWidth` | integer | 4096 | Maximum viewport width |
| `MaxViewportHeight` | integer | 4096 | Maximum viewport height |
| `MaxWaitMs` | integer | 30000 | Maximum wait time |

**Example - Restrict file access:**
```json
{
  "Security": {
    "AllowedBasePaths": [
      "/home/user/projects",
      "/var/www/html"
    ],
    "BlockedUrlPatterns": [
      "localhost",
      "127\\.0\\.0\\.1",
      "192\\.168\\."
    ]
  }
}
```

### Environment Variables

Override configuration using environment variables:

```bash
# Set browser type
export ScreenshotServer__Browser__Type=Firefox

# Set max concurrent pages
export ScreenshotServer__MaxConcurrentPages=10

# Disable headless mode
export ScreenshotServer__Browser__Headless=false

# Run server
dotnet run --project src/ScreenshotMcp.Server
```

**Windows (PowerShell):**
```powershell
$env:ScreenshotServer__Browser__Type="Firefox"
dotnet run --project src/ScreenshotMcp.Server
```

---

## Available Tools

The server exposes three MCP tools:

### screenshot_page

Renders HTML content or a URL and returns a screenshot.

#### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `html` | string | No* | - | Raw HTML content to render |
| `filePath` | string | No* | - | Absolute path to HTML file |
| `url` | string | No* | - | URL to capture (http/https) |
| `width` | integer | No | 1280 | Viewport width (pixels) |
| `height` | integer | No | 720 | Viewport height (pixels) |
| `fullPage` | boolean | No | false | Capture full scrollable page |
| `devicePreset` | string | No | - | Device preset name (see below) |
| `waitForSelector` | string | No | - | CSS selector to wait for |
| `waitMs` | integer | No | 0 | Additional wait time (ms) |
| `darkMode` | boolean | No | false | Emulate dark color scheme |
| `format` | string | No | "png" | Output format (png/jpeg) |
| `quality` | integer | No | 80 | JPEG quality (1-100) |
| `scale` | number | No | 1.0 | Scale factor (0.1-1.0) |
| `maxHeight` | integer | No | 0 | Max height for full-page (0=unlimited) |
| `thumbnail` | boolean | No | false | Quick preview mode |

*Exactly one of `html`, `filePath`, or `url` must be provided.

#### Examples

**Capture raw HTML:**
```json
{
  "html": "<html><body><h1>Hello World</h1></body></html>",
  "width": 800,
  "height": 600
}
```

**Capture local file:**
```json
{
  "filePath": "/home/user/projects/index.html",
  "fullPage": true
}
```

**Capture URL:**
```json
{
  "url": "https://example.com",
  "devicePreset": "mobile"
}
```

**Wait for dynamic content:**
```json
{
  "html": "<html><body><div id='content'>Loading...</div><script>setTimeout(() => document.getElementById('content').innerHTML = 'Loaded!', 1000)</script></body></html>",
  "waitForSelector": "#content",
  "waitMs": 1500
}
```

**Dark mode:**
```json
{
  "url": "https://example.com",
  "darkMode": true
}
```

**Optimized JPEG:**
```json
{
  "url": "https://example.com",
  "format": "jpeg",
  "quality": 70,
  "scale": 0.75
}
```

#### Response

```json
{
  "type": "image",
  "data": "iVBORw0KGgoAAAANSUhEUg...",
  "mimeType": "image/png"
}
```

### screenshot_multi

Captures screenshots at multiple viewport sizes in a single call.

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `html` | string | No* | Raw HTML content |
| `filePath` | string | No* | Path to HTML file |
| `url` | string | No* | URL to capture |
| `viewports` | string | Yes | JSON array of viewport configs |
| `waitForSelector` | string | No | CSS selector to wait for |
| `waitMs` | integer | No | Additional wait time (ms) |
| `darkMode` | boolean | No | Emulate dark color scheme |
| `format` | string | No | Output format (png/jpeg) |
| `quality` | integer | No | JPEG quality (1-100) |
| `scale` | number | No | Scale factor (0.1-1.0) |
| `compact` | boolean | No | Compact mode (JPEG 70%, 0.75 scale) |

*Exactly one of `html`, `filePath`, or `url` must be provided.

#### Viewports Format

**Array of preset names:**
```json
{
  "viewports": "[\"desktop\", \"tablet\", \"mobile\"]"
}
```

**Array of custom configs:**
```json
{
  "viewports": "[{\"width\": 800, \"height\": 600}, {\"width\": 1024, \"height\": 768}]"
}
```

**Mixed presets and custom:**
```json
{
  "viewports": "[\"desktop\", {\"width\": 1440, \"height\": 900}]"
}
```

#### Examples

**Responsive testing:**
```json
{
  "url": "https://example.com",
  "viewports": "[\"desktop\", \"tablet\", \"mobile\"]"
}
```

**Custom viewports:**
```json
{
  "html": "<html><body>Responsive Layout</body></html>",
  "viewports": "[{\"width\": 320, \"height\": 568}, {\"width\": 768, \"height\": 1024}, {\"width\": 1920, \"height\": 1080}]"
}
```

**Compact mode for faster results:**
```json
{
  "url": "https://example.com",
  "viewports": "[\"desktop\", \"mobile\"]",
  "compact": true
}
```

#### Response

```json
{
  "content": [
    {
      "type": "image",
      "data": "iVBORw0KGgoAAAANSUhEUg...",
      "mimeType": "image/png",
      "annotations": {
        "width": 1280,
        "height": 720
      }
    },
    {
      "type": "image",
      "data": "iVBORw0KGgoAAAANSUhEUg...",
      "mimeType": "image/png",
      "annotations": {
        "width": 375,
        "height": 667
      }
    }
  ]
}
```

### list_presets

Returns available device presets.

#### Parameters

None.

#### Response

```json
{
  "type": "text",
  "text": "{\"presets\": [{\"name\": \"desktop\", \"width\": 1280, \"height\": 720, ...}, ...]}"
}
```

---

## Device Presets

Device presets provide predefined viewport configurations with accurate user agents.

### Available Presets

| Preset Name | Width | Height | Scale | Device |
|-------------|-------|--------|-------|--------|
| `desktop` | 1280 | 720 | 1.0 | Standard desktop (16:9) |
| `desktop-hd` | 1920 | 1080 | 1.0 | Full HD desktop |
| `tablet` | 768 | 1024 | 2.0 | iPad (portrait) |
| `tablet-landscape` | 1024 | 768 | 2.0 | iPad (landscape) |
| `mobile` | 375 | 667 | 2.0 | iPhone SE |
| `mobile-large` | 414 | 896 | 3.0 | iPhone 11 Pro Max |

### Using Presets

**With screenshot_page:**
```json
{
  "url": "https://example.com",
  "devicePreset": "mobile"
}
```

**With screenshot_multi:**
```json
{
  "url": "https://example.com",
  "viewports": "[\"desktop\", \"tablet\", \"mobile\"]"
}
```

### Preset Details

Presets include:
- **Viewport dimensions** for accurate rendering
- **Device scale** (pixel ratio)
- **User agent string** matching the device
- **Mobile viewport meta tag** support

Example: Using `mobile` preset is equivalent to:
```json
{
  "width": 375,
  "height": 667,
  "scale": 2.0,
  "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"
}
```

---

## Common Use Cases

### 1. AI-Assisted Web Design

**Scenario:** AI generates HTML, user provides feedback, AI iterates

**Workflow:**
```
1. User: "Create a pricing page with three tiers"
2. AI generates HTML using modern CSS
3. AI calls screenshot_page to show the design
4. User: "Make the middle tier more prominent"
5. AI modifies HTML and captures new screenshot
6. Repeat until satisfied
```

**Example MCP call:**
```json
{
  "html": "<!DOCTYPE html><html>...</html>",
  "width": 1280,
  "height": 1024,
  "fullPage": true
}
```

### 2. Responsive Design Testing

**Scenario:** Test how a page looks across different devices

**Example:**
```json
{
  "url": "https://yoursite.com/new-feature",
  "viewports": "[\"desktop-hd\", \"tablet\", \"tablet-landscape\", \"mobile\", \"mobile-large\"]"
}
```

**Result:** 5 screenshots showing the page at different viewport sizes

### 3. Landing Page Preview

**Scenario:** Preview a complete landing page before deployment

**Example:**
```json
{
  "filePath": "/home/user/projects/landing-page/index.html",
  "fullPage": true,
  "format": "jpeg",
  "quality": 90
}
```

### 4. Dark Mode Testing

**Scenario:** Verify dark mode CSS works correctly

**Example:**
```json
{
  "html": "<!DOCTYPE html><html><head><style>@media (prefers-color-scheme: dark) { body { background: #000; color: #fff; } }</style></head><body><h1>Dark Mode Test</h1></body></html>",
  "darkMode": true
}
```

### 5. Dynamic Content Capture

**Scenario:** Capture page after JavaScript renders content

**Example:**
```json
{
  "url": "https://example.com/dashboard",
  "waitForSelector": ".chart-loaded",
  "waitMs": 2000
}
```

This waits for the `.chart-loaded` element to appear, then waits an additional 2 seconds before capturing.

### 6. Component Gallery

**Scenario:** Create visual documentation of UI components

**Example:**
```json
{
  "html": "<!DOCTYPE html><html><head><link rel='stylesheet' href='styles.css'></head><body><div class='button-primary'>Primary Button</div><div class='button-secondary'>Secondary Button</div></body></html>",
  "filePath": "/home/user/projects/component-library/index.html",
  "width": 800,
  "height": 400
}
```

### 7. Email Template Preview

**Scenario:** Preview HTML email templates

**Example:**
```json
{
  "filePath": "/home/user/templates/newsletter.html",
  "width": 600,
  "fullPage": true
}
```

Email templates are typically 600px wide, matching common email client constraints.

### 8. Full Page Documentation

**Scenario:** Capture entire documentation page for PDF conversion

**Example:**
```json
{
  "url": "https://docs.example.com/api-reference",
  "fullPage": true,
  "maxHeight": 10000,
  "format": "png"
}
```

### 9. Quick Thumbnail Generation

**Scenario:** Generate small preview thumbnails quickly

**Example:**
```json
{
  "url": "https://example.com",
  "thumbnail": true
}
```

Equivalent to:
```json
{
  "url": "https://example.com",
  "format": "jpeg",
  "quality": 60,
  "scale": 0.5
}
```

### 10. Cross-Browser Comparison

**Scenario:** Compare rendering across different browsers

**Setup different configs:**

**Chromium config:**
```json
{
  "Browser": {
    "Type": "Chromium"
  }
}
```

**Firefox config:**
```json
{
  "Browser": {
    "Type": "Firefox"
  }
}
```

Run separate server instances and compare screenshots.

---

## Image Optimization

### Format Selection

| Format | Best For | Pros | Cons |
|--------|----------|------|------|
| PNG | Screenshots with text, UI elements | Lossless, sharp text | Larger file size |
| JPEG | Photos, complex graphics | Smaller file size (60-90% reduction) | Lossy compression |

**Recommendation:**
- Use PNG for UI mockups, documentation, text-heavy pages
- Use JPEG for marketing pages, photo galleries

### Quality Settings

JPEG quality parameter (1-100):

| Quality | File Size | Use Case |
|---------|-----------|----------|
| 90-100 | Large | High-quality archives |
| 70-89 | Medium | General use (recommended) |
| 50-69 | Small | Thumbnails, previews |
| 1-49 | Very small | Not recommended (artifacts) |

**Example:**
```json
{
  "format": "jpeg",
  "quality": 80
}
```

### Scaling

Reduce image dimensions to decrease file size:

| Scale | Resolution | File Size | Use Case |
|-------|------------|-----------|----------|
| 1.0 | Full (e.g., 1920x1080) | 100% | Original quality |
| 0.75 | 75% (e.g., 1440x810) | ~56% | High quality |
| 0.5 | 50% (e.g., 960x540) | ~25% | Medium quality |
| 0.25 | 25% (e.g., 480x270) | ~6% | Thumbnails |

**Example:**
```json
{
  "scale": 0.75
}
```

### Optimization Strategies

#### Strategy 1: Balanced Quality
```json
{
  "format": "jpeg",
  "quality": 80,
  "scale": 0.9
}
```
**Result:** ~60% smaller, minimal visible quality loss

#### Strategy 2: Maximum Compression
```json
{
  "format": "jpeg",
  "quality": 70,
  "scale": 0.75
}
```
**Result:** ~80% smaller, still acceptable quality

#### Strategy 3: Quick Thumbnails
```json
{
  "thumbnail": true
}
```
**Equivalent to:**
```json
{
  "format": "jpeg",
  "quality": 60,
  "scale": 0.5
}
```
**Result:** ~90% smaller, suitable for previews

#### Strategy 4: Compact Multi-Viewport
```json
{
  "viewports": "[\"desktop\", \"mobile\"]",
  "compact": true
}
```
**Equivalent to:**
```json
{
  "format": "jpeg",
  "quality": 70,
  "scale": 0.75
}
```

### Full Page Optimization

Full-page screenshots can be very large. Use `maxHeight` to limit:

```json
{
  "fullPage": true,
  "maxHeight": 5000,
  "format": "jpeg",
  "quality": 75
}
```

This captures up to 5000 pixels vertically, preventing excessive memory use.

---

## Troubleshooting

### Common Issues

#### Issue: "Browser not found" error

**Symptoms:**
```
Error: Executable doesn't exist at /path/to/chromium
```

**Solution:**
```bash
# Install browser
cd src/ScreenshotMcp.Server/bin/Debug/net8.0
./playwright.sh install chromium
```

#### Issue: Timeout during navigation

**Symptoms:**
```
Error: Timeout 30000ms exceeded during navigation
```

**Solutions:**

1. **Increase timeout:**
```json
{
  "Defaults": {
    "Timeout": 60000
  }
}
```

2. **Check network connectivity:**
```bash
curl -I https://example.com
```

3. **Try different wait state:**
The server uses `WaitUntilState.NetworkIdle`. Some sites may never reach this state.

**Workaround:** Use `waitMs` instead:
```json
{
  "url": "https://example.com",
  "waitMs": 3000
}
```

#### Issue: "Element not found" with waitForSelector

**Symptoms:**
```
Error: Timeout waiting for selector '#my-element'
```

**Solutions:**

1. **Verify selector is correct:**
Open page in browser, check DevTools

2. **Increase wait time:**
```json
{
  "waitForSelector": "#my-element",
  "waitMs": 5000
}
```

3. **Use different selector:**
Try more specific or generic selector

#### Issue: Temp files accumulating

**Symptoms:**
`/tmp/mcp-screenshots/` directory growing

**Solutions:**

1. **Check cleanup service:**
```bash
# View logs for cleanup service
grep "TempFileCleanup" /path/to/logs
```

2. **Reduce cleanup interval:**
```json
{
  "CleanupIntervalMinutes": 30
}
```

3. **Manual cleanup:**
```bash
find /tmp/mcp-screenshots -type f -mtime +1 -delete
```

#### Issue: High memory usage

**Symptoms:**
Server consuming excessive RAM

**Solutions:**

1. **Reduce concurrent pages:**
```json
{
  "MaxConcurrentPages": 3
}
```

2. **Use image optimization:**
```json
{
  "format": "jpeg",
  "quality": 70,
  "scale": 0.75
}
```

3. **Limit full-page height:**
```json
{
  "fullPage": true,
  "maxHeight": 5000
}
```

#### Issue: Screenshot is blank

**Symptoms:**
Screenshot returns but image is white/blank

**Causes & Solutions:**

1. **Page not fully loaded:**
```json
{
  "waitMs": 2000
}
```

2. **Content loaded asynchronously:**
```json
{
  "waitForSelector": ".main-content"
}
```

3. **CSS/JS not loaded from file path:**
Ensure CSS/JS paths are absolute or relative to HTML file

#### Issue: Security violation errors

**Symptoms:**
```
Error: File path not in allowed directories
Error: URL matches blocked pattern
```

**Solutions:**

1. **Check AllowedBasePaths:**
```json
{
  "Security": {
    "AllowedBasePaths": [
      "/home/user/projects"
    ]
  }
}
```

2. **Check BlockedUrlPatterns:**
```json
{
  "Security": {
    "BlockedUrlPatterns": []
}
```

3. **Use absolute paths:**
```json
{
  "filePath": "/absolute/path/to/file.html"
}
```

### Debugging

#### Enable verbose logging

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ScreenshotMcp.Server": "Trace"
    }
  }
}
```

#### Run with visible browser

```json
{
  "Browser": {
    "Headless": false
  }
}
```

This opens a visible browser window for debugging.

#### Check server logs

Logs output to console by default. Look for:
- Request parameters
- Navigation events
- Screenshot capture events
- Error stack traces

### Getting Help

If issues persist:

1. Check existing issues on GitHub
2. Review documentation in `/docs`
3. Create issue with:
   - Environment details (OS, .NET version, browser)
   - Complete error message
   - Reproduction steps
   - Relevant configuration

---

## Tips and Best Practices

### Performance Tips

1. **Reuse viewports in screenshot_multi:**
   - Single navigation, multiple captures
   - Faster than multiple screenshot_page calls

2. **Use appropriate quality settings:**
   - Don't use quality=100 unless necessary
   - quality=80 is usually indistinguishable

3. **Scale down large screenshots:**
   - scale=0.75 reduces file size by ~56%
   - Minimal quality impact for most use cases

4. **Limit full-page captures:**
   - Use maxHeight to prevent excessive memory use
   - Consider if viewport capture is sufficient

5. **Use thumbnail mode for previews:**
   - Much faster than full-quality captures
   - 90% smaller file sizes

### Security Best Practices

1. **Restrict file access in production:**
```json
{
  "Security": {
    "AllowedBasePaths": [
      "/var/www/html",
      "/opt/projects"
    ]
  }
}
```

2. **Block internal networks:**
```json
{
  "Security": {
    "BlockedUrlPatterns": [
      "localhost",
      "127\\.0\\.0\\.1",
      "192\\.168\\.",
      "10\\.",
      "172\\.(1[6-9]|2[0-9]|3[0-1])\\."
    ]
  }
}
```

3. **Set reasonable resource limits:**
```json
{
  "Security": {
    "MaxViewportWidth": 4096,
    "MaxViewportHeight": 4096,
    "MaxWaitMs": 30000
  }
}
```

### Design Workflow Tips

1. **Start with presets:**
   - Use device presets for common viewports
   - Ensures accurate device emulation

2. **Test dark mode early:**
   - Many users prefer dark mode
   - Use `darkMode: true` to verify

3. **Use fullPage for complete views:**
   - Captures entire scrollable content
   - Great for documentation, full designs

4. **Wait for dynamic content:**
   - Modern sites load content asynchronously
   - Use waitForSelector to ensure content is ready

5. **Iterate quickly:**
   - Use thumbnail mode during iteration
   - Switch to full quality for final captures

### Responsive Design Tips

1. **Test common breakpoints:**
```json
{
  "viewports": "[
    {\"width\": 320, \"height\": 568},
    {\"width\": 768, \"height\": 1024},
    {\"width\": 1280, \"height\": 720},
    {\"width\": 1920, \"height\": 1080}
  ]"
}
```

2. **Test both orientations for tablets:**
```json
{
  "viewports": "[\"tablet\", \"tablet-landscape\"]"
}
```

3. **Use scale parameter for retina displays:**
```json
{
  "width": 375,
  "height": 667,
  "scale": 2.0
}
```

### Content Tips

1. **Use semantic HTML:**
   - Proper HTML structure renders better
   - Use DOCTYPE declaration

2. **Include viewport meta tag for mobile:**
```html
<meta name="viewport" content="width=device-width, initial-scale=1">
```

3. **Use absolute URLs for external resources:**
```html
<link rel="stylesheet" href="https://cdn.example.com/styles.css">
```

4. **Inline critical CSS for file paths:**
   - External stylesheets may not load from file://
   - Use `<style>` tags for reliability

### Optimization Tips

1. **Batch viewport captures:**
   - Use screenshot_multi instead of multiple screenshot_page calls
   - More efficient (single navigation)

2. **Choose format wisely:**
   - PNG: Text, UI, diagrams
   - JPEG: Photos, complex graphics

3. **Use compact mode for multi-viewport:**
   - Significantly faster
   - Smaller file sizes
   - Good quality for most use cases

4. **Adjust MaxConcurrentPages based on load:**
   - Default is 5
   - Increase for high throughput
   - Decrease for limited memory

### Debugging Tips

1. **Test HTML locally first:**
   - Save to file, open in browser
   - Verify rendering before screenshot

2. **Use browser DevTools:**
   - Test selectors
   - Verify network requests
   - Check console for errors

3. **Enable headless=false for debugging:**
   - Watch browser behavior
   - See exactly what's being captured

4. **Check temp directory:**
   - Verify temp files are being created
   - Confirm cleanup is working

---

## Advanced Usage

### Custom Viewport Configurations

Beyond presets, use custom viewports for specific needs:

**Ultra-wide desktop:**
```json
{
  "width": 3440,
  "height": 1440
}
```

**Square (social media):**
```json
{
  "width": 1080,
  "height": 1080
}
```

**Custom mobile (Pixel 7):**
```json
{
  "width": 412,
  "height": 915,
  "scale": 2.625
}
```

### Complex Wait Conditions

**Wait for multiple elements:**
```html
<script>
  Promise.all([
    document.querySelector('.header').complete,
    document.querySelector('.content').complete,
    document.querySelector('.footer').complete
  ]).then(() => {
    document.body.classList.add('ready');
  });
</script>
```

```json
{
  "waitForSelector": "body.ready"
}
```

**Wait for animation to complete:**
```json
{
  "waitMs": 3000
}
```

### Testing Email Templates

Email clients have specific constraints:

```json
{
  "width": 600,
  "fullPage": true,
  "darkMode": false
}
```

**Best practices:**
- Width: 600px (standard email width)
- fullPage: true (capture entire email)
- Test both light and dark modes
- Inline all CSS (email clients strip `<style>` tags)

### Capturing Web Components

Modern web components may require specific timing:

```json
{
  "url": "https://example.com",
  "waitForSelector": "my-web-component[loaded]",
  "waitMs": 1000
}
```

Wait for custom element's `loaded` attribute, then additional 1 second.

---

## Summary

The MCP Screenshot Server provides flexible, high-performance screenshot capabilities for AI-assisted workflows:

- **Three tools:** screenshot_page, screenshot_multi, list_presets
- **Flexible input:** HTML, file paths, or URLs
- **Device presets:** Accurate emulation of common devices
- **Image optimization:** Format, quality, and scaling options
- **Security:** Configurable file and URL restrictions
- **Performance:** Browser pooling and concurrency control

For more information:
- **Architecture:** See [ARCHITECTURE.md](/home/cpike/Workspace/screenshot-mcp/docs/ARCHITECTURE.md)
- **Development:** See [DEVELOPER.md](/home/cpike/Workspace/screenshot-mcp/docs/DEVELOPER.md)
- **Design Spec:** See [DESIGN_SPEC.md](/home/cpike/Workspace/screenshot-mcp/docs/DESIGN_SPEC.md)

Happy screenshotting!
