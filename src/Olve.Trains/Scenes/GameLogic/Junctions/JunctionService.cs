using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Microsoft.Extensions.Logging;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Utilities.CollectionExtensions;

namespace Olve.Trains.Scenes.Game.Junctions;

public class JunctionService(ILogger<JunctionService> logger) : BaseEntityService<Junction>(logger)
{
    private readonly Dictionary<Id<Junction>, HashSet<JunctionConnection>> _junctionConnections = new();
    private readonly Dictionary<TilePosition, Id<Junction>> _junctions = new();

    public Event<Id<Junction>> OnJunctionConnectionsUpdated { get; } = new();
    
    public Result<Id<Junction>> AddJunctionConnection(Id<Track> trackId, TrackEndpoint trackEndpoint)
    {
        var tilePosition = ToTilePosition(trackEndpoint.Point);
        var junctionId = _junctions.GetOrAdd(tilePosition, Id.New<Junction>);
        Junction trackJunction = new(junctionId, tilePosition);

        var connections = _junctionConnections.GetOrAdd(junctionId, NewJunctionConnections);
        JunctionConnection connection = new(trackId, trackEndpoint);
        connections.Add(connection);
        
        logger.LogDebug("Connected {ConnectionCount} tracks at '{TilePosition}'", connections.Count, tilePosition);

        if (!Exists(junctionId) && Add(trackJunction).TryPickProblems(out var problems))
        {
            return problems;
        }
        
        OnJunctionConnectionsUpdated.Invoke(junctionId);
        return junctionId;
    }

    public DeletionResult RemoveJunctionConnection(Id<Track> trackId, TrackEndpoint trackEndpoint)
    {
        var tilePosition = ToTilePosition(trackEndpoint.Point);
        if (!_junctions.TryGetValue(tilePosition, out var junctionId))
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
            _junctions.Remove(tilePosition);
            if (Remove(junctionId).TryPickProblems(out var problems))
            {
                return DeletionResult.Error(problems);
            }
        }
        
        OnJunctionConnectionsUpdated.Invoke(junctionId);
        
        return DeletionResult.Success();
    }

    public bool TryGetJunctionId(TrackEndpoint trackEndpoint, out Id<Junction> junctionId)
    {
        return _junctions.TryGetValue(ToTilePosition(trackEndpoint.Point), out junctionId);
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
    
    private static TilePosition ToTilePosition(Vector3D<float> point) => new((int)point.X, (int)(point.Y * 8), (int)point.Z);
    private static HashSet<JunctionConnection> NewJunctionConnections() => new(1);

    public bool IsConnected(Id<Track> trackId, TrackEndpoint trackEndpoint)
    {
        return _junctions.TryGetValue(ToTilePosition(trackEndpoint.Point), out var junctionId)
               && _junctionConnections.TryGetValue(junctionId, out var connections)
               && connections.Any(x => x.TrackId == trackId);
    }
}