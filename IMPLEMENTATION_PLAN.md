# MCP Screenshot Server - Implementation Plan

## Executive Summary

This document provides a comprehensive implementation plan for the MCP Screenshot Server project. The server will provide browser-based rendering and screenshot capabilities through the Model Context Protocol (MCP), enabling AI agents to generate HTML/CSS/JS prototypes, capture visual output, and iterate on designs through a feedback loop.

---

## 1. Requirement Summary

### Core Requirements

- **screenshot_page Tool**: Render HTML content from raw HTML, file path, or URL and return a base64-encoded PNG screenshot
- **screenshot_multi Tool** (Optional): Capture screenshots at multiple viewport sizes in a single call
- **list_presets Tool** (Optional): Return available device presets for viewport configuration

### Key Capabilities

- Configurable viewport dimensions (width/height)
- Full page capture mode for scrollable content
- Device preset emulation (desktop, tablet, mobile variants)
- Wait conditions (CSS selector, millisecond delay)
- Dark mode emulation
- Concurrent page handling with pool management
- Temporary file lifecycle management for raw HTML rendering

---

## 2. Architectural Considerations

### 2.1 Existing System Components

The project directory is currently empty except for `spec.md`. This is a greenfield implementation.

### 2.2 Integration Requirements

| Component | Integration Point | Notes |
|-----------|------------------|-------|
| ModelContextProtocol SDK | NuGet package `ModelContextProtocol` v0.4.1-preview | Official C# SDK maintained by Microsoft/Anthropic |
| Microsoft.Playwright | NuGet package `Microsoft.Playwright` v1.42+ | Browser automation and screenshot capture |
| .NET Generic Host | `Microsoft.Extensions.Hosting` | Application lifecycle, DI, configuration |
| MCP Client | stdio transport | Claude Desktop or other MCP clients |

### 2.3 Architectural Patterns to Follow

1. **Dependency Injection**: Use Microsoft.Extensions.DependencyInjection throughout
2. **Options Pattern**: Configuration via `IOptions<T>` with appsettings.json support
3. **Repository/Service Pattern**: Clean separation between tool handlers and business logic
4. **Async/Await**: All browser operations must be asynchronous
5. **Disposable Pattern**: Proper cleanup of Playwright resources via `IAsyncDisposable`

### 2.4 Security Considerations

| Concern | Mitigation Strategy |
|---------|---------------------|
| Arbitrary file access | Whitelist allowed base directories for `filePath` parameter |
| Malicious URLs | URL validation, optional domain whitelist/blacklist |
| Resource exhaustion | Max viewport dimensions (e.g., 4096x4096), concurrent page limits |
| Timeout attacks | Hard render timeout with cancellation token |
| Memory leaks | Proper page disposal, background cleanup job for temp files |

### 2.5 Performance Considerations

| Concern | Strategy |
|---------|----------|
| Browser startup latency | Lazy initialization, keep browser instance alive |
| Page creation overhead | Consider page pooling for high-throughput scenarios |
| Concurrent requests | SemaphoreSlim-based concurrency control |
| Large screenshots | PNG compression, consider JPEG option for photos |
| Temp file accumulation | Immediate cleanup + background sweep job |

### 2.6 Data Model Implications

No persistent data storage required. All operations are stateless request/response.

---

## 3. Project Structure

### 3.1 Recommended Solution Structure

