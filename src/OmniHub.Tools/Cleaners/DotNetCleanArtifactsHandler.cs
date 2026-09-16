using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Tools.Cleaners;

public class DotNetCleanArtifactsHandler : IToolHandler
{
    public string HandlerId => "dotnet.clean.binobj";

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (!request.Parameters.TryGetValue("RootPath", out var rootPath) || string.IsNullOrWhiteSpace(rootPath))
        {
            progress.Report(new LogEntry("❌ Missing required parameter: RootPath", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "RootPath missing");
        }

        if (!Directory.Exists(rootPath))
        {
            progress.Report(new LogEntry($"❌ Directory does not exist: {rootPath}", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "Directory not found");
        }

        progress.Report(new LogEntry($"🔍 Recursively scanning for bin & obj folders in: {rootPath}", LogLevel.Info));

        await Task.Yield();

        var directoriesToDelete = new List<string>();
        try
        {
            FindBinObjDirectories(rootPath, directoriesToDelete, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            progress.Report(new LogEntry("🛑 Scan cancelled by user.", LogLevel.Warning));
            return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, "Cancelled");
        }
        catch (Exception ex)
        {
            progress.Report(new LogEntry($"⚠️ Error scanning directory: {ex.Message}", LogLevel.Warning));
        }

        if (directoriesToDelete.Count == 0)
        {
            progress.Report(new LogEntry("✨ No bin or obj folders found. Workspace is already clean!", LogLevel.Success));
            return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
        }

        progress.Report(new LogEntry($"🧹 Found {directoriesToDelete.Count} build directories to delete. Starting cleanup...", LogLevel.Standard));

        long totalFreedBytes = 0;
        int successfulDeletions = 0;

        foreach (var dir in directoriesToDelete)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var dirSize = CalculateDirectorySize(dir);
                Directory.Delete(dir, recursive: true);
                totalFreedBytes += dirSize;
                successfulDeletions++;
                progress.Report(new LogEntry($"🗑️ Deleted: {dir} ({dirSize / (1024.0 * 1024.0):F2} MB)", LogLevel.Standard));
            }
            catch (Exception ex)
            {
                progress.Report(new LogEntry($"⚠️ Failed to delete {dir}: {ex.Message}", LogLevel.Warning));
            }
        }

        stopwatch.Stop();
        var freedMb = totalFreedBytes / (1024.0 * 1024.0);
        progress.Report(new LogEntry(
            $"🎉 Cleanup completed! Deleted {successfulDeletions}/{directoriesToDelete.Count} folders. Freed {freedMb:F2} MB of disk space in {stopwatch.Elapsed.TotalSeconds:F2}s.",
            LogLevel.Success));

        return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
    }

    private static void FindBinObjDirectories(string currentDir, List<string> foundList, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var dirName = Path.GetFileName(currentDir);
        if (string.Equals(dirName, "bin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dirName, "obj", StringComparison.OrdinalIgnoreCase))
        {
            foundList.Add(currentDir);
            // Don't recurse into bin/obj once identified
            return;
        }

        try
        {
            foreach (var subDir in Directory.GetDirectories(currentDir))
            {
                ct.ThrowIfCancellationRequested();
                var subName = Path.GetFileName(subDir);
                if (subName.StartsWith('.') || string.Equals(subName, "node_modules", StringComparison.OrdinalIgnoreCase))
                    continue; // Skip hidden folders / node_modules

                FindBinObjDirectories(subDir, foundList, ct);
            }
        }
        catch
        {
            // Ignore permission issues on system folders
        }
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

