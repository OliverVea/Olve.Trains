using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Trains.Scenes.GameLogic.Money;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackPlacingService(ILogger<TrackPlacingService> logger, TrackService trackService, MoneyService moneyService)
{
    public Result<IReadOnlyList<Id<Track>>> PlaceTrack(TrackEndpoint startEndpoint, TrackEndpoint endEndpoint)
    {
        var delta = endEndpoint.Point - startEndpoint.Point;
        if (delta.Length < MathConstants.Epsilon)
        {
            return Result.Success<IReadOnlyList<Id<Track>>>([]);
        }

        var estimatedLength = delta.Length;
        var cost = (int)MathF.Ceiling(estimatedLength * MoneyConstants.TrackCostPerMeter);
        // TODO: The cost is charged before any track is placed. If a sub-track fails below, the charge is kept and
        // the tracks already placed stay. Refund on failure (single segment: full refund; several sub-tracks: decide
        // between rolling back the placed tracks and a partial refund). See the result-handling epic in TODO.md.
        if (!moneyService.TryCharge(cost, $"place track ({estimatedLength:F1}m)"))
        {
            return new ResultProblem("Cannot afford track: need {0}, have {1}", cost, moneyService.Balance);
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

        logger.LogInformation(
            "Created track '{TrackId}' from ({StartX:F2},{StartY:F2},{StartZ:F2}) to ({EndX:F2},{EndY:F2},{EndZ:F2}) dir ({StartDirX:F2},{StartDirY:F2},{StartDirZ:F2})->({EndDirX:F2},{EndDirY:F2},{EndDirZ:F2})",
            trackId,
            startEndpoint.Point.X, startEndpoint.Point.Y, startEndpoint.Point.Z,
            endEndpoint.Point.X, endEndpoint.Point.Y, endEndpoint.Point.Z,
            startEndpoint.Tangent.X, startEndpoint.Tangent.Y, startEndpoint.Tangent.Z,
            endEndpoint.Tangent.X, endEndpoint.Tangent.Y, endEndpoint.Tangent.Z);

        return trackId;
    }
}
