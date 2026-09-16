using CommunityToolkit.Mvvm.ComponentModel;
using OmniHub.Core.Models;

namespace OmniHub.UI.ViewModels;

public partial class OnlineToolItemViewModel : ObservableObject
{
    public OnlineToolCatalogItem Item { get; }
    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isArabic;
    [ObservableProperty] private string? _installedVersion;
    [ObservableProperty] private bool _isTrustedRepository;

    public OnlineToolItemViewModel(OnlineToolCatalogItem item, bool installed, bool isArabic, string? installedVersion = null, bool isTrustedRepository = false)
    {
        Item = item;
        IsInstalled = installed;
        IsArabic = isArabic;
        InstalledVersion = installedVersion;
        IsTrustedRepository = isTrustedRepository;
    }

    public string DisplayName
    {
        get
        {
            var title = IsArabic && !string.IsNullOrWhiteSpace(Item.NameAr)
                ? Item.NameAr
                : (!string.IsNullOrWhiteSpace(Item.Name) ? Item.Name : Item.Title);

            if (string.IsNullOrWhiteSpace(title))
            {
                title = !string.IsNullOrWhiteSpace(Item.Name) ? Item.Name : Item.Id;
            }

            return title;
        }
    }

    public string DisplayDescription
    {
        get
        {
            var desc = IsArabic && !string.IsNullOrWhiteSpace(Item.DescriptionAr)
                ? Item.DescriptionAr
                : Item.Description;

            return desc ?? "";
        }
    }

    /// <summary>
    /// Vector Glyph icon for Segoe Fluent Icons / Segoe MDL2 Assets based on category.
    /// </summary>
    public string GlyphIcon
    {
        get
        {
            var cat = (Item.Category ?? "").Trim().ToLowerInvariant();
            var id = (Item.Id ?? "").Trim().ToLowerInvariant();

            // Games & Godot: Game Controller ()
            if (cat.Contains("godot") || cat.Contains("game") || id.Contains("godot") || id.Contains("game"))
                return "\uE7FC";

            // .NET & Programming: Code Glyph ()
            if (cat.Contains("dotnet") || cat.Contains("code") || cat.Contains("git") || id.Contains("dotnet") || id.Contains("git") || id.Contains("xml") || id.Contains("script"))
                return "\uE943";

            // Media & Assets: Video/Audio ()
            if (cat.Contains("media") || cat.Contains("asset") || cat.Contains("audio") || cat.Contains("video") || id.Contains("media") || id.Contains("ico") || id.Contains("ogg") || id.Contains("ffmpeg"))
                return "\uE714";

            // System, Maintenance, & Processes: Settings/Repair ()
            if (cat.Contains("system") || cat.Contains("util") || cat.Contains("process") || id.Contains("system") || id.Contains("dll") || id.Contains("kill") || id.Contains("clean"))
                return "\uE713";

            // Default: Tools/Repair ()
            return "\uE90F";
        }
    }

    // Phase 2 - Version / Update.
    public bool UpdateAvailable => IsInstalled && !string.IsNullOrWhiteSpace(InstalledVersion) &&
        !string.Equals(InstalledVersion, Item.Version, StringComparison.OrdinalIgnoreCase);

    public string VersionLineText => IsArabic
        ? (UpdateAvailable ? $"مثبّت: {InstalledVersion} → متوفر: {Item.Version}" : $"الإصدار: {Item.Version}")
        : (UpdateAvailable ? $"Installed: {InstalledVersion} → Online: {Item.Version}" : $"Version: {Item.Version}");

    public string PublisherText => IsArabic ? $"الناشر: {(string.IsNullOrWhiteSpace(Item.Publisher) ? "غير معروف" : Item.Publisher)}"
                                             : $"Publisher: {(string.IsNullOrWhiteSpace(Item.Publisher) ? "Unknown" : Item.Publisher)}";

    public string TrustBadgeText => IsTrustedRepository
        ? (IsArabic ? "✅ مصدر موثوق" : "✅ Trusted Repository")
        : (IsArabic ? "⚠ مصدر غير موثوق" : "⚠ Unknown/Untrusted Repository");

    public bool HasSha256 => !string.IsNullOrWhiteSpace(Item.Sha256);
    public string Sha256Text => $"SHA-256: {Item.Sha256}";

    public string InstallText => IsInstalled
        ? (UpdateAvailable ? (IsArabic ? "تحديث متوفر" : "Update available") : (IsArabic ? "مثبّت" : "Installed"))
        : (IsArabic ? "تثبيت" : "Install");

    public string UpdateButtonText => IsArabic ? "تحديث متوفر" : "Update available";

    public bool CanInstall => (!IsInstalled || UpdateAvailable) && !IsBusy;
    public bool ShowInstallButton => !IsInstalled && !IsBusy;
    public bool ShowUpdateButton => IsInstalled && UpdateAvailable && !IsBusy;

    // Phase 2 - Uninstall.
    public string UninstallText => IsArabic ? "إلغاء التثبيت" : "Uninstall";
    public bool ShowUninstallButton => IsInstalled && !IsBusy;

    partial void OnIsArabicChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(InstallText));
        OnPropertyChanged(nameof(UpdateButtonText));
        OnPropertyChanged(nameof(VersionLineText));
        OnPropertyChanged(nameof(PublisherText));
        OnPropertyChanged(nameof(TrustBadgeText));
        OnPropertyChanged(nameof(UninstallText));
    }

    partial void OnIsInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(InstallText));
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(ShowInstallButton));
        OnPropertyChanged(nameof(ShowUninstallButton));
        OnPropertyChanged(nameof(ShowUpdateButton));
        OnPropertyChanged(nameof(UpdateAvailable));
        OnPropertyChanged(nameof(VersionLineText));
    }

    partial void OnInstalledVersionChanged(string? value)
    {
        OnPropertyChanged(nameof(UpdateAvailable));
        OnPropertyChanged(nameof(VersionLineText));
        OnPropertyChanged(nameof(InstallText));
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(ShowInstallButton));
        OnPropertyChanged(nameof(ShowUpdateButton));
        OnPropertyChanged(nameof(ShowUninstallButton));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(ShowInstallButton));
        OnPropertyChanged(nameof(ShowUpdateButton));
        OnPropertyChanged(nameof(ShowUninstallButton));
    }

    partial void OnIsTrustedRepositoryChanged(bool value) => OnPropertyChanged(nameof(TrustBadgeText));
}
