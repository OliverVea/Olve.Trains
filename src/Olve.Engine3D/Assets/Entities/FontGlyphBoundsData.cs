namespace Olve.Engine3D.Assets.Entities;

public readonly record struct FontGlyphBoundsData
{
    public required Rectangle<float> PlaneBounds { get; init; }
    public required Rectangle<float> AtlasBounds { get; init; }
}