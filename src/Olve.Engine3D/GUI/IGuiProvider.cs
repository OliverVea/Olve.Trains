using Microsoft.Extensions.DependencyInjection;
using Olve.Engine3D.GUI.Collision;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Input;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;

namespace Olve.Engine3D.GUI;

public static class GuiServiceRegistration
{
    public static IServiceCollection AddGuiServices(this IServiceCollection services)
    {
        services.AddSingleton<GuiAnchorService>();
        services.AddSingleton<GuiLayoutService>();
        services.AddSingleton<GuiNodeStateService>();
        services.AddSingleton<GuiActivationService>();
        services.AddSingleton<GuiStateListenerService>();
        services.AddSingleton<GuiStyleApplierService>();
        services.AddSingleton<GuiAnimationService>();
        services.AddSingleton<GuiStyleRegistry>();
        services.AddSingleton<GuiElementService>();
        services.AddSingleton<GuiNodeService>();
        services.AddSingleton<GuiCollisionService>();
        services.AddSingleton<GuiFocusService>();
        services.AddSingleton<GuiMouseInputService>();
        return services;
    }
}
