using Microsoft.Extensions.DependencyInjection;
using OmniHub.Core.Interfaces;
using OmniHub.Tools.Cleaners;
using OmniHub.Tools.Localization;
using OmniHub.Tools.Media;
using OmniHub.Tools.SysTools;

namespace OmniHub.Tools.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOmniHubTools(this IServiceCollection services)
    {
        services.AddSingleton<IToolHandler, GodotCacheNukerHandler>();
        services.AddSingleton<IToolHandler, DotNetCleanArtifactsHandler>();
        services.AddSingleton<IToolHandler, IcoGeneratorHandler>();
        services.AddSingleton<IToolHandler, XmlLocalizationCheckerHandler>();
        services.AddSingleton<IToolHandler, DllUnblockerHandler>();
        services.AddSingleton<IToolHandler, CrashLogCleanerHandler>();
        services.AddSingleton<IToolHandler, HungProcessKillerHandler>();

        return services;
    }

    public static void RegisterToolHandlers(this IToolRegistry registry, IEnumerable<IToolHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            registry.RegisterHandler(handler);
        }
    }
}
