using CommunityToolkit.Mvvm.ComponentModel;

namespace OmniHub.UI.ViewModels;

/// <summary>
/// Localized content for the User Manual. The view only binds to this VM.
/// </summary>
public partial class UserManualViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isArabic = true;

    public IReadOnlyDictionary<string, string> T => IsArabic ? Arabic : English;

    partial void OnIsArabicChanged(bool value)
    {
        OnPropertyChanged(nameof(T));
    }

    private static readonly IReadOnlyDictionary<string, string> Arabic =
        new Dictionary<string, string>
        {
            ["Header"] = "دليل الاستخدام الشامل لمنصة OmniHub v1.0",
            ["Close"] = "إغلاق ✕ (Esc)",
            ["Topics"] = "أقسام الدليل (TOPICS)",
            ["NavOverview"] = "البداية والأجنحة والاختصارات",
            ["NavGodot"] = "جناح Godot Engine",
            ["NavDotNet"] = "جناح .NET & Git Hub",
            ["NavMedia"] = "جناح الوسائط والأصول",
            ["NavSystem"] = "جناح صيانة النظام والعمليات",
            ["NavCustom"] = "الأدوات المخصصة (JSON)",
            ["NavOnline"] = "متجر الأدوات عبر الإنترنت",

            // 1. Overview & Shortcuts & Persistence
            ["OverviewTitle"] = "🚀 مرحباً بك في OmniHub (Developer & Gaming Omni-Hub)",
            ["OverviewIntro"] = "المنصة مصممة لتكون مركز قيادة موحد ومطور لكافة المهام اليومية في تطوير الألعاب والبرمجيات وإدارة النظام، وتغنيك تماماً عن كتابة الأوامر المتكررة والمملة في موجهات الأوامر.",
            ["ShortcutsTitle"] = "⚡ اختصارات لوحة المفاتيح السريعة (Keyboard Shortcuts):",
            ["Shortcut1"] = "• Ctrl + K أو Ctrl + P: فتح وإغلاق لوحة الأوامر السريعة (Command Palette) لتشغيل أي أداة بالبحث اللحظي.",
            ["Shortcut2"] = "• F12: إظهار / إخفاء موجه الأوامر المباشر (Live Terminal) في الأسفل في أي وقت.",
            ["Shortcut3"] = "• Esc: إغلاق النوافذ المنبثقة وشريط البحث والدليل الفوري.",
            ["WingsPersistenceTitle"] = "📁 تنظيم الأجنحة وتذكر المسارات تلقائياً (%LOCALAPPDATA%\\OmniHub)",
            ["WingsPersistence1"] = "• حفظ واسترجاع المسارات الذكي: عند تحديد مسار ملف أو مجلد لأي أداة، يقوم OmniHub تلقائياً بحفظ مدخلاتك واسترجاعها في الجلسات القادمة داخل ملف %LOCALAPPDATA%\\OmniHub\\user-state.json دون الحاجة لإعادة اختيار المجلدات يدوياً في كل تشغيل.",
            ["WingsPersistence2"] = "• مدخلات تفاعلية متجاوبة: تدعم الأدوات استعراض الملفات (FilePath)، المجلدات (DirectoryPath)، النصوص، الأرقام، القوائم المنسدلة، وخانات الاختيار (Checkboxes).",
            ["TerminalTitle"] = "💡 موجه الأوامر المدمج (Live Terminal) والتحكم بالعمليات",
            ["TerminalBody"] = "عند الضغط على 'تشغيل الأداة' في أي بطاقة، يرتفع موجه الأوامر المباشر تلقائياً ويعرض المخرجات بنظام التدفق اللحظي. يمكنك في أي لحظة إيقاف العملية بزر 'Kill Process' الأحمر وتحرير موارد النظام.",

            // 2. Godot Engine Hub
            ["GodotTitle"] = "🎮 جناح محرك الألعاب (Godot Engine Hub)",
            ["GodotIntro"] = "أدوات مخصصة لأتمتة مشاريع محرك جودو (Godot 4.x / 3.x) وتسريع دورة العمل اليومية:",
            ["Godot1Title"] = "1. التصدير التلقائي الصامت (Godot Headless Export)",
            ["Godot1a"] = "• الهدف: تصدير نسختك التنفيذية (.exe) بدون فتح محرر Godot اليدوي وبدون واجهات إضافية.",
            ["Godot1b"] = "• متى تستخدمه: عندما تجري تعديلات وتريد تصدير نسخة تجريبية سريعة بضغطة زر إلى مجلد build.",
            ["Godot1c"] = "• نصيحة: تأكد من أن اسم الـ Export Preset يطابق الاسم المعرف في export_presets.cfg (مثل Windows Desktop).",
            ["Godot2Title"] = "2. منظف الكاش العميق (Godot Deep Cache Nuker)",
            ["Godot2a"] = "• الهدف: حذف مجلد .godot الداخلي وإزالة أقفال الملفات المؤقتة (.lock) مع حساب المساحة المحررة.",
            ["Godot2b"] = "• متى تستخدمه: عند مواجهة أخطاء غريبة في الاستيراد (Import Issues) أو تلف الشيدرات أو كراش عند فتح المشروع.",
            ["Godot3Title"] = "3. فاحص سلامة السكربتات (GDScript Syntax Checker)",
            ["Godot3a"] = "• الهدف: تشغيل فحص سريع في وضع Headless لاكتشاف الأخطاء اللغوية أو المراجع المفقودة قبل رفع الكود على Git.",

            // 3. .NET & Git Hub
            ["DotNetTitle"] = "📦 جناح النشر وإدارة الأكواد (.NET & Git Hub)",
            ["DotNet1Title"] = "1. النشر المستقل (Single-File Publisher)",
            ["DotNet1a"] = "• الهدف: بناء ملف .exe مستقل ومضغوط بالكامل يعمل على ويندوز دون الحاجة لتثبيت .NET مسبقاً.",
            ["DotNet1b"] = "• متى تستخدمه: عند تجهيز البرامج لتسليمها للمستخدم النهائي كحزمة متكاملة وسريعة التشغيل.",
            ["DotNet2Title"] = "2. منظف مجلدات bin و obj (Disk Saver)",
            ["DotNet2a"] = "• الهدف: البحث الشجري في مجلد المشروع وحذف مخلفات البناء المؤقتة واسترجاع الجيجابايتات الضائعة.",
            ["DotNet2b"] = "• متى تستخدمه: دورياً لتوفير مساحة القرص، أو قبل أرشفة/ضغط مجلد الأكواد ورفعه.",
            ["DotNet3Title"] = "3. مدقق ملفات التعريب والترجمة (XML Localization Checker)",
            ["DotNet3a"] = "• الهدف: مقارنة ملف الترجمة الأساسي (en.xml) بملف التعريب (ar.xml) لكشف العبارات المنسية أو المفاتيح الفارغة.",

            // 4. Media Pipeline
            ["MediaTitle"] = "🎨 جناح الوسائط وتجهيز الأصول (Media Pipeline)",
            ["Media1Title"] = "1. صانع الأيقونات الشامل (Multi-Size ICO Generator)",
            ["Media1a"] = "• الهدف: تحويل أي صورة PNG/JPG إلى حزمة .ico رسمية متعددة الطبقات (16، 32، 48، 64، 128، 256 بكسل).",
            ["Media1b"] = "• متى تستخدمه: عند إنشاء أيقونة لتطبيق أو لعبة لتظهر حادة ونقية على شريط المهام وسطح المكتب بدون تشوه.",
            ["Media2Title"] = "2. محول الصوتيات ومضغط الفيديو للألعاب (FFmpeg Pipeline)",
            ["Media2a"] = "• تحويل ملفات WAV وMP3 إلى OGG Vorbis عالية النقاء لتقليل حجم أصول اللعبة الصوتية.",
            ["Media2b"] = "• ضغط مقاطع الفيديو لتقليل حجمها التخزيني مع الحفاظ على وضوح سينمائي ممتاز.",

            // 5. System & Utilities
            ["SystemTitle"] = "🛠️ جناح صيانة النظام وأدوات الألعاب (System & Utilities)",
            ["System1Title"] = "1. فك حظر ملفات ومودات DLL المحملة (Unblock DLLs)",
            ["System1a"] = "• الهدف: إزالة وسوم أمان ويندوز (Zone.Identifier Alternate Data Stream) عن ملفات DLL والمودات المحملة.",
            ["System1b"] = "• متى تستخدمه: عند تحميل إضافات GDExtension أو مودات ألعاب من الإنترنت ورفض ويندوز تشغيلها.",
            ["System2Title"] = "2. صائد ومنهي العمليات والبرامج المعلقة (Hung Process Killer)",
            ["System2a"] = "• جلب البرامج المتجمدة النشطة (🔄): انقر زر التحديث 🔄 لفحص كافة العمليات النشطة في ويندوز والتقاط النوافذ المعلقة التي لا تستجيب (Not Responding) تلقائياً.",
            ["System2b"] = "• القائمة السريعة لأشهر الألعاب (Presets): اختر فورياً من القائمة المنسدلة أشهر الألعاب والبيئات المتكررة (مثل Bannerlord, GTA5, Godot, dotnet) لتصفيتها بنقرة واحدة.",
            ["System2c"] = "• الإنهاء الجذري الفوري: يقوم بإنهاء شجرة العمليات المتجمدة بالكامل وتحرير موارد المعالج والذاكرة وإزالة أقفال الملفات العالقة دون الحاجة لفتح مدير المهام (Task Manager).",
            ["System3Title"] = "3. منظف سجلات الكراش والملفات المؤقتة (Crash Dump Cleaner)",
            ["System3a"] = "• الهدف: فحص وحذف ملفات تفريغ انهيار الألعاب (Crash Dumps .dmp) والسجلات المؤقتة لتوفير مساحة القرص.",
            ["System3b"] = "• متى تستخدمه: دورياً أو عند انخفاض مساحة قرص النظام (C:) بعد جلسات تطوير واختبار ألعاب طويلة.",

            // 6. Custom JSON Tools
            ["CustomTitle"] = "🧩 الأدوات المخصصة (Custom JSON Tools)",
            ["CustomIntro"] = "تعتمد OmniHub على معمارية ديناميكية بالكامل؛ يمكنك إضافة أي أداة تشغيل أو أتمتة عبر ملفات JSON دون الحاجة لكتابة سطر C# واحد أو إعادة بناء البرنامج.",
            ["CustomFilesTitle"] = "1. إضافة أداة بإنشاء ملف JSON في configs/tools/",
            ["CustomFilesBody"] = "لإضافة أداة جديدة فورياً، أنشئ ملفاً بصيغة `.tools.json` (مثلاً `mytool.tools.json`) داخل مجلد `configs/tools/`. يكتشف OmniHub الأداة تلقائياً عبر نظام Hot Reload ويقوم بإنشاء جناحها وعرضها فوراً.",
            ["CustomVariablesTitle"] = "2. ربط المدخلات بالأوامر عبر المتغيرات ${ParameterId}",
            ["CustomVariablesBody"] = "استخدم الرمز ${ParameterId} داخل حقول `executable` أو `arguments` أو `workingDirectory`. عند التشغيل، يستبدل OmniHub المتغير بالقيمة المدخلة من المستخدم تلقائياً.",
            ["CustomManagementTitle"] = "3. إدارة الأدوات والأجنحة من الواجهة الرسومية",
            ["CustomManagementBody"] = "يمكنك النقر على زر '+ أداة مخصصة' لإضافة مدخلات وأوامر الأداة عبر نافذة مرئية سهلة، أو زر '+ إضافة جناح' لإنشاء أجنحة جديدة، مع إمكانية تعديل الأداة أو حذفها أو نقلها بين الأجنحة بكل مرونة.",
            ["CustomBrowseTitle"] = "4. الوصول السريع لمجلد الإعدادات (configs/tools/)",
            ["CustomBrowseBody"] = "انقر زر المجلد 📂 في رأس الواجهة لفتح مجلد `configs/tools/` مباشرة في مستكشف ويندوز لفحص الملفات أو مشاركتها.",
            ["CustomExampleTitle"] = "5. مثال ملف JSON متكامل للأداة المخصصة",

            // 7. Online Tools Store
            ["OnlineTitle"] = "🌐 متجر الأدوات عبر الإنترنت (Online Tools Store)",
            ["OnlineIntro"] = "نافذة متجر سحابي تتيح لك استعراض وتنزيل تعريفات أدوات مجتمعية ورسمية بنقرة زر واحدة دون تحميل أو تشغيل أي برامج تنفيذية غير موثوقة.",
            ["OnlineCatalogTitle"] = "1. تصفح الكتالوج السحابي (Cloud Catalog)",
            ["OnlineCatalogBody"] = "يتم تحميل كتالوج الأدوات تلقائياً عند فتح نافذة المتجر مع تقسيم شبكي متجاوب من عمودين ومربع بحث فوري للوصول السريع إلى أي أداة.",
            ["OnlineInstallTitle"] = "2. التثبيت التلقائي بنقرة زر (1-Click Install & Hot Reload)",
            ["OnlineInstallBody"] = "اضغط زر 'تثبيت' بجانب أي أداة؛ يتم تنزيل ملف تعريف JSON فقط وحفظه داخل مجلد `configs/tools/online.<id>.tools.json`. يعيد OmniHub تحميل الواجهة فورياً (Hot Reload) لتظهر الأداة الجديدة في جناحها دون الحاجة لإعادة تشغيل البرنامج.",
            ["OnlineSecurityTitle"] = "3. الأمان والمستودعات الموثوقة (Security & Trust)",
            ["OnlineSecurityBody"] = "مستودع المطور الرسمي على GitHub موثق افتراضياً (Auto-Trusted) وتظهر عليه شارة الأمان الخضراء. تدعم المنصة فحص بصمة SHA-256 للتحقق من سلامة الملف المنزَّل. مهم جداً: لا يتم تنزيل أو تشغيل أي ملف تنفيذي (.exe) من الإنترنت — فقط تعريفات JSON الآمنة.",
            ["OnlineUpdateTitle"] = "4. التحديثات والعمل بدون إنترنت (Updates & Offline Mode)",
            ["OnlineUpdateBody"] = "إذا توفر إصدار أحدث للأداة في الكتالوج، يظهر ختم 'تحديث متوفر' لتحديث التعريف بضغطة زر. وفي حال انقطاع الإنترنت، يعمل المتجر في وضع عدم الاتصال (Offline Mode) بالاعتماد على الكاش المحلي.",
            ["OnlineTroubleshootTitle"] = "5. حل المشاكل الشائعة (Troubleshooting)",
            ["OnlineTroubleshootBody"] = "«تعذر تحميل الكتالوج»: تأكد من توفر اتصال بالإنترنت وصحة الرابط. «فشل التثبيت»: قد يكون الملف غير مكتمل أو فشل التحقق من بصمة SHA-256. في كافة الأحوال، لا يتعطل التطبيق ويعرض تنبيهاً نصياً واضحاً.",

            ["Footer"] = "OmniHub v1.0 • Developer & Gaming Omni-Hub • تم البناء بـ .NET 8 و WPF"
        };

    private static readonly IReadOnlyDictionary<string, string> English =
        new Dictionary<string, string>
        {
            ["Header"] = "OmniHub v1.0 Comprehensive User Manual",
            ["Close"] = "Close ✕ (Esc)",
            ["Topics"] = "MANUAL TOPICS",
            ["NavOverview"] = "Getting Started & Wings",
            ["NavGodot"] = "Godot Engine Hub",
            ["NavDotNet"] = ".NET & Git Hub",
            ["NavMedia"] = "Media & Assets",
            ["NavSystem"] = "System & Utilities",
            ["NavCustom"] = "Custom Tools (JSON)",
            ["NavOnline"] = "Online Tools Store",

            // 1. Overview & Shortcuts & Persistence
            ["OverviewTitle"] = "🚀 Welcome to OmniHub (Developer & Gaming Omni-Hub)",
            ["OverviewIntro"] = "OmniHub is a unified command center designed for everyday game development, software engineering, and system maintenance tasks, eliminating repetitive manual commands.",
            ["ShortcutsTitle"] = "⚡ Keyboard Shortcuts:",
            ["Shortcut1"] = "• Ctrl + K or Ctrl + P: Open/close the Command Palette to search and launch any tool instantly.",
            ["Shortcut2"] = "• F12: Toggle the embedded Live Terminal output pane at any time.",
            ["Shortcut3"] = "• Esc: Close modals, search dialogs, or the manual overlay.",
            ["WingsPersistenceTitle"] = "📁 Sidebar Wings & Automatic Path Persistence (%LOCALAPPDATA%\\OmniHub)",
            ["WingsPersistence1"] = "• Smart Path Memory: Whenever you select folder or file paths, OmniHub automatically persists your inputs to %LOCALAPPDATA%\\OmniHub\\user-state.json and restores them across app restarts, so you never have to re-browse.",
            ["WingsPersistence2"] = "• Responsive Dynamic Inputs: Supports FilePath, DirectoryPath, Text, Number, Dropdown options, and Checkboxes.",
            ["TerminalTitle"] = "💡 Live Terminal & Process Management",
            ["TerminalBody"] = "When you click 'Run Tool', the terminal pane opens automatically and streams process stdout/stderr live. You can terminate any running process at any time using the red 'Kill Process' button.",

            // 2. Godot Engine Hub
            ["GodotTitle"] = "🎮 Godot Engine Hub",
            ["GodotIntro"] = "Automate everyday Godot 4.x / 3.x project workflows and speed up development cycles:",
            ["Godot1Title"] = "1. Godot Headless Export",
            ["Godot1a"] = "• Goal: Export your executable (.exe) without opening the Godot editor or waiting on UI.",
            ["Godot1b"] = "• Use it when: You want a fast build directly into your build folder after code or asset changes.",
            ["Godot1c"] = "• Tip: Make sure your Export Preset name matches export_presets.cfg (e.g. Windows Desktop).",
            ["Godot2Title"] = "2. Godot Deep Cache Nuker",
            ["Godot2a"] = "• Goal: Clear the internal .godot cache and stale .lock files while calculating freed storage.",
            ["Godot2b"] = "• Use it when: Encountering bizarre import errors, shader glitch artifacts, or editor startup crashes.",
            ["Godot3Title"] = "3. GDScript Syntax Checker",
            ["Godot3a"] = "• Goal: Run a fast headless validation to catch syntax errors or broken references before committing to Git.",

            // 3. .NET & Git Hub
            ["DotNetTitle"] = "📦 .NET & Git Hub",
            ["DotNet1Title"] = "1. Single-File Publisher",
            ["DotNet1a"] = "• Goal: Produce a self-contained, compressed single-file .exe that runs without requiring a preinstalled .NET runtime.",
            ["DotNet1b"] = "• Use it when: Packaging production-ready software for end users.",
            ["DotNet2Title"] = "2. bin & obj Folder Cleaner (Disk Saver)",
            ["DotNet2a"] = "• Goal: Recursively scan solution directories and purge compiler build artifacts, reclaiming gigabytes of disk space.",
            ["DotNet2b"] = "• Use it periodically or before zipping/archiving source repositories.",
            ["DotNet3Title"] = "3. XML Localization Checker",
            ["DotNet3a"] = "• Goal: Compare base translation files (en.xml) with localized files (ar.xml) to catch missing strings or untranslated keys.",

            // 4. Media Pipeline
            ["MediaTitle"] = "🎨 Media & Asset Pipeline",
            ["Media1Title"] = "1. Multi-Size ICO Generator",
            ["Media1a"] = "• Goal: Convert any PNG/JPG image into a compliant Windows .ico containing 16, 32, 48, 64, 128, and 256px icon layers.",
            ["Media1b"] = "• Use it when: Crafting polished application or game icons for crisp display across Windows taskbar and desktop.",
            ["Media2Title"] = "2. FFmpeg Audio & Video Pipeline",
            ["Media2a"] = "• Convert WAV/MP3 files to high-fidelity OGG Vorbis to optimize game memory footprint.",
            ["Media2b"] = "• Compress cutscene and trailer video files with excellent visual fidelity.",

            // 5. System & Utilities
            ["SystemTitle"] = "🛠️ System & Game Utilities",
            ["System1Title"] = "1. Unblock Downloaded DLLs & Files",
            ["System1a"] = "• Goal: Strip the Windows 'Mark of the Web' (Zone.Identifier Alternate Data Stream) from downloaded DLLs and binaries.",
            ["System1b"] = "• Use it when: Third-party GDExtension plugins or C# assemblies fail to load due to Windows origin security blocks.",
            ["System2Title"] = "2. Hung & Frozen Process Killer",
            ["System2a"] = "• Scan Active Frozen Programs (🔄): Click the refresh button 🔄 to scan all running processes and detect windows marked as 'Not Responding' in real-time.",
            ["System2b"] = "• Quick Game Presets: Instantly pick popular game and development processes (e.g. Bannerlord, GTA5, Godot, dotnet) from the dropdown for 1-click filtering.",
            ["System2c"] = "• Clean Immediate Termination: Forcibly terminates the entire hung process tree, releasing CPU/RAM usage and stuck file handles without launching Task Manager.",
            ["System3Title"] = "3. Game Crash & Temp Log Cleaner",
            ["System3a"] = "• Goal: Purge stale mini-dump files (.dmp), crash reports, and engine temp logs to reclaim drive space.",
            ["System3b"] = "• Use it periodically or when disk storage gets tight after extended playtesting sessions.",

            // 6. Custom JSON Tools
            ["CustomTitle"] = "🧩 Custom JSON Tools",
            ["CustomIntro"] = "OmniHub is 100% dynamic config-driven; you can introduce new automation tools and wings simply with JSON definitions without modifying C# or XAML.",
            ["CustomFilesTitle"] = "1. Adding Tools by Creating a JSON file in configs/tools/",
            ["CustomFilesBody"] = "To add a new tool, create a `.tools.json` file (e.g. `mytool.tools.json`) inside `configs/tools/`. OmniHub instantly detects and hot-reloads it into its dedicated wing.",
            ["CustomVariablesTitle"] = "2. Binding Parameters with ${ParameterId}",
            ["CustomVariablesBody"] = "Use token references like `${ParameterId}` inside `executable`, `arguments`, or `workingDirectory`. OmniHub interpolates them at runtime with user inputs.",
            ["CustomManagementTitle"] = "3. Managing Tools & Wings from the UI",
            ["CustomManagementBody"] = "Use the '+ Custom Tool' button to configure tools visually, '+ Add Wing' to create dynamic wings, and edit, delete, or move tools between wings easily.",
            ["CustomBrowseTitle"] = "4. Quick Access to Configs Folder",
            ["CustomBrowseBody"] = "Click the 📂 folder button in the top bar to open `configs/tools/` directly in Windows File Explorer.",
            ["CustomExampleTitle"] = "5. Complete Custom Tool JSON Example",

            // 7. Online Tools Store
            ["OnlineTitle"] = "🌐 Online Tools Store",
            ["OnlineIntro"] = "Browse and install curated community and official tool definitions with 1 click without downloading or running untrusted binaries.",
            ["OnlineCatalogTitle"] = "1. Browsing the Cloud Catalog",
            ["OnlineCatalogBody"] = "The catalog is automatically fetched from the cloud repository upon opening the store, featuring a 2-column responsive layout and instant search.",
            ["OnlineInstallTitle"] = "2. 1-Click Installation & Hot Reload",
            ["OnlineInstallBody"] = "Click 'Install' on any tool to download its JSON definition into `configs/tools/online.<id>.tools.json`. OmniHub hot-reloads the UI immediately without restarting the application.",
            ["OnlineSecurityTitle"] = "3. Trusted Repositories & SHA-256 Verification",
            ["OnlineSecurityBody"] = "The official GitHub repository is trusted by default (Auto-Trusted) and displays a green security badge. SHA-256 hash checks verify downloaded file integrity. Important: OmniHub never downloads or runs executable binaries (.exe) from the web — only safe JSON definitions.",
            ["OnlineUpdateTitle"] = "4. Updates & Offline Cache Mode",
            ["OnlineUpdateBody"] = "When a newer tool version is published, an 'Update available' badge appears for 1-click updating. If internet access drops, the store seamlessly operates in Offline Mode via local cache.",
            ["OnlineTroubleshootTitle"] = "5. Troubleshooting",
            ["OnlineTroubleshootBody"] = "\"Could not load catalog\": Check your internet connection and catalog URL. \"Install failed\": Definition may be incomplete or failed SHA-256 validation. The application will never crash and always provides a helpful status notification.",

            ["Footer"] = "OmniHub v1.0 • Developer & Gaming Omni-Hub • Built with .NET 8 and WPF"
        };
}
