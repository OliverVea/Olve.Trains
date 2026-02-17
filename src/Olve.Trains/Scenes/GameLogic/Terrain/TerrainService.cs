using Olve.Engine3D;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Industries;

namespace Olve.Trains.Scenes.GameLogic.Terrain;

public class TerrainService
    (BuildingService buildingService, BuildingBlueprintLibraryService blueprintLibraryService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([blueprintLibraryService]);

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

        buildingService.AddBuilding(blueprintLibraryService.ResidentialBlueprint, new BuildingPosition(new TilePosition(3,1,10)));

        return Result.Success();
    }
}