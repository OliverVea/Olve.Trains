using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.Game.Terrain;

public class TerrainService : ISceneService
{
    public TerrainData? Terrain { get; private set; }

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
        
        Terrain = new TerrainData
        {
            Heightmap = heightmap
        };
        
        return Result.Success();
    }
}