```
screenshot-mcp/
├── src/
│   └── ScreenshotMcp.Server/
│       ├── Program.cs                      # Application entry point
│       ├── ScreenshotMcp.Server.csproj     # Project file
│       ├── appsettings.json                # Default configuration
│       ├── appsettings.Development.json    # Development overrides
│       │
│       ├── Configuration/
│       │   ├── ScreenshotServerOptions.cs  # Root configuration class
│       │   ├── BrowserOptions.cs           # Browser-specific settings
│       │   ├── DefaultsOptions.cs          # Default parameter values
│       │   └── SecurityOptions.cs          # Security constraints
│       │
│       ├── Models/
│       │   ├── DevicePreset.cs             # Device preset definition
│       │   ├── DevicePresets.cs            # Static preset catalog
│       │   ├── ScreenshotRequest.cs        # Internal request model
│       │   ├── ScreenshotResult.cs         # Internal result model
│       │   ├── ViewportConfig.cs           # Viewport dimensions
│       │   └── ContentSource.cs            # Enum for html/file/url
│       │
│       ├── Services/
│       │   ├── IBrowserPoolManager.cs      # Browser pool interface
│       │   ├── BrowserPoolManager.cs       # Browser lifecycle management
│       │   ├── IScreenshotService.cs       # Screenshot service interface
│       │   ├── ScreenshotService.cs        # Core screenshot logic
│       │   ├── ITempFileManager.cs         # Temp file interface
│       │   ├── TempFileManager.cs          # Temp file lifecycle
│       │   └── TempFileCleanupService.cs   # Background cleanup job
│       │
│       ├── Validation/
│       │   ├── ISecurityValidator.cs       # Security validation interface
│       │   ├── SecurityValidator.cs        # File path/URL validation
│       │   ├── IInputValidator.cs          # Input validation interface
│       │   └── InputValidator.cs           # Parameter validation
│       │
│       ├── Tools/
│       │   ├── ScreenshotPageTool.cs       # screenshot_page implementation
│       │   ├── ScreenshotMultiTool.cs      # screenshot_multi implementation
│       │   └── ListPresetsTool.cs          # list_presets implementation
│       │
│       └── Extensions/
│           ├── ServiceCollectionExtensions.cs  # DI registration helpers
│           └── PlaywrightExtensions.cs         # Playwright helper methods
│
├── tests/
│   ├── ScreenshotMcp.Server.Tests/
│   │   ├── ScreenshotMcp.Server.Tests.csproj
│   │   │
│   │   ├── Unit/
│   │   │   ├── Validation/
│   │   │   │   ├── SecurityValidatorTests.cs
│   │   │   │   └── InputValidatorTests.cs
│   │   │   ├── Models/
│   │   │   │   └── DevicePresetsTests.cs
│   │   │   └── Services/
│   │   │       └── TempFileManagerTests.cs
│   │   │
│   │   ├── Integration/
│   │   │   ├── ScreenshotServiceTests.cs
│   │   │   ├── BrowserPoolManagerTests.cs
│   │   │   └── ToolIntegrationTests.cs
│   │   │
│   │   └── Fixtures/
│   │       ├── simple.html
│   │       ├── responsive.html
│   │       ├── async-load.html
│   │       └── long-page.html
│   │
│   └── ScreenshotMcp.Server.E2E/
│       ├── ScreenshotMcp.Server.E2E.csproj
│       └── McpClientTests.cs               # End-to-end MCP protocol tests
│
├── docs/
│   ├── architecture.md                     # Architecture documentation
│   ├── configuration.md                    # Configuration reference
│   └── security.md                         # Security considerations
│
├── spec.md                                 # Original specification
├── IMPLEMENTATION_PLAN.md                  # This document
├── README.md                               # Project overview and setup
├── .gitignore                              # Git ignore rules
├── Directory.Build.props                   # Shared MSBuild properties
└── ScreenshotMcp.sln                       # Solution file
```

### 3.2 File-by-File Implementation Breakdown

#### Core Infrastructure Files

| File | Purpose | Priority |
|------|---------|----------|
| `ScreenshotMcp.Server.csproj` | Project file with NuGet references | P0 |
| `Program.cs` | Host builder, DI setup, MCP server configuration | P0 |
| `appsettings.json` | Default configuration values | P0 |
| `Directory.Build.props` | Shared build properties, nullable, implicit usings | P0 |

#### Configuration Files

| File | Purpose | Priority |
|------|---------|----------|
| `ScreenshotServerOptions.cs` | Root options class binding to `ScreenshotServer` section | P0 |
| `BrowserOptions.cs` | Browser type, headless mode, launch arguments | P0 |
| `DefaultsOptions.cs` | Default viewport, timeout, wait values | P0 |
| `SecurityOptions.cs` | Allowed paths, blocked URLs, max dimensions | P1 |

#### Model Files

