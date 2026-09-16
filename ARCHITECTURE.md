# 🧭 OmniHub — خريطة السياق المعمارية (ARCHITECTURE.md)

> **الغرض من هذا الملف:** مرجع سياقي فوري (Context Map) يوضح بنية مشروع **OmniHub** (WPF / .NET 8 / MVVM) دون الحاجة لقراءة كل الكود من جديد في كل جلسة. يعتمد المشروع على **محرك تحميل أدوات ديناميكي (Dynamic Tool Engine)** يقرأ تعريفات الأدوات من ملفات JSON خارجية، ما يجعل إضافة أداة جديدة عملية "تكوين لا برمجة" (Config, not Code) في الغالب.

---

## 1. شجرة المشروع (Clean Architecture Tree)

المشروع مقسّم إلى 4 طبقات (Projects) داخل `OmniHub.sln`، بالإضافة إلى `configs/` (بيانات خارجية) و `tests/` (اختبارات الوحدة):

```
OmniHub/
│
├── OmniHub.sln
├── configs/                          # 🗂️ طبقة البيانات — تُقرأ في وقت التشغيل (Runtime)
│   ├── appsettings.json              # إعدادات عامة (GlobalSettings) مثل مسار ffmpeg و Godot
│   └── tools/                        # كل ملف = "جناح" (Wing) من الأدوات
│       ├── dotnet.tools.json         # جناح .NET & Git
│       ├── godot.tools.json          # جناح Godot Engine
│       ├── media.tools.json          # جناح الوسائط (صور/صوت/فيديو)
│       └── system.tools.json         # جناح صيانة النظام
│
├── src/
│   ├── OmniHub.Core/                 # 🎯 طبقة العقود (Contracts) — لا تعتمد على أي شيء آخر
│   │   ├── Enums/
│   │   │   ├── WingCategory.cs       # (Godot, DotNet, Media, System, Custom)
│   │   │   ├── ExecutionType.cs      # (ExternalProcess, PowerShell, InternalTool)
│   │   │   ├── ParameterType.cs      # (Text, FilePath, DirectoryPath, Dropdown, Checkbox, Number)
│   │   │   └── LogLevel.cs
│   │   ├── Models/
│   │   │   ├── ToolDefinition.cs     # ⭐ النموذج المقابل لكل عنصر JSON (Schema)
│   │   │   ├── ToolParameter.cs      # وصف حقل إدخال ديناميكي واحد
│   │   │   ├── ToolExecutionRequest.cs
│   │   │   ├── ToolGuideInfo.cs      # نصوص دليل الاستخدام (ثنائي اللغة)
│   │   │   ├── LogEntry.cs           # سطر واحد في التيرمنال الحي
│   │   │   └── ProcessExecutionResult.cs
│   │   └── Interfaces/               # العقود التي تُموّل بقية الطبقات (DIP)
│   │       ├── IConfigManager.cs     # تحميل/مراقبة ملفات JSON
│   │       ├── IToolRegistry.cs      # سجلّ معالجات الأدوات الداخلية
│   │       ├── IToolHandler.cs       # عقد "الأداة المبرمجة يدوياً" (C# Handler)
│   │       ├── ITokenInterpolator.cs # محرك استبدال ${Tokens}
│   │       └── IProcessRunner.cs     # تشغيل عمليات خارجية / PowerShell
│   │
│   ├── OmniHub.Engine/               # ⚙️ طبقة التنفيذ العامة (Generic Runtime Engine)
│   │   ├── Configuration/ConfigManager.cs      # قراءة + FileSystemWatcher (Hot-Reload)
│   │   ├── Templating/TokenInterpolator.cs     # Regex لاستبدال ${Param} / ${env:X} / ${timestamp}
│   │   ├── Process/ProcessRunner.cs            # تشغيل + بث المخرجات الحية + Kill Tree
│   │   ├── Services/ToolRegistry.cs            # ConcurrentDictionary<HandlerId, IToolHandler>
│   │   └── Extensions/EngineServiceExtensions.cs  # AddOmniHubEngine() — تسجيل DI
│   │
│   ├── OmniHub.Tools/                # 🔧 طبقة الأدوات الداخلية المبرمجة يدوياً (InternalTool Handlers)
│   │   ├── Cleaners/
│   │   │   ├── DotNetCleanArtifactsHandler.cs   # dotnet.clean.binobj
│   │   │   └── GodotCacheNukerHandler.cs        # godot.cache.nuker
│   │   ├── Localization/XmlLocalizationCheckerHandler.cs
│   │   ├── Media/IcoGeneratorHandler.cs         # media.ico.generator (System.Drawing)
│   │   ├── SysTools/
│   │   │   ├── CrashLogCleanerHandler.cs
│   │   │   ├── DllUnblockerHandler.cs
│   │   │   └── HungProcessKillerHandler.cs
│   │   └── Extensions/ServiceCollectionExtensions.cs  # AddOmniHubTools() + RegisterToolHandlers()
│   │
│   └── OmniHub.UI/                   # 🖥️ طبقة العرض (WPF / MVVM)
│       ├── App.xaml.cs               # 🚪 Composition Root — بناء حاوية الـ DI بالكامل
│       ├── MainWindow.xaml(.cs)      # النافذة الرئيسية (شريط الأجنحة + التيرمنال السفلي)
│       ├── ViewModels/
│       │   ├── MainViewModel.cs      # ⭐ ViewModel الجذر — تبديل الأجنحة، التعريب، تنسيق الحالة
│       │   ├── ToolItemViewModel.cs  # ⭐ يمثّل بطاقة أداة واحدة + منطق التنفيذ (ExecuteAsync)
│       │   ├── ParameterViewModel.cs # حقل إدخال ديناميكي واحد + التحقق (IsValid)
│       │   ├── TerminalViewModel.cs  # سجل اللوق الحي + عدادات الأخطاء/التحذيرات
│       │   └── CommandPaletteViewModel.cs  # لوحة الأوامر السريعة (Ctrl+K)
│       ├── Components/               # UserControls قابلة لإعادة الاستخدام
│       │   ├── ToolCardControl.xaml(.cs)         # بطاقة عرض/تشغيل الأداة
│       │   ├── DynamicParameterControl.xaml(.cs) # يرسم الحقل حسب ParameterType
│       │   ├── LiveTerminalControl.xaml(.cs)     # عارض اللوق السفلي
│       │   ├── CommandPaletteOverlay.xaml(.cs)   # نافذة البحث العائمة
│       │   └── UserManualOverlay.xaml(.cs)       # نافذة الدليل الإرشادي
│       ├── Converters/                # ComparisonConverter, LogLevelToColorConverter
│       └── Styles/                    # Colors.xaml, Controls.xaml (الهوية البصرية)
│
└── tests/OmniHub.Tests/
    ├── ConfigManagerTests.cs
    ├── TokenInterpolatorTests.cs
    ├── IcoGeneratorTests.cs
    ├── LocalizationAndValidationTests.cs
    └── QuickLauncherViewModelTests.cs
```

