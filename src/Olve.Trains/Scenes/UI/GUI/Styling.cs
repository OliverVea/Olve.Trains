using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.GUI.Styling;

namespace Olve.Trains.Scenes.UI.GUI;

public static class Styles
{
    public static readonly GuiElementStyling<Box> MenuButtonStyle = new()
    {
        Setup = b =>
        {
            b.BackgroundColor = new ValueTuple<float, float, float, float>(0.2f, 0.2f, 0.2f, 0.3f);
            b.AspectRatio = 1;
            b.Justify = Justify.Center;
            b.Align = Align.Center;
            b.BorderWidth = 1f;
            b.BorderRadius = 6f;
        },
        OnHoverEnter = b =>
        {
            b.BorderColor = new ValueTuple<float, float, float, float>(1f, 1f, 1f, 1f);
        },
        OnHoverExit = b =>
        {
            b.BorderColor = new ValueTuple<float, float, float, float>(0.2f, 0.2f, 0.2f, 0.3f);
        }
    };
}