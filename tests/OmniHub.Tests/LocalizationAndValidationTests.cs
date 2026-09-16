using Microsoft.Extensions.DependencyInjection;
using OmniHub.Core.Enums;
using OmniHub.Core.Interfaces;
using OmniHub.Core.Models;
using OmniHub.Engine.Configuration;
using OmniHub.Engine.Extensions;
using OmniHub.Tools.Extensions;
using OmniHub.UI.ViewModels;
using Xunit;

namespace OmniHub.Tests;

public class LocalizationAndValidationTests
{
    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddOmniHubEngine();
        services.AddOmniHubTools();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void OptionalParameter_ShouldBeValid_WhenValueIsEmpty()
    {
        var param = new ToolParameter
        {
            Id = "ProcessNameFilter",
            Label = "Process Name Filter (Optional)",
            IsRequired = false,
            DefaultValue = ""
        };

        var pvm = new ParameterViewModel(param);

        Assert.True(pvm.IsValid, "Optional parameter should be valid when value is empty.");
        pvm.Value = "   ";
        Assert.True(pvm.IsValid, "Optional parameter should be valid even with whitespace.");
    }

    [Fact]
    public void RequiredParameter_ShouldBeInvalid_WhenValueIsEmpty()
    {
        var param = new ToolParameter
        {
            Id = "RequiredField",
            Label = "Required Field",
            IsRequired = true,
            DefaultValue = ""
        };

        var pvm = new ParameterViewModel(param);

        Assert.False(pvm.IsValid, "Required parameter must be invalid when empty.");
        pvm.Value = "ValidValue";
        Assert.True(pvm.IsValid, "Required parameter must be valid when filled.");
    }

    [Fact]
    public void HungProcessKiller_ShouldHaveCanExecuteTrue_WhenOptionalFieldIsEmpty()
    {
        var provider = CreateServiceProvider();
        var processRunner = provider.GetRequiredService<IProcessRunner>();
        var tokenInterpolator = provider.GetRequiredService<ITokenInterpolator>();
        var toolRegistry = provider.GetRequiredService<IToolRegistry>();
        var configManager = provider.GetRequiredService<IConfigManager>();

        var tool = new ToolDefinition
        {
            Id = "system.hung.killer",
            Title = "Hung / Unresponsive Process Killer",
            TitleAr = "إنهاء العمليات والبرامج المعلقة إجبارياً",
            Description = "Terminates hung processes",
            DescriptionAr = "فحص ورصد كافة البرامج والألعاب المتجمدة أو المعلقة في الخلفية وإنهائها فورياً وتحرير الموارد.",
            Category = "System",
            ExecutionType = ExecutionType.InternalTool,
            InternalToolHandlerId = "system.hung.killer",
            Parameters = new List<ToolParameter>
            {
                new()
                {
                    Id = "ProcessNameFilter",
                    Label = "Process Name Filter (Optional)",
                    IsRequired = false,
                    DefaultValue = ""
                }
            }
        };

        var terminal = new TerminalViewModel(processRunner);
        var vm = new ToolItemViewModel(
            tool,
            processRunner,
            tokenInterpolator,
            toolRegistry,
            configManager,
            terminal,
            () => { });

        Assert.True(vm.IsValid, "Tool with only optional empty fields should be valid.");
        Assert.True(vm.CanExecute, "Tool CanExecute should be true so Run Tool button is enabled.");
        Assert.True(vm.ExecuteCommand.CanExecute(null), "RelayCommand.CanExecute should be true.");
    }

