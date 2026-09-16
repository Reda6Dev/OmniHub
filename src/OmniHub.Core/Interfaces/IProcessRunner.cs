using OmniHub.Core.Models;

namespace OmniHub.Core.Interfaces;

public interface IProcessRunner
{
    bool IsRunning { get; }
    Task<ProcessExecutionResult> RunAsync(
        string executable,
        string arguments,
        string? workingDir,
        IProgress<LogEntry>? progress,
        CancellationToken cancellationToken);

    Task<ProcessExecutionResult> RunPowerShellAsync(
        string script,
        string? workingDir,
        IProgress<LogEntry>? progress,
        CancellationToken cancellationToken);

    void KillCurrent();
}

