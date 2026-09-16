using System.Text.Json;
using System.Text.Json.Serialization;
using OmniHub.Core.Interfaces;

namespace OmniHub.Engine.Services;

public class UserStateEnvelope
{
    [JsonPropertyName("lastPaths")]
    public Dictionary<string, string> LastPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Lightweight service that persists user state (such as last selected file and directory paths)
/// to a local JSON file.
/// </summary>
public class UserStateService : IUserStateService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly Dictionary<string, string> _lastPaths = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string FilePath => _filePath;

    public UserStateService(string? customFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            _filePath = customFilePath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "OmniHub");
            _filePath = Path.Combine(dir, "user_state.json");
        }

        LoadInternal();
    }

    public string? GetLastPath(string toolId, string parameterId)
    {
        if (string.IsNullOrWhiteSpace(parameterId))
            return null;

        lock (_lock)
        {
            // 1. Check tool-specific parameter path
            if (!string.IsNullOrWhiteSpace(toolId) &&
                _lastPaths.TryGetValue($"{toolId}:{parameterId}", out var toolSpecific) &&
                !string.IsNullOrWhiteSpace(toolSpecific))
            {
                return toolSpecific;
            }

            // 2. Fall back to generic parameter ID path
            if (_lastPaths.TryGetValue(parameterId, out var generic) &&
                !string.IsNullOrWhiteSpace(generic))
            {
                return generic;
            }

            return null;
        }
    }

    public void SetLastPath(string toolId, string parameterId, string path)
    {
        if (string.IsNullOrWhiteSpace(parameterId) || string.IsNullOrWhiteSpace(path))
            return;

        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(toolId))
            {
                _lastPaths[$"{toolId}:{parameterId}"] = path.Trim();
            }
            _lastPaths[parameterId] = path.Trim();
        }
    }

    public Task LoadAsync()
    {
        LoadInternal();
        return Task.CompletedTask;
    }

    private void LoadInternal()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var envelope = JsonSerializer.Deserialize<UserStateEnvelope>(json, JsonOptions);
            if (envelope?.LastPaths != null)
            {
                lock (_lock)
                {
                    _lastPaths.Clear();
                    foreach (var (k, v) in envelope.LastPaths)
                    {
                        if (!string.IsNullOrWhiteSpace(k) && !string.IsNullOrWhiteSpace(v))
                        {
                            _lastPaths[k] = v;
                        }
                    }
                }
            }
        }
        catch
        {
            // Graceful fallback on corrupt file or IO issues
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            UserStateEnvelope envelope;
            lock (_lock)
            {
                envelope = new UserStateEnvelope
                {
                    LastPaths = new Dictionary<string, string>(_lastPaths, StringComparer.OrdinalIgnoreCase),
                    UpdatedAtUtc = DateTime.UtcNow
                };
            }

            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(envelope, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // Best effort persistence; must never crash the application
        }
    }
}

