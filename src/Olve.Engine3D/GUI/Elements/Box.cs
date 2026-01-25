using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI.Elements;

public class Box : GuiElement, IRenderableAsRectangle
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

    // Border convenience properties for XML
    public float? BorderWidth { get; set; }
    public (float L, float T, float R, float B)? BorderWidthSides { get; set; }
    public (float R, float G, float B, float A)? BorderColor { get; set; }
    public float? BorderRadius { get; set; }
    public (float TL, float TR, float BR, float BL)? BorderRadiusCorners { get; set; }

    // Full border override
    public Layout.Border? Border { get; set; }

    private Layout.Border ComputeBorder()
    {
        // If explicit Border is set, use it
        if (Border.HasValue) return Border.Value;

        // Otherwise, build from convenience properties
        if (!BorderColor.HasValue) return Layout.Border.None;

        var thickness = BorderWidthSides.HasValue
            ? new Thickness(new Dp(BorderWidthSides.Value.L), new Dp(BorderWidthSides.Value.T),
                           new Dp(BorderWidthSides.Value.R), new Dp(BorderWidthSides.Value.B))
            : Thickness.All(new Dp(BorderWidth ?? 0f));

        var color = new RGBA(BorderColor.Value.R, BorderColor.Value.G,
                            BorderColor.Value.B, BorderColor.Value.A);

        var radius = BorderRadiusCorners.HasValue
            ? new Layout.BorderRadius(new Dp(BorderRadiusCorners.Value.TL), new Dp(BorderRadiusCorners.Value.TR),
                                     new Dp(BorderRadiusCorners.Value.BR), new Dp(BorderRadiusCorners.Value.BL))
            : Layout.BorderRadius.All(new Dp(BorderRadius ?? 0f));

        return new Layout.Border(thickness, color, radius);
    }

    public override LayoutBox? LayoutBox => new LayoutBox()
    {
        Size = new SizeSpec(Dp.FromNullable(Width), Dp.FromNullable(Height), Weight),
        Gap = new Dp(Gap),
        Justify = Justify,
        Align = Align,
        LayoutAxis = Vertical ? UIAxis.Y : UIAxis.X,
        Padding = Thickness.All(Padding),
        Margin = Thickness.All(Margin),
        Border = ComputeBorder(),
    };

    public IRenderableAsRectangle.Data TexturedRectangleData => new()
    {
        Color = BackgroundColor is {} bg ? new Vector4D<float>(bg.R,  bg.G, bg.B, Alpha) : Vector4D<float>.Zero,
        Border = ComputeBorder(),
    };
}