| File | Purpose | Priority |
|------|---------|----------|
| `DevicePreset.cs` | Record type for preset definition (width, height, scale, user agent) | P0 |
| `DevicePresets.cs` | Static class with all predefined presets | P0 |
| `ScreenshotRequest.cs` | Internal DTO for screenshot operations | P0 |
| `ScreenshotResult.cs` | Internal DTO for operation results | P0 |
| `ViewportConfig.cs` | Width/height/scale value object | P0 |
| `ContentSource.cs` | Enum: Html, FilePath, Url | P0 |

#### Service Files

| File | Purpose | Priority |
|------|---------|----------|
| `IBrowserPoolManager.cs` | Interface for browser pool operations | P0 |
| `BrowserPoolManager.cs` | Playwright browser lifecycle, page acquisition/release | P0 |
| `IScreenshotService.cs` | Interface for screenshot capture operations | P0 |
| `ScreenshotService.cs` | Core business logic for capturing screenshots | P0 |
| `ITempFileManager.cs` | Interface for temp file operations | P0 |
| `TempFileManager.cs` | Create, track, and delete temp HTML files | P0 |
| `TempFileCleanupService.cs` | Background service for orphan cleanup | P1 |

#### Validation Files

| File | Purpose | Priority |
|------|---------|----------|
| `ISecurityValidator.cs` | Interface for security validation | P0 |
| `SecurityValidator.cs` | File path and URL validation logic | P0 |
| `IInputValidator.cs` | Interface for input validation | P0 |
| `InputValidator.cs` | Parameter validation, mutual exclusivity checks | P0 |

#### Tool Files

| File | Purpose | Priority |
|------|---------|----------|
| `ScreenshotPageTool.cs` | MCP tool implementation for screenshot_page | P0 |
| `ScreenshotMultiTool.cs` | MCP tool implementation for screenshot_multi | P2 |
| `ListPresetsTool.cs` | MCP tool implementation for list_presets | P2 |

#### Extension Files

| File | Purpose | Priority |
|------|---------|----------|
| `ServiceCollectionExtensions.cs` | `AddScreenshotServer()` extension method | P0 |
| `PlaywrightExtensions.cs` | Helper methods for viewport/device configuration | P0 |

---

## 4. Dependencies and Setup Requirements

### 4.1 NuGet Packages

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- MCP SDK -->
    <PackageReference Include="ModelContextProtocol" Version="0.4.1-preview.1" />

    <!-- Hosting and DI -->
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />

    <!-- Browser Automation -->
    <PackageReference Include="Microsoft.Playwright" Version="1.42.0" />

    <!-- Logging -->
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 4.2 Test Project Dependencies

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
  <PackageReference Include="xunit" Version="2.7.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7" />
  <PackageReference Include="Moq" Version="4.20.70" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="Microsoft.Playwright" Version="1.42.0" />
</ItemGroup>
```

### 4.3 Post-Build Requirements

After building, Playwright browser binaries must be installed:

```bash
# Install Playwright CLI tool
dotnet tool install --global Microsoft.Playwright.CLI

# Install browser binaries (run from project directory)
playwright install chromium

