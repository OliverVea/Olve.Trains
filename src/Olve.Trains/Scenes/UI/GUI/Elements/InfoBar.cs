using Olve.Engine3D.GUI.Layout;
using OneOf;

namespace Olve.Trains.Scenes.UI.GUI.Elements;

public class InfoBar
{
    public static IGuiElement Create()
    {
        return new PanelGuiElement
        {
            Size = new Vector2D<float>(1920, 50),
            BackgroundColor = Color.Black,
        };
    }
}

public class PanelGuiElement : GuiComponentWithChildrenBase
{
    public required Color BackgroundColor { get; set; }
    public required Vector2D<float> Size { get; set; }
    
    public override IReadOnlyList<GuiTarget> Targets =>
    [
        new PanelGuiTarget(BackgroundColor, Size)
    ];
}

public abstract class GuiComponentWithChildrenBase
{
    public abstract IReadOnlyList<GuiTarget> Targets { get; }
    public IReadOnlyList<GuiComponentBase> Children { get; init; } = [];

    public GuiElement Build() => new(Targets, Children.Select(x => x.Build()).ToArray());
}

public abstract class GuiComponentBase
{
    public abstract IReadOnlyList<GuiTarget> Targets { get; }

    public GuiElement Build() => new(Targets, []);
}

public class GuiElement(IReadOnlyList<GuiTarget> targets, IReadOnlyList<GuiElement> children)
{
    public IReadOnlyList<GuiTarget> Targets { get; } = targets;
    public IReadOnlyList<GuiElement> Children { get; } = children;
}

[GenerateOneOf]
public partial class GuiTarget : OneOfBase<PanelGuiTarget>
{
    
}

public class PanelGuiTarget(Color backgroundColor, Vector2D<float> size)
{
    public Color BackgroundColor => backgroundColor;
    public Vector2D<float> Size => size;
}