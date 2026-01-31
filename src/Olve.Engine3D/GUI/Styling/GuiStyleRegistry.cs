using Olve.Engine3D.GUI.Elements;
using Olve.Logging;
using RegistryKey = (Olve.Engine3D.GUI.Styling.StyleKey, System.Type);

namespace Olve.Engine3D.GUI.Styling;

public class GuiStyleRegistry(ILoggingManager loggingManager)
{
    private readonly Dictionary<RegistryKey, IGuiElementStyling> _styles = new();

    public void Register<T>(GuiElementStyling<T> styling) where T : GuiElement
    {
        var registryKey = GetRegistryKey<T>(styling.StyleKey);
        _styles[registryKey] = styling;
        loggingManager.Log(LogLevel.Debug, $"Registered style of type '{typeof(T).Name}' and key '{styling.StyleKey.Value}'");
    }

    public Result ApplyState(GuiElement guiElement, GuiNodeState state)
    {
        if (guiElement.StyleKey is not { } styleKey)
        {
            return Result.Success();
        }

        var registryKey = (styleKey, guiElement.GetType());

        if (!_styles.TryGetValue(registryKey, out var style))
        {
            return new ResultProblem("No style found with key '{0}' and type '{1}'", registryKey.Item1.Value, registryKey.Item2.Name);
        }

        if (!style.TryApplyState(guiElement, state))
        {
            return new ResultProblem("Failed to apply style of type '{0}' to element of type '{1}'", registryKey.Item2.Name, guiElement.GetType().Name);
        }

        return Result.Success();
    }

    private static RegistryKey GetRegistryKey<T>(in StyleKey styleKey) => (styleKey, typeof(T));
}
