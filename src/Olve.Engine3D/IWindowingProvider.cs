using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.Utilities;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Olve.Engine3D;

public static class WindowingServiceRegistration
{
    public static IServiceCollection AddWindowingServices(this IServiceCollection services)
    {
        services.AddSingleton<Provider<IWindow>>();
        services.AddSingleton<Provider<IInputContext>>();
        return services;
    }
}
