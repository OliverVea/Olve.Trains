using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Utilities.CollectionExtensions;

namespace Olve.Trains.Scenes.Game.Junctions;

public class JunctionService(ILoggingManager loggingManager) : BaseEntityService<Junction>(loggingManager)
{
    private readonly Dictionary<Id<Junction>, HashSet<JunctionConnection>> _junctionConnections = new();
    private readonly Dictionary<TilePosition, Id<Junction>> _junctions = new();

    public Event<Id<Junction>> OnJunctionConnectionsUpdated { get; } = new();
    
    public Result<Id<Junction>> AddJunctionConnection(Id<Track> trackId, TrackPoint trackPoint)
    {
        var tilePosition = ToTilePosition(trackPoint.Point);
        var junctionId = _junctions.GetOrAdd(tilePosition, Id<Junction>.New);
        Junction trackJunction = new(junctionId, tilePosition);

        var connections = _junctionConnections.GetOrAdd(junctionId, NewJunctionConnections);
        JunctionConnection connection = new(trackId, trackPoint);
        connections.Add(connection);
        
        LoggingManager.Log(LogLevel.Debug, $"Connected {connections.Count} tracks at '{tilePosition}'");

        if (!Exists(junctionId) && Add(trackJunction).TryPickProblems(out var problems))
        {
            return problems;
        }
        
        OnJunctionConnectionsUpdated.Invoke(junctionId);
        return junctionId;
    }

    public DeletionResult RemoveJunctionConnection(Id<Track> trackId, TrackPoint trackPoint)
    {
        var tilePosition = ToTilePosition(trackPoint.Point);
        if (!_junctions.TryGetValue(tilePosition, out var junctionId))
        {
            return DeletionResult.NotFound();
        }

        if (_junctionConnections.TryGetValue(junctionId, out var connections))
        {
            JunctionConnection connection = new(trackId, trackPoint);
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

    public bool TryGetJunctionId(TrackPoint trackPoint, out Id<Junction> junctionId)
    {
        return _junctions.TryGetValue(ToTilePosition(trackPoint.Point), out junctionId);
    }

    public IReadOnlyCollection<JunctionConnection> GetConnections(TrackPoint trackPoint)
    {
        if (TryGetJunctionId(trackPoint, out var junctionId))
        {
            var connections = GetConnections(junctionId);
            return connections
                .Where(x => (x.TrackPoint.Tangent + trackPoint.Tangent).Length < MathConstants.Epsilon)
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

    public bool IsConnected(Id<Track> trackId, TrackPoint trackPoint)
    {
        return _junctions.TryGetValue(ToTilePosition(trackPoint.Point), out var junctionId)
               && _junctionConnections.TryGetValue(junctionId, out var connections)
               && connections.Any(x => x.TrackId == trackId);
    }
}