using CommunityToolkit.Mvvm.ComponentModel;

namespace OmniHub.UI.ViewModels;

/// <summary>
/// Presentation model for a sidebar wing discovered from tool JSON configuration.
/// </summary>
public partial class WingItemViewModel : ObservableObject
{
    public string Id { get; }

    [ObservableProperty]
    private string _titleEn;

    [ObservableProperty]
    private string _titleAr;

    [ObservableProperty]
    private string _icon;

    [ObservableProperty]
    private string _subtitleEn;

    [ObservableProperty]
    private string _subtitleAr;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isBuiltIn;

    [ObservableProperty]
    private bool _isArabic;

    public WingItemViewModel(
        string id,
        string titleEn,
        string titleAr,
        string icon,
        string subtitleEn = "",
        string subtitleAr = "")
    {
        Id = id;
        TitleEn = string.IsNullOrWhiteSpace(titleEn) ? id : titleEn;
        TitleAr = string.IsNullOrWhiteSpace(titleAr) ? TitleEn : titleAr;
        Icon = string.IsNullOrWhiteSpace(icon) ? "🧩" : icon;
        SubtitleEn = subtitleEn;
        SubtitleAr = subtitleAr;
    }

    public string DisplayTitle => IsArabic ? TitleAr : TitleEn;
    public string DisplaySubtitle => IsArabic ? SubtitleAr : SubtitleEn;

    /// <summary>
    /// Vector Glyph icon for Segoe Fluent Icons / Segoe MDL2 Assets based on Wing ID.
    /// </summary>
    public string GlyphIcon
    {
        get
        {
            var id = (Id ?? "").Trim().ToLowerInvariant();
            if (id.Contains("godot") || id.Contains("game")) return "\uE7FC";
            if (id.Contains("dotnet") || id.Contains("code") || id.Contains("git")) return "\uE943";
            if (id.Contains("media") || id.Contains("asset")) return "\uE714";
            if (id.Contains("system") || id.Contains("util")) return "\uE713";
            return "\uE90F";
        }
    }

    partial void OnIsArabicChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(DisplaySubtitle));
    }
}
