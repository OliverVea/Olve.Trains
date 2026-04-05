using Microsoft.Extensions.Logging;
using Olve.Engine3D.Math;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Environment;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.GameLogic.Collision;

public class PlacementClearanceService(
    ILogger<PlacementClearanceService> logger,
    CollisionSystem collisionSystem,
    EnvironmentalObjectCollisionService environmentalObjectCollisionService,
    EnvironmentalObjectService environmentalObjectService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    TrackService trackService,
    TrackSplineService trackSplineService)
{
    private static readonly HashSet<Id<ColliderGroup>> AutoClearableGroups = [ColliderGroups.Environment];

    public Result ClearForBuilding(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        var (minX, minZ, maxX, maxZ) = BuildingValidationService.GetBounds(building.Position, blueprint.Footprint);
        var area = FootprintToAABB(minX, minZ, maxX, maxZ);

        ClearArea(area);

        return Result.Success();
    }

    public Result ClearForTrack(Id<Track> trackId)
    {
        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackId);
        }

        var spline = trackSplineService.CreateSpline(track.Start, track.End);
        if (spline.GetPoints(TrackSegmentHelper.SegmentCount + 1)
            .TryPickProblems(out var problems, out var points))
        {
            return problems.Prepend("Failed to get track spline points for clearance");
        }

        var pointArray = points.ToArray();
        for (var i = 0; i < TrackSegmentHelper.SegmentCount; i++)
        {
            var segmentFrom = pointArray[i];
            var segmentTo = pointArray[i + 1];
            var matrix = TrackSegmentHelper.ComputeSegmentOBBMatrix(segmentFrom, segmentTo);
            var segmentAABB = TrackSegmentHelper.HalfUnitBox.GetWorldAABB(matrix);

            ClearArea(segmentAABB);
        }

        return Result.Success();
    }

    public void ClearArea(AABB area)
    {
        var hits = collisionSystem.QueryOverlapAABB(area, AutoClearableGroups);
        var toDelete = new HashSet<Id<EnvironmentalObject>>();

        foreach (var hit in hits)
        {
            if (!ColliderGroups.IsAutoClearable(hit.Group)) continue;

            if (environmentalObjectCollisionService.TryGetObjectId(hit.ColliderId, out var objectId))
            {
                toDelete.Add(objectId);
            }
        }

        foreach (var objectId in toDelete)
        {
            if (!environmentalObjectService.TryGetObject(objectId, out _)) continue;

            environmentalObjectService.DeleteObject(objectId);
            logger.LogDebug("Auto-cleared environmental object {ObjectId} during placement", objectId);
        }
    }

    public static AABB FootprintToAABB(int minX, int minZ, int maxX, int maxZ)
    {
        return new AABB(
            new Vector3D<float>(minX, float.MinValue, minZ),
            new Vector3D<float>(maxX + 1, float.MaxValue, maxZ + 1));
    }
}
