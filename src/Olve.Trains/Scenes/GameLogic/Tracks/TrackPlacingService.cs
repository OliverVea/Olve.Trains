using Microsoft.Extensions.Logging;
using Olve.Engine3D;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackPlacingService(ILogger<TrackPlacingService> logger, TrackService trackService)
{
    public Result<IReadOnlyList<Id<Track>>> PlaceTrack(TrackEndpoint startEndpoint, TrackEndpoint endEndpoint)
    {
        var delta = endEndpoint.Point - startEndpoint.Point;
        if (delta.Length < MathConstants.Epsilon)
        {
            return Result.Success<IReadOnlyList<Id<Track>>>([]);
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

        var createdIds = new List<Id<Track>>();
        foreach (var (start, end) in tracks)
        {
            if (PlaceSingleTrack(start, end).TryPickProblems(out var problems, out var trackId))
            {
                return problems;
            }

            createdIds.Add(trackId);
        }

        return createdIds;
    }

    private Result<Id<Track>> PlaceSingleTrack(TrackEndpoint startEndpoint, TrackEndpoint endEndpoint)
    {
        var pointDistance = (startEndpoint.Point - endEndpoint.Point).Length;
        if (pointDistance < 0.01f)
        {
            logger.LogWarning("Tried to place track with distance 0");
            return Id.New<Track>();
        }

        startEndpoint = startEndpoint with { Tangent = -startEndpoint.Tangent };

        if (trackService.AddTrack(startEndpoint, endEndpoint).TryPickProblems(out var problems, out var trackId))
        {
            return problems.Prepend("Failed to add track");
        }

        logger.LogInformation("Created track with id '{TrackId}'", trackId);
        logger.LogDebug("Placed track from {StartEndpoint} to {EndEndpoint}", startEndpoint, endEndpoint);

        return trackId;
    }
}
