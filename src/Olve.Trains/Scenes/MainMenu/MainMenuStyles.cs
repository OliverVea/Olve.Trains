using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Trains.Scenes.GameUI.GUI;

namespace Olve.Trains.Scenes.MainMenu;

public static class MainMenuStyles
{
    private const float BorderRadius = 8;
    private const float BorderWidth = 1.5f;
    private static readonly (float R, float G, float B, float A) DefaultBorder = (0.3f, 0.3f, 0.3f, 0.5f);
    private static readonly (float R, float G, float B, float A) FocusBorder = (1f, 1f, 1f, 1f);
    private static readonly (float R, float G, float B, float A) ButtonBackground = (0.27f, 0.3f, 0.28f, 0.8f);

    public static readonly GuiElementStyling<Box> MainMenuButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(MainMenuButtonStyle)),
        StateTransitions = Styles.ButtonTransitions,
        OnStateChanged = (box, weights) =>
        {
            var pressed = weights[GuiNodeState.Pressed];
            var focused = weights[GuiNodeState.Focused];

            box.BackgroundColor = ButtonBackground;
            box.Width = 200;
            box.Height = 50;
            box.BorderRadius = BorderRadius;
            box.BorderWidth = BorderWidth + 0.5f * pressed;
            box.BorderColor = Lerp(DefaultBorder, FocusBorder, focused);
            box.Padding = Lerp(8f, 12f, pressed);
            box.Justify = Justify.Center;
            box.Align = Align.Center;
        }
    };

    public static readonly GuiElementStyling<Text> MainMenuButtonTextStyle = new()
    {
        StyleKey = new StyleKey(nameof(MainMenuButtonTextStyle)),
        StateTransitions =  Styles.ButtonTransitions,
        OnStateChanged = (text, weights) =>
        {
            var focused = weights[GuiNodeState.Focused];

            text.Color = (0.9f, 0.9f, 0.9f, 1f);
            text.FontSize = 20;
            text.FontWeight = focused * 1.5f;
            text.FontSize = Lerp(16f, 18f, focused);
        }
    };

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static (float R, float G, float B, float A) Lerp(
        (float R, float G, float B, float A) a,
        (float R, float G, float B, float A) b,
        float t)
    {
        return (
            Lerp(a.R, b.R, t),
            Lerp(a.G, b.G, t),
            Lerp(a.B, b.B, t),
            Lerp(a.A, b.A, t));
    }
}
