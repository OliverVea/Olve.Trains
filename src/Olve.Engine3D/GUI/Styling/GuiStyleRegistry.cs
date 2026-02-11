using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Elements;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStyleRegistry(ILogger<GuiStyleRegistry> logger)
{
    private readonly Dictionary<StyleKey, IGuiElementStyling> _styles = new();

    public void Register<T>(GuiElementStyling<T> styling) where T : GuiElement
    {
        _styles[styling.StyleKey] = styling;
        logger.LogDebug("Registered style of type '{TypeName}' and key '{StyleKey}'", typeof(T).Name, styling.StyleKey.Value);
    }

    public bool TryGetStyle(GuiElement guiElement, [MaybeNullWhen(false)] out IGuiElementStyling style)
    {
        style = null;
        return guiElement.StyleKey is {} styleKey && _styles.TryGetValue(styleKey, out style);
    }
}
