using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameLogic;

public class CollisionRaycastService(
    MouseManager mouseManager,
    TerrainRaycastService terrainRaycastService,
    CollisionSystem collisionSystem)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([terrainRaycastService]);

    public Event<RaycastHit> OnColliderClicked { get; } = new();

    public Result Update(TimeSpan deltaTime)
    {
        if (!mouseManager.State.IsButtonPressed(MouseButton.Left))
        {
            return Result.Success();
        }

        if (terrainRaycastService.MouseRay is not { } mouseRay)
        {
            return Result.Success();
        }

        var hits = collisionSystem.Raycast(mouseRay);

        if (hits.Count > 0)
        {
            OnColliderClicked.Invoke(hits[0]);
        }

        return Result.Success();
    }
}
