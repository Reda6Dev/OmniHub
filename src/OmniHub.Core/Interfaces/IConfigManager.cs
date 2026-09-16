using OmniHub.Core.Models;

namespace OmniHub.Core.Interfaces;

public interface IConfigManager
{
    IReadOnlyList<ToolDefinition> Tools { get; }
    IReadOnlyList<WingDefinition> Wings { get; }
    IReadOnlyDictionary<string, string> GlobalSettings { get; }
    Task LoadConfigurationsAsync(string configsDirectory);
    event Action? OnConfigurationsReloaded;
    Task SaveSettingAsync(string key, string value);
}

