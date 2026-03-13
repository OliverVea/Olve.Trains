using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Assets.Entities;
using Silk.NET.Maths;

namespace Olve.Engine3D.GUI.Elements;

public interface IRenderableAsRectangle
{
    record Data
    {
        public RGBA Color { get; init; } = RGBA.White;
        public AssetPath<TextureData<RGBA>>? TexturePath { get; init; }
        public Border? Border { get; init; }
        public Vector2D<float> UvMin { get; init; } = Vector2D<float>.Zero;
        public Vector2D<float> UvMax { get; init; } = Vector2D<float>.One;
    }

    Data TexturedRectangleData { get; }
}
