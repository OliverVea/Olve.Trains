using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.GUI.Elements;

namespace Olve.Engine3D.GUI.Styling;

public class GuiStyleRegistry(ILogger<GuiStyleRegistry> logger)
{
    private readonly Dictionary<StyleKey, IGuiElementStyling> _styles = new();

    public void Register(IGuiElementStyling styling)
    {
        _styles[styling.StyleKey] = styling;
        logger.LogDebug("Registered style of type '{TypeName}' and key '{StyleKey}'", styling.GetType().Name, styling.StyleKey.Value);
    }

    public bool Unregister(IGuiElementStyling styling)
    {
        logger.LogDebug("Unregistering style of type '{TypeName}' and key '{StyleKey}'", styling.GetType().Name, styling.StyleKey.Value);
        return _styles.Remove(styling.StyleKey);
    }

    public bool TryGetStyle(GuiElement guiElement, [MaybeNullWhen(false)] out IGuiElementStyling style)
    {
        style = null;
        return guiElement.StyleKey is {} styleKey && _styles.TryGetValue(styleKey, out style);
    }
}
