using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;
using Olve.Trains.Scenes.GameRendering;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public class DeletionToolService(
    TerrainRaycastService terrainRaycastService,
    ToolManagementService toolManagementService,
    TrackSplineService trackSplineService,
    TrackService trackService,
    VehicleService vehicleService,
    VehiclePositionService vehiclePositionService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    MouseManager mouseManager,
    ILogger<DeletionToolService> logger) : BaseToolService<DeletionToolService.State>(toolManagementService, new State())
{
    public record State(bool ActivatedThisFrame = false);

    private const float SnappingDistance = 1.0f;
    private const float VehicleDetectionDistance = 1.5f;

    public static Id<Tool> ToolId { get; } = Id.New<Tool>();
    protected override Tool Tool => new(ToolId, "Delete Entities");

    protected override Result<Pass> OnSelectedInput(TimeSpan deltaTime)
    {
        var activatedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };
        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate(TimeSpan deltaTime)
    {
        if (terrainRaycastService.TerrainIntersection is not { } terrainIntersection)
        {
            return Result.Success();
        }

        if (!ToolState.ActivatedThisFrame)
        {
            return Result.Success();
        }

        // Priority: Vehicle > Building > Track

        // 1. Check for vehicles near the cursor
        if (TryDeleteNearestVehicle(terrainIntersection))
        {
            return Result.Success();
        }

        // 2. Check for buildings at the tile position
        if (terrainRaycastService.TerrainIntersectionTile is { } tilePosition
            && TryDeleteBuildingAtTile(tilePosition))
        {
            return Result.Success();
        }

        // 3. Check for tracks near the cursor
        TryDeleteNearestTrack(terrainIntersection);

        return Result.Success();
    }

    private bool TryDeleteNearestVehicle(Vector3D<float> position)
    {
        Id<Vehicle>? closestVehicleId = null;
        var closestDistanceSq = VehicleDetectionDistance * VehicleDetectionDistance;

        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
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
                closestVehicleId = vehicleId;
            }
        }

        if (closestVehicleId is not { } id)
        {
            return false;
        }

        var result = vehicleService.DeleteVehicle(id);
        if (result.TryPickProblems(out var problems))
        {
            logger.LogWarning("Failed to delete vehicle {VehicleId}: {Problems}", id, problems);
            return false;
        }

        logger.LogInformation("Deleted vehicle {VehicleId}", id);
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
