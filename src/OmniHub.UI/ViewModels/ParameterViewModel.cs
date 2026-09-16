using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OmniHub.Core.Enums;
using OmniHub.Core.Models;
using OmniHub.UI.Components;

namespace OmniHub.UI.ViewModels;

public partial class ParameterViewModel : ObservableObject
{
    public ToolParameter Definition { get; }

    [ObservableProperty]
    private string _value = string.Empty;

    partial void OnValueChanged(string value)
    {
        OnPropertyChanged(nameof(IsValid));
        OnValidationChanged?.Invoke();
    }

    public Action? OnValidationChanged { get; set; }
    public bool IsValid => !IsRequired || !string.IsNullOrWhiteSpace(Value);

    [ObservableProperty]
    private bool _isArabic = true;

    partial void OnIsArabicChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayLabel));
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(BrowseFileText));
        OnPropertyChanged(nameof(BrowseFolderText));
        OnPropertyChanged(nameof(ProcessPresetsLabel));
        OnPropertyChanged(nameof(RefreshProcessesText));
        if (IsProcessPicker) InitProcessPresets();
    }

    public string Id => Definition.Id;
    public string Label => Definition.Label;
    public string Description => Definition.Description;
    public string DisplayLabel => IsArabic ? LocalizeLabel(Definition.Label, Definition.Id) : Definition.Label;
    public string DisplayDescription => IsArabic ? LocalizeDescription(Definition.Description, Definition.Id) : Definition.Description;
    public string BrowseFileText => IsArabic ? "استعراض ملف..." : "Browse File...";
    public string BrowseFolderText => IsArabic ? "استعراض مجلد..." : "Browse Folder...";
    public ParameterType Type => Definition.Type;
    public List<string> Options => Definition.Options;
    public bool IsRequired => Definition.IsRequired;

    public bool IsText => Type == ParameterType.Text || Type == ParameterType.Number;
    public bool IsProcessPicker => string.Equals(Id, "ProcessNameFilter", StringComparison.OrdinalIgnoreCase);
    public bool IsStandardText => IsText && !IsProcessPicker;
    public bool IsFilePath => Type == ParameterType.FilePath || Type == ParameterType.OutputFile;
    public bool IsDirectoryPath => Type == ParameterType.DirectoryPath;
    public bool IsDropdown => Type == ParameterType.Dropdown;
    public bool IsCheckbox => Type == ParameterType.Checkbox;
    public bool IsOutputFile => DynamicParameterControl.IsOutputFileParameter(this);

    public Func<string, string?>? GetSiblingParameterValue { get; set; }
    public Func<IEnumerable<ParameterViewModel>>? GetAllParameters { get; set; }

    public ObservableCollection<string> ProcessPresets { get; } = new();
    [ObservableProperty] private string? _selectedProcessPreset;

    public string ProcessPresetsLabel => IsArabic ? "اختصارات سريعة للألعاب:" : "Quick Game Presets:";
    public string RefreshProcessesText => IsArabic ? "🔄 جلب العمليات النشطة" : "🔄 Refresh Active Processes";

    partial void OnSelectedProcessPresetChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("--"))
            return;
        Value = value;
    }

    private void InitProcessPresets()
    {
        ProcessPresets.Clear();
        ProcessPresets.Add(IsArabic ? "-- اختر عملية أو اكتب اسماً --" : "-- Select process or type name --");
        var defaults = new[] { "Bannerlord", "GTA5", "Godot", "dotnet" };
        foreach (var d in defaults) ProcessPresets.Add(d);
        if (Definition.Options != null)
        {
            foreach (var opt in Definition.Options)
            {
                if (!string.IsNullOrWhiteSpace(opt) && !ProcessPresets.Contains(opt))
                    ProcessPresets.Add(opt);
            }
        }
    }

    [RelayCommand]
    private void RefreshActiveProcesses()
    {
        try
        {
            var currentVal = Value;
            InitProcessPresets();
            var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrWhiteSpace(p.ProcessName))
                    {
                        running.Add(p.ProcessName);
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }

            foreach (var name in running.OrderBy(x => x))
            {
                if (!ProcessPresets.Contains(name))
                {
                    ProcessPresets.Add(name);
                }
            }
        }
        catch { }
    }

    public static string LocalizeLabel(string label, string id)
    {
        return id switch
        {
            "ProjectPath" => "مسار المشروع / الملف المستهدف",
            "ExportPreset" => "اسم إعداد التصدير (Preset)",
            "OutputFile" => "مسار الملف التنفيذي الناتج (.exe)",
            "Configuration" => "إعداد البناء (Configuration)",
            "Runtime" => "بيئة التشغيل المستهدفة (RID)",
            "SelfContained" => "تضمين حزمة الدوت نت (مستقل ذاتياً)",
            "OutputDir" => "مجلد التصدير والنشر",
            "InputImage" => "صورة المصدر (PNG / JPG / BMP)",
            "OutputIco" => "ملف الأيقونة الناتج (.ico)",
            "InputAudio" => "ملف الصوت المصدر (WAV / MP3)",
            "OutputOgg" => "ملف الـ OGG الناتج",
            "Quality" => "مستوى الجودة (0-10)",
            "TargetDirectory" => "المجلد المستهدف",
            "BranchName" => "اسم الفرع (Branch)",
            "CommitMessage" => "رسالة الاعتماد (Commit Message)",
            "RemoteUrl" => "رابط المستودع البعيد (Remote URL)",
            "ProcessName" => "اسم العملية / البرنامج",
            "ProcessName" or "ProcessNameFilter" => "اسم العملية / البرنامج المستهدف (اختياري)",
            "PortNumber" => "رقم المنفذ (Port)",
            "CleanTempFiles" => "تنظيف الملفات المؤقتة",
            "TargetFolder" => "مجلد التنظيف المستهدف (أو اتركه للافتراضي)",
            "BaseFile" => "ملف اللغة الأساسي (مثل en.xml)",
            "TargetFile" => "ملف لغة الهدف (مثل ar.xml)",
            "RootPath" => "المجلد الجذري للمشاريع",
            "RepoPath" => "مجلد المستودع (Git)",
            "InputVideo" => "ملف الفيديو المصدر (MP4 / MOV / AVI)",
            "OutputVideo" => "مسار ملف الفيديو الناتج المضغوط",
            _ => label switch
            {
                "Project / Solution Path" => "مسار المشروع / الحل",
                "Project Directory" => "مجلد المشروع",
                "Build Configuration" => "إعداد البناء",
                "Target Runtime Identifier (RID)" => "معرف بيئة التشغيل (RID)",
                "Self-Contained (Bundle .NET Runtime)" => "تضمين حزمة الدوت نت (مستقل)",
                "Publish Output Directory" => "مجلد النشر والتصدير",
                "Source Image (PNG / JPG / BMP)" => "صورة المصدر (PNG / JPG)",
                "Output ICO File" => "ملف الأيقونة الناتج (.ico)",
                "Input Audio File" => "ملف الصوت المصدر",
                "Output OGG File" => "ملف الـ OGG الناتج",
                "OGG Quality (0-10)" => "مستوى الجودة (0-10)",
                "Process Name Filter (Optional)" => "اسم العملية المستهدفة (اختياري)",
                "Folder to Clean (or leave blank for standard app temp)" => "المجلد المراد تنظيفه (اختياري)",
                "Target Directory to Unblock" => "المجلد المراد فك حظر ملفاته",
                _ => label
            }
        };
    }

    public static string LocalizeDescription(string description, string id)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;
        return id switch
        {
            "ProjectPath" => "المسار الكامل للمشروع أو ملف الإعدادات",
            "ExportPreset" => "الاسم المطابق للإعداد في ملف export_presets.cfg",
            "OutputFile" => "مسار حفظ الملف التنفيذي النهائي",
            "Configuration" => "وضع البناء النهائي للمشروع",
            "Runtime" => "معمارية النظام المستهدف للتشغيل",
            "SelfContained" => "حزم كل المكتبات ليعمل بدون تثبيت إضافي",
            "OutputDir" => "المجلد الذي ستوضع فيه الملفات الناتجة",
            "InputImage" => "ملف الصورة المراد تحويله إلى أيقونة",
            "OutputIco" => "مسار حفظ الأيقونة النهائية",
            "InputAudio" => "الملف الصوتي المراد ضغطه أو تحويله",
            "OutputOgg" => "مسار حفظ الملف الصوتي بعد التحويل",
            "Quality" => "جودة الصوت من 0 (أصغر حجم) إلى 10 (أعلى نقاوة)",
            "ProcessNameFilter" => "فلتر اختياري بالاسم (مثل godot أو dotnet) أو اتركه فارغاً لفحص كافة العمليات المعلقة",
            "TargetFolder" => "مسار المجلد المطلوب تنظيفه، أو اتركه فارغاً لتنظيف سجلات انهيار النظام التلقائية",
            "BaseFile" => "ملف لغة المصدر المكتمل لمطابقة المفاتيح",
            "TargetFile" => "ملف لغة الهدف المراد فحصه لاكتشاف المفاتيح المفقودة",
            "RootPath" => "المجلد للبحث الشجري وحذف مجلدات bin و obj",
            "RepoPath" => "مسار مجلد مستودع Git المحلي",
            "InputVideo" => "ملف الفيديو المراد ضغطه وتقليل حجمه",
            "OutputVideo" => "مسار حفظ ملف الفيديو بعد الضغط",
            _ => description
        };
    }

    public ParameterViewModel(ToolParameter definition)
    {
        Definition = definition;
        Value = definition.DefaultValue ?? string.Empty;
        if (IsProcessPicker)
        {
            InitProcessPresets();
        }
    }

    [RelayCommand]
    public void Browse()
    {
        if (IsFilePath)
        {
            DynamicParameterControl.BrowseFile(this);
        }
        else if (IsDirectoryPath)
        {
            var dialog = new OpenFolderDialog
            {
                Title = IsArabic
                    ? (!string.IsNullOrWhiteSpace(DisplayLabel) ? $"اختيار {DisplayLabel}" : "اختيار مجلد")
                    : $"Select {Label}"
            };

            var initialDir = SuggestInitialDirectory();
            if (!string.IsNullOrWhiteSpace(initialDir) && System.IO.Directory.Exists(initialDir))
            {
                dialog.InitialDirectory = initialDir;
            }

            if (dialog.ShowDialog() == true)
            {
                Value = dialog.FolderName;
            }
        }
    }

    public string GetDefaultExtension()
    {
        if (!string.IsNullOrWhiteSpace(Definition.Filter))
        {
            var match = System.Text.RegularExpressions.Regex.Match(Definition.Filter, @"\*\.([a-zA-Z0-9]+)");
            if (match.Success)
            {
                var ext = match.Groups[1].Value;
                if (!ext.Equals("*", StringComparison.OrdinalIgnoreCase))
                {
                    return "." + ext.ToLowerInvariant();
                }
            }
        }

        if (Id.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) >= 0) return ".ico";
        if (Id.IndexOf("Ogg", StringComparison.OrdinalIgnoreCase) >= 0) return ".ogg";
        if (Id.IndexOf("Video", StringComparison.OrdinalIgnoreCase) >= 0 || Id.IndexOf("Mp4", StringComparison.OrdinalIgnoreCase) >= 0) return ".mp4";
        if (Id.IndexOf("Xml", StringComparison.OrdinalIgnoreCase) >= 0) return ".xml";
        if (Id.IndexOf("Json", StringComparison.OrdinalIgnoreCase) >= 0) return ".json";
        if (Id.IndexOf("Exe", StringComparison.OrdinalIgnoreCase) >= 0) return ".exe";

        return string.Empty;
    }

    public string SuggestOutputFileName()
    {
        // 1. If Value is already set to a valid non-template file name
        if (!string.IsNullOrWhiteSpace(Value) && !Value.Contains("${"))
        {
            try
            {
                var existingName = System.IO.Path.GetFileName(Value);
                if (!string.IsNullOrWhiteSpace(existingName))
                    return existingName;
            }
            catch { }
        }

        var ext = GetDefaultExtension();

        // 2. If DefaultValue has a template like "${InputImage}.ico"
        if (!string.IsNullOrWhiteSpace(Definition.DefaultValue) && Definition.DefaultValue.Contains("${"))
        {
            var defVal = Definition.DefaultValue;
            var match = System.Text.RegularExpressions.Regex.Match(defVal, @"\$\{([^}]+)\}(.*)");
            if (match.Success)
            {
                var refId = match.Groups[1].Value;
                var suffix = match.Groups[2].Value; // e.g. ".ico" or "_compressed.mp4"
                var refVal = GetSiblingParameterValue?.Invoke(refId);
                if (!string.IsNullOrWhiteSpace(refVal) && !refVal.Contains("${"))
                {
                    try
                    {
                        var baseName = System.IO.Path.GetFileNameWithoutExtension(refVal);
                        if (!string.IsNullOrWhiteSpace(baseName))
                        {
                            return baseName + suffix;
                        }
                    }
                    catch { }
                }
            }
        }

        // 3. Infer from any input file parameter in the tool
        if (GetAllParameters != null)
        {
            var inputParam = GetAllParameters().FirstOrDefault(p => p.IsFilePath && !p.IsOutputFile && !string.IsNullOrWhiteSpace(p.Value) && !p.Value.Contains("${"));
            if (inputParam != null)
            {
                try
                {
                    var baseName = System.IO.Path.GetFileNameWithoutExtension(inputParam.Value);
                    if (!string.IsNullOrWhiteSpace(baseName))
                    {
                        return baseName + ext;
                    }
                }
                catch { }
            }
        }

        // 4. Default fallback names based on Id or Extension
        if (string.Equals(ext, ".ico", StringComparison.OrdinalIgnoreCase) || Id.IndexOf("Ico", StringComparison.OrdinalIgnoreCase) >= 0)
            return "app.ico";
        if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase) || Id.IndexOf("Ogg", StringComparison.OrdinalIgnoreCase) >= 0)
            return "audio.ogg";
        if (string.Equals(ext, ".mp4", StringComparison.OrdinalIgnoreCase) || Id.IndexOf("Video", StringComparison.OrdinalIgnoreCase) >= 0)
            return "video.mp4";
        if (string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
            return "app.exe";
        if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            return "archive.zip";

        return !string.IsNullOrWhiteSpace(ext) ? $"output{ext}" : "output.bin";
    }

    public string? SuggestInitialDirectory()
    {
        // 1. If Value has an existing directory
        if (!string.IsNullOrWhiteSpace(Value) && !Value.Contains("${"))
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(Value);
                if (!string.IsNullOrWhiteSpace(dir))
                    return dir;
            }
            catch { }
        }

        // 2. From referenced input parameter or any sibling input parameter
        if (GetAllParameters != null)
        {
            var inputParam = GetAllParameters().FirstOrDefault(p => p.IsFilePath && !p.IsOutputFile && !string.IsNullOrWhiteSpace(p.Value) && !p.Value.Contains("${"));
            if (inputParam != null)
            {
                try
                {
                    var dir = System.IO.Path.GetDirectoryName(inputParam.Value);
                    if (!string.IsNullOrWhiteSpace(dir))
                        return dir;
                }
                catch { }
            }
        }

        return null;
    }
}

