using System.Xml.Linq;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.Tools.Localization;

public class XmlLocalizationCheckerHandler : IToolHandler
{
    public string HandlerId => "xml.localization.checker";

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ToolExecutionRequest request,
        IProgress<LogEntry> progress,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (!request.Parameters.TryGetValue("BaseFile", out var baseFile) || string.IsNullOrWhiteSpace(baseFile))
        {
            progress.Report(new LogEntry("❌ Missing parameter: BaseFile", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "BaseFile missing");
        }

        if (!request.Parameters.TryGetValue("TargetFile", out var targetFile) || string.IsNullOrWhiteSpace(targetFile))
        {
            progress.Report(new LogEntry("❌ Missing parameter: TargetFile", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "TargetFile missing");
        }

        if (!File.Exists(baseFile))
        {
            progress.Report(new LogEntry($"❌ Base file does not exist: {baseFile}", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "Base file not found");
        }

        if (!File.Exists(targetFile))
        {
            progress.Report(new LogEntry($"❌ Target file does not exist: {targetFile}", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, "Target file not found");
        }

        progress.Report(new LogEntry($"📖 Loading Base XML: {Path.GetFileName(baseFile)}", LogLevel.Info));
        progress.Report(new LogEntry($"📖 Loading Target XML: {Path.GetFileName(targetFile)}", LogLevel.Info));

        await Task.Yield();

        try
        {
            var baseDoc = XDocument.Load(baseFile);
            var targetDoc = XDocument.Load(targetFile);

            var baseEntries = ExtractTranslationEntries(baseDoc);
            var targetEntries = ExtractTranslationEntries(targetDoc);

            progress.Report(new LogEntry($"📊 Base entries found: {baseEntries.Count}", LogLevel.Standard));
            progress.Report(new LogEntry($"📊 Target entries found: {targetEntries.Count}", LogLevel.Standard));

            var missingKeys = new List<string>();
            var untranslatedIdentical = new List<string>();
            var emptyValues = new List<string>();

            foreach (var (key, baseValue) in baseEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!targetEntries.TryGetValue(key, out var targetValue))
                {
                    missingKeys.Add(key);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(targetValue))
                    {
                        emptyValues.Add(key);
                    }
                    else if (string.Equals(baseValue.Trim(), targetValue.Trim(), StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrWhiteSpace(baseValue) && baseValue.Length > 2)
                    {
                        untranslatedIdentical.Add(key);
                    }
                }
            }

            // Report findings
            if (missingKeys.Count > 0)
            {
                progress.Report(new LogEntry($"⚠️ Missing Keys in Target ({missingKeys.Count}):", LogLevel.Warning));
                foreach (var k in missingKeys.Take(25))
                {
                    progress.Report(new LogEntry($"   - {k}", LogLevel.Warning));
                }
                if (missingKeys.Count > 25)
                {
                    progress.Report(new LogEntry($"   ... and {missingKeys.Count - 25} more missing keys.", LogLevel.Warning));
                }
            }

            if (emptyValues.Count > 0)
            {
                progress.Report(new LogEntry($"⚠️ Empty Values in Target ({emptyValues.Count}):", LogLevel.Warning));
                foreach (var k in emptyValues.Take(10))
                {
                    progress.Report(new LogEntry($"   - {k}", LogLevel.Warning));
                }
            }

            if (untranslatedIdentical.Count > 0)
            {
                progress.Report(new LogEntry($"ℹ️ Potentially Untranslated (Identical to Base) ({untranslatedIdentical.Count}):", LogLevel.Standard));
                foreach (var k in untranslatedIdentical.Take(10))
                {
                    progress.Report(new LogEntry($"   - {k}: \"{baseEntries[k]}\"", LogLevel.Standard));
                }
            }

            stopwatch.Stop();

            if (missingKeys.Count == 0 && emptyValues.Count == 0)
            {
                progress.Report(new LogEntry(
                    $"🎉 All {baseEntries.Count} keys are completely present and populated! (Checked in {stopwatch.Elapsed.TotalSeconds:F2}s)",
                    LogLevel.Success));
                return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
            }
            else
            {
                progress.Report(new LogEntry(
                    $"⚠️ Localization audit finished: {missingKeys.Count} missing keys, {emptyValues.Count} empty values. (Duration: {stopwatch.Elapsed.TotalSeconds:F2}s)",
                    LogLevel.Warning));
                return new ProcessExecutionResult(0, true, stopwatch.Elapsed);
            }
        }
        catch (OperationCanceledException)
        {
            progress.Report(new LogEntry("🛑 Audit cancelled.", LogLevel.Warning));
            return new ProcessExecutionResult(-1, false, stopwatch.Elapsed, "Cancelled");
        }
        catch (Exception ex)
        {
            progress.Report(new LogEntry($"💥 Failed parsing XML: {ex.Message}", LogLevel.Error));
            return new ProcessExecutionResult(1, false, stopwatch.Elapsed, ex.Message);
        }
    }

    private static Dictionary<string, string> ExtractTranslationEntries(XDocument doc)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Pattern 1: ResX style <data name="Key"><value>Text</value></data>
        var resxData = doc.Descendants("data").Where(d => d.Attribute("name") != null);
        foreach (var el in resxData)
        {
            var key = el.Attribute("name")!.Value;
            var val = el.Element("value")?.Value ?? string.Empty;
            dict[key] = val;
        }

        if (dict.Count > 0)
            return dict;

        // Pattern 2: Android/Generic <string name="Key">Text</string>
        var stringElements = doc.Descendants("string").Where(s => s.Attribute("name") != null);
        foreach (var el in stringElements)
        {
            var key = el.Attribute("name")!.Value;
            dict[key] = el.Value;
        }

        if (dict.Count > 0)
            return dict;

        // Pattern 3: Key-Value child elements <Root><SomeKey>Value</SomeKey></Root>
        if (doc.Root != null)
        {
            foreach (var el in doc.Root.Elements())
            {
                if (!el.HasElements)
                {
                    dict[el.Name.LocalName] = el.Value;
                }
            }
        }

        return dict;
    }
}

