using Olve.Engine3D;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.ShaderExtensions;

namespace Olve.Trains.Scenes.GameLogic.Terrain;

public class TerrainRaycastService(
    MouseManager mouseManager,
    CameraSceneService cameraSceneService,
    TerrainService terrainService,
    GridService gridService,
    CollisionSystem collisionSystem,
    TerrainHighlightSettings terrainHighlightSettings) : ISceneService
{
    public Ray3D<float>? MouseRay { get; private set; }
    public Vector3D<float>? TerrainIntersection { get; private set; }
    public Vector3D<float>? TerrainIntersectionTileCenter => TerrainIntersection is { } world
        ? gridService.ToTileCenter(gridService.ToTilePosition(world))
        : null;
    public TilePosition? TerrainIntersectionTile => TerrainIntersection is { } world
        ? gridService.ToTilePosition(world)
        : null;

    public int Priority => SceneServicePriority.FromDependencies([cameraSceneService, terrainService]);

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

        if (MouseRay is not { } ray)
        {
            return Result.Success();
        }

        var hits = collisionSystem.Raycast(ray, ColliderGroups.Terrain);

        if (hits.Count > 0)
        {
            TerrainIntersection = hits[0].HitPosition;
        }

        return Result.Success();
    }

    public void ApplyTerrainIntersectionParameters(IWorldMousePositionShader shader)
    {
        if (terrainHighlightSettings.ShowGrid && TerrainIntersection is {} intersection)
        {
            shader.MousePosition = intersection;
        }
        else
        {
            shader.MousePosition = new Vector3D<float>(-1000f, -1000f, -1000f);
        }
    }
}
