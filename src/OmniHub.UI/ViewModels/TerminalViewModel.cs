using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;

namespace OmniHub.UI.ViewModels;

public partial class TerminalViewModel : ObservableObject
{
    private readonly IProcessRunner _processRunner;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private int _warningCount;

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public event Action? OnLogAppended;

    public TerminalViewModel(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public void Append(LogEntry entry)
    {
        void AddLog()
        {
            Logs.Add(entry);
            if (entry.Level == LogLevel.Error) ErrorCount++;
            if (entry.Level == LogLevel.Warning) WarningCount++;

            // Cap logs at 3000 to prevent unbounded memory usage
            if (Logs.Count > 3000)
            {
                Logs.RemoveAt(0);
            }

            OnLogAppended?.Invoke();
        }

        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.InvokeAsync(AddLog);
        }
        else
        {
            AddLog();
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Logs.Clear();
        ErrorCount = 0;
        WarningCount = 0;
        StatusText = "Ready";
    }

    [RelayCommand]
    private void CopyLogs()
    {
        try
        {
            var text = string.Join(Environment.NewLine, Logs.Select(l => $"[{l.Timestamp:HH:mm:ss}] [{l.Level}] {l.Message}"));
            Clipboard.SetText(text);
        }
        catch { }
    }

    [RelayCommand]
    private void KillProcess()
    {
        _processRunner.KillCurrent();
        Append(new LogEntry("🛑 Cancellation requested by user.", LogLevel.Warning));
    }
}

