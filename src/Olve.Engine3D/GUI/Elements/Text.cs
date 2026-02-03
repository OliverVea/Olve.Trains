using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering.Entities;

namespace Olve.Engine3D.GUI.Elements;

public class Text : GuiElement, IRenderableAsText
{
    public (float R, float G, float B, float A)? Color { get; set; }
    public string Content { get; set; } = string.Empty;
    public FontData? Font { get; set; }
    public float FontSize { get; set; } = 16f;
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
        new Vector4D<float>(Color?.R ?? 1, Color?.G ?? 1, Color?.B ?? 1, Color?.A ?? 1),
        Align
    );
}

public interface IRenderableAsText
{
    TextRenderData TextRenderData { get; }
}

public record TextRenderData(
    FontData? Font,
    string Content,
    float FontSize,
    Vector4D<float> Color,
    Align Align
);
