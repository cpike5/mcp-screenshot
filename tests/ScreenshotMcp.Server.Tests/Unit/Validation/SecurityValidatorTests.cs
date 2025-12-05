using FluentAssertions;
using Microsoft.Extensions.Options;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Validation;
using Xunit;

namespace ScreenshotMcp.Server.Tests.Unit.Validation;

public class SecurityValidatorTests : IDisposable
{
    private readonly SecurityValidator _validator;
    private readonly string _testFilePath;

    public SecurityValidatorTests()
    {
        var options = Options.Create(new ScreenshotServerOptions
        {
            Security = new SecurityOptions
            {
                AllowedBasePaths = [],
                BlockedUrlPatterns = ["malicious.com", "evil.net"],
                MaxViewportWidth = 4096,
                MaxViewportHeight = 4096,
                MaxWaitMs = 30000
            }
        });

        _validator = new SecurityValidator(options);

        // Create a temporary test file
        _testFilePath = Path.GetTempFileName();
        File.WriteAllText(_testFilePath, "<html><body>Test</body></html>");
    }

    [Fact]
    public void ValidateFilePath_WithExistingFile_ReturnsTrue()
    {
        var result = _validator.ValidateFilePath(_testFilePath, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateFilePath_WithNonExistentFile_ReturnsFalse()
    {
        var result = _validator.ValidateFilePath("/nonexistent/path/file.html", out var error);

        result.Should().BeFalse();
        error.Should().Contain("File not found");
    }

    [Fact]
    public void ValidateFilePath_WithEmptyPath_ReturnsFalse()
    {
        var result = _validator.ValidateFilePath("", out var error);

        result.Should().BeFalse();
        error.Should().Contain("empty");
    }

    [Fact]
    public void ValidateFilePath_WithWhitespace_ReturnsFalse()
    {
        var result = _validator.ValidateFilePath("   ", out var error);

        result.Should().BeFalse();
        error.Should().Contain("empty");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:3000")]
    [InlineData("https://www.google.com/search?q=test")]
    public void ValidateUrl_WithValidHttpUrl_ReturnsTrue(string url)
    {
        var result = _validator.ValidateUrl(url, out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    public void ValidateUrl_WithNonHttpScheme_ReturnsFalse(string url)
    {
        var result = _validator.ValidateUrl(url, out var error);

        result.Should().BeFalse();
        error.Should().Contain("scheme");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("://missing-scheme.com")]
    public void ValidateUrl_WithInvalidFormat_ReturnsFalse(string url)
    {
        var result = _validator.ValidateUrl(url, out var error);

        result.Should().BeFalse();
        error.Should().Contain("Invalid URL format");
    }

    [Theory]
    [InlineData("https://malicious.com/page")]
    [InlineData("https://evil.net")]
    [InlineData("https://www.evil.net/path")]
    public void ValidateUrl_WithBlockedPattern_ReturnsFalse(string url)
    {
        var result = _validator.ValidateUrl(url, out var error);

        result.Should().BeFalse();
        error.Should().Contain("blocked");
    }

    [Fact]
    public void ValidateUrl_WithEmptyUrl_ReturnsFalse()
    {
        var result = _validator.ValidateUrl("", out var error);

        result.Should().BeFalse();
        error.Should().Contain("empty");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
