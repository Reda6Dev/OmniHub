using System.Text.RegularExpressions;

namespace OmniHub.Engine.Process;

public partial class DependencyVerificationResult
{
    public bool IsValid { get; }
    public string? ResolvedPath { get; }
    public string ErrorMessageAr { get; }
    public string ErrorMessageEn { get; }

    public string GetMessage(bool isArabic) => isArabic ? ErrorMessageAr : ErrorMessageEn;

    private DependencyVerificationResult(bool isValid, string? resolvedPath, string errorMessageAr, string errorMessageEn)
    {
        IsValid = isValid;
        ResolvedPath = resolvedPath;
        ErrorMessageAr = errorMessageAr;
        ErrorMessageEn = errorMessageEn;
    }

    public static DependencyVerificationResult Valid(string? resolvedPath = null) =>
        new(true, resolvedPath, string.Empty, string.Empty);

    public static DependencyVerificationResult Failure(string ar, string en) =>
        new(false, null, ar, en);
}

/// <summary>
/// Verifies whether external processes and dependencies exist and can be launched safely
/// before process execution is attempted.
/// </summary>
public static partial class DependencyVerifier
{
    [GeneratedRegex(@"\$\{([a-zA-Z0-9_:\.\-]+)\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    public static DependencyVerificationResult Verify(string executable, string? workingDir)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return DependencyVerificationResult.Failure(
                "❌ لم يتم تحديد البرنامج أو الأمر التنفيذي لهذه الأداة.",
                "❌ No executable command was specified for this tool.");
        }

        var trimmed = executable.Trim();

        // 1. Check for unresolved token like ${GodotExe} or ${FFmpegPath}
        var match = TokenRegex().Match(trimmed);
        if (match.Success)
        {
            var tokenKey = match.Groups[1].Value;
            return DependencyVerificationResult.Failure(
                $"❌ الأداة تعتمد على «{tokenKey}»، لكن المسار غير محدد في إعدادات النظام (Settings). يرجى تعيين المسار أولاً في الإعدادات لتتمكن من تشغيل الأداة.",
                $"❌ The tool relies on '{tokenKey}', but its path is not configured in Settings. Please set the path in Settings before running this tool.");
        }

        // 2. If it's a specific path (rooted or contains slashes)
        if (trimmed.Contains(Path.DirectorySeparatorChar) ||
            trimmed.Contains(Path.AltDirectorySeparatorChar) ||
            Path.IsPathRooted(trimmed))
        {
            if (File.Exists(trimmed))
            {
                return DependencyVerificationResult.Valid(Path.GetFullPath(trimmed));
            }

            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                var combined = Path.Combine(workingDir, trimmed);
                if (File.Exists(combined))
                {
                    return DependencyVerificationResult.Valid(Path.GetFullPath(combined));
                }
            }

            return DependencyVerificationResult.Failure(
                $"❌ تعذر العثور على البرنامج التنفيذي في المسار المحدد: «{trimmed}». يرجى التأكد من وجود الملف أو تحديث مساره في الإعدادات.",
                $"❌ Executable not found at the specified path: '{trimmed}'. Please make sure the file exists or update its path in Settings.");
        }

        // 3. Command name only (e.g. dotnet, git, ffmpeg, godot)
        if (TryResolveCommandInPath(trimmed, workingDir, out var resolved))
        {
            return DependencyVerificationResult.Valid(resolved);
        }

        return DependencyVerificationResult.Failure(
            $"❌ الأمر «{trimmed}» غير مثبت أو غير مسجل في متغيرات البيئة (PATH). يرجى تثبيت الأداة أو تعيين مسارها في الإعدادات.",
            $"❌ The command '{trimmed}' is not installed or not registered in the system PATH. Please install the tool or set its path in Settings.");
    }

    private static bool TryResolveCommandInPath(string command, string? workingDir, out string? resolvedPath)
    {
        resolvedPath = null;
        var ext = Path.GetExtension(command);

        var extensions = new List<string>();
        if (!string.IsNullOrEmpty(ext))
        {
            extensions.Add(string.Empty);
        }
        else
        {
            var pathext = Environment.GetEnvironmentVariable("PATHEXT");
            if (!string.IsNullOrWhiteSpace(pathext))
            {
                extensions.AddRange(pathext.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                extensions.AddRange(new[] { ".exe", ".cmd", ".bat", ".com" });
            }
        }

        // Check working directory first
        if (!string.IsNullOrWhiteSpace(workingDir) && Directory.Exists(workingDir))
        {
            foreach (var e in extensions)
            {
                var test = Path.Combine(workingDir, command + e);
                if (File.Exists(test))
                {
                    resolvedPath = Path.GetFullPath(test);
                    return true;
                }
            }
        }

        // Check PATH environment variable
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathVar))
        {
            var dirs = pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawDir in dirs)
            {
                var dir = rawDir.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                    continue;

                foreach (var e in extensions)
                {
                    var test = Path.Combine(dir, command + e);
                    if (File.Exists(test))
                    {
                        resolvedPath = Path.GetFullPath(test);
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

