using Microsoft.Extensions.Logging;
using Olve.Engine3D.Input;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Depots;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Silk.NET.Input;

namespace Olve.Trains.Scenes.GameUI.Tools;

public class DeletionToolService(
    MouseRaycastService mouseRaycastService,
    ToolManagementService toolManagementService,
    TrackService trackService,
    TrackCollisionService trackCollisionService,
    TrainService trainService,
    TrainCollisionService trainCollisionService,
    BuildingService buildingService,
    BuildingCollisionService buildingCollisionService,
    StationService stationService,
    DepotService depotService,
    MouseManager mouseManager,
    ILogger<DeletionToolService> logger,
    TerrainHighlightSettings terrainHighlightSettings) : BaseToolService<DeletionToolService.State>(toolManagementService, new State())
{
    public record State(bool ActivatedThisFrame = false);

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

    protected override Result<Pass> OnSelectedInput()
    {
        var activatedThisFrame = mouseManager.State.IsButtonPressed(MouseButton.Left);
        ToolState = ToolState with { ActivatedThisFrame = activatedThisFrame };
        return Pass.Pass;
    }

    protected override Result OnSelectedUpdate()
    {
        if (!ToolState.ActivatedThisFrame)
        {
            return Result.Success();
        }

        foreach (var hit in mouseRaycastService.Hits)
        {
            if (hit.Group == ColliderGroups.Terrain)
            {
                continue;
            }

            if (TryDeleteFromHit(hit))
            {
                return Result.Success();
            }
        }

        return Result.Success();
    }

    private bool TryDeleteFromHit(RaycastHit hit)
    {
        if (hit.Group == ColliderGroups.Train)
        {
            return TryDeleteTrain(hit.ColliderId);
        }

        if (hit.Group == ColliderGroups.Building)
        {
            return TryDeleteBuilding(hit.ColliderId);
        }

        if (hit.Group == ColliderGroups.Track)
        {
            return TryDeleteTrack(hit.ColliderId);
        }

        return false;
    }

    private bool TryDeleteTrain(Id<Collider> colliderId)
    {
        if (!trainCollisionService.TryGetTrainId(colliderId, out var trainId))
        {
            return false;
        }

        var result = trainService.DeleteTrain(trainId);
        if (result.TryPickProblems(out var problems))
        {
            logger.LogWarning("Failed to delete train {TrainId}: {Problems}", trainId, problems);
            return false;
        }

        logger.LogInformation("Deleted train {TrainId}", trainId);
        return true;
    }

    private bool TryDeleteBuilding(Id<Collider> colliderId)
    {
        if (!buildingCollisionService.TryGetBuildingId(colliderId, out var buildingId))
        {
            return false;
        }

        if (!stationService.CanDeleteStationForBuilding(buildingId))
        {
            logger.LogWarning("Cannot delete building {BuildingId}: station track is occupied", buildingId);
            return false;
        }

        if (!depotService.CanDeleteDepotForBuilding(buildingId))
        {
            logger.LogWarning("Cannot delete building {BuildingId}: depot track is occupied", buildingId);
            return false;
        }

        var result = buildingService.DeleteBuilding(buildingId);
        if (result.TryPickProblems(out var problems))
        {
            logger.LogWarning("Failed to delete building {BuildingId}: {Problems}", buildingId, problems);
            return false;
        }

        logger.LogInformation("Deleted building {BuildingId}", buildingId);
        return true;
    }

    private bool TryDeleteTrack(Id<Collider> colliderId)
    {
        if (!trackCollisionService.TryGetTrackId(colliderId, out var trackId))
        {
            return false;
        }

        var result = trackService.DeleteTrack(trackId);
        if (result.TryPickProblems(out var problems))
        {
            logger.LogWarning("Failed to delete track {TrackId}: {Problems}", trackId, problems);
            return false;
        }

        logger.LogInformation("Deleted track {TrackId}", trackId);
        return true;
    }
}
