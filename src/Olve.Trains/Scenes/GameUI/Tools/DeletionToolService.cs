using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public class DeletionToolService(
    MouseRaycastService mouseRaycastService,
    ToolManagementService toolManagementService,
    TrackSplineService trackSplineService,
    TrackService trackService,
    TrainService trainService,
    TrainPositionService trainPositionService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    MouseManager mouseManager,
    ILogger<DeletionToolService> logger,
    TerrainHighlightSettings terrainHighlightSettings) : BaseToolService<DeletionToolService.State>(toolManagementService, new State())
{
    public record State(bool ActivatedThisFrame = false);

    private const float SnappingDistance = 1.0f;
    private const float TrainDetectionDistance = 1.5f;

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Delete Entities");

    protected override State OnToolSelected(State toolState)
    {
        terrainHighlightSettings.ShowGrid = true;
        return toolState;
    }

    protected override State OnToolDeselected(State toolState)
    {
        terrainHighlightSettings.ShowGrid = false;
        return toolState;
    }

    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };
        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (mouseRaycastService.TerrainIntersection is not { } terrainIntersection)
        {
            return Result.Success();
        }

        if (!ToolState.ActivatedThisFrame)
        {
            return Result.Success();
        }

        // Priority: Train > Building > Track

        // 1. Check for trains near the cursor
        if (TryDeleteNearestTrain(terrainIntersection))
        {
            return Result.Success();
        }

        // 2. Check for buildings at the tile position
        if (mouseRaycastService.TerrainIntersectionTile is { } tilePosition
            && TryDeleteBuildingAtTile(tilePosition))
        {
            return Result.Success();
        }

        // 3. Check for tracks near the cursor
        TryDeleteNearestTrack(terrainIntersection);

        return Result.Success();
    }

    private bool TryDeleteNearestTrain(Vector3D<float> position)
    {
        Id<Train>? closestTrainId = null;
        var closestDistanceSq = TrainDetectionDistance * TrainDetectionDistance;

        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            if (trackSplineService.GetPoint(trackPosition.TrackPoint.TrackId, trackPosition.TrackPoint.Time)
                .TryPickProblems(out _, out var worldPosition))
            {
                continue;
            }

            var distanceSq = (worldPosition - position).LengthSquared;
            if (distanceSq < closestDistanceSq)
            {
                closestDistanceSq = distanceSq;
                closestTrainId = trainId;
            }
        }

        if (closestTrainId is not { } id)
        {
            return false;
        }

        var result = trainService.DeleteTrain(id);
        if (result.TryPickProblems(out var problems))
        {
            logger.LogWarning("Failed to delete train {TrainId}: {Problems}", id, problems);
            return false;
        }

        logger.LogInformation("Deleted train {TrainId}", id);
        return true;
    }

    private bool TryDeleteBuildingAtTile(TilePosition tilePosition)
    {
        foreach (var building in buildingService.Buildings)
        {
            if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
            {
                continue;
            }

            var (minX, minZ, maxX, maxZ) = BuildingValidationService.GetBounds(building.Position, blueprint.Footprint);

            if (tilePosition.X >= minX && tilePosition.X <= maxX
                && tilePosition.Z >= minZ && tilePosition.Z <= maxZ)
            {
                var result = buildingService.DeleteBuilding(building.Id);
                if (result.TryPickProblems(out var problems))
                {
                    logger.LogWarning("Failed to delete building {BuildingId}: {Problems}", building.Id, problems);
                    return false;
                }

                logger.LogInformation("Deleted building {BuildingId}", building.Id);
                return true;
            }
        }

        return false;
    }

    private bool TryDeleteNearestTrack(Vector3D<float> position)
    {
        if (trackSplineService.GetClosestTrackPoint(position, SnappingDistance, out var closestTrackPoint)
            .TryPickProblems(out _, out var foundClosestPoint))
        {
            return false;
        }

        if (!foundClosestPoint)
        {
            return false;
        }

        var result = trackService.DeleteTrack(closestTrackPoint.TrackId);
        if (result.TryPickProblems(out var problems))
        {
            logger.LogWarning("Failed to delete track {TrackId}: {Problems}", closestTrackPoint.TrackId, problems);
            return false;
        }

        logger.LogInformation("Deleted track {TrackId}", closestTrackPoint.TrackId);
        return true;
    }
}
