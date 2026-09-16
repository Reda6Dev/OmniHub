using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OmniHub.Core.Enums;

namespace OmniHub.UI.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(16, 185, 129));   // #10B981
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(239, 68, 68));       // #EF4444
    private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(245, 158, 11));    // #F59E0B
    private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(6, 182, 212));        // #06B6D4
    private static readonly SolidColorBrush StandardBrush = new(Color.FromRgb(203, 213, 225));  // #CBD5E1

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Success => SuccessBrush,
                LogLevel.Error => ErrorBrush,
                LogLevel.Warning => WarningBrush,
                LogLevel.Info => InfoBrush,
                _ => StandardBrush
            };
        }

        return StandardBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

