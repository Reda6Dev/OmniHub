using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OmniHub.UI.ViewModels;

public partial class CommandPaletteViewModel : ObservableObject
{
    private readonly Func<IEnumerable<ToolItemViewModel>> _allToolsProvider;
    private readonly Action<ToolItemViewModel> _onToolSelected;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ToolItemViewModel? _selectedTool;

    public ObservableCollection<ToolItemViewModel> FilteredTools { get; } = new();

    public CommandPaletteViewModel(
        Func<IEnumerable<ToolItemViewModel>> allToolsProvider,
        Action<ToolItemViewModel> onToolSelected)
    {
        _allToolsProvider = allToolsProvider;
        _onToolSelected = onToolSelected;
    }

    partial void OnSearchQueryChanged(string value)
    {
        Filter();
    }

    public void Refresh()
    {
        SearchQuery = string.Empty;
        Filter();
    }

    private void Filter()
    {
        FilteredTools.Clear();
        var all = _allToolsProvider.Invoke();

        var query = SearchQuery.Trim();
        var matches = string.IsNullOrWhiteSpace(query)
            ? all
            : all.Where(t =>
                t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Category.ToString().Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var item in matches.Take(20))
        {
            FilteredTools.Add(item);
        }

        SelectedTool = FilteredTools.FirstOrDefault();
    }

    [RelayCommand]
    private void ExecuteSelected()
    {
        if (SelectedTool != null)
        {
            _onToolSelected.Invoke(SelectedTool);
        }
    }
}

