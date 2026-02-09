namespace Olve.Engine3D.Assets.Entities;

public readonly record struct GlyphIndexPair(uint Value)
{
    public static GlyphIndexPair FromRunes(GlyphIndex left, GlyphIndex right) => new(((uint)left.Value << 16) | right.Value);

    public GlyphIndex Left  => new((ushort)(Value >> 16));
    public GlyphIndex Right => new((ushort)(Value & 0xFFFF));
}