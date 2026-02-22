using Olve.Engine3D;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.ShaderExtensions;
using Olve.Trains.Scenes.GameLogic.Terrain;

namespace Olve.Trains.Scenes.GameRendering;

public class TerrainRaycastService(
    MouseManager mouseManager,
    CameraSceneService cameraSceneService,
    TerrainService terrainService) : ISceneService
{
    private HeightmapRaycaster? _heightmapRaycaster;

    public Ray3D<float>? MouseRay { get; private set; }
    public Vector3D<float>? TerrainIntersection { get; private set; }
    public Vector3D<float>? TerrainIntersectionTileCenter => FromTerrainIntersection((x, y, z) =>
        new Vector3D<float>(float.Floor(x) + 0.5f, float.Round(y * terrainService.TilesPerMeterHeight) * terrainService.TileStepHeight, float.Floor(z) + 0.5f));
    public TilePosition? TerrainIntersectionTile => FromTerrainIntersection((x, y, z) =>
        new TilePosition((int)x, (int)(y * terrainService.TilesPerMeterHeight), (int)z));

    public int Priority => SceneServicePriority.FromDependencies([cameraSceneService, terrainService]);

    public Result Load()
    {
        if (terrainService.Terrain is not { } terrain)
        {
            return new ResultProblem("TerrainService.Terrain is null");
        }

        _heightmapRaycaster = new HeightmapRaycaster(terrain.Heightmap);

        return Result.Success();
    }

    public Result<Pass> Input(TimeSpan deltaTime)
    {
        var mouseCoordinates = mouseManager.State.NormalizedPosition;

        if (mouseCoordinates.X is >= -1 and <= 1
            && mouseCoordinates.Y is >= -1 and <= 1
            && cameraSceneService.Camera.GetRay(mouseCoordinates).TryPickValue(out var mouseRay))
        {
            MouseRay = mouseRay;
        }
        else
        {
            MouseRay = null;
        }

        return Pass.Pass;
    }

    public Result Update(TimeSpan deltaTime)
    {
        TerrainIntersection = null;

        if (_heightmapRaycaster is null)
        {
            return new ResultProblem("HeightmapRaycaster is null");
        }

        if (MouseRay is null)
        {
            return Result.Success();
        }

        if (_heightmapRaycaster.TryRaycast(MouseRay.Value, out var terrainIntersection))
        {
            TerrainIntersection = terrainIntersection;
        }

        return Result.Success();
    }

    public void ApplyTerrainIntersectionParameters(IWorldMousePositionShader shader)
    {
        if (TerrainIntersection is {} intersection)
        {
            shader.MousePosition = intersection;
        }
        else
        {
            shader.MousePosition = new Vector3D<float>(-1000f, -1000f, -1000f);
        }
    }

    private T? FromTerrainIntersection<T>(Func<float, float, float, T> xyzTransform) => TerrainIntersection is {X: var x, Y: var y, Z: var z} ? xyzTransform(x, y, z) : default;
}