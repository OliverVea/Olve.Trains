using Olve.Engine3D;
using Olve.Engine3D.Camera;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Terrain;

namespace Olve.Trains.Scenes.GameUI;

public class MouseRaycastService(
    MouseManager mouseManager,
    CameraSceneService cameraSceneService,
    CollisionSystem collisionSystem,
    GridService gridService,
    TerrainHighlightSettings terrainHighlightSettings) : ISceneService
{
    private static readonly IReadOnlyList<RaycastHit> EmptyHits = [];

    private IReadOnlyList<RaycastHit> _hits = EmptyHits;

    public Ray3D<float>? MouseRay { get; private set; }
    public Vector3D<float>? TerrainIntersection { get; private set; }

    public Vector3D<float>? TerrainIntersectionTileCenter => TerrainIntersection is { } world
        ? gridService.ToTileCenter(gridService.ToTilePosition(world))
        : null;

    public TilePosition? TerrainIntersectionTile => TerrainIntersection is { } world
        ? gridService.ToTilePosition(world)
        : null;

    public int Priority => SceneServicePriority.FromDependencies([cameraSceneService]);

    public IEnumerable<RaycastHit> Hits => _hits.Where(hit => collisionSystem.ColliderExists(hit.ColliderId));

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
            _hits = EmptyHits;
            terrainHighlightSettings.MouseWorldPosition = null;
            return Result.Success();
        }

        _hits = collisionSystem.Raycast(ray);
        TerrainIntersection = _hits.FirstOrDefault(x => x.Group == ColliderGroups.Terrain).HitPosition;
        terrainHighlightSettings.MouseWorldPosition = TerrainIntersection;

        return Result.Success();
    }
}
