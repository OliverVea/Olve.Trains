using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering.Entities;

namespace Olve.Engine3D.GUI.Elements;

public class Image : GuiElement, IRenderableAsRectangle
{
    public required AssetPath<TextureData> Texture { get; set; }
    public (float R, float G, float B)? Tint { get; set; }
    public float Alpha { get; set; } = 1f;
    public float? AspectRatio { get; set; }
    public FitMode Fit { get; set; } = FitMode.Contain;

    public override LayoutBox? LayoutBox => new LayoutBox()
    {
        Size = new SizeSpec(null, null, 1f, AspectRatio, Fit)
    };

    public IRenderableAsRectangle.Data TexturedRectangleData => new()
    {
        Color = new Vector4D<float>(Tint?.R ?? 1f, Tint?.G ?? 1f, Tint?.B ?? 1f, Alpha),
        TexturePath = Texture
    };
}
