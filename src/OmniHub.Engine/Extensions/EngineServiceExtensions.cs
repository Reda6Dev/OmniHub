using Microsoft.Extensions.DependencyInjection;
using OmniHub.Core.Interfaces;
using OmniHub.Engine.Configuration;
using OmniHub.Engine.Process;
using OmniHub.Engine.Services;
using OmniHub.Engine.Templating;

namespace OmniHub.Engine.Extensions;

public static class EngineServiceExtensions
{
    public static IServiceCollection AddOmniHubEngine(this IServiceCollection services)
    {
        services.AddSingleton<ITokenInterpolator, TokenInterpolator>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<IConfigManager, ConfigManager>();
        services.AddSingleton<IUserStateService, UserStateService>();

        return services;
    }
}

