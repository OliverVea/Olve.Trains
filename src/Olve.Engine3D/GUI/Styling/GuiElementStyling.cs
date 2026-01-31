using Olve.Engine3D.GUI.Elements;

namespace Olve.Engine3D.GUI.Styling;

public class GuiElementStyling<T>  where T : GuiElement
{
    public Action<T>? Setup { get; init; }

    public Action<T>? OnHoverEnter { get; init; }
    public Action<T>? OnHoverExit { get; init; }
}
