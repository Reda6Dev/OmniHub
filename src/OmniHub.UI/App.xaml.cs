using System.Windows;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OmniHub.Core.Interfaces;
using OmniHub.Engine.Extensions;
using OmniHub.Tools.Extensions;
using OmniHub.UI.ViewModels;

namespace OmniHub.UI;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterGlobalExceptionLogging();
        try
        {
            base.OnStartup(e);

        var services = new ServiceCollection();

        // Register Engine & Core Services
        services.AddOmniHubEngine();

        // Register Internal Native Tool Handlers
        services.AddOmniHubTools();

        // Register UI ViewModels & Views
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Connect Handlers to ToolRegistry
        var registry = _serviceProvider.GetRequiredService<IToolRegistry>();
        var handlers = _serviceProvider.GetServices<IToolHandler>();
        registry.RegisterToolHandlers(handlers);

        // Show Main Window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            LogException("OnStartup", ex);
            MessageBox.Show(
                $"OmniHub crashed during startup.\n\n{ex.GetType().FullName}: {ex.Message}\n\nA detailed log was saved to:\n{GetLogPath()}",
                "OmniHub Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private void RegisterGlobalExceptionLogging()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            LogException("DispatcherUnhandledException", e.Exception);
            e.Handled = true;
            MessageBox.Show(
                $"OmniHub encountered an unhandled UI error.\n\n{e.Exception.GetType().FullName}: {e.Exception.Message}\n\nDetailed log:\n{GetLogPath()}",
                "OmniHub Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                LogException("AppDomain.UnhandledException", ex);
            else
                LogText("AppDomain.UnhandledException", e.ExceptionObject?.ToString() ?? "Unknown exception object");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    private static string GetLogPath() => Path.Combine(AppContext.BaseDirectory, "omnihub-startup-error.log");

    private static void LogException(string source, Exception ex)
    {
        LogText(source, ex.ToString());
    }

    private static void LogText(string source, string text)
    {
        try
        {
            File.AppendAllText(
                GetLogPath(),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}{Environment.NewLine}{text}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}");
        }
        catch
        {
            // Never let diagnostic logging cause another crash.
        }
    }

    public static void ApplyLanguage(bool isArabic)
    {
        if (Current == null) return;
        var cultureCode = isArabic ? "ar" : "en";
        var dictUri = new Uri($"Resources/Strings.{cultureCode}.xaml", UriKind.Relative);

        var merged = Current.Resources.MergedDictionaries;
        var existingDict = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));
        if (existingDict != null)
        {
            merged.Remove(existingDict);
        }

        try
        {
            var newDict = new ResourceDictionary { Source = dictUri };
            merged.Add(newDict);
        }
        catch
        {
            // Fallback if needed
        }
    }
}
