using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;
using Olve.Trains.Scenes.GameUI.Indicators;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public class TrainPlacingToolService(
    MouseRaycastService mouseRaycastService,
    ToolManagementService toolManagementService,
    TrackArrowIndicatorService arrowIndicatorService,
    TrackSplineService trackSplineService,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService,
    MouseManager mouseManager,
    KeyboardManager keyboardManager) : BaseToolService<TrainPlacingToolService.State>(toolManagementService, new State())
{
    public record State(bool Forward = true, bool ActivatedThisFrame = false);

    private const float SnappingDistance = 1f;

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Place Trains on Tracks");

    private Id<ArrowIndicator> _arrowIndicatorId;

    public override Result Load()
    {
        if (arrowIndicatorService.AddArrowIndicator().TryPickProblems(out var problems, out _arrowIndicatorId))
        {
            return problems;
        }

        return base.Load();
    }

    public override Result Unload()
    {
        if (arrowIndicatorService.RemoveArrowIndicator(_arrowIndicatorId).TryPickProblems(out var problems))
        {
            return problems;
        }

        return base.Unload();
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
        if (mouseRaycastService.TerrainIntersection is not { } terrainIntersection)
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

        if (trackSplineService.GetPoint(closestTrackPoint.TrackId, closestTrackPoint.Time)
            .TryPickProblems(out problems, out var worldPosition))
        {
            return problems;
        }

        if (trackSplineService.GetTangent(closestTrackPoint.TrackId, closestTrackPoint.Time)
            .TryPickProblems(out problems, out var tangent))
        {
            return problems;
        }

        if (ToolState.ActivatedThisFrame)
        {
            var velocity = ToolState.Forward ? 6f : -6f;
            var vehicleTrackPosition = new VehicleTrackPosition(closestTrackPoint, velocity);

            if (vehicleService.AddVehicle("Vehicle :D").TryPickProblems(out problems, out var vehicleId)
                || vehiclePositionService.SetTrackPosition(vehicleId, vehicleTrackPosition).TryPickProblems(out problems))
            {
                return problems;
            }
        }

        arrowIndicatorService.Show(_arrowIndicatorId);

        var direction = ToolState.Forward ? tangent : -tangent;

        arrowIndicatorService.SetPosition(_arrowIndicatorId, worldPosition, direction);

        return Result.Success();
    }

}