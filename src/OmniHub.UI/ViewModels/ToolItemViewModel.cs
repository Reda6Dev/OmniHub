using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;
using OmniHub.Engine.Process;

namespace OmniHub.UI.ViewModels;

public partial class ToolItemViewModel : ObservableObject
{
    private readonly IProcessRunner _processRunner;
    private readonly ITokenInterpolator _tokenInterpolator;
    private readonly IToolRegistry _toolRegistry;
    private readonly IConfigManager _configManager;
    private readonly IUserStateService? _userStateService;
    private readonly TerminalViewModel _terminal;
    private readonly Action _openTerminalAction;
    private readonly Action<ToolDefinition> _editAction;
    private readonly Action<ToolDefinition> _deleteAction;
    private readonly Action<ToolDefinition, string> _moveAction;
    private readonly Func<IEnumerable<WingItemViewModel>> _wingsProvider;

    public ToolDefinition Tool { get; }

    public string Id => Tool.Id;
    public string Title => (IsArabic && !string.IsNullOrWhiteSpace(Tool.TitleAr))
        ? Tool.TitleAr
        : (IsArabic ? LocalizeTitle(Tool.Title, Tool.Id) : Tool.Title);

    public string Description => (IsArabic && !string.IsNullOrWhiteSpace(Tool.DescriptionAr))
        ? Tool.DescriptionAr
        : (IsArabic ? LocalizeDescription(Tool.Description, Tool.Id) : Tool.Description);

    public string Category => Tool.Category;
    public string IconKey => Tool.IconKey;
    public bool HasParameters => Parameters.Count > 0;

    /// <summary>
    /// Vector Glyph icon for Segoe Fluent Icons / Segoe MDL2 Assets based on category and tool type.
    /// </summary>
    public string GlyphIcon
    {
        get
        {
            var cat = (Tool.Category ?? "").Trim().ToLowerInvariant();
            var id = (Tool.Id ?? "").Trim().ToLowerInvariant();
            var iconKey = (Tool.IconKey ?? "").Trim().ToLowerInvariant();

            // Games & Godot: Game Controller ()
            if (cat.Contains("godot") || cat.Contains("game") || id.Contains("godot") || iconKey.Contains("game"))
                return "\uE7FC";

            // .NET & Programming: Code Glyph ()
            if (cat.Contains("dotnet") || cat.Contains("code") || cat.Contains("git") || id.Contains("dotnet") || id.Contains("git") || id.Contains("xml") || iconKey.Contains("code"))
                return "\uE943";

            // Media & Assets: Video/Audio ()
            if (cat.Contains("media") || cat.Contains("asset") || cat.Contains("audio") || cat.Contains("video") || id.Contains("media") || id.Contains("ico") || id.Contains("ogg") || iconKey.Contains("media"))
                return "\uE714";

            // System, Maintenance, & Processes: Settings/Repair ()
            if (cat.Contains("system") || cat.Contains("util") || cat.Contains("process") || id.Contains("system") || id.Contains("dll") || id.Contains("hung") || id.Contains("log") || iconKey.Contains("repair") || iconKey.Contains("settings") || iconKey.Contains("shield") || iconKey.Contains("zap"))
                return "\uE713";

            // Custom Tools / Default: Repair/Tools ()
            return "\uE90F";
        }
    }

    public ToolGuideInfo? Guide => Tool.Guide;
    public bool HasGuide => Tool.Guide != null;
    public bool IsUserTool => Tool.Id.StartsWith("custom.", StringComparison.OrdinalIgnoreCase);

