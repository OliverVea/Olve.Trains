using BigGustave;
using Olve.Engine3D.Graphics;
using Olve.OpenRaster;

namespace Olve.Engine3D.IO.Images;

public class HeightmapParser
{
    public Result<GeometryData<TriangleIndex>> ParseHeightmap(Span<float> heights, uint width)
    {
        var vertices = new Vector3D<float>[heights.Length];
        var indices = new TriangleIndex[(width - 1) * (heights.Length / width - 1) * 2];

        for (var j = 0; j < heights.Length / width; ++j)
        {
            for (var i = 0; i < width; ++i)
            {
                var index = j * width + i;
                vertices[index] = new Vector3D<float>(i, heights[(int)index], j);
            }
        }

        var indexIndex = 0;
        for (var j = 0u; j < heights.Length / width - 1; ++j)
        {
            for (var i = 0u; i < width - 1; ++i)
            {
                var index = j * width + i;
                indices[indexIndex++] = new TriangleIndex(index, index + 1, index + width);
                indices[indexIndex++] = new TriangleIndex(index + 1, index + width + 1, index + width);
            }
        }

        return new GeometryData<TriangleIndex>
        {
            Vertices = vertices,
            Indices = indices
        };
    }
}

public class MeshLayerParser(float heightPerStep = 0.25f, int zeroHeight = 128, int heightStep = 8) : ILayerParser<GeometryData<TriangleIndex>>
{
    public Result<GeometryData<TriangleIndex>> ParseLayer(Stream stream)
    {
        var png = Png.Open(stream);
        
        var maxVertexCount = png.Width * png.Height;

        Span<float> heights = stackalloc float[maxVertexCount];

        for (var j = 0; j < png.Height; ++j)
        {
            for (var i = 0; i < png.Width; ++i)
            {
                var height = png.GetPixel(i, j).R;
                heights[j * png.Width + i] = (height - zeroHeight) / heightStep * heightPerStep;
            }
        }

        return new HeightmapParser().ParseHeightmap(heights, (uint)png.Width);
    }
}