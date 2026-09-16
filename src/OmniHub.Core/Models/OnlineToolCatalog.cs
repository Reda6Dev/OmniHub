using System.Text.Json.Serialization;

namespace OmniHub.Core.Models;

public class OnlineToolCatalog
{
    public int Version { get; set; } = 1;

    [JsonPropertyName("catalogVersion")]
    public string CatalogVersionString
    {
        get => Version.ToString();
        set
        {
            if (int.TryParse(value, out var v)) Version = v;
        }
    }

    public List<OnlineToolCatalogItem> Tools { get; set; } = new();
}

public class OnlineToolCatalogItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title
    {
        get => Name;
        set
        {
            if (string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(value))
                Name = value;
        }
    }

    public string NameAr { get; set; } = string.Empty;

    [JsonPropertyName("titleAr")]
    public string TitleAr
    {
        get => NameAr;
        set
        {
            if (string.IsNullOrWhiteSpace(NameAr) && !string.IsNullOrWhiteSpace(value))
                NameAr = value;
        }
    }

    [JsonPropertyName("title_ar")]
    public string TitleArAlt
    {
        get => NameAr;
        set
        {
            if (string.IsNullOrWhiteSpace(NameAr) && !string.IsNullOrWhiteSpace(value))
                NameAr = value;
        }
    }

    [JsonPropertyName("name_ar")]
    public string NameArAlt
    {
        get => NameAr;
        set
        {
            if (string.IsNullOrWhiteSpace(NameAr) && !string.IsNullOrWhiteSpace(value))
                NameAr = value;
        }
    }

    public string Category { get; set; } = "Custom";

    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc
    {
        get => Description;
        set
        {
            if (string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(value))
                Description = value;
        }
    }

    public string DescriptionAr { get; set; } = string.Empty;

    [JsonPropertyName("description_ar")]
    public string DescriptionArAlt
    {
        get => DescriptionAr;
        set
        {
            if (string.IsNullOrWhiteSpace(DescriptionAr) && !string.IsNullOrWhiteSpace(value))
                DescriptionAr = value;
        }
    }

    [JsonPropertyName("descAr")]
    public string DescAr
    {
        get => DescriptionAr;
        set
        {
            if (string.IsNullOrWhiteSpace(DescriptionAr) && !string.IsNullOrWhiteSpace(value))
                DescriptionAr = value;
        }
    }

    public string DefinitionUrl { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string UrlAlt
    {
        get => DefinitionUrl;
        set
        {
            if (string.IsNullOrWhiteSpace(DefinitionUrl) && !string.IsNullOrWhiteSpace(value))
                DefinitionUrl = value;
        }
    }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrlAlt
    {
        get => DefinitionUrl;
        set
        {
            if (string.IsNullOrWhiteSpace(DefinitionUrl) && !string.IsNullOrWhiteSpace(value))
                DefinitionUrl = value;
        }
    }

    [JsonPropertyName("download_url")]
    public string DownloadUrlAlt2
    {
        get => DefinitionUrl;
        set
        {
            if (string.IsNullOrWhiteSpace(DefinitionUrl) && !string.IsNullOrWhiteSpace(value))
                DefinitionUrl = value;
        }
    }

    [JsonPropertyName("definition_url")]
    public string DefinitionUrlAlt
    {
        get => DefinitionUrl;
        set
        {
            if (string.IsNullOrWhiteSpace(DefinitionUrl) && !string.IsNullOrWhiteSpace(value))
                DefinitionUrl = value;
        }
    }

    public string Version { get; set; } = "1.0.0";
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string AuthorAlt
    {
        get => Publisher;
        set
        {
            if (string.IsNullOrWhiteSpace(Publisher) && !string.IsNullOrWhiteSpace(value))
                Publisher = value;
        }
    }

    [JsonPropertyName("creator")]
    public string CreatorAlt
    {
        get => Publisher;
        set
        {
            if (string.IsNullOrWhiteSpace(Publisher) && !string.IsNullOrWhiteSpace(value))
                Publisher = value;
        }
    }

    public string Icon { get; set; } = "🌐";

    // Phase 2 - security/versioning metadata.
    // Sha256 is optional; when present it is verified against the downloaded definition JSON.
    public string Sha256 { get; set; } = string.Empty;
    // Repository host this catalog entry originates from (derived at load time if not set by the catalog author).
    public string Repository { get; set; } = string.Empty;
}
