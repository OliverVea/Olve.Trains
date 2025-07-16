using Olve.Engine3D.Scenes;
using Olve.Results;
using Olve.Utilities.Collections;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game;

public class TrackLookupService(TrackService trackService) : SceneService
{
    private static readonly IReadOnlySet<GraphNode> EmptyGraphNodes = new HashSet<GraphNode>();
    private static readonly IReadOnlySet<Id<Track>> EmptyTrackIds = new HashSet<Id<Track>>();
    
    public readonly record struct GraphNode(int X, int Y, int Z);

    private readonly ManyToManyLookup<GraphNode, Id<Track>> _manyToManyLookup = new();
    
    private readonly HashSet<Id<Track>> _tracksToUpdate = [];

    public IReadOnlySet<GraphNode> GetNodesForTrack(Id<Track> trackId)
    {
        return _manyToManyLookup.Get(trackId).Match(x => x, _ => EmptyGraphNodes);
    }

    public IReadOnlySet<Id<Track>> GetTracksForNode(GraphNode graphNode)
    {
        return _manyToManyLookup.Get(graphNode).Match(x => x, _ => EmptyTrackIds);
    }
    
    public override Result Load()
    {
        trackService.OnTrackAdded += OnTrackAdded;
        trackService.OnTrackRemoved += OnTrackRemoved;
        
        return Result.Success();
    }

    private void OnTrackAdded(Track track)
    {
        _tracksToUpdate.Add(track.Id);
    }
    
    private void OnTrackRemoved(Track track)
    {
        _tracksToUpdate.Add(track.Id);
    }

    public override Result Update(TimeSpan deltaTime)
    {
        if (_tracksToUpdate.Count == 0)
        {
            return Result.Success();
        }
        
        foreach (var trackId in _tracksToUpdate)
        {
            if (trackService.TryGetTrack(trackId, out var track))
            {
                var startNode = new GraphNode((int)track.Start.Point.X, (int)track.Start.Point.Y, (int)track.Start.Point.Z);
                var endNode = new GraphNode((int)track.End.Point.X, (int)track.End.Point.Y, (int)track.End.Point.Z);

                _manyToManyLookup.Set(startNode, trackId, true);
                _manyToManyLookup.Set(endNode, trackId, true);
            }
            else
            {
                _manyToManyLookup.Remove(trackId);
            }
        }

        _tracksToUpdate.Clear();
        
        return Result.Success();
    }

    public override Result Unload()
    {
        trackService.OnTrackAdded -= OnTrackAdded;
        trackService.OnTrackRemoved -= OnTrackRemoved;
        
        return Result.Success();
    }
}