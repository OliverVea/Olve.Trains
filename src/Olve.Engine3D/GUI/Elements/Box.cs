using Olve.Engine3D.GUI.Layout;

namespace Olve.Engine3D.GUI.Elements;

public class Box : GuiElement, IRenderableAsRectangle
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public float Weight { get; set; }
    public float Gap { get; set; }
    public bool Vertical { get; set; }
    public float Margin { get; set; }
    public float? MarginBottom { get; set; }
    public float? MarginTop { get; set; }
    public float? MarginLeft { get; set; }
    public float? MarginRight { get; set; }
    public float? MarginVertical { get; set; }
    public float? MarginHorizontal { get; set; }
    public float Padding { get; set; }
    public float? PaddingBottom { get; set; }
    public float? PaddingTop { get; set; }
    public float? PaddingLeft { get; set; }
    public float? PaddingRight { get; set; }
    public float? PaddingVertical { get; set; }
    public float? PaddingHorizontal { get; set; }
    public float? AspectRatio { get; set; }
    public Justify Justify { get; set; } = Justify.Start;
    public Align Align { get; set; } = Align.Start;
    public (float R, float G, float B, float A)? BackgroundColor { get; set; }
    public float? BorderWidth { get; set; }
    public (float L, float T, float R, float B)? BorderWidthSides { get; set; }
    public (float R, float G, float B, float A)? BorderColor { get; set; }
    public float? BorderRadius { get; set; }
    public (float TL, float TR, float BR, float BL)? BorderRadiusCorners { get; set; }

    // Full border override
    public Border? Border { get; set; }

    private Border ComputeBorder()
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
            ? new BorderRadius(new Dp(BorderRadiusCorners.Value.TL), new Dp(BorderRadiusCorners.Value.TR),
                                     new Dp(BorderRadiusCorners.Value.BR), new Dp(BorderRadiusCorners.Value.BL))
            : Layout.BorderRadius.All(new Dp(BorderRadius ?? 0f));

        return new Border(thickness, color, radius);
    }

    public override LayoutBox? LayoutBox => new LayoutBox
    {
        Size = new SizeSpec(
            PreferredWidth: Dp.FromNullable(Width),
            PreferredHeight: Dp.FromNullable(Height),
            ResizingWeight: Weight,
            AspectRatio: AspectRatio),
        Gap = new Dp(Gap),
        Justify = Justify,
        Align = Align,
        LayoutAxis = Vertical ? UIAxis.Y : UIAxis.X,
        Margin = new Thickness(
            new Dp(MarginLeft ?? MarginHorizontal ?? Margin),
            new Dp(MarginTop ?? MarginVertical ?? Margin),
            new Dp(MarginRight ?? MarginHorizontal ?? Margin),
            new Dp(MarginBottom ?? MarginVertical ?? Margin)),
        Padding = new Thickness(
            new Dp(PaddingLeft ?? PaddingHorizontal ?? Padding),
            new Dp(PaddingTop ?? PaddingVertical ?? Padding),
            new Dp(PaddingRight ?? PaddingHorizontal ?? Padding),
            new Dp(PaddingBottom ?? PaddingVertical ?? Padding)),
        Border = ComputeBorder(),
    };

    public IRenderableAsRectangle.Data TexturedRectangleData => new()
    {
        Color = BackgroundColor is {} bg
            ? new Vector4D<float>(bg.R,  bg.G, bg.B, bg.A)
            : Vector4D<float>.Zero,
        Border = ComputeBorder(),
    };
}
