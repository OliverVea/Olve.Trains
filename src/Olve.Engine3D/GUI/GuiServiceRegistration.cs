using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.TryAddScoped<GuiAnchorService>();
        services.TryAddScoped<GuiLayoutService>();
        services.TryAddScoped<GuiNodeStateService>();
        services.TryAddScoped<GuiActivationService>();
        services.TryAddScoped<GuiStyleApplierService>();
        services.TryAddScoped<GuiAnimationService>();
        services.TryAddScoped<GuiStyleRegistry>();
        services.TryAddScoped<GuiElementService>();
        services.TryAddScoped<GuiNodeService>();
        services.TryAddScoped<GuiCollisionService>();
        services.TryAddScoped<GuiFocusService>();
        services.TryAddScoped<GuiMouseInputService>();
        services.TryAddScoped<GuiSliderService>();
        services.TryAddScoped<GuiCheckboxService>();
        services.TryAddScoped<GuiRadioButtonService>();
        services.TryAddScoped<GuiDropdownService>();
        return services;
    }
}
