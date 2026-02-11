using Olve.Engine3D;
using Olve.Engine3D.Math;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Stations;

public class StationPlatformAreaService(ILogger<StationPlatformAreaService> logger,
    StationPlatformService stationPlatformService,
    TrackSplineService trackSplineService) : ISceneService
{
    private readonly Dictionary<Id<StationPlatform>, Id<AABB>> _stationPlatformAABBLookup = new();
    private readonly AABBLinearLookup<Id<StationPlatform>> _stationPlatformLookup = new();
    private readonly EventQueue<Id<StationPlatform>> _stationPlatformAddedQueue = new(stationPlatformService.OnAdded);
    private readonly EventQueue<Id<StationPlatform>> _stationPlatformRemovedQueue = new(stationPlatformService.OnRemoved);

    public Result Load()
    {
        _stationPlatformAddedQueue
            .SetHandler(OnAdded)
            .Init();
        _stationPlatformRemovedQueue
            .SetHandler(OnRemoved)
            .Init();
        return Result.Success();
    }

    public Result Unload()
    {
        _stationPlatformAddedQueue.Cleanup();
        _stationPlatformRemovedQueue.Cleanup();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_stationPlatformAddedQueue
            .Update()
            .TryPickProblems(out var problems))
        {
            logger.Log(problems);
        }

        if (_stationPlatformRemovedQueue
            .Update()
            .TryPickProblems(out problems))
        {
            logger.Log(problems);
        }

        return Result.Success();
    }

    public IReadOnlyCollection<Id<StationPlatform>> GetStationPlatformsIntersecting(AABB queryArea)
        => _stationPlatformLookup.Query(queryArea);

    public IReadOnlyCollection<Id<StationPlatform>> GetStationPlatformsContaining(Vector3D<float> queryPoint)
        => _stationPlatformLookup.Query(queryPoint);

    private Result OnAdded(Id<StationPlatform> stationPlatformId)
    {
        if (_stationPlatformAABBLookup.TryGetValue(stationPlatformId, out var id))
        {
            logger.LogWarning("Skipping re-registering station platform with id '{StationPlatformId}' as it already has aabb with id '{Id}'", stationPlatformId, id);
            return Result.Success();
        }

        if (!stationPlatformService.TryGet(stationPlatformId, out var stationPlatform))
        {
            return new ResultProblem("Could not find station platform with id '{0}'", stationPlatformId);
        }

        if (trackSplineService.GetEnds(stationPlatform.TrackId)
            .TryPickProblems(out var problems, out var stationTrackEnds))
        {
            return problems.Prepend("Could not create AABB for station platform with id '{0}' and track id '{1}'", stationPlatformId, stationPlatform.TrackId);
        }

        var (start, end) = stationTrackEnds;
        var aabb = GetCellGridAABB(start, end);

        var aabbId = _stationPlatformLookup.Add(aabb, stationPlatformId);
        _stationPlatformAABBLookup.Add(stationPlatformId, aabbId);

        logger.LogDebug("Registered AABB '{Aabb}' with id '{AabbId}' for station platform with id '{StationPlatformId}'", aabb, aabbId, stationPlatformId);

        return Result.Success();
    }

    private static AABB GetCellGridAABB(Vector3D<float> a, Vector3D<float> b)
    {
        var min = new Vector3D<float>(GetMin(a.X, b.X), GetMin(a.Y, b.Y), GetMin(a.Z, b.Z));
        var max = new Vector3D<float>(GetMax(a.X, b.X), GetMax(a.Y, b.Y), GetMax(a.Z, b.Z));

        return new AABB(min, max);

        static float GetMax(float fA, float fB) => float.Max(float.Ceiling(fA), float.Ceiling(fB));
        static float GetMin(float fA, float fB) => float.Min(float.Floor(fA), float.Floor(fB));
    }

    private Result OnRemoved(Id<StationPlatform> stationPlatformId)
    {
        if (_stationPlatformAABBLookup.TryGetValue(stationPlatformId, out var aabbId))
        {
            if (!_stationPlatformLookup.Remove(aabbId))
            {
                logger.LogWarning("Platform '{StationPlatformId}' with no registered AABB was deleted", stationPlatformId);
            }

            _stationPlatformAABBLookup.Remove(stationPlatformId);
        }
        else
        {
            logger.LogWarning("Platform '{StationPlatformId}' with no registered AABB id was deleted", stationPlatformId);
        }

        return Result.Success();
    }
}