using Olve.Results;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game;

public class TrackService
{
    private readonly SortedList<Id<Track>, Track> _tracks = [];
    
    public Action<Track>? OnTrackAdded { get; set; }
    public Action<Track>? OnTrackRemoved { get; set; }
    
    public Result<Id<Track>> AddTrack(TrackPoint start, TrackPoint end)
    {
        var trackId = Id<Track>.New();
        Track track = new(trackId, start, end);
        
        _tracks.Add(track.Id, track);
        
        OnTrackAdded?.Invoke(track);
        
        return track.Id;
    }
    
    public Result RemoveTrack(Id<Track> trackId)
    {
        if (!_tracks.TryGetValue(trackId, out var track))
        {
            return new ResultProblem("Track with id '{0}' not found", trackId);
        }
        
        _tracks.Remove(trackId);
        
        OnTrackRemoved?.Invoke(track);
        
        return Result.Success();
    }
    
    public bool TryGetTrack(Id<Track> trackId, out Track track)
    {
        return _tracks.TryGetValue(trackId, out track);
    }
}