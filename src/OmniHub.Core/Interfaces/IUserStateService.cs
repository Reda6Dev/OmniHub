namespace OmniHub.Core.Interfaces;

/// <summary>
/// Service responsible for managing and persisting user session state, such as last used file and directory paths.
/// </summary>
public interface IUserStateService
{
    /// <summary>
    /// Gets the last path used for a specific tool and parameter.
    /// Falls back to the general parameter ID if no tool-specific path exists.
    /// </summary>
    string? GetLastPath(string toolId, string parameterId);

    /// <summary>
    /// Records the last path used for a specific tool and parameter.
    /// </summary>
    void SetLastPath(string toolId, string parameterId, string path);

    /// <summary>
    /// Persists the current state asynchronously to local storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Loads state from local storage.
    /// </summary>
    Task LoadAsync();
}

