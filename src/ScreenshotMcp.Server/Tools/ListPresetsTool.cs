using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using ScreenshotMcp.Server.Models;

namespace ScreenshotMcp.Server.Tools;

[McpServerToolType]
public class ListPresetsTool
{
    [McpServerTool(Name = "list_presets")]
    [Description("Returns a list of available device presets for viewport configuration")]
    public Task<object> ExecuteAsync()
    {
        var presets = DevicePresets.GetAll()
            .Select(p => new
            {
                name = p.Name,
                width = p.Width,
                height = p.Height,
                scale = p.Scale,
                userAgent = p.UserAgent
            })
            .ToList();

        var json = JsonSerializer.Serialize(new { presets }, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Task.FromResult<object>(new
        {
            type = "text",
            text = json
        });
    }
}
