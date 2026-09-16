using System.Windows;
using System.Windows.Input;
using OmniHub.UI.ViewModels;

namespace OmniHub.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.IsCommandPaletteOpen) or nameof(MainViewModel.IsQuickLauncherOpen))
            {
                if (_viewModel.IsCommandPaletteOpen)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        PaletteOverlayControl.FocusSearchBox();
                    }, System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        };

        Loaded += async (s, e) =>
        {
            await _viewModel.InitializeAsync();
        };
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_viewModel.IsCommandPaletteOpen)
            {
                _viewModel.IsCommandPaletteOpen = false;
                e.Handled = true;
            }
            else if (_viewModel.IsUserManualOpen)
            {
                _viewModel.IsUserManualOpen = false;
                e.Handled = true;
            }
        }
    }

    private void OnPaletteCloseRequested(object sender, RoutedEventArgs e)
    {
        _viewModel.IsCommandPaletteOpen = false;
    }

    private void OnManualCloseRequested(object sender, RoutedEventArgs e)
    {
        _viewModel.IsUserManualOpen = false;
    }

    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (MaximizeRestoreIcon != null)
        {
            MaximizeRestoreIcon.Text = WindowState == WindowState.Maximized ? "❐" : "🗖";
        }
    }
}