using Olve.Engine3D.GUI.Elements;
using Olve.Engine3D.GUI.Styling;

namespace Olve.Trains.Scenes.UI.GUI;

public static class Styles
{
    public static readonly GuiElementStyling<Box> MenuButtonStyle = new()
    {
        OnHoverEnter = b =>
        {
            b.BorderColor = new ValueTuple<float, float, float, float>(1f, 1f, 1f, 1f);
            b.BorderWidth = 1f;
        },
        OnHoverExit = b =>
        {
            b.BorderColor = new ValueTuple<float, float, float, float>(0.2f, 0.2f, 0.2f, 0.3f);
            b.BorderWidth = 0.5f;
        }
    };
}