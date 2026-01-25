namespace Olve.Engine3D.GUI.Layout;

public readonly record struct LayoutBox()
{
    public SizeSpec Size { get; init; } = SizeSpec.None;
    public Thickness Padding { get; init; } = Thickness.Zero;
    public Thickness Margin { get; init; } = Thickness.Zero;
    public Align Align { get; init; } = Align.Start;
    public Justify Justify { get; init; } = Justify.Start;
    public UIAxis LayoutAxis { get; init; }
    public Border Border { get; init; } = Border.None;
    public Dp Gap { get; init; }
    
    public Dp HorizontalChrome => Margin.Horizontal + Border.Width.Horizontal +  Padding.Horizontal;
    public Dp VerticalChrome => Margin.Vertical + Border.Width.Vertical +  Padding.Vertical;
    public Vector2D<Dp> Chrome => new(HorizontalChrome, VerticalChrome);

    public Dp GetGapForAxis(UIAxis axis) => axis == LayoutAxis ? Gap : Dp.Zero;
    public Dp GetChromeForAxis(UIAxis axis) => axis == UIAxis.X ? HorizontalChrome : VerticalChrome;
}