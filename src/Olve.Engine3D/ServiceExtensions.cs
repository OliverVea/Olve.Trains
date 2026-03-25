using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;

namespace Olve.Engine3D;

public static class ServiceExtensions
{
    public static IServiceCollection AddCoreEngineServices(this IServiceCollection services)
    {
        services.TryAddTransient<EntityStoreFactory>();
        services.AddSingleton<SceneScopeAccessor>();
        return services;
    }
}