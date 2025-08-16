using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Results;
using Olve.Utilities.CollectionExtensions;
using Olve.Utilities.Ids;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackJunctionService(ILoggingManager loggingManager) : BaseEntityService<TrackJunction>(loggingManager)
{
    private readonly Dictionary<Id<TrackJunction>, HashSet<JunctionConnection>> _junctionConnections = new();
    private readonly Dictionary<TilePosition, Id<TrackJunction>> _trackJunctions = new();
    
    public Result<Id<TrackJunction>> AddJunctionConnection(Id<Track> trackId, TrackPoint trackPoint)
    {
        var tilePosition = ToTilePosition(trackPoint.Point);
        var junctionId = _trackJunctions.GetOrAdd(tilePosition, Id<TrackJunction>.New);
        TrackJunction trackJunction = new(junctionId, tilePosition);

        var connections = _junctionConnections.GetOrAdd(junctionId, NewJunctionConnections);
        JunctionConnection connection = new(trackId, trackPoint);
        connections.Add(connection);
        
        LoggingManager.Log(LogLevel.Debug, $"Connected {connections.Count} tracks at '{tilePosition}'");

        return Exists(junctionId) ? junctionId :  Add(trackJunction);
    }

    public DeletionResult RemoveJunctionConnection(Id<Track> trackId, TrackPoint trackPoint)
    {
        var tilePosition = ToTilePosition(trackPoint.Point);
        if (!_trackJunctions.TryGetValue(tilePosition, out var junctionId))
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

        if (connections is not null)
        {
            return DeletionResult.Success();
        }
        
        _trackJunctions.Remove(tilePosition);
        return Remove(junctionId);
    }

    public IReadOnlyCollection<JunctionConnection> GetConnections(TrackPoint trackPoint)
    {
        var tilePosition = ToTilePosition(trackPoint.Point);
        if (_trackJunctions.TryGetValue(tilePosition, out var junctionId))
        {
            return GetConnections(junctionId);
        }

        return [];
    }

    public IReadOnlyCollection<JunctionConnection> GetConnections(Id<TrackJunction> junctionId)
    {
        if (_junctionConnections.TryGetValue(junctionId, out var connections))
        {
            return connections;
        }

        return [];
    }
    
    private static TilePosition ToTilePosition(Vector3D<float> point) => new((int)point.X, (int)(point.Y * 8), (int)point.Z);
    private static HashSet<JunctionConnection> NewJunctionConnections() => new(1);
}