### تبعية الطبقات (Dependency Direction)
```
OmniHub.UI  ──depends on──▶  OmniHub.Engine ──┐
     │                                         ├──▶ OmniHub.Core (Enums / Models / Interfaces)
     └────────────────────▶  OmniHub.Tools ────┘
```
`OmniHub.Core` لا يعتمد على أي مشروع آخر (Zero dependency) — وهذا ما يسمح بتحميل الأدوات ديناميكياً دون كسر فصل الطبقات.

---

## 2. فهرس الوظائف والمسؤوليات (Component Index)

| الملف | الطبقة | المسؤولية الأساسية |
|---|---|---|
| `ToolDefinition.cs` | Core.Models | تمثيل C# لبنية عنصر أداة في ملف JSON (Id, Title, Category, ExecutionType, Executable/Arguments, Parameters, Guide). يدعم أسماء JSON بديلة (`titleAr` / `title_ar`) عبر خصائص Alt للتوافق. |
| `ToolParameter.cs` | Core.Models | وصف حقل إدخال واحد (Id, Label, Type, DefaultValue, Options, Filter, IsRequired). |
| `ToolExecutionRequest.cs` | Core.Models | حزمة تُمرَّر لأي `IToolHandler` تحتوي تعريف الأداة + قيم المعاملات الفعلية. |
| `ToolGuideInfo.cs` | Core.Models | نصوص "الدليل الإرشادي" ثنائية اللغة (ماذا يفعل / متى يُستخدم / مثال). |
| `LogEntry.cs` | Core.Models | سطر لوق واحد (Message, Level, Timestamp) — وحدة البث للتيرمنال الحي. |
| `IConfigManager.cs` / `ConfigManager.cs` | Core / Engine | تحميل جميع ملفات `configs/tools/*.json` + `appsettings.json`، مراقبة التغييرات عبر `FileSystemWatcher` مع Debounce (400ms)، وإطلاق `OnConfigurationsReloaded` لإعادة بناء القوائم في الواجهة تلقائياً (Hot-Reload بدون إعادة تشغيل). |
| `ITokenInterpolator.cs` / `TokenInterpolator.cs` | Core / Engine | محرك استبدال الرموز `${Token}` داخل `Arguments` / `WorkingDirectory`، بترتيب أولوية: معاملات المستخدم ← إعدادات عامة (`GlobalSettings`) ← رموز مدمجة (`timestamp`, `date`, `appdata`...) ← متغيرات بيئة (`env:NAME`). |
| `IProcessRunner.cs` / `ProcessRunner.cs` | Core / Engine | تشغيل عملية خارجية أو سكربت PowerShell، بث `stdout`/`stderr` سطراً بسطر كـ `LogEntry` عبر `IProgress<LogEntry>`، مع تصنيف تلقائي لمستوى اللوق (Error/Warning/Success)، ودعم `KillCurrent()` لإنهاء شجرة العمليات كاملة. |
| `IToolRegistry.cs` / `ToolRegistry.cs` | Core / Engine | سجلّ (Dictionary) يربط `HandlerId` النصي في ملف JSON بمعالج C# فعلي (`IToolHandler`) — يُستخدم فقط حين `ExecutionType = InternalTool`. |
| `IToolHandler.cs` (وكل Handler في `OmniHub.Tools`) | Core / Tools | عقد "الأداة المبرمجة يدوياً" — يُستخدم للمهام التي يصعب تنفيذها كأمر Shell بسيط (مثل توليد ICO عبر `System.Drawing`، أو حذف مجلدات bin/obj شجرياً). |
| `EngineServiceExtensions.cs` / `ServiceCollectionExtensions.cs` | Engine / Tools | نقاط تسجيل DI (`AddOmniHubEngine()`, `AddOmniHubTools()`, `RegisterToolHandlers()`) — تُستدعى من `App.xaml.cs`. |
| `App.xaml.cs` | UI | **جذر التركيب (Composition Root)**: يبني `ServiceCollection`، يسجّل Engine + Tools + ViewModels، يربط كل `IToolHandler` بـ `IToolRegistry`، ثم يعرض `MainWindow`. |
| `MainViewModel.cs` | UI.ViewModels | الجذر الحالة للواجهة: تبديل "الأجنحة" (`SelectedWing`)، إدارة التعريب (`IsArabic`)، فتح/إغلاق التيرمنال ولوحة الأوامر ودليل الاستخدام، وإعادة بناء `CurrentWingTools` عند كل Hot-Reload. |
| `ToolItemViewModel.cs` | UI.ViewModels | يمثّل بطاقة أداة واحدة؛ يحوّل معاملات المستخدم إلى `Dictionary<string,string>`، يقرر مسار التنفيذ (`InternalTool` / `PowerShell` / `ExternalProcess`)، ويحرس التنفيذ عبر `CanExecute` (`!IsRunning && IsValid`). |
| `ParameterViewModel.cs` | UI.ViewModels | يغلّف `ToolParameter` بمنطق تحقق فوري (`IsValid`)، ويدعم حوارات اختيار الملف/المجلد (`OpenFileDialog` / `OpenFolderDialog`) حسب `ParameterType`. |
| `TerminalViewModel.cs` | UI.ViewModels | يخزن `ObservableCollection<LogEntry>` (سقف 3000 سطر)، يحسب عدادات الأخطاء/التحذيرات، ويدعم `Clear` / `CopyLogs` / `KillProcess`. |
| `CommandPaletteViewModel.cs` | UI.ViewModels | فهرسة وبحث فوري (Fuzzy-ish Contains) عبر جميع الأدوات من كل الأجنحة، وتنفيذ الأداة المختارة مباشرة. |
| `ToolCardControl.xaml` | UI.Components | القالب البصري لبطاقة الأداة (العنوان، الوصف، الحقول الديناميكية، زر التشغيل، زر الدليل). |
| `DynamicParameterControl.xaml` | UI.Components | يرسم عنصر الإدخال المناسب (TextBox / ComboBox / CheckBox / زر Browse) حسب `ParameterViewModel.Type`. |
| `LiveTerminalControl.xaml` | UI.Components | يعرض `TerminalViewModel.Logs` حية مع تلوين حسب `LogLevel` (عبر `LogLevelToColorConverter`). |
| `configs/appsettings.json` | Config | إعدادات عامة قابلة للاستخدام كـ `${Token}` في أي أداة (مثل `${GodotExe}`, `${FFmpegPath}`). |
| `configs/tools/*.tools.json` | Config | تعريفات الأدوات الفعلية — كل ملف يُحمَّل ويُدمج تلقائياً بغض النظر عن اسمه (أي ملف `.json` داخل `configs/tools/`). |

