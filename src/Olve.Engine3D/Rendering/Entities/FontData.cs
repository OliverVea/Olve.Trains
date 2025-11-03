using System.Collections.Frozen;
using System.Text;
using Olve.Engine3D.Assets;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Entities;

public enum FontAtlasType
{
    MSDF,
}

public readonly record struct GlyphIndex(ushort Value);

public readonly record struct GlyphIndexPair(uint Value)
{
    public static GlyphIndexPair FromRunes(GlyphIndex left, GlyphIndex right) => new(((uint)left.Value << 16) | right.Value);

    public GlyphIndex Left  => new((ushort)(Value >> 16));
    public GlyphIndex Right => new((ushort)(Value & 0xFFFF));
}

public class FontData
{
    public Id<FontData> Id { get; init; } = Olve.Utilities.Ids.Id.New<FontData>();
    public required FontAtlasData  Atlas { get; init; }
    public required FontMetricsData Metrics { get; init; }
    public required FrozenDictionary<Rune, FontGlyphData> GlyphData { get; init; }
    public required FrozenDictionary<GlyphIndexPair, float> KerningAdjustments { get; init; }
}

public class FontAtlasData
{
    public required AssetPath<TextureData> FontAtlas { get; init; }
    public required FontAtlasType FontAtlasType { get; init; }
    public required Vector2D<int> FontAtlasSize { get; init; }
    public required int DistanceRange { get; init; }
    public required int DistanceRangeMiddle { get; init; }
    public required int GlyphSize { get; init; }
}

public readonly record struct FontMetricsData
{
    public required float EmSize { get; init; }
    public required float LineHeight { get; init; }
    public required float Ascender { get; init; }
    public required float Descender { get; init; }
    public required float UnderlineY { get; init; }
    public required float UnderlineThickness { get; init; }
    public required bool YPointsDown { get; init; }
}

public readonly struct FontGlyphData
{
    public required GlyphIndex Index { get; init; }
    public required float Advance { get; init; }
    public FontGlyphBoundsData? Bounds { get; init; }
}

public readonly record struct FontGlyphBoundsData
{
    public required Rectangle<float> PlaneBounds { get; init; }
    public required Rectangle<float> AtlasBounds { get; init; }
}