using System.ComponentModel;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;

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
        StateTransitions = new()
        {
            [GuiNodeState.Focused] = new StateTransition(
                In: new GuiTransition(new Ms(100), Easing.EaseOut),
                Out: new GuiTransition(new Ms(100), Easing.EaseIn)),
            [GuiNodeState.Pressed] = new StateTransition(
                In: new GuiTransition(new Ms(25), Easing.EaseIn),
                Out: new GuiTransition(new Ms(50), Easing.EaseOut)),
        },
        OnStateChanged = (b, weights) =>
        {
            var pressed = weights[GuiNodeState.Pressed];
            var focused = weights[GuiNodeState.Focused];

            b.Height = 60;
            b.AspectRatio = 1;
            b.BorderRadius = DefaultBorderRadius;
            b.BorderWidth = DefaultBorderWidth + 0.5f * pressed;
            b.BorderColor = Lerp(DefaultBorder, FocusBorder, focused);
            b.Padding = Lerp(DefaultPadding, DefaultPadding * 2, pressed);
        }
    };

    public static readonly GuiElementStyling<Box> MenuBarBackground = new()
    {
        StyleKey = new StyleKey(nameof(MenuBarBackground)),
        OnStateChanged = (b, _) =>
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
        StateTransitions = new()
        {
            [GuiNodeState.Active] = new StateTransition(
                In: new GuiTransition(new Ms(25), Easing.EaseIn),
                Out: new GuiTransition(new Ms(25),  Easing.EaseOut)),
        },
        OnStateChanged = (img, weights) =>
        {
            var active = weights[GuiNodeState.Active];

            img.AspectRatio = 1;
            img.Tint = Lerp((0.75f, 0.75f, 0.75f), (1f, 1f, 1f), active);
        }
    };

    public static readonly GuiElementStyling<Box> InfoBarBackground = new()
    {
        StyleKey = new StyleKey(nameof(InfoBarBackground)),
        OnStateChanged =  (box, weights) =>
        {
            box.BackgroundColor = PanelBackground;
            box.Align = Align.Stretch;
            box.Padding = DefaultPadding;
        }
    };

    public static readonly GuiElementStyling<Box> InfoBarSection = new()
    {
        StyleKey = new StyleKey(nameof(InfoBarSection)),
        OnStateChanged = (box, weights) =>
        {
            box.Weight = 1f;
            box.Align = Align.Center;
        }
    };

    public static readonly GuiElementStyling<Text> InfoBarClockText = new()
    {
        StyleKey = new StyleKey(nameof(InfoBarClockText)),
        OnStateChanged = (text, weights) =>
        {

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

    private static (float R, float G, float B) Lerp(
        (float R, float G, float B) a,
        (float R, float G, float B) b,
        float t)
    {
        return (
            Lerp(a.R, b.R, t),
            Lerp(a.G, b.G, t),
            Lerp(a.B, b.B, t));
    }
}