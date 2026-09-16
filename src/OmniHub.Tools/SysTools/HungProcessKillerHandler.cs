using System.Diagnostics;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Tools.SysTools;

public class HungProcessKillerHandler : IToolHandler
{
    public string HandlerId => "system.hung.killer";

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        request.Parameters.TryGetValue("ProcessNameFilter", out var nameFilter);
        nameFilter = nameFilter?.Trim() ?? string.Empty;

        progress.Report(new LogEntry(
            string.IsNullOrEmpty(nameFilter)
                ? "🔍 Scanning system for all unresponsive/hung processes..."
                : $"🔍 Scanning system for hung processes matching: '{nameFilter}'...",
            LogLevel.Info));

        await Task.Yield();

        var killedCount = 0;
        var processes = System.Diagnostics.Process.GetProcesses();

        foreach (var process in processes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!string.IsNullOrEmpty(nameFilter) &&
                    !process.ProcessName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check if process has a UI window and is not responding
                if (process.MainWindowHandle != IntPtr.Zero && !process.Responding)
                {
                    progress.Report(new LogEntry(
                        $"⚠️ Found hung process: {process.ProcessName} (PID: {process.Id}) - Unresponsive window!",
                        LogLevel.Warning));

                    try
                    {
                        process.Kill(entireProcessTree: true);
                        killedCount++;
                        progress.Report(new LogEntry(
                            $"💀 Terminated hung process: {process.ProcessName} (PID: {process.Id})",
                            LogLevel.Success));
                    }
                    catch (Exception ex)
                    {
                        progress.Report(new LogEntry(
                            $"⚠️ Failed to kill PID {process.Id}: {ex.Message}",
                            LogLevel.Warning));
                    }
                }
            }
            catch
            {
                // Access denied on system/protected processes is expected
            }
            finally
            {
                process.Dispose();
            }
        }

        stopwatch.Stop();
        if (killedCount > 0)
        {
            progress.Report(new LogEntry(
                $"🎉 Terminated {killedCount} hung processes in {stopwatch.Elapsed.TotalSeconds:F2}s.",
                LogLevel.Success));
        }
        else
        {
            progress.Report(new LogEntry(
                $"✨ System check complete. No unresponsive/hung processes detected! (Checked {processes.Length} processes in {stopwatch.Elapsed.TotalSeconds:F2}s)",
                LogLevel.Success));
        }

        return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
    }
}
