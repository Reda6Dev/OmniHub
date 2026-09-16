using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Tools.SysTools;

public class CrashLogCleanerHandler : IToolHandler
{
    public string HandlerId => "system.clean.logs";

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var targetFolder = request.Parameters.TryGetValue("TargetFolder", out var tf) && !string.IsNullOrWhiteSpace(tf)
            ? tf
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps");

        progress.Report(new LogEntry($"🔍 Inspecting crash and log directory: {targetFolder}", LogLevel.Info));

        await Task.Yield();

        if (!Directory.Exists(targetFolder))
        {
            progress.Report(new LogEntry("✨ Directory does not exist or has no crash dumps. Nothing to clean!", LogLevel.Success));
            return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
        }

        long bytesFreed = 0;
        int filesDeleted = 0;

        try
        {
            var files = Directory.GetFiles(targetFolder, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext is ".dmp" or ".mdmp" or ".log" or ".tmp" or ".bak";
                });

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fi = new FileInfo(file);
                    bytesFreed += fi.Length;
                    File.Delete(file);
                    filesDeleted++;
                    progress.Report(new LogEntry($"🗑️ Deleted: {Path.GetFileName(file)} ({fi.Length / 1024.0:F1} KB)", LogLevel.Standard));
                }
                catch
                {
                    // Ignore locked files
                }
            }
        }
        catch (OperationCanceledException)
        {
            progress.Report(new LogEntry("🛑 Operation cancelled by user.", LogLevel.Warning));
            return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, "Cancelled");
        }
        catch (Exception ex)
        {
            progress.Report(new LogEntry($"⚠️ Error reading directory: {ex.Message}", LogLevel.Warning));
        }

        stopwatch.Stop();
        var freedMb = bytesFreed / (1024.0 * 1024.0);
        progress.Report(new LogEntry(
            $"🎉 Crash cleanup finished! Removed {filesDeleted} files. Freed {freedMb:F2} MB in {stopwatch.Elapsed.TotalSeconds:F2}s.",
            LogLevel.Success));

        return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
    }
}