# Or install all browsers
playwright install
```

### 4.4 Development Environment Setup

1. .NET 8.0 SDK
2. IDE with C# support (VS Code with C# Dev Kit, Visual Studio 2022, JetBrains Rider)
3. Playwright browsers installed
4. For testing: xUnit test adapter

---

## 5. Implementation Phases

### Phase 1: Foundation (Priority 0)

**Duration**: 2-3 days

**Goal**: Minimal working MCP server with basic screenshot capability

#### Tasks

1. **Project Setup**
   - Create solution and project structure
   - Add NuGet package references
   - Configure Directory.Build.props

2. **Configuration Infrastructure**
   - Implement options classes
   - Create appsettings.json with defaults
   - Wire up configuration binding in Program.cs

3. **Core Models**
   - Implement DevicePreset and DevicePresets
   - Create ScreenshotRequest/Result DTOs
   - Define ContentSource enum

4. **Browser Pool Manager**
   - Implement IBrowserPoolManager interface
   - Create BrowserPoolManager with:
     - Lazy browser initialization
     - Page acquisition with semaphore
     - Page release with proper disposal
     - IAsyncDisposable implementation

5. **Temp File Manager**
   - Implement ITempFileManager interface
   - Create TempFileManager with:
     - Unique file name generation
     - Write and track temp files
     - Immediate cleanup after use

6. **Basic Screenshot Service**
   - Implement IScreenshotService interface
   - Create ScreenshotService with:
     - Content source handling (HTML, file, URL)
     - Basic viewport configuration
     - Screenshot capture and base64 encoding

7. **screenshot_page Tool**
   - Implement tool class with MCP attributes
   - Parameter validation
   - Error handling with MCP error codes

8. **Program.cs Integration**
   - Configure Generic Host
   - Register services
   - Configure MCP server with stdio transport
   - Register tools from assembly

#### Acceptance Criteria - Phase 1

- [ ] Server starts without errors
- [ ] Server responds to MCP tool discovery
- [ ] screenshot_page with raw HTML returns base64 PNG
- [ ] screenshot_page with file path returns base64 PNG
- [ ] screenshot_page with URL returns base64 PNG
- [ ] Custom viewport dimensions work correctly
- [ ] Errors return proper MCP error responses

---

### Phase 2: Device Presets and Wait Conditions (Priority 1)

**Duration**: 1-2 days

**Goal**: Full feature parity for screenshot_page tool

#### Tasks

1. **Device Preset Resolution**
   - Extend ScreenshotService to resolve device presets
   - Apply viewport dimensions from preset
   - Set device scale factor
   - Configure user agent string

2. **Wait Conditions**
   - Implement waitForSelector with timeout
   - Implement waitMs delay
   - Proper timeout error handling

3. **Dark Mode Emulation**
   - Configure color scheme preference
   - Test with media query detection

4. **Full Page Capture**
   - Implement fullPage screenshot option
   - Handle scrollable content correctly

5. **Input Validation**
   - Implement InputValidator
   - Mutual exclusivity check (html/filePath/url)
   - Viewport dimension limits
   - Wait time limits

6. **Security Validation**
   - Implement SecurityValidator
   - File path whitelist checking
   - URL validation and optional blocklist

7. **Background Cleanup Service**
   - Implement TempFileCleanupService
   - Configurable cleanup interval
   - Clean orphaned files older than threshold

#### Acceptance Criteria - Phase 2

- [ ] All device presets work correctly
- [ ] waitForSelector waits for element before capture
- [ ] waitMs adds delay before capture
- [ ] darkMode applies dark color scheme
- [ ] fullPage captures entire scrollable content
- [ ] Invalid inputs return appropriate error codes
- [ ] File path restrictions are enforced
- [ ] Background cleanup removes old temp files

---

### Phase 3: Optional Tools (Priority 2)

**Duration**: 1-2 days

**Goal**: Implement screenshot_multi and list_presets tools

#### Tasks

1. **list_presets Tool**
   - Return JSON array of all presets
   - Include all preset properties

2. **screenshot_multi Tool**
   - Accept array of viewport configs or preset names
   - Capture screenshots sequentially or in parallel
   - Return array of images with viewport annotations
   - Handle mixed preset/custom viewport inputs

3. **Multi-Screenshot Optimization**
   - Reuse page instance across viewport changes
   - Consider parallel capture for independent viewports

#### Acceptance Criteria - Phase 3

- [ ] list_presets returns all defined presets
- [ ] screenshot_multi captures at multiple viewports
- [ ] Each image includes viewport annotations
- [ ] Mixed preset/custom viewports work correctly

---

### Phase 4: Testing and Documentation (Priority 1)

**Duration**: 2-3 days

**Goal**: Comprehensive test coverage and documentation

#### Tasks

1. **Unit Tests**
   - SecurityValidator tests (valid/invalid paths, URLs)
   - InputValidator tests (all validation rules)
   - DevicePresets tests (preset lookup, values)
   - TempFileManager tests (create, cleanup)

2. **Integration Tests**
   - BrowserPoolManager tests (acquire, release, concurrency)
   - ScreenshotService tests (all content sources, presets, options)
   - Full tool integration tests

3. **Test Fixtures**
   - Create HTML test files:
     - simple.html - Basic static content
     - responsive.html - Media query responsive design
     - async-load.html - JavaScript delayed content
     - long-page.html - Scrollable content

4. **Documentation**
   - README.md with setup instructions
   - Configuration reference
   - Architecture documentation
   - Security documentation

#### Acceptance Criteria - Phase 4

- [ ] Unit test coverage > 80%
- [ ] Integration tests pass reliably
- [ ] Test fixtures cover all scenarios
- [ ] Documentation is complete and accurate

---

### Phase 5: Hardening and Polish (Priority 2)

**Duration**: 1-2 days

**Goal**: Production readiness

#### Tasks

1. **Error Handling Improvements**
   - Comprehensive exception handling
   - Retry logic for transient Playwright errors
   - Graceful degradation

2. **Logging Enhancement**
   - Structured logging throughout
   - Debug-level browser operation logs
   - Error context for troubleshooting

3. **Performance Optimization**
   - Connection pooling review
   - Memory usage profiling
   - Garbage collection optimization

4. **Configuration Validation**
   - Startup validation of required settings
   - Helpful error messages for misconfiguration

5. **Docker Support**
   - Create Dockerfile
   - Multi-stage build for smaller image
   - Alpine-based image with Playwright dependencies

#### Acceptance Criteria - Phase 5

- [ ] No unhandled exceptions in normal operation
- [ ] Logs provide useful debugging information
- [ ] Memory usage is stable under load
- [ ] Configuration errors are caught at startup
- [ ] Docker image builds and runs successfully

---

## 6. Timeline and Dependency Map

```
Week 1
├── Day 1-2: Phase 1 (Foundation)
│   ├── Project setup
│   ├── Configuration
│   ├── Core models
│   └── Browser pool manager
│
├── Day 3: Phase 1 (continued)
│   ├── Temp file manager
│   ├── Basic screenshot service
│   └── screenshot_page tool
│
├── Day 4-5: Phase 2 (Features)
│   ├── Device presets
│   ├── Wait conditions
│   └── Dark mode, full page

