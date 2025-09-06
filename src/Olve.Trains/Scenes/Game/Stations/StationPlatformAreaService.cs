using Olve.Engine3D;
using Olve.Engine3D.Math;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Stations;

public class StationPlatformAreaService(ILoggingManager loggingManager,
    StationPlatformService stationPlatformService,
    TrackSplineService trackSplineService) : BaseEntityListeningService<StationPlatform>(loggingManager, stationPlatformService)
{
    private readonly Dictionary<Id<StationPlatform>, Id<AABB>> _stationPlatformAABBLookup = new();
    private readonly AABBLinearLookup<Id<StationPlatform>> _stationPlatformLookup = new();

    protected override (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions() => (true, true);

    public IReadOnlyCollection<Id<StationPlatform>> GetStationPlatformsIntersecting(AABB queryArea)
        => _stationPlatformLookup.Query(queryArea);

    public IReadOnlyCollection<Id<StationPlatform>> GetStationPlatformsContaining(Vector3D<float> queryPoint)
        => _stationPlatformLookup.Query(queryPoint);

    protected override Result OnAdded(Id<StationPlatform> stationPlatformId)
    {
        if (_stationPlatformAABBLookup.TryGetValue(stationPlatformId, out var id))
        {
            LoggingManager.Log(LogLevel.Warning, $"Skipping re-registering station platform with id '{stationPlatformId}' as it already has aabb with id '{id}'");
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

        LoggingManager.Log(LogLevel.Debug, $"Registered AABB '{aabb}' with id '{aabbId}' for station platform with id '{stationPlatformId}'");

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

    protected override Result OnRemoved(Id<StationPlatform> stationPlatformId)
    {
        if (_stationPlatformAABBLookup.TryGetValue(stationPlatformId, out var aabbId))
        {
            if (!_stationPlatformLookup.Remove(aabbId))
            {
                LoggingManager.Log(LogLevel.Warning, $"Platform '{stationPlatformId}' with no registered AABB was deleted");
            }

            _stationPlatformAABBLookup.Remove(stationPlatformId);
        }
        else
        {
            LoggingManager.Log(LogLevel.Warning, $"Platform '{stationPlatformId}' with no registered AABB id was deleted");
        }

        return Result.Success();
    }
}