---

## 3. سير البيانات والتنفيذ (Execution Pipeline)

```
[1] بدء التشغيل
    App.xaml.cs → يبني DI Container → MainViewModel.InitializeAsync()
         │
         ▼
[2] تحميل التكوين
    ConfigManager.LoadConfigurationsAsync("configs")
      • يقرأ appsettings.json  → GlobalSettings
      • يقرأ كل *.json في configs/tools/ → List<ToolDefinition>
      • يُفعّل FileSystemWatcher (مراقبة تغييرات حية + Debounce 400ms)
         │
         ▼
[3] بناء الواجهة
    MainViewModel.RefreshTools()
      • لكل ToolDefinition → إنشاء ToolItemViewModel
      • لكل ToolParameter داخله → إنشاء ParameterViewModel (يربط IsValid بالتحقق الفوري)
      • تصفية القائمة حسب SelectedWing (Godot/DotNet/Media/System/Custom)
         │
         ▼
[4] تفاعل المستخدم
    المستخدم يملأ الحقول في ToolCardControl (عبر DynamicParameterControl)
      • كل تغيير قيمة → ParameterViewModel.OnValueChanged → IsValid يُعاد حسابه
      • ToolItemViewModel.CanExecute = !IsRunning && Parameters.All(IsValid)
      • ExecuteCommand (RelayCommand) يُعطَّل تلقائياً إذا CanExecute = false
         │
         ▼
[5] الضغط على "تشغيل" → ToolItemViewModel.ExecuteAsync()
      • فتح درج التيرمنال تلقائياً (openTerminalAction)
      • تجميع Dictionary<paramId, value> + GlobalSettings
      • التفرّع حسب Tool.ExecutionType:
          ┌─ InternalTool   → ToolRegistry.GetHandler(InternalToolHandlerId) → handler.ExecuteAsync(...)
          ├─ PowerShell     → TokenInterpolator.Interpolate(Arguments) → ProcessRunner.RunPowerShellAsync(...)
          └─ ExternalProcess→ TokenInterpolator.Interpolate(Executable/Arguments/WorkingDirectory)
                              → ProcessRunner.RunAsync(...)
         │
         ▼
[6] البث الحي للمخرجات
    ProcessRunner / IToolHandler يُصدر IProgress<LogEntry> لكل سطر stdout/stderr
      • DetermineLogLevel() يصنّف كل سطر (Error/Warning/Success/Standard)
      • TerminalViewModel.Append(entry) على UI Dispatcher
         • يحدّث Logs (سقف 3000 سطر) + عدادات الأخطاء/التحذيرات
         → LiveTerminalControl يعرض السطر فوراً مع التلوين
         │
         ▼
[7] الإنهاء
    IsRunning = false → StatusText = "Finished" → ExecuteCommand.NotifyCanExecuteChanged()
    (يمكن الإلغاء في أي لحظة عبر TerminalViewModel.KillProcess → ProcessRunner.KillCurrent (Kill Tree))
```

