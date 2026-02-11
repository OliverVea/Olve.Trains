using Olve.Engine3D;
using Microsoft.Extensions.Logging;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackPlacingService(ILogger<TrackPlacingService> logger, TrackService trackService)
{
    public Result PlaceTrack(TrackEndpoint startEndpoint, TrackEndpoint endEndpoint)
    {
        var delta = endEndpoint.Point - startEndpoint.Point;
        if (delta.Length < MathConstants.Epsilon)
        {
            return Result.Success();
        }

        List<(TrackEndpoint Start, TrackEndpoint End)> tracks = [];
            
        if (startEndpoint.IsOnStraightLineWith(endEndpoint))
        {
            var deltaNormalized = Vector3D.Normalize(delta);
            var subtracks = float.Round(delta.Length);
            for (var i = 0; i < subtracks; i++)
            {
                var subtrackStart = startEndpoint.Point + deltaNormalized * i;
                var subtrackEnd = startEndpoint.Point + deltaNormalized * (i + 1);
                    
                tracks.Add((
                    new TrackEndpoint(subtrackStart, deltaNormalized), 
                    new TrackEndpoint(subtrackEnd, deltaNormalized)));
            }
        }
        else
        {
            tracks.Add((startEndpoint, endEndpoint));
        }

        var trackResults = tracks.Select(p => PlaceSingleTrack(p.Start, p.End));
        if (trackResults.TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    private Result PlaceSingleTrack(TrackEndpoint startEndpoint, TrackEndpoint endEndpoint)
    {
        var pointDistance = (startEndpoint.Point - endEndpoint.Point).Length;
        if (pointDistance < 0.01f)
        {
            logger.LogWarning("Tried to place track with distance 0");
            return Result.Success();
        }
        
        startEndpoint = startEndpoint with { Tangent = -startEndpoint.Tangent };
                
        if (trackService.AddTrack(startEndpoint, endEndpoint).TryPickProblems(out var problems, out var trackId))
        {
            return problems.Prepend("Failed to add track");
        }
                
        logger.LogInformation("Created track with id '{TrackId}'", trackId);
        logger.LogDebug("Placed track from {StartEndpoint} to {EndEndpoint}", startEndpoint, endEndpoint);
        
        return Result.Success();
    }
}