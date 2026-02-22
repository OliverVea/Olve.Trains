using Olve.Engine3D;
using Olve.Trains.Scenes.GameLogic.Terrain;

namespace Olve.Trains.Scenes.GameLogic;

public class GridService(TerrainService terrainService)
{
    public Vector3D<float> ToTileOrigin(TilePosition tile) =>
        new(tile.X, tile.Y * terrainService.TileStepHeight, tile.Z);

    public Vector3D<float> ToTileCenter(TilePosition tile) =>
        new(tile.X + 0.5f, tile.Y * terrainService.TileStepHeight, tile.Z + 0.5f);

    public TilePosition ToTilePosition(Vector3D<float> world) =>
        new(
            (int)float.Floor(world.X),
            (int)float.Round(world.Y * terrainService.TilesPerMeterHeight),
            (int)float.Floor(world.Z));
}
