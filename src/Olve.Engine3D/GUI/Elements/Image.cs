using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Assets.Entities;
using Silk.NET.Maths;

namespace Olve.Engine3D.GUI.Elements;

public class Image : GuiElement, IRenderableAsRectangle
{
    public required AssetPath<TextureData<RGBA>> Texture { get; set; }
    public RGBA Tint { get; set; } = RGBA.White;
    public float Alpha { get; set; } = 1f;
    public float? AspectRatio { get; set; }
    public FitMode Fit { get; set; } = FitMode.Contain;
    public Rectangle<float>? SourceRect { get; set; }
    public Vector2D<int>? TextureSize { get; set; }

    public override LayoutBox? LayoutBox => new LayoutBox()
    {
        Size = new SizeSpec(null, null, 1f, AspectRatio, Fit)
    };

    public IRenderableAsRectangle.Data TexturedRectangleData
    {
        get
        {
            var uvMin = Vector2D<float>.Zero;
            var uvMax = Vector2D<float>.One;

            if (SourceRect is { } rect && TextureSize is { } size)
            {
                // Flip Y: atlas pixel coords have Y=0 at top, OpenGL UVs have Y=0 at bottom
                uvMin = new Vector2D<float>(rect.Origin.X / size.X, 1f - (rect.Origin.Y + rect.Size.Y) / size.Y);
                uvMax = new Vector2D<float>((rect.Origin.X + rect.Size.X) / size.X, 1f - rect.Origin.Y / size.Y);
            }

            return new()
            {
                Color = Tint,
                TexturePath = Texture,
                UvMin = uvMin,
                UvMax = uvMax,
            };
        }
    }
}
