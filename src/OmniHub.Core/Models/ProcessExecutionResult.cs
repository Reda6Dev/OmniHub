namespace OmniHub.Core.Models;

public record ProcessExecutionResult(
    int ExitCode,
    bool Success,
    TimeSpan Duration,
    string? ErrorMessage = null);

