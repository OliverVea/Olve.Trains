using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GameLogic.Terrain;

public class TerrainService : ISceneService
{
    private TerrainData? _terrain;
    public TerrainData Terrain => _terrain ?? throw new NotInitializedException<TerrainData>();
    public float TileStepHeight => _terrain?.Heightmap.Step ?? 1f;
    public int TilesPerMeterHeight => (int)float.Round(1 /  TileStepHeight);

    public Result Load()
    {
        const int length = 50;
        const int width = 50;

        var heights = new int[length * width];
        Array.Fill(heights, 1);

        // Small hill visible in integration test (camera targets 12.5, 0, 10)
        for (var z = 3; z <= 6; z++)
        {
            for (var x = 12; x <= 15; x++)
            {
                heights[z * width + x] = 2;
            }
        }

        HeightmapData heightmap = new()
        {
            Heights = heights,
            Width = width,
            Length = length,
            Step = 0.125f
        };

        _terrain = new TerrainData
        {
            Heightmap = heightmap
        };

        return Result.Success();
    }
}