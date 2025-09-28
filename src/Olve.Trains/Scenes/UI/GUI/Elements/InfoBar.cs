using Olve.Engine3D;
using OneOf;

namespace Olve.Trains.Scenes.UI.GUI.Elements;

public class GuiElement(GuiTarget target, IReadOnlyList<GuiElement> children)
{
    public GuiTarget Target { get; } = target;
    public IReadOnlyList<GuiElement> Children { get; } = children;
}

[GenerateOneOf]
public partial class GuiTarget : OneOfBase<PanelGuiTarget>
{
}

public class PanelGuiTarget(RGBA backgroundColor, Vector2D<float> size)
{
    public RGBA BackgroundColor => backgroundColor;
    public Vector2D<float> Size => size;
}


public static class Components
{
    public static GuiElement MakePanel()
    {
        
    }
}


/*
- (now)
- Box
- (future)
- Text
- Button
- Image


<Box Height=50 Weight=1 Layout="Horizontal">

</Box>

*/