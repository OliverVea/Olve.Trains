using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Results;

namespace Olve.Trains.Scenes.Game.Terrain;

public class TerrainService(ILoggingManager loggingManager) : SceneService(loggingManager)
{
    public TerrainData? Terrain { get; set; }

    protected override Result OnLoad()
    {
        var terrainResult = AssetLoader.LoadAsset(Terrains.terrain01);
        if (terrainResult.TryPickProblems(out var problems, out var terrain))
        {
            return problems;
        }
        
        Terrain = terrain;
        
        return Result.Success();
    }
}