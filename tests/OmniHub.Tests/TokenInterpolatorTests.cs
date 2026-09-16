using OmniHub.Engine.Templating;
using Xunit;

namespace OmniHub.Tests;

public class TokenInterpolatorTests
{
    [Fact]
    public void Interpolate_ShouldReplaceParameters_Successfully()
    {
        var interpolator = new TokenInterpolator();
        var template = "godot --path \"${ProjectPath}\" --export-release \"${Preset}\" \"${OutputFile}\"";
        var parameters = new Dictionary<string, string>
        {
            { "ProjectPath", "C:\\MyGame" },
            { "Preset", "Windows Desktop" },
            { "OutputFile", "C:\\MyGame\\bin\\game.exe" }
        };

        var result = interpolator.Interpolate(template, parameters);

        Assert.Equal("godot --path \"C:\\MyGame\" --export-release \"Windows Desktop\" \"C:\\MyGame\\bin\\game.exe\"", result);
    }

    [Fact]
    public void Interpolate_ShouldFallbackToGlobalVariables()
    {
        var interpolator = new TokenInterpolator();
        var template = "\"${GodotExe}\" --headless";
        var parameters = new Dictionary<string, string>();
        var globals = new Dictionary<string, string>
        {
            { "GodotExe", "D:\\Engines\\Godot_v4.exe" }
        };

        var result = interpolator.Interpolate(template, parameters, globals);

        Assert.Equal("\"D:\\Engines\\Godot_v4.exe\" --headless", result);
    }

    [Fact]
    public void Interpolate_ShouldResolveBuiltInDynamicTokens()
    {
        var interpolator = new TokenInterpolator();
        var template = "backup_${Date}_${Timestamp}";
        var parameters = new Dictionary<string, string>();

        var result = interpolator.Interpolate(template, parameters);

        Assert.DoesNotContain("${Date}", result);
        Assert.DoesNotContain("${Timestamp}", result);
        Assert.StartsWith("backup_", result);
    }
}

