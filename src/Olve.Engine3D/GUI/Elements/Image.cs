using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Engine3D.GUI.Elements;

public class Image : GuiElement, IRenderableAsRectangle
{
    public required AssetPath<TextureData<RGBA>> Texture { get; set; }
    public RGBA Tint { get; set; } = RGBA.White;
    public float Alpha { get; set; } = 1f;
    public float? AspectRatio { get; set; }
    public FitMode Fit { get; set; } = FitMode.Contain;

    public override LayoutBox? LayoutBox => new LayoutBox()
    {
        Size = new SizeSpec(null, null, 1f, AspectRatio, Fit)
    };

    public IRenderableAsRectangle.Data TexturedRectangleData => new()
    {
        Color = Tint,
        TexturePath = Texture
    };
}
