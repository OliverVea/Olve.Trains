using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Assets.Entities;

namespace Olve.Engine3D.GUI.Elements;

public interface IRenderableAsRectangle
{
    record Data
    {
        public RGBA Color { get; init; } = RGBA.White;
        public AssetPath<TextureData<RGBA>>? TexturePath { get; init; }
        public Border? Border { get; init; }
    }

    Data TexturedRectangleData { get; }
}
