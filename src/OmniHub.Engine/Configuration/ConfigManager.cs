using System.Text.Json;
using System.Text.Json.Serialization;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Engine.Configuration;

public class ConfigManager : IConfigManager, IDisposable
{
    private readonly object _lock = new();
    private readonly List<ToolDefinition> _tools = new();
    private readonly List<WingDefinition> _wings = new();
    private readonly Dictionary<string, string> _globalSettings = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;
    private string _configsDirectory = string.Empty;

    public IReadOnlyList<ToolDefinition> Tools
    {
        get
        {
            lock (_lock)
            {
                return _tools.ToList();
            }
        }
    }

    public IReadOnlyList<WingDefinition> Wings
    {
        get { lock (_lock) return _wings.ToList(); }
    }

    public IReadOnlyDictionary<string, string> GlobalSettings
    {
        get
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_globalSettings);
            }
        }
    }

    public event Action? OnConfigurationsReloaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task LoadConfigurationsAsync(string configsDirectory)
    {
        _configsDirectory = configsDirectory;

        if (!Directory.Exists(configsDirectory))
        {
            Directory.CreateDirectory(configsDirectory);
        }

        await ReloadInternalAsync();
        SetupWatcher();
    }

    public async Task SaveSettingAsync(string key, string value)
    {
        lock (_lock)
        {
            _globalSettings[key] = value;
        }

        if (string.IsNullOrEmpty(_configsDirectory))
            return;

        var settingsPath = Path.Combine(_configsDirectory, "appsettings.json");
        try
        {
            var json = JsonSerializer.Serialize(_globalSettings, JsonOptions);
            await File.WriteAllTextAsync(settingsPath, json);
        }
        catch
        {
            // Ignore temporary write conflicts
        }
    }

    private async Task ReloadInternalAsync()
    {
        var loadedTools = new List<ToolDefinition>();
        var loadedSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var loadedWings = new List<WingDefinition>();

        // 1. Load appsettings.json
        var settingsPath = Path.Combine(_configsDirectory, "appsettings.json");
        if (File.Exists(settingsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(settingsPath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
                if (dict != null)
                {
                    foreach (var (k, v) in dict)
                    {
                        loadedSettings[k] = v;
                    }
                }
            }
            catch
            {
                // Fallback on corrupt settings file
            }
        }

        // 2. Load user-created wing definitions.
        var wingsDir = Path.Combine(_configsDirectory, "wings");
        if (Directory.Exists(wingsDir))
        {
            foreach (var file in Directory.GetFiles(wingsDir, "*.wing.json", SearchOption.AllDirectories))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var wing = JsonSerializer.Deserialize<WingDefinition>(json, JsonOptions);
                    if (wing != null && !string.IsNullOrWhiteSpace(wing.Id))
                    {
                        wing.IsBuiltIn = false;
                        loadedWings.Add(wing);
                    }
                }
                catch { }
            }
        }

        // 3. Load root tools.json if present
        var rootToolsPath = Path.Combine(_configsDirectory, "tools.json");
        if (File.Exists(rootToolsPath))
        {
            await LoadToolsFromFile(rootToolsPath, loadedTools);
        }

        // 4. Load all files in tools/ directory
        var toolsDir = Path.Combine(_configsDirectory, "tools");
        if (Directory.Exists(toolsDir))
        {
            var files = Directory.GetFiles(toolsDir, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                await LoadToolsFromFile(file, loadedTools);
            }
        }

        lock (_lock)
        {
            _tools.Clear();
            _tools.AddRange(loadedTools);
            _wings.Clear();
            _wings.AddRange(loadedWings);

            _globalSettings.Clear();
            foreach (var (k, v) in loadedSettings)
            {
                _globalSettings[k] = v;
            }
        }

        OnConfigurationsReloaded?.Invoke();
    }

    private static async Task LoadToolsFromFile(string filePath, List<ToolDefinition> outputList)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            
            // Try deserializing as List<ToolDefinition> first
            try
            {
                var list = JsonSerializer.Deserialize<List<ToolDefinition>>(json, JsonOptions);
                if (list != null)
                {
                    outputList.AddRange(list);
                    return;
                }
            }
            catch
            {
                // Try deserializing as single ToolDefinition
                var single = JsonSerializer.Deserialize<ToolDefinition>(json, JsonOptions);
                if (single != null)
                {
                    outputList.Add(single);
                }
            }
        }
        catch
        {
            // Skip invalid json files safely
        }
    }

    private void SetupWatcher()
    {
        if (_watcher != null || string.IsNullOrEmpty(_configsDirectory) || !Directory.Exists(_configsDirectory))
            return;

        try
        {
            _watcher = new FileSystemWatcher(_configsDirectory)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                Filter = "*.json",
                EnableRaisingEvents = true
            };

            var debounceTimer = new System.Timers.Timer(400) { AutoReset = false };
            debounceTimer.Elapsed += async (_, _) =>
            {
                await ReloadInternalAsync();
            };

            _watcher.Changed += (_, _) => debounceTimer.Start();
            _watcher.Created += (_, _) => debounceTimer.Start();
            _watcher.Deleted += (_, _) => debounceTimer.Start();
            _watcher.Renamed += (_, _) => debounceTimer.Start();
        }
        catch
        {
            // FileSystemWatcher might fail on some restricted filesystems
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}

