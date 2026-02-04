using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;
using Olve.Trains.Scenes.Rendering;
using Olve.Trains.Scenes.UI.Indicators;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.UI.Tools;

public class TrainPlacingToolService(ILoggingManager loggingManager,
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    TrackArrowIndicatorService arrowIndicatorService,
    TrackSplineService trackSplineService,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager) : BaseToolService<TrainPlacingToolService.State>(loggingManager, toolManagementService, new State())
{
    public record State(bool Forward = true, bool ActivatedThisFrame = false);

    private const float SnappingDistance = 1f;

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Trains on Tracks");

    private Id<ArrowIndicator> _arrowIndicatorId;

    protected override Result OnLoad()
    {
        if (arrowIndicatorService.AddArrowIndicator().TryPickProblems(out var problems, out _arrowIndicatorId))
        {
            return problems;
        }

        return base.OnLoad();
    }

    protected override Result OnUnload()
    {
        if (arrowIndicatorService.RemoveArrowIndicator(_arrowIndicatorId).TryPickProblems(out var problems))
        {
            return problems;
        }

        return base.OnUnload();
    }

    protected override State OnToolSelected(State toolState)
    {
        arrowIndicatorService.Show(_arrowIndicatorId);
        return toolState with { ActivatedThisFrame = false };
    }

    protected override State OnToolDeselected(State toolState)
    {
        arrowIndicatorService.Hide(_arrowIndicatorId);
        return toolState;
    }

    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame =  mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };

        if (keyboardManager.State.IsKeyPressed(Key.R))
        {
            ToolState = ToolState with { Forward = !ToolState.Forward };
        }

        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (terrainRaycastService.TerrainIntersection is not { } terrainIntersection)
        {
            return Result.Success();
        }

        if (trackSplineService.GetClosestTrackPoint(terrainIntersection, SnappingDistance, out var closestTrackPoint)
            .TryPickProblems(out var problems, out var foundClosestPoint))
        {
            return problems;
        }

        if (!foundClosestPoint)
        {
            arrowIndicatorService.Hide(_arrowIndicatorId);
            return Result.Success();
        }

        if (ToolState.ActivatedThisFrame)
        {
            TrackPosition trackPosition = null; // ???

            if (vehicleService.AddVehicle("Vehicle :D").TryPickProblems(out problems, out var vehicleId)
                || vehiclePositionService.SetTrackPosition(vehicleId, trackPosition).TryPickProblems(out problems))
            {
                return problems;
            }
        }

        arrowIndicatorService.Show(_arrowIndicatorId);

        var direction = ToolState.Forward
            ? closestTrackPoint.Tangent
            : -closestTrackPoint.Tangent;

        arrowIndicatorService.SetPosition(_arrowIndicatorId, closestTrackPoint.Point, direction);

        return Result.Success();
    }

}