    [Fact]
    public void ToolLocalization_ShouldSwitchDynamically_OnLanguageToggle()
    {
        var provider = CreateServiceProvider();
        var processRunner = provider.GetRequiredService<IProcessRunner>();
        var tokenInterpolator = provider.GetRequiredService<ITokenInterpolator>();
        var toolRegistry = provider.GetRequiredService<IToolRegistry>();
        var configManager = provider.GetRequiredService<IConfigManager>();

        var tool = new ToolDefinition
        {
            Id = "godot.headless.export",
            Title = "Godot Headless Export",
            TitleAr = "تصدير مشروع جودو (Headless)",
            Description = "Export Godot project silently",
            DescriptionAr = "تصدير لعبة جودو تلقائياً في الخلفية بدون واجهة إلى Windows Desktop أو إعدادات أخرى."
        };

        var terminal = new TerminalViewModel(processRunner);
        var vm = new ToolItemViewModel(
            tool,
            processRunner,
            tokenInterpolator,
            toolRegistry,
            configManager,
            terminal,
            () => { });

        // Default is Arabic
        vm.IsArabic = true;
        Assert.Equal("تصدير مشروع جودو (Headless)", vm.Title);
        Assert.Equal("تصدير لعبة جودو تلقائياً في الخلفية بدون واجهة إلى Windows Desktop أو إعدادات أخرى.", vm.Description);
        Assert.Equal("تشغيل الأداة", vm.RunButtonText);

        // Switch to English
        vm.IsArabic = false;
        Assert.Equal("Godot Headless Export", vm.Title);
        Assert.Equal("Export Godot project silently", vm.Description);
        Assert.Equal("Run Tool", vm.RunButtonText);

        // Switch back to Arabic
        vm.IsArabic = true;
        Assert.Equal("تصدير مشروع جودو (Headless)", vm.Title);
        Assert.Equal("تصدير لعبة جودو تلقائياً في الخلفية بدون واجهة إلى Windows Desktop أو إعدادات أخرى.", vm.Description);
        Assert.Equal("تشغيل الأداة", vm.RunButtonText);
    }

    [Theory]
    [InlineData("godot.headless.export", "تصدير مشروع جودو (Headless)")]
    [InlineData("godot.cache.nuker", "تنظيف كاش جودو وإصلاح التلف")]
    [InlineData("godot.gdscript.check", "فاحص صحة سكربتات GDScript")]
    [InlineData("dotnet.singlefile.publish", "حزم المشروع في ملف تنفيذي مستقل (.exe)")]
    [InlineData("dotnet.clean.binobj", "تنظيف مخلفات البناء وتوفير المساحة")]
    [InlineData("git.quick.commit", "حفظ ورفع التعديلات إلى Git")]
    [InlineData("xml.localization.checker", "فاحص مفاتيح ونصوص ملفات التعريب")]
    [InlineData("media.ico.generator", "منشئ أيقونات الويندوز متعددة المقاسات")]
    [InlineData("media.audio.to_ogg", "محول الصوتيات لصيغة OGG للألعاب")]
    [InlineData("system.unblock.dlls", "فك حظر ملفات ومودات DLL المحملة")]
    [InlineData("system.clean.logs", "منظف سجلات الكراش والملفات المؤقتة")]
    [InlineData("system.hung.killer", "إنهاء العمليات والبرامج المعلقة إجبارياً")]
    public void LocalizeTitle_All12Tools_ShouldHaveCorrectArabicTranslation(string toolId, string expectedArabicTitle)
    {
        var localized = ToolItemViewModel.LocalizeTitle(string.Empty, toolId);
        Assert.Equal(expectedArabicTitle, localized);
    }

    [Fact]
    public async Task ConfigManager_ShouldLoadTitleArAndDescriptionAr_FromAllJsonFiles()
    {
        using var configManager = new ConfigManager();
        var configsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "configs");
        var fullConfigsPath = Path.GetFullPath(configsDir);

        await configManager.LoadConfigurationsAsync(fullConfigsPath);

        Assert.NotEmpty(configManager.Tools);

        var hungKiller = configManager.Tools.FirstOrDefault(t => t.Id == "system.hung.killer");
        Assert.NotNull(hungKiller);
        Assert.Equal("إنهاء العمليات والبرامج المعلقة إجبارياً", hungKiller.TitleAr);
        Assert.False(string.IsNullOrWhiteSpace(hungKiller.DescriptionAr));
        Assert.False(hungKiller.Parameters.First(p => p.Id == "ProcessNameFilter").IsRequired);

        foreach (var tool in configManager.Tools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.TitleAr), $"Tool '{tool.Id}' must have TitleAr populated from config.");
            Assert.False(string.IsNullOrWhiteSpace(tool.DescriptionAr), $"Tool '{tool.Id}' must have DescriptionAr populated from config.");
        }
    }
}

