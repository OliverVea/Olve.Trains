using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Olve.Engine3D.Utilities;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Olve.Engine3D;

public static class WindowingServiceRegistration
{
    public static IServiceCollection AddWindowingServices(this IServiceCollection services)
    {
        services.TryAddSingleton<Provider<IWindow>>();
        services.TryAddSingleton<Provider<IInputContext>>();
        return services;
    }
}
