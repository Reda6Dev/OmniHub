using System.Diagnostics;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Engine.Process;

public class ProcessRunner : IProcessRunner
{
    private readonly object _processLock = new();
    private System.Diagnostics.Process? _currentProcess;

    public bool IsRunning
    {
        get
        {
            lock (_processLock)
            {
                return _currentProcess != null && !_currentProcess.HasExited;
            }
        }
    }

    public async Task<ProcessExecutionResult> RunAsync(
        string executable,
        string arguments,
        string? workingDir,
        IProgress<LogEntry>? progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var verification = DependencyVerifier.Verify(executable, workingDir);
        if (!verification.IsValid)
        {
            progress?.Report(new LogEntry(verification.ErrorMessageAr, LogLevel.Error));
            progress?.Report(new LogEntry(verification.ErrorMessageEn, LogLevel.Error));
            return new ProcessExecutionResult(-1, false, TimeSpan.Zero, verification.ErrorMessageEn);
        }

        var targetExecutable = verification.ResolvedPath ?? executable;

        progress?.Report(new LogEntry($"🚀 Starting: {targetExecutable} {arguments}", LogLevel.Info));
        if (!string.IsNullOrWhiteSpace(workingDir))
        {
            progress?.Report(new LogEntry($"📂 Working Directory: {workingDir}", LogLevel.Standard));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = targetExecutable,
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDir) ? Directory.GetCurrentDirectory() : workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new System.Diagnostics.Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        lock (_processLock)
        {
            _currentProcess = process;
        }

        using var registration = cancellationToken.Register(() =>
        {
            KillCurrent();
        });

        try
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    var level = DetermineLogLevel(e.Data, isErrorStream: false);
                    progress?.Report(new LogEntry(e.Data, level));
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    var level = DetermineLogLevel(e.Data, isErrorStream: true);
                    progress?.Report(new LogEntry(e.Data, level));
                }
            };

            var started = process.Start();
            if (!started)
            {
                return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, "Failed to start process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            stopwatch.Stop();
            var success = process.ExitCode == 0;
            var endLevel = success ? LogLevel.Success : LogLevel.Error;

            progress?.Report(new LogEntry(
                $"🏁 Finished with exit code: {process.ExitCode} (Took {stopwatch.Elapsed.TotalSeconds:F2}s)",
                endLevel));

            return new ProcessExecutionResult(process.ExitCode, success, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            progress?.Report(new LogEntry("🛑 Process was cancelled by user.", LogLevel.Warning));
            return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, "Operation cancelled.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            progress?.Report(new LogEntry($"💥 Execution error: {ex.Message}", LogLevel.Error));
            return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, ex.Message);
        }
        finally
        {
            lock (_processLock)
            {
                _currentProcess?.Dispose();
                _currentProcess = null;
            }
        }
    }

    public async Task<ProcessExecutionResult> RunPowerShellAsync(
        string script,
        string? workingDir,
        IProgress<LogEntry>? progress,
        CancellationToken cancellationToken)
    {
        // Execute script via Windows PowerShell CLI safely
        var escapedScript = script.Replace("\"", "\\\"");
        var arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{escapedScript}\"";
        return await RunAsync("powershell.exe", arguments, workingDir, progress, cancellationToken);
    }

    public void KillCurrent()
    {
        lock (_processLock)
        {
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                try
                {
                    // .NET 8 support to kill full process tree
                    _currentProcess.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore exceptions if already terminated
                }
            }
        }
    }

    private static LogLevel DetermineLogLevel(string message, bool isErrorStream)
    {
        if (isErrorStream)
            return LogLevel.Error;

        var lower = message.ToLowerInvariant();
        if (lower.Contains("error:") || lower.Contains("fatal:") || lower.Contains("exception:"))
            return LogLevel.Error;
        if (lower.Contains("warn:") || lower.Contains("warning:"))
            return LogLevel.Warning;
        if (lower.Contains("success") || lower.Contains("succeeded") || lower.Contains("complete!"))
            return LogLevel.Success;

        return LogLevel.Standard;
    }
}

