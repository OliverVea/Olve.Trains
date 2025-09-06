using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Scenes;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Terrain;

public class TerrainService(ILoggingManager loggingManager) : SceneService(loggingManager)
{
    public TerrainData? Terrain { get; private set; }

    protected override Result OnLoad()
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