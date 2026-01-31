using Olve.Engine3D.GUI.Elements;

namespace Olve.Engine3D.GUI.Styling;

public class GuiElementStyling<T> : IGuiElementStyling  where T : GuiElement
{
    public required StyleKey StyleKey { get; init; }

    public Action<T>? OnSetup { get; init; }
    public bool TryOnSetup(GuiElement guiElement) => TryApply(guiElement, OnSetup);

    public Action<T>? OnHoverEnter { get; init; }
    public bool TryOnHoverEnter(GuiElement guiElement) => TryApply(guiElement, OnHoverEnter);
    public Action<T>? OnHoverExit { get; init; }
    public bool TryOnHoverExit(GuiElement guiElement) => TryApply(guiElement, OnHoverExit);

    public Action<T>? OnFocusEnter { get; init; }
    public bool TryOnFocusEnter(GuiElement guiElement) => TryApply(guiElement, OnFocusEnter);
    public Action<T>? OnFocusExit { get; init; }
    public bool TryOnFocusExit(GuiElement guiElement) => TryApply(guiElement, OnFocusExit);

    private static bool TryApply(GuiElement guiElement, Action<T>? action)
    {
        if (guiElement is not T t)
        {
            return false;
        }

        action?.Invoke(t);
        return true;
    }
}

public interface IGuiElementStyling
{
    StyleKey StyleKey { get; }

    bool TryOnSetup(GuiElement guiElement);
    bool TryOnHoverEnter(GuiElement guiElement);
    bool TryOnHoverExit(GuiElement guiElement);
    bool TryOnFocusEnter(GuiElement guiElement);
    bool TryOnFocusExit(GuiElement guiElement);
}