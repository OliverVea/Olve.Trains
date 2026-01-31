using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering.Entities;

namespace Olve.Engine3D.GUI.Elements;

public class Text : GuiElement, IRenderableAsText
{
    public required string Content { get; set; }
    public required (float R, float G, float B, float A) Color { get; set; }
    public FontData? Font { get; set; }
    public float FontSize { get; set; } = 16f;
    public Align Align { get; set; } = Align.Start;

    // Layout properties
    public int? Width { get; set; }
    public int? Height { get; set; }
    public float Weight { get; set; } = 0f;

    public override LayoutBox? LayoutBox => new LayoutBox()
    {
        Size = new SizeSpec(Dp.FromNullable(Width), Dp.FromNullable(Height), Weight),
    };

    public TextRenderData TextRenderData => new(
        Font,
        Content,
        FontSize,
        new Vector4D<float>(Color.R, Color.G, Color.B, Color.A),
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
