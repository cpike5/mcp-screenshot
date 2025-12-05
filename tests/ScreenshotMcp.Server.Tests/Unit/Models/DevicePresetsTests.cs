using FluentAssertions;
using ScreenshotMcp.Server.Models;
using Xunit;

namespace ScreenshotMcp.Server.Tests.Unit.Models;

public class DevicePresetsTests
{
    [Fact]
    public void GetAll_ReturnsAllSixPresets()
    {
        var presets = DevicePresets.GetAll().ToList();

        presets.Should().HaveCount(6);
    }

    [Theory]
    [InlineData("desktop", 1280, 720, 1)]
    [InlineData("desktop-hd", 1920, 1080, 1)]
    [InlineData("tablet", 768, 1024, 2)]
    [InlineData("tablet-landscape", 1024, 768, 2)]
    [InlineData("mobile", 375, 667, 2)]
    [InlineData("mobile-large", 414, 896, 3)]
    public void GetPreset_ReturnsCorrectDimensions(string name, int expectedWidth, int expectedHeight, float expectedScale)
    {
        var preset = DevicePresets.GetPreset(name);

        preset.Should().NotBeNull();
        preset!.Width.Should().Be(expectedWidth);
        preset.Height.Should().Be(expectedHeight);
        preset.Scale.Should().Be(expectedScale);
    }

    [Theory]
    [InlineData("Desktop")]
    [InlineData("DESKTOP")]
    [InlineData("dEsKtOp")]
    public void GetPreset_IsCaseInsensitive(string name)
    {
        var preset = DevicePresets.GetPreset(name);

        preset.Should().NotBeNull();
        preset!.Name.Should().Be("desktop");
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("iphone")]
    [InlineData("")]
    public void GetPreset_ReturnsNullForUnknownPreset(string name)
    {
        var preset = DevicePresets.GetPreset(name);

        preset.Should().BeNull();
    }

    [Fact]
    public void AllPresets_HaveNonEmptyUserAgents()
    {
        var presets = DevicePresets.GetAll();

        foreach (var preset in presets)
        {
            preset.UserAgent.Should().NotBeNullOrEmpty($"Preset '{preset.Name}' should have a user agent");
        }
    }
}
