using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;

namespace Olve.Trains.Scenes.UI.GUI;

public static class Styles
{
    public const float DefaultBorderRadius = 6;
    public const float DefaultPadding = 2;
    public const float DefaultBorderWidth = 1.5f;

    public static readonly (float R, float G, float B, float A) DefaultBorder = (0.2f, 0.2f, 0.2f, 0.3f);
    public static readonly (float R, float G, float B, float A) PanelBackground = (0.35f, 0.35f, 0.35f, 0.3f);
    public static readonly (float R, float G, float B, float A) FocusBorder = (1, 1, 1, 1);

    public static readonly GuiElementStyling<Box> MenuButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(MenuButtonStyle)),
        OnStateChanged = (b, state) =>
        {
            b.Height = 60;
            b.AspectRatio = 1;
            b.BorderRadius = DefaultBorderRadius;
            b.BorderColor = DefaultBorder;
            b.BorderWidth = DefaultBorderWidth;
            b.Padding = DefaultPadding;

            if (state.HasFlag(GuiNodeState.Focused))
            {
                b.BorderColor = FocusBorder;
            }

            if (state.HasFlag(GuiNodeState.Pressed))
            {
                b.Padding = DefaultPadding * 2;
            }
        }
    };

    public static readonly GuiElementStyling<Box> MenuBarBackground = new()
    {
        StyleKey = new StyleKey(nameof(MenuBarBackground)),
        OnStateChanged = (b, state) =>
        {
            b.BackgroundColor = PanelBackground;
            b.Padding = DefaultPadding;
            b.BorderColor = DefaultBorder;
            b.BorderWidth = DefaultBorderWidth;
            b.BorderRadius = DefaultBorderRadius;
        }
    };

    public static readonly GuiElementStyling<Image> ToolIconStyle = new()
    {
        StyleKey = new StyleKey(nameof(ToolIconStyle)),
        OnStateChanged = (img, state) =>
        {
            img.Tint = (0.75f, 0.75f, 0.75f);
            img.AspectRatio = 1;

            if (state.HasFlag(GuiNodeState.Active))
            {
                img.Tint = null;
            }
        }
    };
}