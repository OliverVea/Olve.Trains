using BigGustave;
using Microsoft.Extensions.Options;
using Olve.Engine3D.Rendering.Entities;
using Olve.OpenRaster;
using Olve.Results;
using Olve.Trains.AssetPipeline.Options;

namespace Olve.Trains.AssetPipeline.Assets;

public class HeightmapLayerParser(IOptions<TerrainOptions> terrainOptions) : ILayerParser<HeightmapData>
{
    public Result<HeightmapData> ParseLayer(Stream stream)
    {
        var png = Png.Open(stream);

        var maxVertexCount = png.Width * png.Height;

        var heights = new int[maxVertexCount];

        for (var j = 0; j < png.Height; ++j)
        {
            for (var i = 0; i < png.Width; ++i)
            {
                var height = png.GetPixel(i, j).R;
                heights[j * png.Width + i] = (height - terrainOptions.Value.ZeroHeight) / terrainOptions.Value.HeightStep;
            }
        }

        return new HeightmapData
        {
            Width = png.Width,
            Length = png.Height,
            Step = terrainOptions.Value.HeightPerStep,
            Heights = heights
        };
    }
}