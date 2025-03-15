namespace Olve.Trains.Terrain;

public class Terrain(int width, int length)
{
    private readonly int[] _heights = new int[(width + 1) * (length + 1)];

    public IReadOnlyCollection<int> Heights => _heights;

    public int Width => width;
    public int Length => length;

    public int GridPointCount => (Width + 1) * (Length + 1);
    public int GridLineCount => (Width + 1) * Length + Width * (Length + 1);
    public int TileCount => Width * Length;

    public int this[GridCoordinate coordinate]
    {
        get => _heights[GetIndex(coordinate)];
        set => _heights[GetIndex(coordinate)] = value;
    }

    private int GetIndex(GridCoordinate coordinate) => coordinate.Z * length + coordinate.X;
}