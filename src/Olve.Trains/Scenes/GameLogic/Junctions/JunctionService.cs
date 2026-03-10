using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Utilities.CollectionExtensions;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

public class JunctionService(ILogger<JunctionService> logger, EntityStoreFactory entityStoreFactory, GridService gridService)
{
    private readonly EntityStore<Junction> _junctions = entityStoreFactory.Create<Junction>();
    private readonly Dictionary<Id<Junction>, HashSet<JunctionConnection>> _junctionConnections = new();
    private readonly Dictionary<TilePosition, Id<Junction>> _junctionPositions = new();

    public Event<Id<Junction>> OnJunctionConnectionsUpdated { get; } = new();

    public Result<Id<Junction>> AddJunctionConnection(Id<Track> trackId, TrackEndpoint trackEndpoint)
    {
        var tilePosition = ToTilePosition(trackEndpoint.Point);
        var junctionId = _junctionPositions.GetOrAdd(tilePosition, Id.New<Junction>);
        Junction trackJunction = new(junctionId, tilePosition);

        var connections = _junctionConnections.GetOrAdd(junctionId, NewJunctionConnections);
        JunctionConnection connection = new(trackId, trackEndpoint);
        connections.Add(connection);

        logger.LogDebug("Connected {ConnectionCount} tracks at '{TilePosition}'", connections.Count, tilePosition);

        _junctions.Set(trackJunction);
        OnJunctionConnectionsUpdated.Invoke(junctionId);
        return junctionId;
    }

    public DeletionResult RemoveJunctionConnection(Id<Track> trackId, TrackEndpoint trackEndpoint)
    {
        var tilePosition = ToTilePosition(trackEndpoint.Point);
        if (!_junctionPositions.TryGetValue(tilePosition, out var junctionId))
        {
            return DeletionResult.NotFound();
        }

        if (_junctionConnections.TryGetValue(junctionId, out var connections))
        {
            JunctionConnection connection = new(trackId, trackEndpoint);
            connections.Remove(connection);
            if (connections.Count == 0)
            {
                _junctionConnections.Remove(junctionId);
                connections = null;
            }
        }

        if (connections is null)
        {
            _junctionPositions.Remove(tilePosition);
            if (_junctions.Remove(junctionId).TryPickProblems(out var problems))
            {
                return DeletionResult.Error(problems);
            }
        }

        OnJunctionConnectionsUpdated.Invoke(junctionId);

        return DeletionResult.Success();
    }

    public Result OnTrackAdded(Id<Track> trackId, TrackService trackService)
    {
        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackId);
        }

        return Result.Concat(
            AddJunctionConnection(track.Id, track.Start).ToEmptyResult(),
            AddJunctionConnection(track.Id, track.End).ToEmptyResult());
    }

    public Result OnTrackRemoved(Id<Track> trackId, TrackService trackService)
    {
        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackId);
        }

        return Result.Concat(
            RemoveJunctionConnection(track.Id, track.Start).MapToResult(),
            RemoveJunctionConnection(track.Id, track.End).MapToResult());
    }

    public bool TryGetJunctionId(TrackEndpoint trackEndpoint, out Id<Junction> junctionId)
    {
        return _junctionPositions.TryGetValue(ToTilePosition(trackEndpoint.Point), out junctionId);
    }

    public IReadOnlyCollection<JunctionConnection> GetConnections(TrackEndpoint trackEndpoint)
    {
        if (TryGetJunctionId(trackEndpoint, out var junctionId))
        {
            var connections = GetConnections(junctionId);
            return connections
                .Where(x => (x.TrackEndpoint.Tangent + trackEndpoint.Tangent).Length < MathConstants.Epsilon)
                .ToArray();
        }

        return [];
    }

    public IReadOnlyCollection<JunctionConnection> GetConnections(Id<Junction> junctionId)
    {
        if (_junctionConnections.TryGetValue(junctionId, out var connections))
        {
            return connections;
        }

        return [];
    }

    private TilePosition ToTilePosition(Vector3D<float> point) => gridService.ToTilePosition(point);
    private static HashSet<JunctionConnection> NewJunctionConnections() => new(1);

    public bool IsConnected(Id<Track> trackId, TrackEndpoint trackEndpoint)
    {
        return _junctionPositions.TryGetValue(ToTilePosition(trackEndpoint.Point), out var junctionId)
               && _junctionConnections.TryGetValue(junctionId, out var connections)
               && connections.Any(x => x.TrackId == trackId);
    }

    public bool JunctionExists(Id<Junction> junctionId) => _junctions.Exists(junctionId);

    public bool TryGetJunction(Id<Junction> junctionId, out Junction junction) =>
        _junctions.TryGet(junctionId, out junction);
}