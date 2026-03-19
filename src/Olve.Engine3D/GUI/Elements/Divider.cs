using Olve.Engine3D.GUI.Layout;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

/// <summary>
/// A horizontal divider line. Wraps a zero-height Box with a top border.
/// </summary>
public class Divider : GuiElement
{
    public Box Line { get; }

    public Divider(float marginHorizontal = 0f, float marginVertical = 0f,
        RGBA? color = null, float thickness = 1f)
    {
        Interactive = false;

        Line = new Box
        {
            Id = Olve.Utilities.Ids.Id.New<GuiElement>(),
            Name = "Divider/Line",
            Interactive = false,
            Height = 0,
            BorderWidthSides = (0, thickness, 0, 0),
            BorderColor = color ?? new RGBA(0.4f, 0.4f, 0.4f, 0.5f),
            MarginHorizontal = marginHorizontal,
            MarginVertical = marginVertical,
        };

        Children = [Line];
    }

    public override LayoutBox? LayoutBox => new LayoutBox
    {
        Align = Align.Stretch,
    };
}
