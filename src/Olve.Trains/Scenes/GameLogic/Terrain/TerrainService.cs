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