using Olve.Engine3D.Assets;
using Olve.Engine3D.GUI.Layout;
using Olve.Engine3D.Rendering.Entities;

namespace Olve.Engine3D.GUI.Elements;

public interface IRenderableAsRectangle
{
    record Data()
    {
        public Vector4D<float> Color { get; init; } = Vector4D<float>.One;
        public AssetPath<TextureData>? TexturePath { get; init; } = null;
        public Border? Border { get; init; } = null;
    }

    Data TexturedRectangleData { get; }
}