    private static readonly HashSet<string> BuiltInToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "godot.headless.export",
        "godot.cache.nuker",
        "godot.gdscript.check",
        "godot.script.checker",
        "dotnet.singlefile.publish",
        "dotnet.clean.binobj",
        "dotnet.clean.bin_obj",
        "git.quick.status",
        "git.quick.commit",
        "dotnet.git.quick_push",
        "xml.localization.checker",
        "dotnet.loc.checker",
        "media.ico.generator",
        "media.audio.to_ogg",
        "media.video.compress",
        "system.unblock.dlls",
        "system.clean.logs",
        "system.hung.killer"
    };

    public static bool IsBuiltInTool(string id) => !string.IsNullOrWhiteSpace(id) && BuiltInToolIds.Contains(id);

    public bool IsCustom => Tool.IsCustom ||
                            Tool.Id.StartsWith("custom.", StringComparison.OrdinalIgnoreCase) ||
                            Tool.Id.StartsWith("online.", StringComparison.OrdinalIgnoreCase) ||
                            !string.IsNullOrWhiteSpace(Tool.SourceRepository) ||
                            !IsBuiltInTool(Tool.Id);

    public IEnumerable<WingItemViewModel> AvailableWings => _wingsProvider();

    [ObservableProperty]
    private string _moveTargetWing = "";

    [ObservableProperty]
    private bool _isRunning;

    partial void OnIsRunningChanged(bool value)
    {
        ExecuteCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanExecute));
    }

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isGuideOpen;

    [ObservableProperty]
    private bool _isArabic = true;

    partial void OnIsArabicChanged(bool value)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(RunButtonText));
        OnPropertyChanged(nameof(GuideButtonText));
        foreach (var param in Parameters)
        {
            param.IsArabic = value;
        }
    }

    public bool IsEnglish => !IsArabic;
    public string RunButtonText => IsArabic ? "تشغيل الأداة" : "Run Tool";
    public string GuideButtonText => IsArabic ? "دليل / Info" : "Guide / Info";

    public bool IsValid => Parameters.All(p => p.IsValid);
    public bool CanExecute => !IsRunning && IsValid;

    public ObservableCollection<ParameterViewModel> Parameters { get; } = new();

    public ToolItemViewModel(
        ToolDefinition tool,
        IProcessRunner processRunner,
        ITokenInterpolator tokenInterpolator,
        IToolRegistry toolRegistry,
        IConfigManager configManager,
        TerminalViewModel terminal,
        Action openTerminalAction,
        Action<ToolDefinition>? editAction = null,
        Action<ToolDefinition>? deleteAction = null,
        Action<ToolDefinition, string>? moveAction = null,
        Func<IEnumerable<WingItemViewModel>>? wingsProvider = null,
        IUserStateService? userStateService = null)
    {
        Tool = tool;
        _processRunner = processRunner;
        _tokenInterpolator = tokenInterpolator;
        _toolRegistry = toolRegistry;
        _configManager = configManager;
        _userStateService = userStateService;
        _terminal = terminal;
        _openTerminalAction = openTerminalAction;
        _editAction = editAction ?? (_ => { });
        _deleteAction = deleteAction ?? (_ => { });
        _moveAction = moveAction ?? ((_, _) => { });
        _wingsProvider = wingsProvider ?? (() => Array.Empty<WingItemViewModel>());
        _moveTargetWing = Tool.Category;

        foreach (var param in tool.Parameters)
        {
            var pvm = new ParameterViewModel(param)
            {
                IsArabic = IsArabic,
                GetSiblingParameterValue = id => Parameters.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))?.Value,
                GetAllParameters = () => Parameters,
                OnValidationChanged = () =>
                {
                    ExecuteCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsValid));
                    OnPropertyChanged(nameof(CanExecute));
                }
            };

            // Path Persistence: Load previously used path as default if available
            if (_userStateService != null && (pvm.IsFilePath || pvm.IsDirectoryPath))
            {
                var saved = _userStateService.GetLastPath(tool.Id, param.Id);
                if (!string.IsNullOrWhiteSpace(saved))
                {
                    pvm.Value = saved;
                }
            }

            Parameters.Add(pvm);
        }
    }

    [RelayCommand]
    private void EditTool() => _editAction(Tool);

    [RelayCommand]
    private void DeleteTool() => _deleteAction(Tool);

    [RelayCommand]
    private void QuickDeleteTool() => _deleteAction(Tool);

    [RelayCommand]
    private void MoveTool()
    {
        if (!string.IsNullOrWhiteSpace(MoveTargetWing) &&
            !string.Equals(MoveTargetWing, Tool.Category, StringComparison.OrdinalIgnoreCase))
            _moveAction(Tool, MoveTargetWing);
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private void ToggleGuide()
    {
        IsGuideOpen = !IsGuideOpen;
    }

    [RelayCommand]
    public async Task ExecuteAsync()
    {
        if (IsRunning || _processRunner.IsRunning)
        {
            _terminal.Append(new LogEntry("⚠️ Another process is already running.", LogLevel.Warning));
            return;
        }

        // Auto-open terminal drawer
        _openTerminalAction.Invoke();

        IsRunning = true;
        _terminal.IsRunning = true;
        _terminal.StatusText = $"Running: {Title}...";

        var paramDict = Parameters.ToDictionary(p => p.Id, p => p.Value);
        var globalSettings = _configManager.GlobalSettings;

        var progress = new Progress<LogEntry>(entry => _terminal.Append(entry));
        using var cts = new CancellationTokenSource();

        try
        {
            _terminal.Append(new LogEntry($"▶ [{Title}] Initializing...", LogLevel.Info));

            if (Tool.ExecutionType == ExecutionType.InternalTool)
            {
                var handler = _toolRegistry.GetHandler(Tool.InternalToolHandlerId);
                if (handler == null)
                {
                    _terminal.Append(new LogEntry($"❌ Handler not registered: '{Tool.InternalToolHandlerId}'", LogLevel.Error));
                    return;
                }

                var request = new ToolExecutionRequest
                {
                    Tool = Tool,
                    Parameters = paramDict
                };

                await handler.ExecuteAsync(request, progress, cts.Token);
                PersistUsedPaths();
            }
            else if (Tool.ExecutionType == ExecutionType.PowerShell)
            {
                var script = _tokenInterpolator.Interpolate(Tool.Arguments, paramDict, globalSettings);
                var workingDir = _tokenInterpolator.Interpolate(Tool.WorkingDirectory, paramDict, globalSettings);
                var res = await _processRunner.RunPowerShellAsync(script, workingDir, progress, cts.Token);
                if (res.Success)
                {
                    PersistUsedPaths();
                }
            }
            else // ExternalProcess
            {
                var exe = _tokenInterpolator.Interpolate(Tool.Executable, paramDict, globalSettings);
                var args = _tokenInterpolator.Interpolate(Tool.Arguments, paramDict, globalSettings);
                var workingDir = _tokenInterpolator.Interpolate(Tool.WorkingDirectory, paramDict, globalSettings);

                // Smart Dependency Verification: check before launch
                var verification = DependencyVerifier.Verify(exe, workingDir);
                if (!verification.IsValid)
                {
                    _terminal.Append(new LogEntry(verification.GetMessage(IsArabic), LogLevel.Error));
                    _terminal.StatusText = IsArabic ? "تنبيه: متطلب غير متوفر" : "Missing external dependency";
                    return;
                }

                var res = await _processRunner.RunAsync(exe, args, workingDir, progress, cts.Token);
                if (res.Success)
                {
                    PersistUsedPaths();
                }
            }
        }
        catch (Exception ex)
        {
            _terminal.Append(new LogEntry($"💥 Execution error: {ex.Message}", LogLevel.Error));
        }
        finally
        {
            IsRunning = false;
            _terminal.IsRunning = false;
            _terminal.StatusText = "Finished";
        }
    }

    private void PersistUsedPaths()
    {
        if (_userStateService == null) return;
        var changed = false;
        foreach (var p in Parameters.Where(p => (p.IsFilePath || p.IsDirectoryPath) && !string.IsNullOrWhiteSpace(p.Value)))
        {
            _userStateService.SetLastPath(Tool.Id, p.Id, p.Value);
            changed = true;
        }

        if (changed)
        {
            _ = _userStateService.SaveAsync();
        }
    }

    public static string LocalizeTitle(string title, string id)
    {
        return id switch
        {
            "godot.headless.export" => "تصدير مشروع جودو (Headless)",
            "godot.cache.nuker" => "تنظيف كاش جودو وإصلاح التلف",
            "godot.gdscript.check" or "godot.script.checker" => "فاحص صحة سكربتات GDScript",
            "dotnet.singlefile.publish" => "حزم المشروع في ملف تنفيذي مستقل (.exe)",
            "dotnet.clean.binobj" or "dotnet.clean.bin_obj" => "تنظيف مخلفات البناء وتوفير المساحة",
            "git.quick.status" => "استعراض حالة وتفرعات Git",
            "git.quick.commit" or "dotnet.git.quick_push" => "حفظ ورفع التعديلات إلى Git",
            "xml.localization.checker" or "dotnet.loc.checker" => "فاحص مفاتيح ونصوص ملفات التعريب",
            "media.ico.generator" => "منشئ أيقونات الويندوز متعددة المقاسات",
            "media.audio.to_ogg" => "محول الصوتيات لصيغة OGG للألعاب",
            "media.video.compress" => "ضاغط فيديوهات الألعاب (H.264 MP4)",
            "system.unblock.dlls" => "فك حظر ملفات ومودات DLL المحملة",
            "system.clean.logs" => "منظف سجلات الكراش والملفات المؤقتة",
            "system.hung.killer" => "إنهاء العمليات والبرامج المعلقة إجبارياً",
            _ => title switch
            {
                "Godot Headless Export" => "تصدير مشروع جودو (Headless)",
                "Godot Deep Cache Nuker" => "تنظيف كاش جودو وإصلاح التلف",
                "GDScript Syntax & Dependency Checker" => "فاحص صحة سكربتات GDScript",
                "Single-File Publisher" => "حزم المشروع في ملف تنفيذي مستقل (.exe)",
                "Clean Bin & Obj Folders" or "Clean Bin & Obj Folders (Disk Saver)" => "تنظيف مخلفات البناء وتوفير المساحة",
                "Git Status & Branch Overview" => "استعراض حالة وتفرعات Git",
                "Git Quick Commit & Push" => "حفظ ورفع التعديلات إلى Git",
                "XML Localization / String Keys Checker" => "فاحص مفاتيح ونصوص ملفات التعريب",
                "Multi-Size Windows ICO Generator" => "منشئ أيقونات الويندوز متعددة المقاسات",
                "Audio to Game OGG Vorbis Converter" => "محول الصوتيات لصيغة OGG للألعاب",
                "Game Video Compressor (H.264 MP4)" => "ضاغط فيديوهات الألعاب (H.264 MP4)",
                "Unblock Downloaded DLLs & Files" => "فك حظر ملفات ومودات DLL المحملة",
                "Game Crash & Temp Log Cleaner" => "منظف سجلات الكراش والملفات المؤقتة",
                "Hung / Unresponsive Process Killer" => "إنهاء العمليات والبرامج المعلقة إجبارياً",
                _ => title
            }
        };
    }

    public static string LocalizeDescription(string description, string id)
    {
        return id switch
        {
            "godot.headless.export" => "تصدير لعبة جودو تلقائياً في الخلفية بدون واجهة إلى Windows Desktop أو إعدادات أخرى.",
            "godot.cache.nuker" => "حذف مجلد كاش .godot وملفات القفل المؤقتة العالقة لحل مشاكل البناء والاستيراد.",
            "godot.gdscript.check" or "godot.script.checker" => "فحص سكربتات GDScript للتأكد من خلوها من الأخطاء اللغوية والمراجع المفقودة.",
            "dotnet.singlefile.publish" => "تجميع ونشر البرنامج كملف .exe مستقل ذاتياً ومضغوط بالكامل بدون الحاجة لتثبيت .NET مسبقاً.",
            "dotnet.clean.binobj" or "dotnet.clean.bin_obj" => "البحث الشجري وحذف كافة مجلدات bin و obj لتوفير مساحة القرص وحل أخطاء التجميع.",
            "git.quick.status" => "معاينة سريعة لحالة ملفات المستودع والتعديلات الجارية وتفرعات Git.",
            "git.quick.commit" or "dotnet.git.quick_push" => "إضافة التعديلات وحفظها ونقلها إلى المستودع البعيد بضغطة زر واحدة.",
            "xml.localization.checker" or "dotnet.loc.checker" => "مقارنة ملفات التعريب والترجمة لاكتشاف العبارات المفقودة أو الفارغة.",
            "media.ico.generator" => "تحويل أي صورة PNG/JPG إلى حزمة أيقونات ويندوز رسمية متعددة المقاسات (16 إلى 256 بكسل).",
            "media.audio.to_ogg" => "تحويل الملفات الصوتية (WAV, MP3, FLAC) إلى صيغة OGG Vorbis المحسنة لمحركات الألعاب.",
            "media.video.compress" => "ضغط الفيديوهات الكبيرة باستخدام ترميز H.264 / AAC لمشاهد الألعاب والعروض الترويجية.",
            "system.unblock.dlls" => "إزالة حظر ويندوز الأمني (Zone.Identifier) عن كافة ملفات ومودات DLL دفعة واحدة.",
            "system.clean.logs" => "فحص وحذف ملفات تفريغ انهيار الألعاب (Crash Dumps) والسجلات المؤقتة لتوفير المساحة.",
            "system.hung.killer" => "فحص ورصد كافة البرامج والألعاب المتجمدة أو المعلقة في الخلفية وإنهائها فورياً وتحرير الموارد.",
            _ => description
        };
    }
}
