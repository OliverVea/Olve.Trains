using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Engine3D.GUI.Elements;

public class Text : GuiElement, IRenderableAsText, IRenderableAsRectangle
{
    public RGBA? Color { get; set; }
    public RGBA? BackgroundColor { get; set; }
    public string Content
    {
        get;
        set
        {
            field = value;
            ComputedSize = null;
        }
    } = string.Empty;
    public FontData? Font { get; set; }
    public float FontSize
    {
        get;
        set
        {
            field = value;
            ComputedSize = null;
        }
    } = 16f;
    public float FontWeight
    {
        get;
        set
        {
            field = value;
            ComputedSize = null;
        }
    } = 0f;
    public Align Align { get; set; } = Align.Start;

    // Layout properties
    public int? Width { get; set; }
    public int? Height { get; set; }
    public float Weight { get; set; } = 0f;

    /// <summary>
    /// Computed size from text measurement, set by GuiTextUpdateService.
    /// </summary>
    public Vector2D<Dp>? ComputedSize { get; set; }

    public override LayoutBox? LayoutBox => new LayoutBox()
    {
        Size = new SizeSpec(
            Dp.FromNullable(Width) ?? ComputedSize?.X,
            Dp.FromNullable(Height) ?? ComputedSize?.Y,
            Weight),
    };

    public TextRenderData TextRenderData => new(
        Font,
        Content,
        FontSize,
        FontWeight,
        Color ?? RGBA.Black,
        Align
    );

    public IRenderableAsRectangle.Data TexturedRectangleData => new()
    {
        Color = BackgroundColor ?? RGBA.Transparent,
    };
}