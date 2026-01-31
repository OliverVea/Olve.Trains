using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;

namespace Olve.Trains.Scenes.UI.GUI;

public static class Styles
{
    private static readonly (float R, float G, float B, float A) DefaultBorder = (0.2f, 0.2f, 0.2f, 0.3f);
    private static readonly (float R, float G, float B, float A) HoverBorder = (0.6f, 0.6f, 0.6f, 1f);
    private static readonly (float R, float G, float B, float A) FocusBorder = (0f, 1f, 0f, 1f);

    public static readonly GuiElementStyling<Box> MenuButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(MenuButtonStyle)),
        OnStateChanged = (b, state) =>
        {
            // Base setup
            b.BackgroundColor = DefaultBorder;
            b.AspectRatio = 1;
            b.Justify = Justify.Center;
            b.Align = Align.Center;
            b.BorderRadius = 6f;

            // Priority: Focused > Hovered > Default
            if (state.HasFlag(GuiElementState.Focused))
            {
                b.BorderColor = FocusBorder;
                b.BorderWidth = 2f;
            }
            else if (state.HasFlag(GuiElementState.Hovered))
            {
                b.BorderColor = HoverBorder;
                b.BorderWidth = 1f;
            }
            else
            {
                b.BorderColor = DefaultBorder;
                b.BorderWidth = 1f;
            }
        }
    };

    public static IReadOnlyCollection<IGuiElementStyling> All { get; } = [
        MenuButtonStyle
    ];
}