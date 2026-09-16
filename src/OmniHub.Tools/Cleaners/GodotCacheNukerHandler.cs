using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Tools.Cleaners;

public class GodotCacheNukerHandler : IToolHandler
{
    public string HandlerId => "godot.cache.nuker";

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (!request.Parameters.TryGetValue("ProjectPath", out var projectPath) || string.IsNullOrWhiteSpace(projectPath))
        {
            progress.Report(new LogEntry("❌ Missing required parameter: ProjectPath", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "ProjectPath missing");
        }

        if (!Directory.Exists(projectPath))
        {
            progress.Report(new LogEntry($"❌ Project directory does not exist: {projectPath}", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "Directory not found");
        }

        progress.Report(new LogEntry($"🔍 Scanning for Godot cache folders in: {projectPath}", LogLevel.Info));

        await Task.Yield();

        var godotCacheFolder = Path.Combine(projectPath, ".godot");
        long bytesFreed = 0;
        int itemsDeleted = 0;

        if (Directory.Exists(godotCacheFolder))
        {
            progress.Report(new LogEntry($"🗑️ Found .godot cache folder: {godotCacheFolder}", LogLevel.Warning));
            try
            {
                bytesFreed += CalculateDirectorySize(godotCacheFolder);
                Directory.Delete(godotCacheFolder, recursive: true);
                itemsDeleted++;
                progress.Report(new LogEntry("✅ Successfully deleted .godot directory.", LogLevel.Success));
            }
            catch (Exception ex)
            {
                progress.Report(new LogEntry($"⚠️ Warning deleting .godot folder: {ex.Message}", LogLevel.Warning));
            }
        }
        else
        {
            progress.Report(new LogEntry("ℹ️ No .godot folder found in project root.", LogLevel.Standard));
        }

        // Also check for stale lock files (.godot.lock, .import locks)
        try
        {
            var lockFiles = Directory.GetFiles(projectPath, "*.lock", SearchOption.TopDirectoryOnly);
            foreach (var lockFile in lockFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(lockFile);
                    bytesFreed += fi.Length;
                    File.Delete(lockFile);
                    itemsDeleted++;
                    progress.Report(new LogEntry($"🔓 Removed stale lock file: {Path.GetFileName(lockFile)}", LogLevel.Standard));
                }
                catch { }
            }
        }
        catch { }

        stopwatch.Stop();
        var freedMb = bytesFreed / (1024.0 * 1024.0);
        progress.Report(new LogEntry(
            $"🎉 Godot cache nuke complete! Items cleaned: {itemsDeleted}, Freed: {freedMb:F2} MB (Duration: {stopwatch.Elapsed.TotalSeconds:F2}s)",
            LogLevel.Success));

        return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
    }

    private static long CalculateDirectorySize(string directory)
    {
        try
        {
            return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Sum(f =>
                {
                    try { return new FileInfo(f).Length; }
                    catch { return 0L; }
                });
        }
        catch
        {
            return 0L;
        }
    }
}

