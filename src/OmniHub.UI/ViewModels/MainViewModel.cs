using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IProcessRunner _processRunner;
    private readonly ITokenInterpolator _tokenInterpolator;
    private readonly IToolRegistry _toolRegistry;
    private readonly IConfigManager _configManager;
    private readonly IUserStateService _userStateService;

    private readonly List<ToolItemViewModel> _allTools = new();

    [ObservableProperty] private string _selectedWing = "Godot";
    [ObservableProperty] private bool _isTerminalOpen = true;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsQuickLauncherOpen))] private bool _isCommandPaletteOpen;
    [ObservableProperty] private bool _isUserManualOpen;
    [ObservableProperty] private bool _isOnlineToolsOpen;
    [ObservableProperty] private double _terminalHeight = 220;
    [ObservableProperty] private bool _isArabic = true;
    [ObservableProperty] private string _windowTitle = "OmniHub - Developer & Gaming Omni-Hub";

    [ObservableProperty] private bool _isWingEditorOpen;
    [ObservableProperty] private bool _isEditingWing;
    private string _editingWingId = "";
    private string _editingToolId = "";
    [ObservableProperty] private string _newWingId = "";
    [ObservableProperty] private string _newWingName = "";
    [ObservableProperty] private string _newWingTitleEn = "";
    [ObservableProperty] private string _newWingTitleAr = "";
    [ObservableProperty] private string _newWingTitleOther = "";
    [ObservableProperty] private string _newWingIcon = "🧩";
    [ObservableProperty] private string _newWingDescriptionEn = "";
    [ObservableProperty] private string _newWingDescriptionAr = "";

    [ObservableProperty] private bool _isToolEditorOpen;
    [ObservableProperty] private string _newToolId = "";
    [ObservableProperty] private string _newToolTitle = "";
    [ObservableProperty] private string _newToolTitleAr = "";
    [ObservableProperty] private string _newToolDescription = "";
    [ObservableProperty] private string _newToolDescriptionAr = "";
    [ObservableProperty] private string _newToolExecutable = "";
    [ObservableProperty] private string _newToolArguments = "";
    [ObservableProperty] private string _newToolWorkingDirectory = "";
    [ObservableProperty] private Core.Enums.ExecutionType _newToolExecutionType = Core.Enums.ExecutionType.ExternalProcess;

    public ObservableCollection<NewToolParameterViewModel> NewToolParameters { get; } = new();

    public GridLength EffectiveTerminalHeight
    {
        get => IsTerminalOpen
            ? new GridLength(TerminalHeight, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);
        set
        {
            // GridSplitter writes a GridLength back through the TwoWay binding.
            // Keep the persisted terminal height while the terminal is open.
            // Ignore the zero value produced when the terminal is collapsed so
            // reopening it restores the previous height.
            if (IsTerminalOpen && value.GridUnitType == GridUnitType.Pixel && value.Value > 0)
            {
                TerminalHeight = value.Value;
            }
        }
    }

    public bool IsQuickLauncherOpen { get => IsCommandPaletteOpen; set => IsCommandPaletteOpen = value; }
    public TerminalViewModel Terminal { get; }
    public CommandPaletteViewModel Palette { get; }
    public UserManualViewModel Manual { get; }
    public OnlineToolsViewModel OnlineTools { get; }
    public IConfigManager ConfigManager => _configManager;
    public string ConfigsDirectory => ResolveConfigsDirectory();
    public ObservableCollection<ToolItemViewModel> CurrentWingTools { get; } = new();
    public ObservableCollection<WingItemViewModel> Wings { get; } = new();

    public WingItemViewModel? SelectedWingItem => Wings.FirstOrDefault(w => string.Equals(w.Id, SelectedWing, StringComparison.OrdinalIgnoreCase));
    public bool CanDeleteSelectedWing => SelectedWingItem is { IsBuiltIn: false };
    public bool CanEditSelectedWing => SelectedWingItem is { IsBuiltIn: false };

    public string CurrentLanguageButtonText => IsArabic ? "🌐 English" : "🌐 عربي";
    public string QuickSearchPlaceholderText => IsArabic ? "البحث السريع والأوامر..." : "Quick launcher & commands...";
    public string ManualButtonText => IsArabic ? "دليل الاستخدام" : "User Manual";
    public string TerminalButtonText => IsArabic ? "الموجه (F12)" : "Terminal (F12)";
    public string HubsSectionHeader => IsArabic ? "الأجنحة ومسارات العمل" : "HUBS & PIPELINES";
    public string OnlineToolsButtonText => IsArabic ? "🌐 الأدوات عبر الإنترنت" : "🌐 Online Tools";
    public string AddWingButtonText => IsArabic ? "＋ إضافة جناح" : "＋ Add Wing";
    public string DeleteWingButtonText => IsArabic ? "حذف الجناح" : "Delete Wing";
    public string WingEditorTitle => IsEditingWing
        ? (IsArabic ? "تعديل الجناح" : "Edit Wing")
        : (IsArabic ? "إضافة جناح جديد" : "Add New Wing");
    public string WingNameLabel => IsArabic ? "اسم الجناح" : "Wing Name";
    public string WingNameHint => IsArabic ? "الاسم الذي سيظهر في القائمة الجانبية." : "This name will appear in the sidebar.";
    public string WingNamePlaceholder => IsArabic ? "مثال: أدوات بايثون" : "Example: Python Tools";
    public string WingTitleOtherLabel => IsArabic ? "الاسم بالإنجليزية (اختياري)" : "Arabic Name (Optional)";
    public string WingTitleOtherHint => IsArabic ? "إذا تركته فارغاً، سيُستخدم الاسم الأساسي تلقائياً." : "Leave blank to use the main name automatically.";
    public string WingTitleOtherPlaceholder => IsArabic ? "مثال: Python Tools" : "مثال: أدوات بايثون";
    public string WingIconLabel => IsArabic ? "اختر أيقونة" : "Choose an Icon";
    public string WingCustomIconLabel => IsArabic ? "أو إيموجي مخصص" : "Or custom emoji";
    public string WingIconHint => IsArabic ? "اختر أيقونة جاهزة أو اكتب إيموجي خاصاً بك." : "Pick a ready icon or enter your own emoji.";
    public string WingDescriptionEnLabel => IsArabic ? "الوصف بالإنجليزية" : "English Description";
    public string WingDescriptionArLabel => IsArabic ? "الوصف بالعربية" : "Arabic Description";
    public string CreateWingButtonText => IsEditingWing
        ? (IsArabic ? "حفظ التعديلات" : "Save Changes")
        : (IsArabic ? "إنشاء الجناح" : "Create Wing");
    public string EditWingButtonText => IsArabic ? "تعديل الجناح" : "Edit Wing";
    public string CancelButtonText => IsArabic ? "إلغاء" : "Cancel";
    public string AddToolButtonText => IsArabic ? "＋ إضافة أداة" : "＋ Add Tool";
    public string ToolEditorTitle => _editingToolId.Length > 0
        ? (IsArabic ? "تعديل الأداة" : "Edit Tool")
        : (IsArabic ? "إضافة أداة جديدة" : "Add New Tool");
    public string ToolTitleLabel => IsArabic ? "اسم الأداة" : "Tool Name";
    public string ToolTitleArLabel => IsArabic ? "الاسم بالعربية (اختياري)" : "Arabic Name (Optional)";
    public string ToolDescriptionLabel => IsArabic ? "الوصف" : "Description";
    public string ToolExecutableLabel => IsArabic ? "البرنامج / الملف التنفيذي" : "Executable / Program";
    public string ToolArgumentsLabel => IsArabic ? "المعاملات Arguments" : "Arguments";
    public string ToolWorkingDirectoryLabel => IsArabic ? "مجلد العمل (اختياري)" : "Working Directory (Optional)";
    public string ToolExecutionTypeLabel => IsArabic ? "نوع التشغيل" : "Execution Type";
    public string ToolParametersLabel => IsArabic ? "المدخلات التي سيطلبها المستخدم" : "User Inputs";
    public string AddParameterButtonText => IsArabic ? "＋ إضافة مدخل" : "＋ Add Input";
    public string SaveToolButtonText => _editingToolId.Length > 0
        ? (IsArabic ? "حفظ التعديلات" : "Save Changes")
        : (IsArabic ? "حفظ الأداة" : "Save Tool");
    public string ToolFileHint => IsArabic ? "سيتم إنشاء ملف .tools.json تلقائياً داخل configs/tools/." : "A .tools.json file will be created automatically inside configs/tools/.";

    public string SelectedWingDisplayTitle => SelectedWingItem?.DisplayTitle ?? (IsArabic ? "لا توجد أجنحة" : "No wings available");
    public string SelectedWingDisplaySubtitle => SelectedWingItem?.DisplaySubtitle ?? (IsArabic ? "أنشئ جناحاً جديداً من زر ＋ إضافة جناح." : "Create a new wing with ＋ Add Wing.");

    public bool ShowCustomEmptyState => string.Equals(SelectedWing, "Custom", StringComparison.OrdinalIgnoreCase) && CurrentWingTools.Count == 0;
    public string CustomEmptyStateTitle => IsArabic ? "لا توجد أدوات مخصصة حتى الآن" : "No Custom Tools Configured Yet";
    public string CustomEmptyStateMessage => IsArabic
        ? "يمكنك إنشاء وتخصيص أدواتك البرمجية عبر ملفات JSON بسهولة. انقر على الزر أدناه لفتح مجلد التكوينات."
        : "Define your own automation tools with simple JSON configs. Click below to open the configuration folder.";
    public string AddCustomToolButtonText => IsArabic ? "إضافة أداة مخصصة (+)" : "Add Custom Tool (+)";

    public MainViewModel(
        IProcessRunner processRunner,
        ITokenInterpolator tokenInterpolator,
        IToolRegistry toolRegistry,
        IConfigManager configManager,
        IUserStateService? userStateService = null)
    {
        _processRunner = processRunner;
        _tokenInterpolator = tokenInterpolator;
        _toolRegistry = toolRegistry;
        _configManager = configManager;
        _userStateService = userStateService ?? new OmniHub.Engine.Services.UserStateService();
        Terminal = new TerminalViewModel(processRunner);
        Manual = new UserManualViewModel { IsArabic = IsArabic };
        OnlineTools = new OnlineToolsViewModel(this) { IsArabic = IsArabic };

        Palette = new CommandPaletteViewModel(
            allToolsProvider: () => _allTools,
            onToolSelected: tool =>
            {
                IsCommandPaletteOpen = false;
                SelectedWing = tool.Category;
                _ = tool.ExecuteAsync();
            });

        _configManager.OnConfigurationsReloaded += () => Application.Current?.Dispatcher.InvokeAsync(RefreshTools);
    }

    public async Task InitializeAsync()
    {
        var configsDir = ResolveConfigsDirectory();
        await _configManager.LoadConfigurationsAsync(configsDir);
        RefreshTools();
        Terminal.Append(new LogEntry("⚡ OmniHub initialized. Dynamic configs loaded successfully.", Core.Enums.LogLevel.Success));
    }

    private string ResolveConfigsDirectory()
    {
        var configsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs");
        if (Directory.Exists(configsDir)) return configsDir;
        var devConfigs = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "configs"));
        return Directory.Exists(devConfigs) ? devConfigs : configsDir;
    }

    partial void OnIsTerminalOpenChanged(bool value) => OnPropertyChanged(nameof(EffectiveTerminalHeight));
    partial void OnTerminalHeightChanged(double value) => OnPropertyChanged(nameof(EffectiveTerminalHeight));

    partial void OnIsArabicChanged(bool value)
    {
        App.ApplyLanguage(value);
        foreach (var tool in _allTools) tool.IsArabic = value;
        Manual.IsArabic = value;
        foreach (var wing in Wings) wing.IsArabic = value;
        OnPropertyChanged(nameof(CurrentLanguageButtonText));
        OnPropertyChanged(nameof(QuickSearchPlaceholderText));
        OnPropertyChanged(nameof(ManualButtonText));
        OnPropertyChanged(nameof(TerminalButtonText));
        OnPropertyChanged(nameof(HubsSectionHeader));
        OnPropertyChanged(nameof(OnlineToolsButtonText));
        OnlineTools.IsArabic = value;
        OnPropertyChanged(nameof(AddWingButtonText));
        OnPropertyChanged(nameof(DeleteWingButtonText));
        OnPropertyChanged(nameof(WingEditorTitle));
        OnPropertyChanged(nameof(CreateWingButtonText));
        OnPropertyChanged(nameof(EditWingButtonText));
        OnPropertyChanged(nameof(WingNameLabel));
        OnPropertyChanged(nameof(WingNameHint));
        OnPropertyChanged(nameof(WingNamePlaceholder));
        OnPropertyChanged(nameof(WingTitleOtherLabel));
        OnPropertyChanged(nameof(WingTitleOtherHint));
        OnPropertyChanged(nameof(WingTitleOtherPlaceholder));
        OnPropertyChanged(nameof(WingIconLabel));
        OnPropertyChanged(nameof(WingCustomIconLabel));
        OnPropertyChanged(nameof(WingIconHint));
        OnPropertyChanged(nameof(WingDescriptionEnLabel));
        OnPropertyChanged(nameof(WingDescriptionArLabel));
        OnPropertyChanged(nameof(CancelButtonText));
        OnPropertyChanged(nameof(AddToolButtonText));
        OnPropertyChanged(nameof(ToolEditorTitle));
        OnPropertyChanged(nameof(ToolTitleLabel));
        OnPropertyChanged(nameof(ToolTitleArLabel));
        OnPropertyChanged(nameof(ToolDescriptionLabel));
        OnPropertyChanged(nameof(ToolExecutableLabel));
        OnPropertyChanged(nameof(ToolArgumentsLabel));
        OnPropertyChanged(nameof(ToolWorkingDirectoryLabel));
        OnPropertyChanged(nameof(ToolExecutionTypeLabel));
        OnPropertyChanged(nameof(ToolParametersLabel));
        OnPropertyChanged(nameof(AddParameterButtonText));
        OnPropertyChanged(nameof(SaveToolButtonText));
        OnPropertyChanged(nameof(ToolFileHint));
        OnPropertyChanged(nameof(SelectedWingDisplayTitle));
        OnPropertyChanged(nameof(SelectedWingDisplaySubtitle));
        OnPropertyChanged(nameof(ShowCustomEmptyState));
        OnPropertyChanged(nameof(CustomEmptyStateTitle));
        OnPropertyChanged(nameof(CustomEmptyStateMessage));
        OnPropertyChanged(nameof(AddCustomToolButtonText));
    }

    partial void OnSelectedWingChanged(string value)
    {
        foreach (var wing in Wings) wing.IsSelected = string.Equals(wing.Id, value, StringComparison.OrdinalIgnoreCase);
        FilterWingTools();
        OnPropertyChanged(nameof(SelectedWingItem));
        OnPropertyChanged(nameof(CanDeleteSelectedWing));
        OnPropertyChanged(nameof(CanEditSelectedWing));
    }

    public void RefreshTools()
    {
        _allTools.Clear();
        foreach (var def in _configManager.Tools)
        {
            var item = new ToolItemViewModel(def, _processRunner, _tokenInterpolator, _toolRegistry, _configManager, Terminal, () => IsTerminalOpen = true,
                EditCustomTool, DeleteCustomTool, MoveCustomTool, () => Wings, _userStateService)
            { IsArabic = IsArabic };
            _allTools.Add(item);
        }
        RebuildWings();
        if (!Wings.Any(w => string.Equals(w.Id, SelectedWing, StringComparison.OrdinalIgnoreCase)))
            SelectedWing = Wings.FirstOrDefault(w => string.Equals(w.Id, "Godot", StringComparison.OrdinalIgnoreCase))?.Id ?? Wings.FirstOrDefault()?.Id ?? "";
        else
            FilterWingTools();

        OnlineTools?.RefreshInstalledStatus();
    }

    private void RebuildWings()
    {
        var previous = SelectedWing;
        Wings.Clear();
        var map = new Dictionary<string, WingItemViewModel>(StringComparer.OrdinalIgnoreCase);

        void AddWing(string id, string en, string ar, string icon, string descEn, string descAr, bool builtIn)
        {
            if (string.IsNullOrWhiteSpace(id) || map.ContainsKey(id)) return;
            var wing = new WingItemViewModel(id, en, ar, icon, descEn, descAr) { IsArabic = IsArabic, IsBuiltIn = builtIn };
            map[id] = wing;
            Wings.Add(wing);
        }

        AddWing("Godot", "Godot Engine Hub", "جناح Godot Engine", "🎮", "Godot automation, exports and cache management.", "أتمتة مشاريع Godot والتصدير وإدارة الكاش.", true);
        AddWing("DotNet", ".NET & Git Hub", "جناح .NET & Git", "📦", ".NET build, publish and Git workflows.", "البناء والنشر وإدارة Git لمشاريع .NET.", true);
        AddWing("Media", "Media & Assets", "الوسائط والأصول", "🎨", "Media conversion and game asset preparation.", "تحويل الوسائط وتجهيز أصول الألعاب.", true);
        AddWing("System", "System & Utilities", "النظام والأدوات", "🛠️", "System maintenance and game utilities.", "صيانة النظام وأدوات الألعاب.", true);
        AddWing("Custom", "Custom JSON Tools", "الأدوات المخصصة (JSON)", "🧩", "User-defined automation tools from JSON.", "أدوات أتمتة يعرّفها المستخدم عبر JSON.", true);

        foreach (var def in _configManager.Wings)
            AddWing(def.Id, def.TitleEn, def.TitleAr, def.Icon, def.DescriptionEn, def.DescriptionAr, false);

        foreach (var group in _configManager.Tools.Where(t => !string.IsNullOrWhiteSpace(t.Category)).GroupBy(t => t.Category.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var en = group.Select(t => t.CategoryEn).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? group.Key;
            var ar = group.Select(t => t.CategoryAr).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? en;
            var icon = group.Select(t => t.CategoryIcon).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "🧩";
            var den = group.Select(t => t.CategoryDescriptionEn).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
            var dar = group.Select(t => t.CategoryDescriptionAr).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? den;
            if (map.TryGetValue(group.Key, out var existing))
            {
                // Tool metadata can improve a built-in/custom wing without replacing its protected status.
                if (!string.IsNullOrWhiteSpace(den)) existing.SubtitleEn = den;
                if (!string.IsNullOrWhiteSpace(dar)) existing.SubtitleAr = dar;
            }
            else AddWing(group.Key, en, ar, icon, den, dar, false);
        }

        foreach (var wing in Wings) wing.IsSelected = string.Equals(wing.Id, previous, StringComparison.OrdinalIgnoreCase);
        OnPropertyChanged(nameof(SelectedWingItem));
        OnPropertyChanged(nameof(CanDeleteSelectedWing));
        OnPropertyChanged(nameof(CanEditSelectedWing));
    }

    private void FilterWingTools()
    {
        CurrentWingTools.Clear();
        foreach (var tool in _allTools.Where(t => string.Equals(t.Category, SelectedWing, StringComparison.OrdinalIgnoreCase)))
        {
            tool.IsArabic = IsArabic;
            CurrentWingTools.Add(tool);
        }
        OnPropertyChanged(nameof(ShowCustomEmptyState));
        OnPropertyChanged(nameof(SelectedWingDisplayTitle));
        OnPropertyChanged(nameof(SelectedWingDisplaySubtitle));
    }

    [RelayCommand] private void ToggleLanguage() => IsArabic = !IsArabic;

    private void EditCustomTool(ToolDefinition tool)
    {
        if (!tool.Id.StartsWith("custom.", StringComparison.OrdinalIgnoreCase)) return;
        _editingToolId = tool.Id;
        OnPropertyChanged(nameof(ToolEditorTitle));
        OnPropertyChanged(nameof(SaveToolButtonText));
        NewToolId = tool.Id;
        NewToolTitle = tool.Title;
        NewToolTitleAr = tool.TitleAr;
        NewToolDescription = tool.Description;
        NewToolDescriptionAr = tool.DescriptionAr;
        NewToolExecutable = tool.Executable;
        NewToolArguments = tool.Arguments;
        NewToolWorkingDirectory = tool.WorkingDirectory;
        NewToolExecutionType = tool.ExecutionType;
        NewToolParameters.Clear();
        foreach (var p in tool.Parameters)
        {
            NewToolParameters.Add(new NewToolParameterViewModel
            {
                Id = p.Id,
                Label = p.Label,
                Type = p.Type,
                DefaultValue = p.DefaultValue,
                IsRequired = p.IsRequired
            });
        }
        IsToolEditorOpen = true;
    }

    private async void DeleteCustomTool(ToolDefinition tool)
    {
        if (ToolItemViewModel.IsBuiltInTool(tool.Id) && !tool.IsCustom)
        {
            MessageBox.Show(
                IsArabic ? "لا يمكن حذف الأدوات المدمجة الأساسية في النظام." : "Built-in core tools cannot be deleted.",
                "OmniHub", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            IsArabic
                ? $"هل أنت متأكد من حذف الأداة «{tool.Title}»؟\nسيتم حذف تعريفها من مجلد configs/tools/."
                : $"Are you sure you want to delete tool '{tool.Title}'?\nIts definition will be removed from configs/tools/.",
            "OmniHub", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        if (await RemoveToolFromJsonAsync(tool.Id))
        {
            Terminal.Append(new LogEntry($"🗑️ Tool '{tool.Title}' removed.", Core.Enums.LogLevel.Info));
            await InitializeAsync();
        }
    }

    private async void MoveCustomTool(ToolDefinition tool, string targetWing)
    {
        if (!tool.Id.StartsWith("custom.", StringComparison.OrdinalIgnoreCase)) return;
        if (string.IsNullOrWhiteSpace(targetWing) || string.Equals(tool.Category, targetWing, StringComparison.OrdinalIgnoreCase)) return;
        if (!await UpdateToolInJsonAsync(tool.Id, existing =>
        {
            existing.Category = targetWing;
            var wing = Wings.FirstOrDefault(w => string.Equals(w.Id, targetWing, StringComparison.OrdinalIgnoreCase));
            existing.CategoryEn = wing?.TitleEn ?? targetWing;
            existing.CategoryAr = wing?.TitleAr ?? targetWing;
            existing.CategoryIcon = wing?.Icon ?? "🧩";
        })) return;
        await InitializeAsync();
        SelectedWing = targetWing;
    }

    private async Task<bool> UpdateToolInJsonAsync(string toolId, Action<ToolDefinition> updater)
    {
        var toolsDir = Path.Combine(ResolveConfigsDirectory(), "tools");
        if (!Directory.Exists(toolsDir)) return false;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
        foreach (var path in Directory.GetFiles(toolsDir, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var list = JsonSerializer.Deserialize<List<ToolDefinition>>(json, options);
                if (list == null) continue;
                var item = list.FirstOrDefault(t => string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
                if (item == null) continue;
                updater(item);
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(list, options));
                return true;
            }
            catch { }
        }
        return false;
    }

    private async Task<bool> RemoveToolFromJsonAsync(string toolId)
    {
        var toolsDir = Path.Combine(ResolveConfigsDirectory(), "tools");
        if (!Directory.Exists(toolsDir)) return false;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
        foreach (var path in Directory.GetFiles(toolsDir, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);

                // 1. Try List<ToolDefinition>
                try
                {
                    var list = JsonSerializer.Deserialize<List<ToolDefinition>>(json, options);
                    if (list != null)
                    {
                        var removed = list.RemoveAll(t => string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
                        if (removed > 0)
                        {
                            if (list.Count == 0)
                                File.Delete(path);
                            else
                                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(list, options));
                            return true;
                        }
                    }
                }
                catch { }

                // 2. Try single ToolDefinition
                var single = JsonSerializer.Deserialize<ToolDefinition>(json, options);
                if (single != null && string.Equals(single.Id, toolId, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(path);
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    [RelayCommand]
    private void OpenAddCustomTool()
    {
        _editingToolId = "";
        OnPropertyChanged(nameof(ToolEditorTitle));
        OnPropertyChanged(nameof(SaveToolButtonText));
        NewToolId = "";
        NewToolTitle = "";
        NewToolTitleAr = "";
        NewToolDescription = "";
        NewToolDescriptionAr = "";
        NewToolExecutable = "";
        NewToolArguments = "";
        NewToolWorkingDirectory = "";
        NewToolExecutionType = Core.Enums.ExecutionType.ExternalProcess;
        NewToolParameters.Clear();
        IsToolEditorOpen = true;
    }

    [RelayCommand]
    private void BrowseToolExecutable()
    {
        var dialog = new OpenFileDialog
        {
            Title = IsArabic ? "اختر البرنامج أو السكربت" : "Select executable or script",
            Filter = IsArabic
                ? "الملفات التنفيذية والسكريبتات|*.exe;*.bat;*.cmd;*.ps1;*.py;*.jar|كل الملفات|*.*"
                : "Executables and scripts|*.exe;*.bat;*.cmd;*.ps1;*.py;*.jar|All files|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
            NewToolExecutable = dialog.FileName;
    }

    [RelayCommand]
    private void AddToolParameter()
    {
        var index = NewToolParameters.Count + 1;
        NewToolParameters.Add(new NewToolParameterViewModel
        {
            Id = $"Input{index}",
            Label = IsArabic ? $"المدخل {index}" : $"Input {index}"
        });
    }

    [RelayCommand]
    private void RemoveToolParameter(NewToolParameterViewModel? parameter)
    {
        if (parameter != null) NewToolParameters.Remove(parameter);
    }

    [RelayCommand]
    private void CancelAddCustomTool() => IsToolEditorOpen = false;

    [RelayCommand]
    private async Task SaveCustomToolAsync()
    {
        var title = (NewToolTitle ?? "").Trim();
        var executable = (NewToolExecutable ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show(IsArabic ? "اكتب اسم الأداة أولاً." : "Enter a tool name first.", "OmniHub", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (NewToolExecutionType == Core.Enums.ExecutionType.ExternalProcess && string.IsNullOrWhiteSpace(executable))
        {
            MessageBox.Show(IsArabic ? "اكتب البرنامج الذي تريد تشغيله." : "Enter the executable/program to run.", "OmniHub", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var parameter in NewToolParameters)
        {
            parameter.Id = CreateParameterId(parameter.Id, parameter.Label);
            if (string.IsNullOrWhiteSpace(parameter.Label)) parameter.Label = parameter.Id;
        }

        var duplicateParameter = NewToolParameters.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicateParameter != null)
        {
            MessageBox.Show(IsArabic ? $"معرّف المدخل مكرر: {duplicateParameter.Key}" : $"Duplicate input ID: {duplicateParameter.Key}", "OmniHub", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var id = string.IsNullOrWhiteSpace(_editingToolId) ? GenerateUniqueToolId(title) : _editingToolId;
        var existingTool = string.IsNullOrWhiteSpace(_editingToolId)
            ? null
            : _configManager.Tools.FirstOrDefault(t => string.Equals(t.Id, _editingToolId, StringComparison.OrdinalIgnoreCase));

        var definition = new ToolDefinition
        {
            Id = id,
            Title = title,
            TitleAr = (NewToolTitleAr ?? "").Trim(),
            Description = (NewToolDescription ?? "").Trim(),
            DescriptionAr = (NewToolDescriptionAr ?? "").Trim(),
            Category = existingTool?.Category ?? "Custom",
            CategoryEn = existingTool?.CategoryEn ?? "Custom JSON Tools",
            CategoryAr = existingTool?.CategoryAr ?? "الأدوات المخصصة (JSON)",
            CategoryIcon = existingTool?.CategoryIcon ?? "🧩",
            CategoryDescriptionEn = existingTool?.CategoryDescriptionEn ?? "User-defined automation tools.",
            CategoryDescriptionAr = existingTool?.CategoryDescriptionAr ?? "أدوات أتمتة يعرّفها المستخدم.",
            ExecutionType = NewToolExecutionType,
            Executable = executable,
            Arguments = (NewToolArguments ?? "").Trim(),
            WorkingDirectory = (NewToolWorkingDirectory ?? "").Trim(),
            IsCustom = true,
            IconKey = "Terminal",
            Parameters = NewToolParameters.Select(p => new ToolParameter
            {
                Id = p.Id,
                Label = p.Label.Trim(),
                Type = p.Type,
                DefaultValue = p.DefaultValue ?? "",
                IsRequired = p.IsRequired
            }).ToList()
        };

        var toolsDir = Path.Combine(ResolveConfigsDirectory(), "tools");
        Directory.CreateDirectory(toolsDir);
        var path = Path.Combine(toolsDir, $"{id}.tools.json");
        var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new[] { definition }, options));
        IsToolEditorOpen = false;
        await InitializeAsync();
        SelectedWing = "Custom";
        MessageBox.Show(IsArabic ? $"تم إنشاء الأداة:\n{id}.tools.json" : $"Tool created:\n{id}.tools.json", "OmniHub", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void CopyToolDefinition(ToolDefinition target, ToolDefinition source)
    {
        target.Title = source.Title;
        target.Name = source.Name;
        target.TitleAr = source.TitleAr;
        target.Description = source.Description;
        target.DescriptionAr = source.DescriptionAr;
        target.Category = source.Category;
        target.CategoryEn = source.CategoryEn;
        target.CategoryAr = source.CategoryAr;
        target.CategoryIcon = source.CategoryIcon;
        target.CategoryDescriptionEn = source.CategoryDescriptionEn;
        target.CategoryDescriptionAr = source.CategoryDescriptionAr;
        target.ExecutionType = source.ExecutionType;
        target.Executable = source.Executable;
        target.Arguments = source.Arguments;
        target.WorkingDirectory = source.WorkingDirectory;
        target.InternalToolHandlerId = source.InternalToolHandlerId;
        target.RequiresAdmin = source.RequiresAdmin;
        target.IsCustom = source.IsCustom;
        target.IconKey = source.IconKey;
        target.Parameters = source.Parameters;
        target.Guide = source.Guide;
    }

    private string GenerateUniqueToolId(string title)
    {
        var baseId = CreateSlug(title);
        if (string.IsNullOrWhiteSpace(baseId)) baseId = "custom-tool";
        var id = $"custom.{baseId}";
        var n = 2;
        while (_configManager.Tools.Any(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)))
            id = $"custom.{baseId}.{n++}";
        return id;
    }

    private static string CreateParameterId(string id, string label)
    {
        var value = string.IsNullOrWhiteSpace(id) ? label : id;
        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        var result = new string(chars);
        return string.IsNullOrWhiteSpace(result) ? "Input" : char.ToUpperInvariant(result[0]) + result[1..];
    }

    public bool IsToolInstalled(string id) => _configManager.Tools.Any(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the locally installed version of an online tool, or null when it isn't installed / has no version.</summary>
    public string? GetInstalledToolVersion(string id)
    {
        var tool = _configManager.Tools.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(tool?.Version) ? null : tool!.Version;
    }

    /// <summary>Repository hosts explicitly marked as trusted from Settings (comma-separated hostnames). Unknown hosts are never auto-trusted.</summary>
    public IReadOnlyList<string> TrustedRepositoryHosts
    {
        get
        {
            _configManager.GlobalSettings.TryGetValue("TrustedRepositories", out var raw);
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public bool IsRepositoryTrusted(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        if (string.Equals(host, "demo", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "offline", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "official", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("Reda6Dev", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return TrustedRepositoryHosts.Any(h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddTrustedRepositoryAsync(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || IsRepositoryTrusted(host)) return;
        var updated = TrustedRepositoryHosts.Append(host).Distinct(StringComparer.OrdinalIgnoreCase);
        await _configManager.SaveSettingAsync("TrustedRepositories", string.Join(",", updated));
    }

    [RelayCommand]
    private void OpenConfigsFolder()
    {
        var configsDir = Path.Combine(ResolveConfigsDirectory(), "tools");
        Directory.CreateDirectory(configsDir);
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = configsDir, UseShellExecute = true }); } catch { }
    }

    [RelayCommand] private void SelectWing(string wing) => SelectedWing = wing;
    [RelayCommand] private void ToggleTerminal() => IsTerminalOpen = !IsTerminalOpen;
    [RelayCommand] private void TogglePalette() { if (IsCommandPaletteOpen) IsCommandPaletteOpen = false; else { Palette.Refresh(); IsCommandPaletteOpen = true; } }
    [RelayCommand] private void OpenPalette() => TogglePalette();
    [RelayCommand] private void ClosePalette() => IsCommandPaletteOpen = false;
    [RelayCommand] private void OpenManual() => IsUserManualOpen = true;
    [RelayCommand]
    private async Task OpenOnlineToolsAsync()
    {
        OnlineTools.StatusText = "";
        IsOnlineToolsOpen = true;
        await OnlineTools.EnsureLoadedAsync();
    }
    [RelayCommand] private void CloseOnlineTools() => IsOnlineToolsOpen = false;
    [RelayCommand] private void CloseManual() => IsUserManualOpen = false;

    [RelayCommand]
    private void OpenAddWing()
    {
        _editingWingId = "";
        IsEditingWing = false;
        OnPropertyChanged(nameof(WingEditorTitle));
        OnPropertyChanged(nameof(CreateWingButtonText));
        NewWingId = ""; NewWingName = ""; NewWingTitleEn = ""; NewWingTitleAr = ""; NewWingTitleOther = ""; NewWingIcon = "🧩"; NewWingDescriptionEn = ""; NewWingDescriptionAr = "";
        IsWingEditorOpen = true;
    }

    [RelayCommand]
    private void EditSelectedWing()
    {
        var wing = SelectedWingItem;
        if (wing == null || wing.IsBuiltIn) return;

        _editingWingId = wing.Id;
        IsEditingWing = true;
        OnPropertyChanged(nameof(WingEditorTitle));
        OnPropertyChanged(nameof(CreateWingButtonText));
        NewWingId = wing.Id;
        NewWingName = IsArabic ? wing.TitleAr : wing.TitleEn;
        NewWingTitleOther = IsArabic ? wing.TitleEn : wing.TitleAr;
        NewWingIcon = wing.Icon;
        NewWingDescriptionEn = wing.SubtitleEn;
        NewWingDescriptionAr = wing.SubtitleAr;
        IsWingEditorOpen = true;
    }

    [RelayCommand]
    private void SelectWingIcon(string icon)
    {
        if (!string.IsNullOrWhiteSpace(icon)) NewWingIcon = icon;
    }

    [RelayCommand]
    private void CancelAddWing()
    {
        IsWingEditorOpen = false;
        IsEditingWing = false;
        OnPropertyChanged(nameof(WingEditorTitle));
        OnPropertyChanged(nameof(CreateWingButtonText));
        _editingWingId = "";
    }

    [RelayCommand]
    private async Task CreateWingAsync()
    {
        var name = (NewWingName ?? "").Trim();
        var otherName = (NewWingTitleOther ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(IsArabic ? "اكتب اسم الجناح أولاً." : "Enter a wing name first.", "OmniHub", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var id = IsEditingWing && !string.IsNullOrWhiteSpace(_editingWingId)
            ? _editingWingId
            : GenerateUniqueWingId(name);
        var ar = IsArabic ? name : (!string.IsNullOrWhiteSpace(otherName) ? otherName : name);
        var en = IsArabic ? (!string.IsNullOrWhiteSpace(otherName) ? otherName : name) : name;

        var wingsDir = Path.Combine(ResolveConfigsDirectory(), "wings");
        Directory.CreateDirectory(wingsDir);
        var definition = new WingDefinition
        {
            Id = id, TitleEn = en, TitleAr = ar, Icon = string.IsNullOrWhiteSpace(NewWingIcon) ? "🧩" : NewWingIcon.Trim(),
            DescriptionEn = NewWingDescriptionEn?.Trim() ?? "", DescriptionAr = NewWingDescriptionAr?.Trim() ?? "", IsBuiltIn = false
        };
        var path = Path.Combine(wingsDir, $"{id}.wing.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(definition, new JsonSerializerOptions { WriteIndented = true }));
        IsWingEditorOpen = false;
        IsEditingWing = false;
        OnPropertyChanged(nameof(WingEditorTitle));
        OnPropertyChanged(nameof(CreateWingButtonText));
        _editingWingId = "";
        await InitializeAsync();
        SelectedWing = id;
    }

    private string GenerateUniqueWingId(string name)
    {
        var slug = CreateSlug(name);
        var baseId = string.IsNullOrWhiteSpace(slug) ? "wing" : slug;
        var id = baseId;
        var number = 2;
        while (Wings.Any(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase)))
            id = $"{baseId}-{number++}";
        return id;
    }

    private static string CreateSlug(string value)
    {
        var arabicMap = new Dictionary<char, string>
        { ['ا']="a",['أ']="a",['إ']="i",['آ']="a",['ب']="b",['ت']="t",['ث']="th",['ج']="j",['ح']="h",['خ']="kh",['د']="d",['ذ']="dh",['ر']="r",['ز']="z",['س']="s",['ش']="sh",['ص']="s",['ض']="d",['ط']="t",['ظ']="z",['ع']="a",['غ']="gh",['ف']="f",['ق']="q",['ك']="k",['ل']="l",['م']="m",['ن']="n",['ه']="h",['و']="w",['ي']="y",['ى']="a",['ة']="h",['ء']="a"
        };
        var chars = new List<char>();
        foreach (var c in value.ToLowerInvariant())
        {
            if (arabicMap.TryGetValue(c, out var mapped)) chars.AddRange(mapped);
            else if (char.IsLetterOrDigit(c)) chars.Add(c);
            else chars.Add('-');
        }
        var slug = new string(chars.ToArray());
        while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
    [RelayCommand]
    private async Task DeleteSelectedWingAsync()
    {
        var wing = SelectedWingItem;
        if (wing == null || wing.IsBuiltIn) return;

        var dependentTools = _allTools.Where(t => string.Equals(t.Category, wing.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        var title = wing.DisplayTitle;
        MessageBoxResult result;

        if (dependentTools.Count > 0)
        {
            var message = IsArabic
                ? $"الجناح «{title}» يحتوي على {dependentTools.Count} أداة.\n\nنعم = نقل الأدوات إلى «الأدوات المخصصة» ثم حذف الجناح.\nلا = إلغاء العملية."
                : $"The wing '{title}' contains {dependentTools.Count} tool(s).\n\nYes = move the tools to 'Custom Tools' and delete the wing.\nNo = cancel.";
            result = MessageBox.Show(message, "OmniHub", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            await MoveToolsToCustomAsync(dependentTools);
        }
        else
        {
            var message = IsArabic
                ? $"هل تريد حذف الجناح «{title}»؟\nسيتم حذف تعريف الجناح فقط."
                : $"Delete wing '{title}'?\nOnly the wing definition will be removed.";
            result = MessageBox.Show(message, "OmniHub", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }

        var path = Path.Combine(ResolveConfigsDirectory(), "wings", $"{wing.Id}.wing.json");
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(IsArabic ? $"تعذر حذف الجناح: {ex.Message}" : $"Could not delete the wing: {ex.Message}", "OmniHub", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SelectedWing = "Custom";
        await InitializeAsync();
        SelectedWing = "Custom";
    }

    private async Task MoveToolsToCustomAsync(IEnumerable<ToolItemViewModel> tools)
    {
        var toolsDir = Path.Combine(ResolveConfigsDirectory(), "tools");
        var changed = false;
        foreach (var item in tools)
        {
            item.Tool.Category = "Custom";
            item.Tool.CategoryEn = "Custom JSON Tools";
            item.Tool.CategoryAr = "الأدوات المخصصة (JSON)";
            item.Tool.CategoryIcon = "🧩";
            item.Tool.CategoryDescriptionEn = "User-defined automation tools from JSON.";
            item.Tool.CategoryDescriptionAr = "أدوات أتمتة يعرّفها المستخدم عبر JSON.";
            changed |= await TryUpdateToolInJsonAsync(item.Tool, toolsDir);
        }

        if (changed)
            await Task.Delay(50);
    }

    private static async Task<bool> TryUpdateToolInJsonAsync(ToolDefinition target, string toolsDir)
    {
        if (!Directory.Exists(toolsDir)) return false;
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        foreach (var file in Directory.GetFiles(toolsDir, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var list = JsonSerializer.Deserialize<List<ToolDefinition>>(json, options);
                if (list != null)
                {
                    var found = list.FirstOrDefault(t => string.Equals(t.Id, target.Id, StringComparison.OrdinalIgnoreCase));
                    if (found == null) continue;
                    found.Category = target.Category;
                    found.CategoryEn = target.CategoryEn;
                    found.CategoryAr = target.CategoryAr;
                    found.CategoryIcon = target.CategoryIcon;
                    found.CategoryDescriptionEn = target.CategoryDescriptionEn;
                    found.CategoryDescriptionAr = target.CategoryDescriptionAr;
                    await File.WriteAllTextAsync(file, JsonSerializer.Serialize(list, options));
                    return true;
                }

                var single = JsonSerializer.Deserialize<ToolDefinition>(json, options);
                if (single != null && string.Equals(single.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                {
                    single.Category = target.Category;
                    single.CategoryEn = target.CategoryEn;
                    single.CategoryAr = target.CategoryAr;
                    single.CategoryIcon = target.CategoryIcon;
                    single.CategoryDescriptionEn = target.CategoryDescriptionEn;
                    single.CategoryDescriptionAr = target.CategoryDescriptionAr;
                    await File.WriteAllTextAsync(file, JsonSerializer.Serialize(single, options));
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

}
