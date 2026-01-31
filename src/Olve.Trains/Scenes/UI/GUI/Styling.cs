using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;

namespace Olve.Trains.Scenes.UI.GUI;

public static class Styles
{

    public static readonly GuiElementStyling<Box> MenuButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(MenuButtonStyle)),
        OnSetup = b =>
        {
            b.BackgroundColor = (0.2f, 0.2f, 0.2f, 0.3f);
            b.AspectRatio = 1;
            b.Justify = Justify.Center;
            b.Align = Align.Center;
            b.BorderWidth = 1f;
            b.BorderRadius = 6f;
            b.BorderColor = (0.2f, 0.2f, 0.2f, 0.3f);
        },
        OnHoverEnter = b =>
        {
            b.BorderColor = (0.6f, 0.6f, 0.6f, 1f);
        },
        OnHoverExit = b =>
        {
            b.BorderColor = (0.2f, 0.2f, 0.2f, 0.3f);
        },
        OnFocusEnter = b =>
        {
            b.BorderColor = (0f, 1f, 0f, 1f);
            b.BorderWidth = 2f;
        },
        OnFocusExit = b =>
        {
            b.BorderColor = (0.2f, 0.2f, 0.2f, 0.3f);
            b.BorderWidth = 1f;
        }
    };

    public static IReadOnlyCollection<IGuiElementStyling> All { get; } = [
        MenuButtonStyle
    ];
}