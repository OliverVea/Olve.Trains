using System.Text;
using Olve.Engine3D.Rendering.Entities;
using Silk.NET.Maths;

namespace Olve.Engine3D.GUI.Text;

/// <summary>
/// Computes glyph positions and UV coordinates from font data for text rendering.
/// </summary>
public static class TextLayoutEngine
{
    /// <summary>
    /// Layout data for a single glyph, with position relative to text origin.
    /// </summary>
    public readonly record struct GlyphLayout(
        Vector2D<float> PositionPx,  // Relative to text origin, in pixels
        Vector2D<float> SizePx,       // Glyph size in pixels
        Vector2D<float> UvMin,        // UV min in atlas (normalized 0..1)
        Vector2D<float> UvMax         // UV max in atlas (normalized 0..1)
    );

    /// <summary>
    /// Computes the layout for text with the given font and size.
    /// Returns glyph positions relative to the text origin.
    /// </summary>
    public static IReadOnlyList<GlyphLayout> ComputeLayout(
        string text,
        FontData font,
        float fontSize)
    {
        var glyphs = new List<GlyphLayout>();
        var scale = fontSize / font.Metrics.EmSize;
        var atlasSize = font.Atlas.FontAtlasSize.As<float>();
        float cursorX = 0f;

        foreach (var rune in text.EnumerateRunes())
        {
            if (!font.GlyphData.TryGetValue(rune, out var glyphData))
            {
                // Fallback for missing glyphs: advance by a quarter em
                cursorX += 0.25f * scale;
                continue;
            }

            if (glyphData.Bounds is { } bounds)
            {
                // Position from PlaneBounds (in em units, scaled to pixels)
                // PlaneBounds.Origin.X is the horizontal bearing (offset from cursor)
                // PlaneBounds.Origin.Y is the vertical offset from baseline
                var x = cursorX + bounds.PlaneBounds.Origin.X * scale;
                var y = bounds.PlaneBounds.Origin.Y * scale;
                var w = bounds.PlaneBounds.Size.X * scale;
                var h = bounds.PlaneBounds.Size.Y * scale;

                // UVs from AtlasBounds (pixel coords → normalized 0..1)
                // Swap Y min/max to flip the glyph vertically (atlas Y=0 at top, but we render top-down)
                var uvMin = new Vector2D<float>(
                    bounds.AtlasBounds.Origin.X / atlasSize.X,
                    (bounds.AtlasBounds.Origin.Y + bounds.AtlasBounds.Size.Y) / atlasSize.Y
                );
                var uvMax = new Vector2D<float>(
                    (bounds.AtlasBounds.Origin.X + bounds.AtlasBounds.Size.X) / atlasSize.X,
                    bounds.AtlasBounds.Origin.Y / atlasSize.Y
                );

                glyphs.Add(new GlyphLayout(
                    new Vector2D<float>(x, y),
                    new Vector2D<float>(w, h),
                    uvMin,
                    uvMax
                ));
            }

            cursorX += glyphData.Advance * scale;
        }

        return glyphs;
    }

    /// <summary>
    /// Measures text size without computing full layout.
    /// Returns (width, lineHeight) in pixels.
    /// </summary>
    public static Vector2D<float> MeasureText(string text, FontData font, float fontSize)
    {
        var scale = fontSize / font.Metrics.EmSize;
        float width = 0f;

        foreach (var rune in text.EnumerateRunes())
        {
            if (font.GlyphData.TryGetValue(rune, out var glyphData))
            {
                width += glyphData.Advance * scale;
            }
            else
            {
                width += 0.25f * scale;  // Fallback
            }
        }

        return new Vector2D<float>(width, font.Metrics.LineHeight * scale);
    }
}
