using MemoryPack;

namespace Olve.Engine3D.Assets.Entities;

[MemoryPackable]
public partial class TextureData<T> : ITextureData where T : unmanaged
{
    public required T[] Pixels { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    public static TextureData<T> Single(T pixel) => new()
    {
        Pixels = [pixel],
        Width = 1,
        Height = 1
    };
}

public interface ITextureData
{
    public int Width { get; }
    public int Height { get; }
}