Week 2
├── Day 1-2: Phase 2 (continued) + Phase 3
│   ├── Validation (input, security)
│   ├── Background cleanup
│   └── Optional tools (list_presets, screenshot_multi)
│
├── Day 3-5: Phase 4 (Testing)
│   ├── Unit tests
│   ├── Integration tests
│   └── Documentation

Week 3 (if needed)
├── Day 1-2: Phase 5 (Hardening)
│   ├── Error handling
│   ├── Logging
│   └── Docker support
```

### Dependency Graph

```
┌──────────────────┐
│ Project Setup    │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐     ┌──────────────────┐
│ Configuration    │────►│ Core Models      │
└────────┬─────────┘     └────────┬─────────┘
         │                        │
         ▼                        ▼
┌──────────────────┐     ┌──────────────────┐
│ BrowserPoolMgr   │     │ TempFileManager  │
└────────┬─────────┘     └────────┬─────────┘
         │                        │
         └──────────┬─────────────┘
                    │
                    ▼
         ┌──────────────────┐
         │ ScreenshotService │
         └────────┬─────────┘
                  │
         ┌────────┼────────┐
         │        │        │
         ▼        ▼        ▼
   ┌─────────┐ ┌──────┐ ┌──────┐
   │ Page    │ │ Multi│ │Presets│
   │ Tool    │ │ Tool │ │ Tool │
   └─────────┘ └──────┘ └──────┘