### نقطتا التوسّع الرئيسيتان
1. **إضافة أداة بدون كود (الحالة الشائعة):** أضف عنصر JSON جديد في `configs/tools/*.json` (أو أنشئ ملفاً جديداً) بنوع `ExecutionType: ExternalProcess` أو `PowerShell`. سيُكتشف تلقائياً عبر `FileSystemWatcher` دون إعادة تشغيل التطبيق.
2. **إضافة أداة بمنطق C# مخصص:** أنشئ Handler جديد في `OmniHub.Tools` يطبّق `IToolHandler`، سجّله في `ServiceCollectionExtensions.AddOmniHubTools()`، ثم أضف تعريف JSON بـ `"executionType": "InternalTool"` و `"internalToolHandlerId": "<HandlerId>"` مطابقاً لـ `HandlerId` في الكلاس.

---

## 4. دليل سريع لإضافة "جناح" (Wing) أو أداة جديدة

| المطلوب | الخطوات |
|---|---|
| **أداة جديدة (سطر أوامر بسيط)** | أضف كائن في `configs/tools/*.json` بالحقول: `id`, `title`, `category`, `executionType: ExternalProcess`, `executable`, `arguments` (بصيغة `${Param}`), `parameters[]`. لا حاجة لأي كود. |
| **أداة جديدة (منطق معقد)** | 1) أنشئ `XyzHandler.cs` في `OmniHub.Tools/<فئة مناسبة>` يطبّق `IToolHandler`. 2) سجّله في `AddOmniHubTools()`. 3) عرّفه في JSON بـ `executionType: InternalTool` و`internalToolHandlerId`. |
| **جناح (Category) جديد بالكامل** | 1) أضف قيمة جديدة في `WingCategory` (Core/Enums). 2) أضف تسميات العرض (Tab label / Title / Subtitle) في `MainViewModel.cs`. 3) أنشئ ملف `configs/tools/<wing>.tools.json` جديد. |
| **نوع حقل إدخال جديد** | 1) أضف قيمة في `ParameterType` (Core/Enums). 2) أضف خاصية `IsXxx` في `ParameterViewModel.cs`. 3) أضف قالب العرض المقابل في `DynamicParameterControl.xaml`. |
| **رمز `${Token}` نظامي جديد** | أضف حالة جديدة داخل `TokenInterpolator.ResolveBuiltInToken()`. |

---

*آخر تحديث لهذا الملف: تم توليده تلقائياً عبر تحليل معماري شامل لمستودع OmniHub. يُنصح بتحديثه يدوياً عند إضافة مشروع (csproj) جديد أو تغيير جوهري في مسار التنفيذ (Execution Pipeline).*
