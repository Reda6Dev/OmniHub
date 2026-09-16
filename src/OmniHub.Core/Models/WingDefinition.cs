namespace OmniHub.Core.Models;

/// <summary>Metadata for a sidebar wing. User-created wings are stored as *.wing.json.</summary>
public class WingDefinition
{
    public string Id { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Icon { get; set; } = "🧩";
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }
}
