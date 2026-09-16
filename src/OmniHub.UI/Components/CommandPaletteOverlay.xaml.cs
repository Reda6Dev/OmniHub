using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OmniHub.UI.ViewModels;

namespace OmniHub.UI.Components;

public partial class CommandPaletteOverlay : UserControl
{
    public static readonly RoutedEvent CloseRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(CloseRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CommandPaletteOverlay));

    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    public CommandPaletteOverlay()
    {
        InitializeComponent();
        IsVisibleChanged += (s, e) =>
        {
            if (IsVisible && Visibility == Visibility.Visible)
            {
                FocusSearchBox();
            }
        };
    }

    public void FocusSearchBox()
    {
        Dispatcher.InvokeAsync(() =>
        {
            SearchBox.Focus();
            Keyboard.Focus(SearchBox);
            SearchBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void OnOverlayPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RaiseClose();
            e.Handled = true;
        }
    }

    private void OnBackdropPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            if (DialogBorder != null && (source == DialogBorder || DialogBorder.IsAncestorOf(source)))
            {
                return;
            }

            RaiseClose();
            e.Handled = true;
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        RaiseClose();
    }

    private void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || (e.Key == Key.K && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
        {
            RaiseClose();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (ResultsList.Items.Count > 0)
            {
                ResultsList.Focus();
                if (ResultsList.SelectedIndex < ResultsList.Items.Count - 1)
                {
                    ResultsList.SelectedIndex++;
                }
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ExecuteSelectedItem();
            e.Handled = true;
        }
    }

    private void OnListKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RaiseClose();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ExecuteSelectedItem();
            e.Handled = true;
        }
    }

    private void ExecuteSelectedItem()
    {
        if (DataContext is CommandPaletteViewModel vm && vm.SelectedTool != null)
        {
            vm.ExecuteSelectedCommand.Execute(null);
        }
    }

    private void RaiseClose()
    {
        Visibility = Visibility.Collapsed;
        RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
    }
}

