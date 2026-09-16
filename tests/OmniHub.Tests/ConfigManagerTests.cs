using OmniHub.Engine.Configuration;
using Xunit;

namespace OmniHub.Tests;

public class ConfigManagerTests
{
    [Fact]
    public async Task LoadConfigurationsAsync_ShouldLoadAllWingTools()
    {
        using var configManager = new ConfigManager();
        var configsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "configs");
        var fullConfigsPath = Path.GetFullPath(configsDir);

        await configManager.LoadConfigurationsAsync(fullConfigsPath);

        Assert.NotEmpty(configManager.Tools);
        Assert.Contains(configManager.Tools, t => t.Category == "Godot");
        Assert.Contains(configManager.Tools, t => t.Category == "DotNet");
        Assert.Contains(configManager.Tools, t => t.Category == "Media");
        Assert.Contains(configManager.Tools, t => t.Category == "System");

        Assert.NotEmpty(configManager.GlobalSettings);
        Assert.True(configManager.GlobalSettings.ContainsKey("GodotExe"));
    }
}

