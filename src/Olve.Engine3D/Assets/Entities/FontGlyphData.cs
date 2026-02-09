namespace Olve.Engine3D.Assets.Entities;

public readonly struct FontGlyphData
{
    public required GlyphIndex Index { get; init; }
    public required float Advance { get; init; }
    public FontGlyphBoundsData? Bounds { get; init; }
}