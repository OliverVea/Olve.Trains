namespace Olve.Engine3D.Assets.Entities;

public class FontAtlasData
{
    public required AssetPath<TextureData> FontAtlas { get; init; }
    public required FontAtlasType FontAtlasType { get; init; }
    public required Vector2D<int> FontAtlasSize { get; init; }
    public required int DistanceRange { get; init; }
    public required int DistanceRangeMiddle { get; init; }
    public required int GlyphSize { get; init; }
}