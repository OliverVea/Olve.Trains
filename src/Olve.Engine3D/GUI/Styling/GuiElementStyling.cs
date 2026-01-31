using Olve.Engine3D.GUI.Elements;

namespace Olve.Engine3D.GUI.Styling;

public class GuiElementStyling<T> : IGuiElementStyling  where T : GuiElement
{
    public required StyleKey StyleKey { get; init; }

    public Action<T, GuiElementState>? OnStateChanged { get; init; }

    public bool TryApplyState(GuiElement guiElement, GuiElementState state)
    {
        if (guiElement is not T t)
        {
            return false;
        }

        OnStateChanged?.Invoke(t, state);
        return true;
    }
}

public interface IGuiElementStyling
{
    StyleKey StyleKey { get; }

    bool TryApplyState(GuiElement guiElement, GuiElementState state);
}