using Olve.Engine3D.GUI.Layout;

namespace Olve.Trains.Scenes.UI.GUI;

public abstract class BaseGuiElement
{
    public virtual GuiElementBox? GuiElementBox => null;
    public BaseGuiElement[] Children { get; set; } = [];
}

public class Box : BaseGuiElement
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public float? Weight { get; set; }
    public (byte R, byte G, byte B) BackgroundColor { get; set; }

    public override GuiElementBox? GuiElementBox => new GuiElementBox()
    {
        Size = new SizeSpec(Dp.FromNullable(Width), Dp.FromNullable(Height), Weight ?? 1f),
    };
}

public class Button : BaseGuiElement
{
    public Action? OnClick { get; set; }
}

public class Divider : BaseGuiElement
{

}