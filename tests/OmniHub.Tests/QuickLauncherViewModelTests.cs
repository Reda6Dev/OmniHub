using Microsoft.Extensions.DependencyInjection;
using OmniHub.Core.Interfaces;
using OmniHub.Engine.Extensions;
using OmniHub.Tools.Extensions;
using OmniHub.UI.ViewModels;
using Xunit;

namespace OmniHub.Tests;

public class QuickLauncherViewModelTests
{
    private MainViewModel CreateViewModel()
    {
        var services = new ServiceCollection();
        services.AddOmniHubEngine();
        services.AddOmniHubTools();
        services.AddSingleton<MainViewModel>();

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IToolRegistry>();
        var handlers = provider.GetServices<IToolHandler>();
        registry.RegisterToolHandlers(handlers);

        var vm = provider.GetRequiredService<MainViewModel>();
        // Wings/tools are populated from configs/ during InitializeAsync; load them synchronously
        // so tests can assert on Wings, CurrentWingTools, etc. right after construction.
        vm.InitializeAsync().GetAwaiter().GetResult();
        return vm;
    }

    [Fact]
    public void InitialState_CommandPaletteAndQuickLauncher_ShouldBeClosedByDefault()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsCommandPaletteOpen, "IsCommandPaletteOpen should be false on startup.");
        Assert.False(vm.IsQuickLauncherOpen, "IsQuickLauncherOpen should be false on startup.");
    }

    [Fact]
    public void TogglePaletteCommand_ShouldToggleOpenAndCloseState()
    {
        var vm = CreateViewModel();

        // 1. Initially closed
        Assert.False(vm.IsCommandPaletteOpen);
        Assert.False(vm.IsQuickLauncherOpen);

        // 2. First toggle: should open
        vm.TogglePaletteCommand.Execute(null);
        Assert.True(vm.IsCommandPaletteOpen, "First toggle should open the palette.");
        Assert.True(vm.IsQuickLauncherOpen, "IsQuickLauncherOpen should be true when palette is open.");

        // 3. Second toggle: should close
        vm.TogglePaletteCommand.Execute(null);
        Assert.False(vm.IsCommandPaletteOpen, "Second toggle should close the palette.");
        Assert.False(vm.IsQuickLauncherOpen, "IsQuickLauncherOpen should be false when palette is closed.");
    }

    [Fact]
    public void ClosePaletteCommand_ShouldCloseWhenOpen()
    {
        var vm = CreateViewModel();

        vm.TogglePaletteCommand.Execute(null);
        Assert.True(vm.IsCommandPaletteOpen);

        vm.ClosePaletteCommand.Execute(null);
        Assert.False(vm.IsCommandPaletteOpen);
        Assert.False(vm.IsQuickLauncherOpen);
    }

    [Fact]
    public void IsQuickLauncherOpen_PropertySetter_ShouldSyncWithIsCommandPaletteOpen()
    {
        var vm = CreateViewModel();
        var notifiedProperties = new List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                notifiedProperties.Add(e.PropertyName);
        };

        vm.IsQuickLauncherOpen = true;

        Assert.True(vm.IsCommandPaletteOpen);
        Assert.True(vm.IsQuickLauncherOpen);
        Assert.Contains(nameof(MainViewModel.IsCommandPaletteOpen), notifiedProperties);
        Assert.Contains(nameof(MainViewModel.IsQuickLauncherOpen), notifiedProperties);

        notifiedProperties.Clear();
        vm.IsQuickLauncherOpen = false;

        Assert.False(vm.IsCommandPaletteOpen);
        Assert.False(vm.IsQuickLauncherOpen);
        Assert.Contains(nameof(MainViewModel.IsCommandPaletteOpen), notifiedProperties);
        Assert.Contains(nameof(MainViewModel.IsQuickLauncherOpen), notifiedProperties);
    }

    [Fact]
    public void EffectiveTerminalHeight_ShouldCollapseToZeroWhenTerminalClosed()
    {
        var vm = CreateViewModel();

        // Initially open
        vm.IsTerminalOpen = true;
        Assert.Equal(220, vm.EffectiveTerminalHeight.Value);

        // Close terminal
        vm.ToggleTerminalCommand.Execute(null);
        Assert.False(vm.IsTerminalOpen);
        Assert.Equal(0, vm.EffectiveTerminalHeight.Value);

        // Open terminal
        vm.ToggleTerminalCommand.Execute(null);
        Assert.True(vm.IsTerminalOpen);
        Assert.Equal(220, vm.EffectiveTerminalHeight.Value);
    }

    [Fact]
    public void ToggleLanguage_ShouldSwitchBetweenArabicAndEnglish()
    {
        var vm = CreateViewModel();

        // Default is Arabic
        Assert.True(vm.IsArabic);
        Assert.Equal("🌐 English", vm.CurrentLanguageButtonText);
        Assert.Equal("جناح Godot Engine", vm.Wings.First(w => w.Id == "Godot").DisplayTitle);

        // Switch to English
        vm.ToggleLanguageCommand.Execute(null);
        Assert.False(vm.IsArabic);
        Assert.Equal("🌐 عربي", vm.CurrentLanguageButtonText);
        Assert.Equal("Godot Engine Hub", vm.Wings.First(w => w.Id == "Godot").DisplayTitle);

        // Switch back to Arabic
        vm.ToggleLanguageCommand.Execute(null);
        Assert.True(vm.IsArabic);
        Assert.Equal("🌐 English", vm.CurrentLanguageButtonText);
        Assert.Equal("جناح Godot Engine", vm.Wings.First(w => w.Id == "Godot").DisplayTitle);
    }

    [Fact]
    public void CustomWing_EmptyState_ShouldBeVisibleWhenNoCustomTools()
    {
        var vm = CreateViewModel();

        vm.SelectWingCommand.Execute("Custom");
        Assert.Equal("Custom", vm.SelectedWing);
        Assert.True(vm.ShowCustomEmptyState, "Empty state should show when Custom wing has no tools.");
    }
}

