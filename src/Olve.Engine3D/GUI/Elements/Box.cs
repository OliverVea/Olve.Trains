using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI.Elements;

public class Box : GuiElement, IRenderableAsTexturedRectangle
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public float Weight { get; set; } = 0f;
    public float Gap { get; set; } = 0;
    public bool Vertical { get; set; } = false;
    public float Margin { get; set; } = 0;
    public float Padding { get; set; } = 0;
    public float Alpha { get; set; } = 1f;
    public Justify Justify { get; set; } = Justify.Start;
    public Align Align { get; set; } = Align.Start;
    public (float R, float G, float B)? BackgroundColor { get; set; }

    public override LayoutBox? LayoutBox => new LayoutBox()
    {
        Size = new SizeSpec(Dp.FromNullable(Width), Dp.FromNullable(Height), Weight),
        Gap = new Dp(Gap),
        Justify = Justify,
        Align = Align,
        LayoutAxis = Vertical ? UIAxis.Y : UIAxis.X,
        Padding = Thickness.All(Padding),
        Margin = Thickness.All(Margin),
    };

    public IRenderableAsTexturedRectangle.Data TexturedRectangleData => new()
    {
        Color = BackgroundColor is {} bg ? new Vector4D<float>(bg.R,  bg.G, bg.B, Alpha) : Vector4D<float>.Zero,
    };
}

public interface IRenderableAsRectangle
{
    record Data()
    {
        public Vector4D<float> Color { get; init; }
    }

    Data RectangleData { get; }
}