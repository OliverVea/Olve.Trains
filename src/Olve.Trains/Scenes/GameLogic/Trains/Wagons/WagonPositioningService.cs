using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class WagonPositioningService(
    TrainWagonService trainWagonService,
    TrackSplineService trackSplineService,
    TrainTrackHistoryService trainTrackHistoryService,
    WagonBlueprintService wagonBlueprintService)
{
    private const float Scale = TrainWorldMatrix.TrainScale;
    private const float LocomotiveLength = 1.0f * Scale;
    private const float DefaultWagonLength = 1.0f * Scale;
    private const float CouplingGap = 0.1f * Scale;

    public readonly record struct WagonPosition(Id<Wagon> WagonId, Id<Track> TrackId, float Time, float Velocity);

    public Result<IReadOnlyList<WagonPosition>> GetWagonPositions(Id<Train> trainId, TrainTrackPosition trackPosition)
    {

        var wagons = trainWagonService.GetWagons(trainId);
        if (wagons.Count == 0)
        {
            return Array.Empty<WagonPosition>();
        }

        var positions = new WagonPosition[wagons.Count];
        var cumulativeOffset = LocomotiveLength / 2f + CouplingGap;

        for (var i = 0; i < wagons.Count; i++)
        {
            var wagon = wagons[i];

            var wagonLength = DefaultWagonLength;
            if (wagonBlueprintService.TryGet(wagon.BlueprintId, out var blueprint))
            {
                wagonLength = blueprint.Length * Scale;
            }

            var offset = cumulativeOffset + wagonLength / 2f;

            if (ComputeWagonPosition(trainId, trackPosition, offset)
                .TryPickProblems(out var problems, out var wagonPos))
            {
                return problems.Prepend("Failed to compute position for wagon '{0}'", wagon.Id);
            }

            positions[i] = new WagonPosition(wagon.Id, wagonPos.TrackId, wagonPos.Time, wagonPos.Velocity);
            cumulativeOffset += wagonLength + CouplingGap;
        }

        return positions;
    }

    private Result<(Id<Track> TrackId, float Time, float Velocity)> ComputeWagonPosition(
        Id<Train> trainId,
        TrainTrackPosition locoPosition,
        float offset)
    {
        var trackId = locoPosition.TrackId;
        var velocity = locoPosition.Velocity;

        if (trackSplineService.GetLength(trackId).TryPickProblems(out var problems, out var trackLength))
        {
            return problems;
        }

        var locoArcDist = locoPosition.Time * trackLength;

        float availableBehind;
        if (velocity > 0)
        {
            availableBehind = locoArcDist;
        }
        else
        {
            availableBehind = trackLength - locoArcDist;
        }

        if (offset <= availableBehind)
        {
            float wagonTime;
            if (velocity > 0)
            {
                wagonTime = (locoArcDist - offset) / trackLength;
            }
            else
            {
                wagonTime = (locoArcDist + offset) / trackLength;
            }

            return (trackId, wagonTime, velocity);
        }

        var overflow = offset - availableBehind;
        var history = trainTrackHistoryService.GetHistory(trainId);

        return WalkHistory(trackId, velocity, overflow, history);
    }

    private Result<(Id<Track> TrackId, float Time, float Velocity)> WalkHistory(
        Id<Track> currentTrackId,
        float currentVelocity,
        float overflow,
        IReadOnlyList<TrainTrackHistoryService.TrackHistoryEntry> history)
    {
        foreach (var entry in history)
        {
            if (trackSplineService.GetLength(entry.TrackId).TryPickProblems(out var problems, out var prevTrackLength))
            {
                return problems;
            }

            if (overflow <= prevTrackLength)
            {
                float wagonTime;
                if (entry.Velocity > 0)
                {
                    wagonTime = (prevTrackLength - overflow) / prevTrackLength;
                }
                else
                {
                    wagonTime = overflow / prevTrackLength;
                }

                return (entry.TrackId, wagonTime, entry.Velocity);
            }

            overflow -= prevTrackLength;
        }

        if (history.Count > 0)
        {
            var lastEntry = history[^1];
            var clampTime = lastEntry.Velocity > 0 ? 0f : 1f;
            return (lastEntry.TrackId, clampTime, lastEntry.Velocity);
        }

        var clampToStart = currentVelocity > 0 ? 0f : 1f;
        return (currentTrackId, clampToStart, currentVelocity);
    }
}
