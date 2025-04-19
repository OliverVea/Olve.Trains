using Olve.Engine3D.Math.Splines;
using Olve.Results;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class TrackService
{
    private readonly List<Hermite3> _tracks = [];
    
    public IReadOnlyList<Hermite3> Tracks => _tracks;
    
    public Action<Hermite3>? OnTrackAdded { get; set; }
    public Action<Hermite3>? OnTrackRemoved { get; set; }
    
    public Result AddTrack(TrackPoint start, TrackPoint end)
    {
        var distance = Vector3D.Distance(start.Point, end.Point);
        
        var startTangent = start.Direction.ToVector3D() * distance;
        var endTangent = end.Direction.ToVector3D() * distance;
        
        var track = new Hermite3([
            new(0, new(start.Point, startTangent, startTangent)),
            new(1, new(end.Point, endTangent, endTangent))
        ]);
        
        _tracks.Add(track);
        
        OnTrackAdded?.Invoke(track);
        
        return Result.Success();
    }
}