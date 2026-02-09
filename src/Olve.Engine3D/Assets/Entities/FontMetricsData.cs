namespace Olve.Engine3D.Assets.Entities;

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