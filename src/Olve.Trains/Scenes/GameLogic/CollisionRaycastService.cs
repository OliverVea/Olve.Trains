using Microsoft.Extensions.Logging;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
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

        foreach (var hit in hits)
        {
            logger.LogInformation(
                "Collision hit: collider={ColliderId}, group={Group}, distance={Distance:F2}",
                hit.ColliderId,
                hit.Group,
                hit.Distance);
        }

        return Result.Success();
    }
}