```

### Parallel Work Opportunities

| Stream A | Stream B | Stream C |
|----------|----------|----------|
| Browser pool manager | Configuration classes | Core models |
| Screenshot service | Input validator | Security validator |
| Tool implementations | Test fixtures | Documentation |

---

## 7. Acceptance Criteria Summary

### Functional Requirements

| Requirement | Criteria |
|-------------|----------|
| screenshot_page with HTML | Raw HTML renders and returns valid PNG base64 |
| screenshot_page with file | Local file renders correctly |
| screenshot_page with URL | Remote URL renders correctly |
| Viewport configuration | Custom width/height applied correctly |
| Device presets | All 6 presets work with correct dimensions/scale |
| Full page capture | Scrollable content captured completely |
| Wait for selector | Capture waits for element to appear |
| Wait milliseconds | Capture delayed by specified time |
| Dark mode | prefers-color-scheme: dark is emulated |
| list_presets | Returns all presets with properties |
| screenshot_multi | Captures at multiple viewports correctly |

### Non-Functional Requirements

| Requirement | Criteria |
|-------------|----------|
| Performance | Screenshot capture < 5 seconds for typical page |
| Concurrency | Handles MaxConcurrentPages simultaneous requests |
| Memory | No memory leaks over extended operation |
| Security | File path restrictions enforced |
| Reliability | Graceful error handling, no crashes |
| Observability | Structured logging for all operations |

### Error Handling

| Error Code | Trigger | Response |
|------------|---------|----------|
| INVALID_INPUT | No content source or multiple sources | Clear message about mutual exclusivity |
| FILE_NOT_FOUND | File path does not exist | Include attempted path in message |
| RENDER_TIMEOUT | Page render exceeds timeout | Include timeout value in message |
| SELECTOR_TIMEOUT | waitForSelector element not found | Include selector in message |
| SECURITY_VIOLATION | File path outside allowed directories | Generic security error (no path leakage) |

---

## 8. Risks and Mitigations

### Technical Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Playwright browser download fails | High | Medium | Document manual installation, provide Docker image with browsers |
| MCP SDK breaking changes | Medium | High | Pin to specific version, test upgrades carefully |
| Memory leaks from browser pages | High | Medium | Comprehensive disposal, page pooling with TTL |
| Concurrent access race conditions | High | Medium | Proper synchronization, thorough concurrency testing |
| Large page screenshot OOM | Medium | Low | Max dimension limits, stream to file if needed |

### Operational Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Temp file accumulation | Medium | Medium | Background cleanup service, startup cleanup |
| Browser process orphans | Medium | Low | Process group management, cleanup on shutdown |
| Configuration misconfiguration | Medium | Medium | Startup validation with clear error messages |
| Timeout too aggressive | Medium | Medium | Sensible defaults, per-request overrides |

### Security Risks

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Path traversal attacks | Critical | Medium | Strict path validation, whitelist-only access |
| SSRF via URL parameter | High | Medium | URL validation, optional domain restrictions |
| Resource exhaustion | Medium | Medium | Viewport limits, concurrency limits, timeouts |
| Sensitive data in screenshots | Medium | Low | Document security implications to users |

---

## 9. Spec Gap Analysis

### Identified Gaps in Specification

1. **Error Response Format**: The spec defines error codes but not the exact MCP error response structure. The implementation should follow MCP SDK conventions.

2. **Browser Selection at Runtime**: Spec mentions configurable browser type, but not whether this is per-request or server-wide. Recommend server-wide configuration only.

3. **Scale Factor for Custom Viewports**: Device presets include scale factor, but custom width/height parameters don't. Consider adding optional `scale` parameter.

4. **Maximum Wait Time**: No explicit limit on `waitMs`. Recommend capping at 30 seconds to prevent resource hogging.

5. **URL Protocol Restrictions**: Spec mentions file:// protocol, but security implications need consideration. Recommend http/https only by default.

6. **Retry Behavior**: Spec mentions retry logic but doesn't specify what triggers a retry or how many attempts. Recommend 1 retry for transient browser errors.

7. **Logging Level Configuration**: No specification for log verbosity control. Recommend standard .NET logging configuration.

8. **Health Check Endpoint**: For monitoring in production, consider adding a health check (outside MCP protocol).

### Recommendations

1. Add `scale` parameter to screenshot_page for parity with presets
2. Enforce 30-second max for waitMs
3. Default to http/https only for URL parameter
4. Add optional `retryOnFailure` parameter (default: true)
5. Document that file:// URLs require explicit configuration
6. Consider health check tool for monitoring

---

## 10. Subagent Task Breakdown

This section outlines tasks for specialized subagents if this project were to use a multi-agent development approach.

### dotnet-specialist Tasks

1. **Project Scaffolding**
   - Create solution structure
   - Configure project files with proper SDK references
   - Set up Directory.Build.props

2. **Core Implementation**
   - All service implementations (BrowserPoolManager, ScreenshotService, TempFileManager)
   - All model classes
   - Configuration classes with options pattern
   - Tool implementations with MCP attributes

3. **Validation Layer**
   - SecurityValidator implementation
   - InputValidator implementation

4. **Testing**
   - Unit test implementations
   - Integration test implementations
   - Test fixture HTML files

### docs-writer Tasks

1. **README.md**
   - Project overview
   - Installation instructions
   - Quick start guide
   - Configuration reference
   - Troubleshooting guide

2. **Architecture Documentation**
   - Component diagram
   - Sequence diagrams for key operations
   - Design decisions and rationale

3. **Security Documentation**
   - Security considerations
   - Configuration for secure deployment
   - Threat model

4. **API Documentation**
   - Tool parameter reference
   - Error code reference
   - Example requests and responses

### design-specialist Tasks

Not applicable for this backend-only project.

### html-prototyper Tasks

1. **Test Fixtures**
   - simple.html - Clean static page for basic testing
   - responsive.html - Media query based responsive layout
   - async-load.html - JavaScript that loads content after delay
   - long-page.html - Extended scrollable content

---

## 11. Implementation Notes

### Key Code Patterns

#### Tool Definition Pattern

```csharp
[McpServerToolType]
public class ScreenshotPageTool
{
    private readonly IScreenshotService _screenshotService;
    private readonly IInputValidator _validator;

