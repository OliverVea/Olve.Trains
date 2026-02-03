using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.GUI.Elements;
using Olve.Logging;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStyleRegistry(ILoggingManager loggingManager)
{
    private readonly Dictionary<StyleKey, IGuiElementStyling> _styles = new();

    public void Register<T>(GuiElementStyling<T> styling) where T : GuiElement
    {
        _styles[styling.StyleKey] = styling;
        loggingManager.Log(LogLevel.Debug, $"Registered style of type '{typeof(T).Name}' and key '{styling.StyleKey.Value}'");
    }

    public bool TryGetStyle(GuiElement guiElement, [MaybeNullWhen(false)] out IGuiElementStyling style)
    {
        style = null;
        return guiElement.StyleKey is {} styleKey && _styles.TryGetValue(styleKey, out style);
    }
}
