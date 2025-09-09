using Olve.Engine3D;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackPlacingService(ILoggingManager loggingManager, TrackService trackService)
{
    public Result PlaceTrack(TrackPoint startPoint, TrackPoint endPoint)
    {
        var delta = endPoint.Point - startPoint.Point;
        if (delta.Length < MathConstants.Epsilon)
        {
            return Result.Success();
        }

        List<(TrackPoint Start, TrackPoint End)> tracks = [];
            
        if (startPoint.IsOnStraightLineWith(endPoint))
        {
            var deltaNormalized = Vector3D.Normalize(delta);
            var subtracks = float.Round(delta.Length);
            for (var i = 0; i < subtracks; i++)
            {
                var subtrackStart = startPoint.Point + deltaNormalized * i;
                var subtrackEnd = startPoint.Point + deltaNormalized * (i + 1);
                    
                tracks.Add((
                    new TrackPoint(subtrackStart, deltaNormalized), 
                    new TrackPoint(subtrackEnd, deltaNormalized)));
            }
        }
        else
        {
            tracks.Add((startPoint, endPoint));
        }

        var trackResults = tracks.Select(p => PlaceSingleTrack(p.Start, p.End));
        if (trackResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result PlaceSingleTrack(TrackPoint startPoint, TrackPoint endPoint)
    {
        var pointDistance = (startPoint.Point - endPoint.Point).Length;
        if (pointDistance < 0.01f)
        {
            loggingManager.Log(LogLevel.Warning, "Tried to place track with distance 0");
            return Result.Success();
        }
        
        startPoint = startPoint with { Tangent = -startPoint.Tangent };
                
        if (trackService.AddTrack(startPoint, endPoint).TryPickProblems(out var problems, out var trackId))
        {
            return problems.Prepend("Failed to add track");
        }
                
        loggingManager.Log(LogLevel.Info, $"Created track with id '{trackId}'");
        loggingManager.Log(LogLevel.Debug, $"Placed track from {startPoint} to {endPoint}");
        
        return Result.Success();
    }
}