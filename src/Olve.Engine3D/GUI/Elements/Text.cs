using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering.Entities;
using Silk.NET.Maths;

namespace Olve.Engine3D.GUI.Elements;

public class Text : GuiElement, IRenderableAsText
{
    public required string Content { get; set; }
    public FontData? Font { get; set; }
    public float FontSize { get; set; } = 16f;
    public (float R, float G, float B)? Color { get; set; }
    public float Alpha { get; set; } = 1f;
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
        Color is {} color ? new Vector4D<float>(color.R, color.G, color.B, Alpha) : new Vector4D<float>(1f, 1f, 1f, Alpha),
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
