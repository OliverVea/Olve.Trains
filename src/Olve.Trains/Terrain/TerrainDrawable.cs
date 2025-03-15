using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Olve.Trains.Terrain;

public class TerrainDrawable(GraphicsDevice graphicsDevice, Terrain terrain) : Olve.Engine3D.Graphics.IDrawable
{
    private readonly BasicEffect _terrainEffect = new(graphicsDevice)
    {
        LightingEnabled = false,
        VertexColorEnabled = true
    };
    
    private readonly BasicEffect _gridEffect = new(graphicsDevice)
    {
        LightingEnabled = false,
        VertexColorEnabled = false,
        AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f),
    };

    private readonly float _hMax = terrain.Heights.Max(), _hMin = terrain.Heights.Min();

    private readonly VertexBuffer _vertexBuffer = new(graphicsDevice, typeof(VertexPositionColor), terrain.GridPointCount, BufferUsage.WriteOnly);

    private readonly IndexBuffer _terrainIndexBuffer = new(graphicsDevice, IndexElementSize.ThirtyTwoBits,
        terrain.TileCount * 6, BufferUsage.WriteOnly);

    private readonly IndexBuffer _gridIndexBuffer = new(graphicsDevice, IndexElementSize.ThirtyTwoBits,
        terrain.TileCount * 6, BufferUsage.WriteOnly);

    public void Initialize()
    {
        var vertices = new VertexPositionColor[terrain.GridPointCount];
        var index = 0;

        for (var z = 0; z <= terrain.Length; z++)
        {
            for (var x = 0; x <= terrain.Width; x++)
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

        _terrainIndexBuffer.SetData(indices);

        var gridIndices = new int[terrain.GridLineCount * 2];
        index = 0;

        for (var z = 0; z <= terrain.Length; z++)
        {
            for (var x = 0; x < terrain.Width; x++)
            {
                GridCoordinate from = new(x, z);
                GridCoordinate to = new(x + 1, z);

                gridIndices[index++] = GetIndex(from);
                gridIndices[index++] = GetIndex(to);
            }
        }

        for (var z = 0; z < terrain.Length; z++)
        {
            for (var x = 0; x <= terrain.Width; x++)
            {
                GridCoordinate from = new(x, z);
                GridCoordinate to = new(x, z + 1);

                gridIndices[index++] = GetIndex(from);
                gridIndices[index++] = GetIndex(to);
            }
        }

        _gridIndexBuffer.SetData(gridIndices);
    }

    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        _terrainEffect.World = world;
        _terrainEffect.View = view;
        _terrainEffect.Projection = projection;

        _gridEffect.World = world;
        _gridEffect.View = view;
        _gridEffect.Projection = projection;

        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.SetVertexBuffer(_vertexBuffer);
            graphicsDevice.Indices = _terrainIndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, terrain.TileCount * 2);
        }

        foreach (var pass in _gridEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.SetVertexBuffer(_vertexBuffer);
            graphicsDevice.Indices = _gridIndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, terrain.GridLineCount);
        }
    }

    private Color GetColor(int x, float y, int z)
    {
        var baseColor = new Color(203, 234, 105, 255); // Greenish color

        var brightness = (y - _hMin) / (_hMax - _hMin);
        brightness = Math.Clamp(brightness, 0f, 1f); // Ensure brightness is within range

        int r = (int)(baseColor.R * brightness);
        int g = (int)(baseColor.G * brightness);
        int b = (int)(baseColor.B * brightness);

        return new Color(r, g, b, 255);
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