    public ScreenshotPageTool(IScreenshotService screenshotService, IInputValidator validator)
    {
        _screenshotService = screenshotService;
        _validator = validator;
    }

    [McpServerTool]
    [Description("Renders HTML content and returns a screenshot")]
    public async Task<McpToolResponse> ScreenshotPage(
        [Description("Raw HTML content to render")] string? html = null,
        [Description("Absolute path to HTML file")] string? filePath = null,
        [Description("URL to capture")] string? url = null,
        [Description("Viewport width in pixels")] int width = 1280,
        [Description("Viewport height in pixels")] int height = 720,
        [Description("Capture full scrollable page")] bool fullPage = false,
        [Description("Predefined device preset")] string? devicePreset = null,
        [Description("CSS selector to wait for")] string? waitForSelector = null,
        [Description("Additional wait time in milliseconds")] int waitMs = 0,
        [Description("Emulate dark color scheme")] bool darkMode = false)
    {
        // Implementation
    }
}
```

#### Browser Pool Pattern

```csharp
public class BrowserPoolManager : IBrowserPoolManager, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly IOptions<ScreenshotServerOptions> _options;

    public async Task<IPage> AcquirePageAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            await EnsureBrowserAsync();
            return await _browser!.NewPageAsync();
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
            await page.CloseAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

#### Screenshot Service Pattern

```csharp
public async Task<ScreenshotResult> CaptureAsync(ScreenshotRequest request, CancellationToken ct)
{
    var page = await _browserPool.AcquirePageAsync(ct);
    try
    {
        // Configure viewport
        await page.SetViewportSizeAsync(request.Viewport.Width, request.Viewport.Height);

        // Configure dark mode if requested
        if (request.DarkMode)
        {
            await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        }

        // Navigate to content
        var url = await ResolveContentUrl(request);
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait conditions
        if (!string.IsNullOrEmpty(request.WaitForSelector))
        {
            await page.WaitForSelectorAsync(request.WaitForSelector);
        }
        if (request.WaitMs > 0)
        {
            await Task.Delay(request.WaitMs, ct);
        }

        // Capture screenshot
        var bytes = await page.ScreenshotAsync(new() { FullPage = request.FullPage });
        return new ScreenshotResult(Convert.ToBase64String(bytes), "image/png");
    }
    finally
    {
        await _browserPool.ReleasePageAsync(page);
    }
}
```

---

## 12. References

### Official Documentation

- [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [ModelContextProtocol NuGet Package](https://www.nuget.org/packages/ModelContextProtocol/)
- [Build MCP Server in C# - .NET Blog](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)
- [Playwright .NET Screenshots](https://playwright.dev/dotnet/docs/screenshots)
- [Microsoft.Playwright NuGet Package](https://www.nuget.org/packages/Microsoft.Playwright/)

### Specification Document

- `/home/cpike/Workspace/screenshot-mcp/spec.md`

---

*This implementation plan was generated based on the specification at `/home/cpike/Workspace/screenshot-mcp/spec.md` and current best practices for MCP server development in C#/.NET.*
