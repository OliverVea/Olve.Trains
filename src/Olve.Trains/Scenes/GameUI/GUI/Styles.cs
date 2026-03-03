using Olve.Engine3D;
using Olve.Engine3D.GUI;
using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;
using Olve.Engine3D.GUI.Styling.Animation;

namespace Olve.Trains.Scenes.GameUI.GUI;

public static class Styles
{
    public const float DefaultBorderRadius = 6;
    public const float DefaultPadding = 2;
    public const float DefaultBorderWidth = 1.5f;

    public static readonly RGBA DefaultBorder = (0.2f, 0.2f, 0.2f, 0.3f);
    public static readonly RGBA PanelBackground = (0.27f, 0.3f, 0.28f, 0.65f);
    public static readonly RGBA FocusBorder = (1, 1, 1, 1);
    public static readonly RGBA DimTextColor = (0.75f, 0.75f, 0.75f, 1f);

    public static readonly Dictionary<GuiNodeState, StateTransition> ButtonTransitions = new()
    {
        [GuiNodeState.Focused] = new StateTransition(
            In: new GuiTransition(new Ms(100), Easing.EaseOut),
            Out: new GuiTransition(new Ms(100), Easing.EaseIn)),
        [GuiNodeState.Pressed] = new StateTransition(
            In: new GuiTransition(new Ms(25), Easing.EaseIn),
            Out: new GuiTransition(new Ms(50), Easing.EaseOut)),
    };

    private static Action<Box, StateWeights> ButtonOnStateChanged(int? width, int? height, int? aspectRatio) => (box, weights) =>
    {
        var pressed = weights[GuiNodeState.Pressed];
        var focused = weights[GuiNodeState.Focused];

        box.Height = height;
        box.AspectRatio = aspectRatio;
        box.Width = width;
        box.BorderRadius = DefaultBorderRadius;
        box.BorderWidth = DefaultBorderWidth + 0.5f * pressed;
        box.BorderColor = Lerp(DefaultBorder, FocusBorder, focused);
        box.Padding = Lerp(DefaultPadding, DefaultPadding * 2, pressed);
        box.Justify = Justify.Center;
        box.Align = Align.Center;
    };

