using System.Runtime.InteropServices;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Tools.SysTools;

public class DllUnblockerHandler : IToolHandler
{
    public string HandlerId => "system.unblock.dlls";

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteFile(string lpFileName);

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (!request.Parameters.TryGetValue("TargetDirectory", out var targetDir) || string.IsNullOrWhiteSpace(targetDir))
        {
            progress.Report(new LogEntry("❌ Missing parameter: TargetDirectory", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "TargetDirectory missing");
        }

        if (!Directory.Exists(targetDir))
        {
            progress.Report(new LogEntry($"❌ Directory not found: {targetDir}", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "Directory not found");
        }

        progress.Report(new LogEntry($"🔍 Scanning for blocked DLLs & binaries in: {targetDir}", LogLevel.Info));

        await Task.Yield();

        var unblockedCount = 0;
        var totalScanned = 0;

        try
        {
            var files = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext is ".dll" or ".exe" or ".zip" or ".7z" or ".rar" or ".sys";
                });

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalScanned++;

                var zoneIdentifierStream = file + ":Zone.Identifier";
                if (DeleteFile(zoneIdentifierStream))
                {
                    unblockedCount++;
                    progress.Report(new LogEntry($"🔓 Unblocked: {Path.GetFileName(file)}", LogLevel.Success));
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
            progress.Report(new LogEntry($"⚠️ Error scanning directory: {ex.Message}", LogLevel.Warning));
        }

        stopwatch.Stop();
        if (unblockedCount > 0)
        {
            progress.Report(new LogEntry(
                $"🎉 Successfully unblocked {unblockedCount} files (Scanned {totalScanned} files in {stopwatch.Elapsed.TotalSeconds:F2}s)!",
                LogLevel.Success));
        }
        else
        {
            progress.Report(new LogEntry(
                $"✅ Scanned {totalScanned} files. None had Windows Zone.Identifier restrictions.",
                LogLevel.Standard));
        }

        return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
    }
}
