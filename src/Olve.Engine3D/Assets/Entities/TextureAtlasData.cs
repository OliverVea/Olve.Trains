namespace Olve.Engine3D.Assets.Entities;

public class TextureAtlasData<T> where T : unmanaged
{
    public required AssetPath<TextureData<T>> Texture { get; init; }
    public required Vector2D<int> Size { get; init; }
}
