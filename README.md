# MCP Screenshot Server

A Model Context Protocol (MCP) server that provides browser-based rendering and screenshot capabilities. Enables AI agents to generate HTML/CSS/JS prototypes, capture visual output, and iterate on designs through a feedback loop.

## Features

- Render HTML content (raw HTML, files, or URLs) in a headless browser
- Capture screenshots at configurable viewport sizes
- Return images in base64 format for AI agent consumption
- Support for device presets (desktop, tablet, mobile)
- Wait conditions for dynamic content
- Dark mode emulation
- Full page capture for scrollable content

## Prerequisites

- .NET 8.0 SDK
- Playwright browsers (installed automatically on first build)

## Installation

1. Clone the repository
2. Restore dependencies and build:

```bash
dotnet build
```

3. Install Playwright browsers (run from the project directory):

```bash
# Windows
pwsh bin/Debug/net8.0/playwright.ps1 install chromium

# Linux/macOS
./bin/Debug/net8.0/playwright.sh install chromium
```

## Usage

### Running the Server

```bash
dotnet run --project src/ScreenshotMcp.Server
```

The server communicates via stdio and can be connected to any MCP-compatible client (like Claude Desktop).

### Configuration with Claude Desktop

Add this configuration to your Claude Desktop settings:

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

## Tools

### `screenshot_page`

Renders HTML content and returns a screenshot.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `html` | string | No* | - | Raw HTML content to render |
| `filePath` | string | No* | - | Absolute path to HTML file |
| `url` | string | No* | - | URL to capture (http/https) |
| `width` | integer | No | 1280 | Viewport width in pixels |
| `height` | integer | No | 720 | Viewport height in pixels |
| `fullPage` | boolean | No | false | Capture full scrollable page |
| `devicePreset` | string | No | - | Predefined device (see presets) |
| `waitForSelector` | string | No | - | CSS selector to wait for |
| `waitMs` | integer | No | 0 | Additional wait time (ms) |
| `darkMode` | boolean | No | false | Emulate dark color scheme |

*One of `html`, `filePath`, or `url` is required.

### `screenshot_multi`

Captures screenshots at multiple viewport sizes in a single call.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `html` | string | No* | Raw HTML content |
| `filePath` | string | No* | Path to HTML file |
| `url` | string | No* | URL to capture |
| `viewports` | string | Yes | JSON array of viewport configs or preset names |
| `waitForSelector` | string | No | CSS selector to wait for |
| `waitMs` | integer | No | Additional wait time (ms) |
| `darkMode` | boolean | No | Emulate dark color scheme |

### `list_presets`

Returns available device presets.

## Device Presets

| Preset | Width | Height | Scale |
|--------|-------|--------|-------|
| `desktop` | 1280 | 720 | 1 |
| `desktop-hd` | 1920 | 1080 | 1 |
| `tablet` | 768 | 1024 | 2 |
| `tablet-landscape` | 1024 | 768 | 2 |
| `mobile` | 375 | 667 | 2 |
| `mobile-large` | 414 | 896 | 3 |

## Configuration

Configuration is done via `appsettings.json`:

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

## Security Considerations

- **File Path Validation**: When `AllowedBasePaths` is configured, only files within those directories can be accessed
- **URL Restrictions**: Configure `BlockedUrlPatterns` to prevent access to sensitive URLs
- **Resource Limits**: Viewport dimensions and wait times are capped to prevent resource exhaustion
- **Timeout Enforcement**: Hard limit on render time to prevent hangs

## Running Tests

```bash
dotnet test
```

## License

MIT
