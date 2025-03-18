using BigGustave;
using Olve.OpenRaster;

namespace Olve.Engine3D.IO.Images;

public class MeshLayerParser(int zeroHeight = 128, int heightStep = 8) : ILayerParser<TriMesh>
{
    public Result<TriMesh> ParseLayer(Stream stream)
    {
        var png = Png.Open(stream);
        
        var vertexCount = png.Width * png.Height;
        Span<byte> heights = stackalloc byte[vertexCount];

        for (var j = 0; j < png.Height; ++j)
        {
            for (var i = 0; i < png.Width; ++i)
            {
                heights[j * png.Width + i] = png.GetPixel(i, j).R;
            }
        }
        
        var vertices = new Vector3D<float>[vertexCount];

        for (var z = 0; z < png.Height; z++)
        {
            for (var x = 0; x < png.Width; x++)
            {
                var i = x + z * png.Width;
                
                var height = heights[i];
                var y = (height - zeroHeight) / heightStep;
                
                vertices[i] = new Vector3D<float>(x, y, z);
            }
        }

        var tileCount = (png.Width - 1) * (png.Height - 1);
        var triangleCount = tileCount * 2;
        var triangles = new int[triangleCount * 3];
        
        Span<GridCoordinate> gridCoordinates = stackalloc GridCoordinate[4];
        var index = 0;

        for (var z = 0; z < png.Height - 1; z++)
        {
            for (var x = 0; x < png.Width - 1; x++)
            {
                TileCoordinate coordinate = new(x, z);

                InsertGridCoordinates(coordinate, gridCoordinates);

                var i0 = GetIndex(gridCoordinates[0], png.Width);
                var i3 = GetIndex(gridCoordinates[3], png.Width);

                var height0 = heights[i0];
                var height3 = heights[i3];
                
                if (height0 == height3)
                {
                    triangles[index++] = GetIndex(gridCoordinates[0], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[2], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[3], png.Width);

                    triangles[index++] = GetIndex(gridCoordinates[0], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[3], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[1], png.Width);
                }
                else
                {
                    triangles[index++] = GetIndex(gridCoordinates[0], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[2], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[1], png.Width);

                    triangles[index++] = GetIndex(gridCoordinates[1], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[2], png.Width);
                    triangles[index++] = GetIndex(gridCoordinates[3], png.Width);
                }
            }
        }

        return new TriMesh
        {
            Vertices = vertices,
            Indices = triangles,
        };
    }

    private static void InsertGridCoordinates(TileCoordinate tileCoordinate, Span<GridCoordinate> gridCoordinates)
    {
        gridCoordinates[0] = new GridCoordinate(tileCoordinate.X, tileCoordinate.Z);
        gridCoordinates[1] = new GridCoordinate(tileCoordinate.X + 1, tileCoordinate.Z);
        gridCoordinates[2] = new GridCoordinate(tileCoordinate.X, tileCoordinate.Z + 1);
        gridCoordinates[3] = new GridCoordinate(tileCoordinate.X + 1, tileCoordinate.Z + 1);
    }

    private int GetIndex(GridCoordinate gridCoordinate, int width) => gridCoordinate.Z * width + gridCoordinate.X;
}