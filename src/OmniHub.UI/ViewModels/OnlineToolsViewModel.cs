using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniHub.Core.Models;

namespace OmniHub.UI.ViewModels;

public partial class OnlineToolsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly HttpClient _http = CreateHttpClient();
    private readonly List<OnlineToolItemViewModel> _all = new();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("OmniHub/1.0");
        return client;
    }

    public ObservableCollection<OnlineToolItemViewModel> Tools { get; } = new();

    [ObservableProperty] private string _catalogUrl = "https://raw.githubusercontent.com/Reda6Dev/OmniHub-Tools/main/catalog.json";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isArabic = true;
    [ObservableProperty] private string _statusText = "";

    public string Title => IsArabic ? "🌐 الأدوات المتوفرة عبر الإنترنت" : "🌐 Online Tools";
    public string Subtitle => IsArabic ? "نزّل تعريف الأداة فقط. لا يتم تنزيل برامج تنفيذية." : "Download tool definitions only. No executables are downloaded.";
    public string CatalogLabel => IsArabic ? "رابط الكتالوج" : "Catalog URL";
    public string LoadText => IsLoading ? (IsArabic ? "جاري التحميل..." : "Loading..." ) : (IsArabic ? "تحديث الكتالوج" : "Refresh Catalog");
    public string SearchPlaceholder => IsArabic ? "ابحث عن أداة..." : "Search tools...";
    public string EmptyText => IsArabic ? "لا توجد أدوات مطابقة." : "No matching tools.";
    public string TrustButtonText => IsArabic ? "🔒 وثّق هذا المصدر" : "🔒 Trust this source";
    public string TrustButtonHint => IsArabic
        ? "أضف مضيف رابط الكتالوج الحالي إلى قائمة Repositories الموثوقة في الإعدادات."
        : "Adds the current catalog URL's host to the trusted repositories list in Settings.";

    public bool IsCurrentRepositoryTrusted =>
        Uri.TryCreate(CatalogUrl?.Trim(), UriKind.Absolute, out var uri) &&
        _main.IsRepositoryTrusted(uri.Host);

    public bool ShowTrustButton => !IsCurrentRepositoryTrusted;

    [ObservableProperty] private OnlineToolItemViewModel? _selectedToolForDetails;
    [ObservableProperty] private bool _isDetailsDialogOpen;

    public string DetailsDialogTitle => IsArabic ? "تفاصيل الأداة" : "Tool Details";
    public string CloseDetailsText => IsArabic ? "إغلاق" : "Close";

    [RelayCommand]
    public void ShowToolDetails(OnlineToolItemViewModel item)
    {
        SelectedToolForDetails = item;
        IsDetailsDialogOpen = true;
    }

    [RelayCommand]
    public void CloseToolDetails()
    {
        IsDetailsDialogOpen = false;
        SelectedToolForDetails = null;
    }

    public string LocalCatalogPath => Path.Combine(_main.ConfigsDirectory, "online", "catalog.json");
    private string CacheFilePath => Path.Combine(_main.ConfigsDirectory, "online", "catalog-cache.json");

    public OnlineToolsViewModel(MainViewModel main)
    {
        _main = main;
        if (_main.ConfigManager?.GlobalSettings != null &&
            (_main.ConfigManager.GlobalSettings.TryGetValue("CatalogUrl", out var customUrl) ||
             _main.ConfigManager.GlobalSettings.TryGetValue("OnlineCatalogUrl", out customUrl)) &&
            !string.IsNullOrWhiteSpace(customUrl))
        {
            _catalogUrl = customUrl.Trim();
        }
    }

    partial void OnCatalogUrlChanged(string value)
    {
        OnPropertyChanged(nameof(IsCurrentRepositoryTrusted));
        OnPropertyChanged(nameof(ShowTrustButton));
    }

    partial void OnIsArabicChanged(bool value)
    {
        foreach (var item in _all) item.IsArabic = value;
        OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(Subtitle)); OnPropertyChanged(nameof(CatalogLabel));
        OnPropertyChanged(nameof(LoadText)); OnPropertyChanged(nameof(SearchPlaceholder)); OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(TrustButtonText)); OnPropertyChanged(nameof(TrustButtonHint));
        OnPropertyChanged(nameof(DetailsDialogTitle)); OnPropertyChanged(nameof(CloseDetailsText));
    }

    [RelayCommand]
    private async Task TrustCurrentRepositoryAsync()
    {
        if (!Uri.TryCreate(CatalogUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            StatusText = IsArabic ? "أدخل رابط كتالوج HTTPS صحيحاً أولاً." : "Enter a valid HTTPS catalog URL first.";
            return;
        }

        await _main.AddTrustedRepositoryAsync(uri.Host);
        foreach (var item in _all) item.IsTrustedRepository = _main.IsRepositoryTrusted(item.Item.Repository);
        OnPropertyChanged(nameof(IsCurrentRepositoryTrusted));
        OnPropertyChanged(nameof(ShowTrustButton));
        StatusText = IsArabic ? $"تمت إضافة {uri.Host} إلى المصادر الموثوقة." : $"Added {uri.Host} to trusted repositories.";
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void PopulateCatalog(OnlineToolCatalog catalog, string repositoryHost = "official")
    {
        _all.Clear();
        foreach (var item in catalog.Tools.Where(IsValidCatalogItem))
        {
            if (string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.Title))
                item.Name = item.Title;
            if (string.IsNullOrWhiteSpace(item.NameAr) && !string.IsNullOrWhiteSpace(item.TitleAr))
                item.NameAr = item.TitleAr;
            if (string.IsNullOrWhiteSpace(item.Icon))
                item.Icon = "🌐";
            if (string.IsNullOrWhiteSpace(item.Repository)) item.Repository = repositoryHost;
            var installed = _main.IsToolInstalled(item.Id);
            var installedVersion = _main.GetInstalledToolVersion(item.Id);
            var trusted = _main.IsRepositoryTrusted(item.Repository);
            _all.Add(new OnlineToolItemViewModel(item, installed, IsArabic, installedVersion, trusted));
        }
        ApplyFilter();
        OnPropertyChanged(nameof(IsCurrentRepositoryTrusted));
        OnPropertyChanged(nameof(ShowTrustButton));
    }

    [RelayCommand]
    public async Task EnsureLoadedAsync()
    {
        if (_all.Count > 0)
        {
            RefreshInstalledStatus();
            return;
        }

        if (!IsLoading)
        {
            await LoadCatalogAsync();
        }
    }

    public static OnlineToolCatalog ParseCatalog(string json, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("Catalog JSON is empty.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 1. If root is a direct JSON Array: [ ... ]
        if (root.ValueKind == JsonValueKind.Array)
        {
            var list = JsonSerializer.Deserialize<List<OnlineToolCatalogItem>>(json, options) ?? new List<OnlineToolCatalogItem>();
            return new OnlineToolCatalog { Tools = list };
        }

        // 2. If root is a JSON Object: { "tools": [ ... ], ... }
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "tools", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    var toolsList = JsonSerializer.Deserialize<List<OnlineToolCatalogItem>>(prop.Value.GetRawText(), options) ?? new List<OnlineToolCatalogItem>();
                    int version = 1;
                    if (root.TryGetProperty("version", out var vProp) && vProp.TryGetInt32(out var vInt))
                        version = vInt;
                    return new OnlineToolCatalog { Version = version, Tools = toolsList };
                }
            }

            // Fallback: standard deserialization into OnlineToolCatalog
            var catalog = JsonSerializer.Deserialize<OnlineToolCatalog>(json, options);
            if (catalog != null) return catalog;
        }

        throw new InvalidDataException("Unrecognized catalog format (expected JSON array or object with 'tools' array).");
    }

    [RelayCommand]
    private async Task LoadCatalogAsync()
    {
        if (!Uri.TryCreate(CatalogUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            StatusText = IsArabic ? "رابط الكتالوج يجب أن يكون HTTPS صحيحاً." : "Catalog URL must be a valid HTTPS URL.";
            return;
        }

        IsLoading = true;
        StatusText = IsArabic ? "جاري جلب الكتالوج..." : "Fetching catalog...";
        try
        {
            using var response = await _http.GetAsync(uri!);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var options = JsonOptions();
            var catalog = ParseCatalog(json, options);
            if (catalog == null || catalog.Tools.Count == 0)
                throw new InvalidDataException("Catalog contains 0 tools.");

            PopulateCatalog(catalog, uri!.Host);
            StatusText = IsArabic ? $"تم تحميل {_all.Count} أداة." : $"Loaded {_all.Count} tool(s).";
            await SaveLocalCatalogAsync(json);
            await SaveCatalogCacheAsync(uri.ToString(), json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OmniHub Catalog Download/Parse Error]: {ex}");
            System.Diagnostics.Debug.WriteLine($"[OmniHub Catalog Download/Parse Error]: {ex}");

            // Offline fallback: load from configs/online/catalog.json
            var offlineCatalog = await TryLoadLocalCatalogAsync();
            if (offlineCatalog != null && offlineCatalog.Tools.Count > 0)
            {
                PopulateCatalog(offlineCatalog, "offline");
                var msg = "تعذر الاتصال بالسحابة، تم تحميل الكتالوج من الكاش المحلي (Offline Mode)";
                StatusText = msg;
                _main.Terminal?.Append(new LogEntry(msg, Core.Enums.LogLevel.Warning));
                return;
            }

            // Secondary fallback: check catalog-cache.json
            var cached = await TryLoadCatalogCacheAsync(uri?.ToString() ?? CatalogUrl ?? "");
            if (cached != null)
            {
                try
                {
                    var catalog = ParseCatalog(cached.Json, JsonOptions());
                    if (catalog != null && catalog.Tools.Count > 0)
                    {
                        PopulateCatalog(catalog, uri?.Host ?? "cache");
                        var msg = "تعذر الاتصال بالسحابة، تم تحميل الكتالوج من الكاش المحلي (Offline Mode)";
                        StatusText = msg;
                        _main.Terminal?.Append(new LogEntry(msg, Core.Enums.LogLevel.Warning));
                        return;
                    }
                }
                catch (Exception cacheEx)
                {
                    Console.WriteLine($"[OmniHub Cache Parse Error]: {cacheEx}");
                }
            }

            StatusText = IsArabic ? $"تعذر تحميل الكتالوج: {ex.Message}" : $"Could not load catalog: {ex.Message}";
            _main.Terminal?.Append(new LogEntry($"❌ Online catalog error: {ex.Message}", Core.Enums.LogLevel.Error));
            Tools.Clear();
        }
        finally { IsLoading = false; OnPropertyChanged(nameof(LoadText)); }
    }

    private async Task SaveLocalCatalogAsync(string json)
    {
        try
        {
            var dir = Path.GetDirectoryName(LocalCatalogPath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(LocalCatalogPath, json);
        }
        catch
        {
            // Best-effort caching
        }
    }

    private async Task<OnlineToolCatalog?> TryLoadLocalCatalogAsync()
    {
        try
        {
            if (!File.Exists(LocalCatalogPath)) return null;
            var json = await File.ReadAllTextAsync(LocalCatalogPath);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return ParseCatalog(json, JsonOptions());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OmniHub Local Catalog Error]: {ex}");
            return null;
        }
    }

    private async Task SaveCatalogCacheAsync(string url, string json)
    {
        try
        {
            var dir = Path.GetDirectoryName(CacheFilePath)!;
            Directory.CreateDirectory(dir);
            var envelope = new CatalogCacheEnvelope { Url = url, FetchedAtUtc = DateTime.UtcNow, Json = json };
            await File.WriteAllTextAsync(CacheFilePath, JsonSerializer.Serialize(envelope, JsonOptions()));
        }
        catch
        {
            // Caching is a best-effort convenience; a failure here must never block the catalog from loading.
        }
    }

    private async Task<CatalogCacheEnvelope?> TryLoadCatalogCacheAsync(string url)
    {
        try
        {
            if (!File.Exists(CacheFilePath)) return null;
            var text = await File.ReadAllTextAsync(CacheFilePath);
            var envelope = JsonSerializer.Deserialize<CatalogCacheEnvelope>(text, JsonOptions());
            if (envelope == null || string.IsNullOrWhiteSpace(envelope.Json)) return null;
            // Only reuse the cache when it matches the URL currently requested, so switching catalogs
            // never silently shows tools from a different, unrelated source.
            if (!string.IsNullOrWhiteSpace(url) && !string.Equals(envelope.Url, url, StringComparison.OrdinalIgnoreCase)) return null;
            return envelope;
        }
        catch
        {
            return null;
        }
    }

    [RelayCommand]
    private async Task InstallAsync(OnlineToolItemViewModel item)
    {
        if (item == null || !item.CanInstall) return;

        if (!Uri.TryCreate(item.Item.DefinitionUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            StatusText = IsArabic ? "رابط تعريف الأداة غير صالح." : "Tool definition URL is invalid.";
            return;
        }

        item.IsBusy = true;
        try
        {
            var options = JsonOptions();
            using var response = await _http.GetAsync(uri!);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(item.Item.Sha256))
            {
                var actualHash = ComputeSha256(json);
                if (!string.Equals(actualHash, item.Item.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(IsArabic ? "فشل التحقق من SHA-256؛ الملف قد يكون تالفاً أو غير أصلي." : "SHA-256 verification failed; the file may be corrupted or tampered with.");
            }

            var definitions = ParseDefinitions(json, options);
            if (definitions.Count == 0) throw new InvalidDataException("No tool definitions found.");
            foreach (var definition in definitions)
            {
                if (definitions.Count == 1 && string.IsNullOrWhiteSpace(definition.Id) && !string.IsNullOrWhiteSpace(item.Item.Id))
                {
                    definition.Id = item.Item.Id;
                }

                if (string.IsNullOrWhiteSpace(definition.Title) && string.IsNullOrWhiteSpace(definition.Name))
                {
                    if (!string.IsNullOrWhiteSpace(item.Item.Title))
                        definition.Title = item.Item.Title;
                    else if (!string.IsNullOrWhiteSpace(item.Item.Name))
                        definition.Name = item.Item.Name;
                    else if (!string.IsNullOrWhiteSpace(item.DisplayName))
                        definition.Title = item.DisplayName;
                }

                if (string.IsNullOrWhiteSpace(definition.Category) && !string.IsNullOrWhiteSpace(item.Item.Category))
                {
                    definition.Category = item.Item.Category;
                }

                if (!IsSafeDefinition(definition, out var validationError))
                    throw new InvalidDataException(validationError);
            }

            var toolsDir = Path.Combine(_main.ConfigsDirectory, "tools");
            Directory.CreateDirectory(toolsDir);
            foreach (var definition in definitions)
            {
                if (definitions.Count > 1 && !string.Equals(definition.Id, item.Item.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Stamp version/publisher/repository so future catalog refreshes can detect updates.
                if (string.IsNullOrWhiteSpace(definition.Version)) definition.Version = item.Item.Version;
                if (string.IsNullOrWhiteSpace(definition.Publisher)) definition.Publisher = item.Item.Publisher;
                if (string.IsNullOrWhiteSpace(definition.Title)) definition.Title = !string.IsNullOrWhiteSpace(definition.Name) ? definition.Name : item.DisplayName;
                if (string.IsNullOrWhiteSpace(definition.Name)) definition.Name = definition.Title;
                definition.SourceRepository = item.Item.Repository;
                definition.IsCustom = true;
                var safeId = SanitizeFileName(definition.Id);
                var path = Path.Combine(toolsDir, $"online.{safeId}.tools.json");
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new[] { definition }, options));
            }

            item.IsInstalled = true;
            item.InstalledVersion = item.Item.Version;
            StatusText = IsArabic ? $"تم تثبيت «{item.DisplayName}» (الإصدار {item.Item.Version})." : $"Installed “{item.DisplayName}” (v{item.Item.Version}).";
            await _main.InitializeAsync();
        }
        catch (Exception ex)
        {
            StatusText = IsArabic ? $"فشل التثبيت: {ex.Message}" : $"Installation failed: {ex.Message}";
        }
        finally { item.IsBusy = false; }
    }

    [RelayCommand]
    private async Task UninstallAsync(OnlineToolItemViewModel item)
    {
        if (item == null || !item.IsInstalled || item.IsBusy) return;

        item.IsBusy = true;
        try
        {
            var toolsDir = Path.Combine(_main.ConfigsDirectory, "tools");
            var safeId = SanitizeFileName(item.Item.Id);
            var path = Path.Combine(toolsDir, $"online.{safeId}.tools.json");

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(toolsDir))
            {
                var options = JsonOptions();
                foreach (var file in Directory.GetFiles(toolsDir, "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var list = JsonSerializer.Deserialize<List<ToolDefinition>>(json, options);
                        if (list != null && list.Any(t => string.Equals(t.Id, item.Item.Id, StringComparison.OrdinalIgnoreCase)))
                        {
                            list.RemoveAll(t => string.Equals(t.Id, item.Item.Id, StringComparison.OrdinalIgnoreCase));
                            if (list.Count == 0)
                                File.Delete(file);
                            else
                                await File.WriteAllTextAsync(file, JsonSerializer.Serialize(list, options));
                            break;
                        }

                        var single = JsonSerializer.Deserialize<ToolDefinition>(json, options);
                        if (single != null && string.Equals(single.Id, item.Item.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(file);
                            break;
                        }
                    }
                    catch { }
                }
            }

            item.IsInstalled = false;
            item.InstalledVersion = null;
            StatusText = IsArabic ? $"تم إلغاء تثبيت «{item.DisplayName}»." : $"Uninstalled “{item.DisplayName}”.";
            await _main.InitializeAsync();
        }
        catch (Exception ex)
        {
            StatusText = IsArabic ? $"تعذر إلغاء التثبيت: {ex.Message}" : $"Could not uninstall: {ex.Message}";
        }
        finally { item.IsBusy = false; }
    }

    public void RefreshInstalledStatus()
    {
        foreach (var item in _all)
        {
            item.IsInstalled = _main.IsToolInstalled(item.Item.Id);
            item.InstalledVersion = _main.GetInstalledToolVersion(item.Item.Id);
        }
    }

    private void ApplyFilter()
    {
        var q = SearchText?.Trim() ?? "";
        Tools.Clear();
        foreach (var item in _all.Where(x => string.IsNullOrWhiteSpace(q) ||
                     x.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     x.DisplayDescription.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     x.Item.Category.Contains(q, StringComparison.OrdinalIgnoreCase))) Tools.Add(item);
    }

    private static bool IsValidCatalogItem(OnlineToolCatalogItem x) =>
        !string.IsNullOrWhiteSpace(x.Id) &&
        (!string.IsNullOrWhiteSpace(x.Name) || !string.IsNullOrWhiteSpace(x.Title)) &&
        !string.IsNullOrWhiteSpace(x.DefinitionUrl);

    /// <summary>
    /// Performs security and required-fields validation on a tool definition.
    /// Checks mandatory fields: Id, Title, Category.
    /// Returns detailed error message when validation fails.
    /// </summary>
    public static bool IsSafeDefinition(ToolDefinition tool, out string errorMessage)
    {
        if (tool == null)
        {
            errorMessage = "فشل التحقق: كائن تعريف الأداة غير صالح (null)";
            return false;
        }

        var toolId = string.IsNullOrWhiteSpace(tool.Id) ? "(غير محدد)" : tool.Id;

        if (string.IsNullOrWhiteSpace(tool.Id))
        {
            errorMessage = "فشل التحقق: الحقل 'Id' مفقود في تعريف الأداة";
            return false;
        }

        if (string.IsNullOrWhiteSpace(tool.Title) && string.IsNullOrWhiteSpace(tool.Name))
        {
            errorMessage = $"فشل التحقق: الحقل 'Title' مفقود في تعريف الأداة '{toolId}'";
            return false;
        }

        if (string.IsNullOrWhiteSpace(tool.Category))
        {
            errorMessage = $"فشل التحقق: الحقل 'Category' مفقود في تعريف الأداة '{toolId}'";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public static bool IsSafeDefinition(ToolDefinition tool) => IsSafeDefinition(tool, out _);

    private static string? ValidateDefinition(ToolDefinition x)
    {
        return IsSafeDefinition(x, out var error) ? null : error;
    }

    private static List<ToolDefinition> ParseDefinitions(string json, JsonSerializerOptions options)
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<ToolDefinition>>(json, options);
            if (list != null) return list;
        }
        catch { }
        var single = JsonSerializer.Deserialize<ToolDefinition>(json, options);
        return single == null ? new List<ToolDefinition>() : new List<ToolDefinition> { single };
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(result) ? "tool" : result;
    }
}

/// <summary>Phase 2 - on-disk envelope for the last successfully fetched catalog, used for offline fallback.</summary>
public class CatalogCacheEnvelope
{
    public string Url { get; set; } = string.Empty;
    public DateTime FetchedAtUtc { get; set; }
    public string Json { get; set; } = string.Empty;
}
