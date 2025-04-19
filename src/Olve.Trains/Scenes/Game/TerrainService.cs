using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Scenes;
using Olve.Results;

namespace Olve.Trains.Scenes.Game;

public class TerrainService : SceneService
{
    public TerrainData? Terrain { get; set; }
    
    public override Result Load()
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