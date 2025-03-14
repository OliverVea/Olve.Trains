using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Olve.Trains;

public readonly record struct GridCoordinate(int X, int Z);
public readonly record struct TileCoordinate(int X, int Z);

public class Terrain(int width, int length)
{
    private readonly int[] _heights = new int[(width + 1) * (length + 1)];

    public IReadOnlyCollection<int> Heights => _heights;

    public int Width => width;
    public int Length => length;

    public int GridPointCount => (Width + 1) * (Length + 1);
    public int TileCount => Width * Length;

    public int this[GridCoordinate coordinate]
    {
        get => _heights[GetIndex(coordinate)];
        set => _heights[GetIndex(coordinate)] = value;
    }

    private int GetIndex(GridCoordinate coordinate) => coordinate.Z * length + coordinate.X;
}

public interface ITerrainGenerator
{
    Terrain Generate(int width, int length, int? seed = null);
}

public class TerrainGenerator : ITerrainGenerator
{
    public Terrain Generate(int width, int length, int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var terrain = new Terrain(width, length);

        for (var z = 0; z <= length; z++)
        {
            for (var x = 0; x <= width; x++)
            {
                GridCoordinate coordinate = new(x, z), behind = new(x, z - 1), left = new(x - 1, z);

                if (x == 0 && z == 0)
                {
                    terrain[coordinate] = 0;
                    continue;
                }

                if (x == 0)
                {
                    terrain[coordinate] = terrain[behind] + random.Next(-1, 2);
                    continue;
                }

                if (z == 0)
                {
                    terrain[coordinate] = terrain[left] + random.Next(-1, 2);
                    continue;
                }

                var leftHeight = terrain[left];
                var behindHeight = terrain[behind];

                if (leftHeight == behindHeight)
                {
                    terrain[coordinate] = leftHeight + random.Next(-1, 2);
                    continue;
                }

                terrain[coordinate] = Math.Min(leftHeight, behindHeight) + random.Next(0, Math.Abs(leftHeight - behindHeight) + 1);
            }
        }

        return terrain;
    }
}

public class TerrainDrawable(GraphicsDevice graphicsDevice, Terrain terrain) : Olve.Engine3D.Graphics.IDrawable
{
    private readonly BasicEffect _basicEffect = new(graphicsDevice)
    {
        LightingEnabled = false,
        VertexColorEnabled = true
    };

    private readonly float _hMax = terrain.Heights.Max(), _hMin = terrain.Heights.Min();

    private readonly VertexBuffer _vertexBuffer = new(graphicsDevice, typeof(VertexPositionColor), terrain.GridPointCount, BufferUsage.WriteOnly);

    private readonly IndexBuffer _indexBuffer = new(graphicsDevice, IndexElementSize.ThirtyTwoBits,
        terrain.TileCount * 6, BufferUsage.WriteOnly);

    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        var vertices = new VertexPositionColor[terrain.GridPointCount];
        var index = 0;

        for (var z = 0; z < terrain.Length + 1; z++)
        {
            for (var x = 0; x < terrain.Width + 1; x++)
            {
                GridCoordinate coordinate = new(x, z);

                var y = terrain[coordinate] / 4f;
                var position = new Vector3(x, y, z);
                var color = GetColor(x, y, z);

                vertices[index++] = new VertexPositionColor(position * 10, color);
            }
        }

        _vertexBuffer.SetData(vertices);

        var indices = new int[terrain.TileCount * 6];
        Span<GridCoordinate> gridCoordinates = stackalloc GridCoordinate[4];
        index = 0;

        for (var z = 0; z < terrain.Length; z++)
        {
            for (var x = 0; x < terrain.Width; x++)
            {
                TileCoordinate coordinate = new(x, z);

                InsertGridCoordinates(coordinate, gridCoordinates);

                var height0 = terrain[gridCoordinates[0]];
                var height3 = terrain[gridCoordinates[3]];

                if (height0 == height3)
                {
                    indices[index++] = GetIndex(gridCoordinates[0]);
                    indices[index++] = GetIndex(gridCoordinates[2]);
                    indices[index++] = GetIndex(gridCoordinates[3]);

                    indices[index++] = GetIndex(gridCoordinates[0]);
                    indices[index++] = GetIndex(gridCoordinates[3]);
                    indices[index++] = GetIndex(gridCoordinates[1]);
                }
                else
                {
                    indices[index++] = GetIndex(gridCoordinates[0]);
                    indices[index++] = GetIndex(gridCoordinates[2]);
                    indices[index++] = GetIndex(gridCoordinates[1]);

                    indices[index++] = GetIndex(gridCoordinates[1]);
                    indices[index++] = GetIndex(gridCoordinates[2]);
                    indices[index++] = GetIndex(gridCoordinates[3]);
                }
            }
        }

        _indexBuffer.SetData(indices);

        _basicEffect.World = world;
        _basicEffect.View = view;
        _basicEffect.Projection = projection;

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.SetVertexBuffer(_vertexBuffer);
            graphicsDevice.Indices = _indexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, terrain.TileCount * 2);
        }
    }

    private Color GetColor(int x, float y, int z)
    {
        var r = (float)x / terrain.Width;
        var g = (float)z / terrain.Length;

        var b = (y * 4 - _hMin) / (_hMax - _hMin);

        return new Color(r, g, b);
    }

    private void InsertGridCoordinates(TileCoordinate tileCoordinate, Span<GridCoordinate> gridCoordinates)
    {
        gridCoordinates[0] = new GridCoordinate(tileCoordinate.X, tileCoordinate.Z);
        gridCoordinates[1] = new GridCoordinate(tileCoordinate.X + 1, tileCoordinate.Z);
        gridCoordinates[2] = new GridCoordinate(tileCoordinate.X, tileCoordinate.Z + 1);
        gridCoordinates[3] = new GridCoordinate(tileCoordinate.X + 1, tileCoordinate.Z + 1);
    }

    private int GetIndex(GridCoordinate gridCoordinate) => gridCoordinate.Z * (terrain.Width + 1) + gridCoordinate.X;

}