using Microsoft.Extensions.Logging;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Scenes.GameUI.Tools;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.GUI;

public class TrainSpeedCycleService(
    ILogger<TrainSpeedCycleService> logger,
    MouseRaycastService mouseRaycastService,
    MouseManager mouseManager,
    TrainCollisionService trainCollisionService,
    TrainPositionService trainPositionService,
    ToolManagementService toolManagementService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([mouseRaycastService]);

    private static readonly float[] SpeedModes = [0f, 2f, 10f];

    private bool _clickedThisFrame;

    public Result<Pass> Input()
    {
        _clickedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        return Pass.Pass;
    }

    public Result Update()
    {
        if (!_clickedThisFrame || toolManagementService.ActiveToolId is not null) return Result.Success();

        foreach (var hit in mouseRaycastService.Hits)
        {
            if (hit.Group != ColliderGroups.Train) continue;

            if (trainCollisionService.TryGetTrainId(hit.ColliderId, out var trainId)
                && trainPositionService.TryGetMotion(trainId, out var motion))
            {
                var nextSpeed = GetNextSpeed(motion.UserTargetSpeed);
                trainPositionService.SetMotion(trainId, motion with { TargetSpeed = nextSpeed, UserTargetSpeed = nextSpeed });
                logger.LogInformation("Train {TrainId} target speed: {PreviousSpeed} -> {NextSpeed}", trainId, motion.TargetSpeed, nextSpeed);
            }

            break;
        }

        return Result.Success();
    }

    private static float GetNextSpeed(float currentTarget)
    {
        for (var i = 0; i < SpeedModes.Length; i++)
        {
            if (float.Abs(currentTarget - SpeedModes[i]) < 0.01f)
            {
                return SpeedModes[(i + 1) % SpeedModes.Length];
            }
        }

        return SpeedModes[0];
    }
}
