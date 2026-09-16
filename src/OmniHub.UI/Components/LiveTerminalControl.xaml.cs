using System.Windows.Controls;
using OmniHub.UI.ViewModels;

namespace OmniHub.UI.Components;

public partial class LiveTerminalControl : UserControl
{
    public LiveTerminalControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TerminalViewModel oldVm)
        {
            oldVm.OnLogAppended -= HandleLogAppended;
        }

        if (e.NewValue is TerminalViewModel newVm)
        {
            newVm.OnLogAppended += HandleLogAppended;
        }
    }

    private void HandleLogAppended()
    {
        if (DataContext is TerminalViewModel { AutoScroll: true } && LogsListBox.Items.Count > 0)
        {
            LogsListBox.ScrollIntoView(LogsListBox.Items[^1]);
        }
    }
}

