using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OmniHub.UI.Components;

public partial class UserManualOverlay : UserControl
{
    public static readonly RoutedEvent CloseRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(CloseRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(UserManualOverlay));

    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    public UserManualOverlay()
    {
        InitializeComponent();
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (ContentScrollViewer == null || SectionOverview == null)
            return;

        SectionOverview.Visibility = NavOverview.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SectionGodot.Visibility = NavGodot.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SectionDotNet.Visibility = NavDotNet.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SectionMedia.Visibility = NavMedia.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SectionSystem.Visibility = NavSystem.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SectionCustom.Visibility = NavCustom.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SectionOnline.Visibility = NavOnline.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        ContentScrollViewer.ScrollToTop();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        RaiseClose();
    }

    private void OnBackdropMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == sender)
        {
            RaiseClose();
        }
    }

    private void RaiseClose()
    {
        RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
    }
}

