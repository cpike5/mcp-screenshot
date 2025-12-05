using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using ScreenshotMcp.Server.Configuration;
using ScreenshotMcp.Server.Validation;
using Xunit;

namespace ScreenshotMcp.Server.Tests.Unit.Validation;

public class InputValidatorTests
{
    private readonly InputValidator _validator;
    private readonly Mock<ISecurityValidator> _securityValidatorMock;

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

        _securityValidatorMock = new Mock<ISecurityValidator>();
        _securityValidatorMock.Setup(v => v.ValidateFilePath(It.IsAny<string>(), out It.Ref<string?>.IsAny))
            .Returns(true);
        _securityValidatorMock.Setup(v => v.ValidateUrl(It.IsAny<string>(), out It.Ref<string?>.IsAny))
            .Returns(true);

        _validator = new InputValidator(options, _securityValidatorMock.Object);
    }

    [Fact]
    public void ValidateScreenshotInput_WithHtml_ReturnsTrue()
    {
        var result = _validator.ValidateScreenshotInput(
            html: "<html></html>",
            filePath: null,
            url: null,
            width: 1280,
            height: 720,
            devicePreset: null,
            waitMs: 0,
            out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateScreenshotInput_WithFilePath_ReturnsTrue()
    {
        var result = _validator.ValidateScreenshotInput(
            html: null,
            filePath: "/path/to/file.html",
            url: null,
            width: 1280,
            height: 720,
            devicePreset: null,
            waitMs: 0,
            out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateScreenshotInput_WithUrl_ReturnsTrue()
    {
        var result = _validator.ValidateScreenshotInput(
            html: null,
            filePath: null,
            url: "https://example.com",
            width: 1280,
            height: 720,
            devicePreset: null,
            waitMs: 0,
            out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateScreenshotInput_WithNoSource_ReturnsFalse()
    {
        var result = _validator.ValidateScreenshotInput(
            html: null,
            filePath: null,
            url: null,
            width: 1280,
            height: 720,
            devicePreset: null,
            waitMs: 0,
            out var error);

        result.Should().BeFalse();
        error.Should().Contain("required");
    }

    [Fact]
    public void ValidateScreenshotInput_WithMultipleSources_ReturnsFalse()
    {
        var result = _validator.ValidateScreenshotInput(
            html: "<html></html>",
            filePath: null,
            url: "https://example.com",
            width: 1280,
            height: 720,
            devicePreset: null,
            waitMs: 0,
            out var error);

        result.Should().BeFalse();
        error.Should().Contain("Only one");
    }

    [Theory]
    [InlineData("desktop")]
    [InlineData("mobile")]
    [InlineData("tablet")]
    public void ValidateScreenshotInput_WithValidPreset_ReturnsTrue(string preset)
    {
        var result = _validator.ValidateScreenshotInput(
            html: "<html></html>",
            filePath: null,
            url: null,
            width: 1280,
            height: 720,
            devicePreset: preset,
            waitMs: 0,
            out var error);

        result.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateScreenshotInput_WithInvalidPreset_ReturnsFalse()
    {
        var result = _validator.ValidateScreenshotInput(
            html: "<html></html>",
            filePath: null,
            url: null,
            width: 1280,
            height: 720,
            devicePreset: "unknown-preset",
            waitMs: 0,
            out var error);

        result.Should().BeFalse();
        error.Should().Contain("Invalid device preset");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5000)]
    public void ValidateScreenshotInput_WithInvalidWidth_ReturnsFalse(int width)
    {
        var result = _validator.ValidateScreenshotInput(
            html: "<html></html>",
            filePath: null,
            url: null,
            width: width,
            height: 720,
            devicePreset: null,
            waitMs: 0,
            out var error);

        result.Should().BeFalse();
        error.Should().Contain("Width");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5000)]
    public void ValidateScreenshotInput_WithInvalidHeight_ReturnsFalse(int height)
    {
        var result = _validator.ValidateScreenshotInput(
            html: "<html></html>",
            filePath: null,
            url: null,
            width: 1280,
            height: height,
            devicePreset: null,
            waitMs: 0,
            out var error);

        result.Should().BeFalse();
        error.Should().Contain("Height");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31000)]
    public void ValidateScreenshotInput_WithInvalidWaitMs_ReturnsFalse(int waitMs)
    {
        var result = _validator.ValidateScreenshotInput(
            html: "<html></html>",
            filePath: null,
            url: null,
            width: 1280,
            height: 720,
            devicePreset: null,
            waitMs: waitMs,
            out var error);

        result.Should().BeFalse();
        error.Should().Contain("WaitMs");
    }
}
