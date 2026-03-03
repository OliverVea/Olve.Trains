using Microsoft.Extensions.Logging;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameLogic;

public class CollisionRaycastService(
    ILogger<CollisionRaycastService> logger,
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
            var hit = hits[0];
            logger.LogInformation("Collider clicked: {ColliderId} (group: {Group}, distance: {Distance:F3})", hit.ColliderId, hit.Group, hit.Distance);
            OnColliderClicked.Invoke(hit);
        }

        return Result.Success();
    }
}
