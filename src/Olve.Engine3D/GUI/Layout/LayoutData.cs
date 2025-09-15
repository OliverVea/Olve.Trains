using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Layout;

public record struct LayoutData(Id<GuiElement> GuiElementId)
{
    public GuiElementBox GuiElementBox { get; set; } = new();
    public Dp? Width { get; set; } = null;
    public Dp? Height { get; set; } = null;
    public Vector2D<Dp>? Position { get; set; } = null;
}