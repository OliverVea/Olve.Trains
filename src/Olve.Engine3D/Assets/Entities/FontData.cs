using System.Collections.Frozen;
using System.Text;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Assets.Entities;

public class FontData
{
    public Id<FontData> Id { get; init; } = Olve.Utilities.Ids.Id.New<FontData>();
    public required TextureAtlasData<RGB> Atlas { get; init; }
    public required FontAtlasType FontAtlasType { get; init; }
    public required int DistanceRange { get; init; }
    public required int DistanceRangeMiddle { get; init; }
    public required int GlyphSize { get; init; }
    public required FontMetricsData Metrics { get; init; }
    public required FrozenDictionary<Rune, FontGlyphData> GlyphData { get; init; }
    public required FrozenDictionary<GlyphIndexPair, float> KerningAdjustments { get; init; }
}