    public static readonly GuiElementStyling<Box> MenuButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(MenuButtonStyle)),
        StateTransitions = ButtonTransitions,
        OnStateChanged = ButtonOnStateChanged(null, 60, 1)
    };

    public static readonly GuiElementStyling<Box> InfoBarButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(InfoBarButtonStyle)),
        StateTransitions = ButtonTransitions,
        OnStateChanged = ButtonOnStateChanged(80, 25, null)
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
            img.Tint = Lerp(RGBA.LightGrey, RGBA.White, active);
        }
    };

    public static readonly GuiElementStyling<Box> InfoBarBackground = new()
    {
        StyleKey = new StyleKey(nameof(InfoBarBackground)),
        OnStateChanged =  (box, _) =>
        {
            box.BackgroundColor = PanelBackground;
            box.Align = Align.Stretch;
            box.Padding = DefaultPadding;
        }
    };

    public static readonly GuiElementStyling<Box> InfoBarSection = new()
    {
        StyleKey = new StyleKey(nameof(InfoBarSection)),
        OnStateChanged = (box, _) =>
        {
            box.Weight = 1f;
            box.Align = Align.Center;
            box.Gap = 8;
        }
    };

    public static readonly GuiElementStyling<Text> InfoTextMedium = new()
    {
        StyleKey = new StyleKey(nameof(InfoTextMedium)),
        OnStateChanged = (text, _) =>
        {
            text.Color = DimTextColor;
            text.FontSize = 12f;
        }
    };

    public static readonly GuiElementStyling<Box> BurgerButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(BurgerButtonStyle)),
        StateTransitions = ButtonTransitions,
        OnStateChanged = (box, weights) =>
        {
            var pressed = weights[GuiNodeState.Pressed];
            var focused = weights[GuiNodeState.Focused];

            box.Width = 25;
            box.Height = 25;
            box.Vertical = true;
            box.Gap = 3;
            box.BorderRadius = DefaultBorderRadius;
            box.BorderWidth = DefaultBorderWidth + 0.5f * pressed;
            box.BorderColor = Lerp(DefaultBorder, FocusBorder, focused);
            box.Padding = Lerp(DefaultPadding, DefaultPadding * 2, pressed);
            box.Justify = Justify.Center;
            box.Align = Align.Center;
        }
    };

    public static readonly GuiElementStyling<Box> BurgerLineStyle = new()
    {
        StyleKey = new StyleKey(nameof(BurgerLineStyle)),
        OnStateChanged = (box, _) =>
        {
            box.Width = 15;
            box.Height = 2;
            box.BackgroundColor = (0.85f, 0.85f, 0.85f, 1f);
        }
    };

    public static readonly GuiElementStyling<Box> BurgerOverlayStyle = new()
    {
        StyleKey = new StyleKey(nameof(BurgerOverlayStyle)),
        OnStateChanged = (box, _) =>
        {
            box.BackgroundColor = (0f, 0f, 0f, 0.5f);
            box.Weight = 1;
            box.Justify = Justify.Center;
            box.Align = Align.Center;
        }
    };

    public static readonly GuiElementStyling<Box> BurgerMenuPanelStyle = new()
    {
        StyleKey = new StyleKey(nameof(BurgerMenuPanelStyle)),
        OnStateChanged = (box, _) =>
        {
            box.BackgroundColor = (0.2f, 0.22f, 0.21f, 0.9f);
            box.Vertical = true;
            box.Padding = 20;
            box.Gap = 10;
            box.BorderColor = DefaultBorder;
            box.BorderWidth = DefaultBorderWidth;
            box.BorderRadius = DefaultBorderRadius;
            box.Justify = Justify.Center;
            box.Align = Align.Center;
        }
    };

    public static readonly GuiElementStyling<Box> ModalButtonStyle = new()
    {
        StyleKey = new StyleKey(nameof(ModalButtonStyle)),
        StateTransitions = ButtonTransitions,
        OnStateChanged = (box, weights) =>
        {
            var pressed = weights[GuiNodeState.Pressed];
            var focused = weights[GuiNodeState.Focused];

            box.BackgroundColor = (0.27f, 0.3f, 0.28f, 0.8f);
            box.Width = 200;
            box.Height = 50;
            box.BorderRadius = 8;
            box.BorderWidth = DefaultBorderWidth + 0.5f * pressed;
            box.BorderColor = Lerp(DefaultBorder, FocusBorder, focused);
            box.Padding = Lerp(8f, 12f, pressed);
            box.Justify = Justify.Center;
            box.Align = Align.Center;
        }
    };

    public static readonly GuiElementStyling<Text> ModalButtonTextStyle = new()
    {
        StyleKey = new StyleKey(nameof(ModalButtonTextStyle)),
        StateTransitions = ButtonTransitions,
        OnStateChanged = (text, weights) =>
        {
            var focused = weights[GuiNodeState.Focused];

            text.Color = (0.9f, 0.9f, 0.9f, 1f);
            text.FontWeight = focused * 1.5f;
            text.FontSize = Lerp(16f, 18f, focused);
        }
    };

    public static readonly GuiElementStyling<Box> SignalPanelOverlayStyle = new()
    {
        StyleKey = new StyleKey(nameof(SignalPanelOverlayStyle)),
        OnStateChanged = (box, _) =>
        {
            box.BackgroundColor = (0f, 0f, 0f, 0f);
            box.Weight = 1;
            box.Justify = Justify.End;
            box.Align = Align.Start;
        }
    };

    public static readonly GuiElementStyling<Box> SignalPanelStyle = new()
    {
        StyleKey = new StyleKey(nameof(SignalPanelStyle)),
        OnStateChanged = (box, _) =>
        {
            box.BackgroundColor = (0.2f, 0.22f, 0.21f, 0.9f);
            box.Vertical = true;
            box.Padding = 15;
            box.Gap = 8;
            box.Width = 250;
            box.BorderColor = DefaultBorder;
            box.BorderWidth = DefaultBorderWidth;
            box.BorderRadius = DefaultBorderRadius;
        }
    };

    public static readonly GuiElementStyling<Text> SignalPanelHeaderStyle = new()
    {
        StyleKey = new StyleKey(nameof(SignalPanelHeaderStyle)),
        OnStateChanged = (text, _) =>
        {
            text.Color = (0.95f, 0.95f, 0.95f, 1f);
            text.FontSize = 18f;
            text.FontWeight = 1f;
        }
    };

    public static readonly GuiElementStyling<Text> SignalPanelInfoTextStyle = new()
    {
        StyleKey = new StyleKey(nameof(SignalPanelInfoTextStyle)),
        OnStateChanged = (text, _) =>
        {
            text.Color = DimTextColor;
            text.FontSize = 12f;
        }
    };

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static RGBA Lerp(RGBA a, RGBA b, float t)
    {
        return (
            Lerp(a.R, b.R, t),
            Lerp(a.G, b.G, t),
            Lerp(a.B, b.B, t),
            Lerp(a.A, b.A, t));
    }
}