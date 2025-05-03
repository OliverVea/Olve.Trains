using Olve.Engine3D.Scenes;
using Olve.Results;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game;

public class GraphService(TrackService trackService) : SceneService
{
    public readonly record struct GraphNode(int X, int Y, int Z);
    
    private readonly Dictionary<GraphNode, List<Id<Track>>> _graph = new();
    
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(100);
    
    private readonly HashSet<Id<Track>> _tracksToUpdate = [];
    
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
        return Result.Success();
    }

    public override Result Unload()
    {
        trackService.OnTrackAdded -= OnTrackAdded;
        trackService.OnTrackRemoved -= OnTrackRemoved;
        
        return Result.Success();
    }
}