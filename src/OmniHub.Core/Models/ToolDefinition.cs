using System.Text.Json.Serialization;
using OmniHub.Core.Enums;

namespace OmniHub.Core.Models;

public class ToolDefinition
{
    public string Id { get; set; } = string.Empty;
    private string _title = string.Empty;
    private string _name = string.Empty;

    public string Title
    {
        get => !string.IsNullOrWhiteSpace(_title) ? _title : _name;
        set
        {
            _title = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_name))
                _name = _title;
        }
    }

    [JsonPropertyName("name")]
    public string Name
    {
        get => !string.IsNullOrWhiteSpace(_name) ? _name : _title;
        set
        {
            _name = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_title))
                _title = _name;
        }
    }

    private string _titleAr = string.Empty;
    private string _nameAr = string.Empty;

    [JsonPropertyName("titleAr")]
    public string TitleAr
    {
        get => !string.IsNullOrWhiteSpace(_titleAr) ? _titleAr : _nameAr;
        set
        {
            _titleAr = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_nameAr))
                _nameAr = _titleAr;
        }
    }

    [JsonPropertyName("nameAr")]
    public string NameAr
    {
        get => !string.IsNullOrWhiteSpace(_nameAr) ? _nameAr : _titleAr;
        set
        {
            _nameAr = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_titleAr))
                _titleAr = _nameAr;
        }
    }

    [JsonPropertyName("title_ar")]
    public string TitleArAlt
    {
        get => TitleAr;
        set { if (!string.IsNullOrEmpty(value)) TitleAr = value; }
    }

    [JsonPropertyName("name_ar")]
    public string NameArAlt
    {
        get => NameAr;
        set { if (!string.IsNullOrEmpty(value)) NameAr = value; }
    }

    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string DescAlt
    {
        get => Description;
        set { if (!string.IsNullOrEmpty(value)) Description = value; }
    }

    [JsonPropertyName("descriptionAr")]
    public string DescriptionAr { get; set; } = string.Empty;

    [JsonPropertyName("description_ar")]
    public string DescriptionArAlt
    {
        get => DescriptionAr;
        set { if (!string.IsNullOrEmpty(value)) DescriptionAr = value; }
    }

    [JsonPropertyName("descAr")]
    public string DescArAlt
    {
        get => DescriptionAr;
        set { if (!string.IsNullOrEmpty(value)) DescriptionAr = value; }
    }

    [JsonPropertyName("desc_ar")]
    public string DescArAlt2
    {
        get => DescriptionAr;
        set { if (!string.IsNullOrEmpty(value)) DescriptionAr = value; }
    }
    // Category is a free-form ID. New categories can be introduced from JSON without code changes.
    public string Category { get; set; } = "Custom";

    // Optional metadata used to build the dynamic sidebar wing list.
    public string CategoryAr { get; set; } = string.Empty;
    public string CategoryEn { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = "🧩";
    public string CategoryDescriptionEn { get; set; } = string.Empty;
    public string CategoryDescriptionAr { get; set; } = string.Empty;
    public ExecutionType ExecutionType { get; set; } = ExecutionType.ExternalProcess;
    public string Executable { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string InternalToolHandlerId { get; set; } = string.Empty;
    public bool RequiresAdmin { get; set; } = false;

    // Phase 2 - Online Tools security/versioning metadata (definition-level, not executable-level).
    public string Version { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string SourceRepository { get; set; } = string.Empty;
    public string IconKey { get; set; } = "Code";
    [JsonPropertyName("isCustom")]
    public bool IsCustom { get; set; }
    public List<ToolParameter> Parameters { get; set; } = new();
    public ToolGuideInfo? Guide { get; set; }
}

