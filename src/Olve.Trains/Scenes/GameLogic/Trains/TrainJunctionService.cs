using Olve.Engine3D;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainJunctionService(TrainPositionService trainPositionService, TrackService trackService, TrackSplineService trackSplineService, JunctionService junctionService)
{
    public Result<TrainJunction> GetTrainJunction(Id<Train> trainId)
    {
        if (!trainPositionService.TryGetTrackPosition(trainId, out var trackPosition))
        {
            return new ResultProblem("Train does not have a track position - likely not on a track");
        }

        if (!trackService.TryGetTrack(trackPosition.TrackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackPosition.TrackId);
        }

        if (trackPosition.Time < MathConstants.Epsilon && junctionService.TryGetJunctionId(track.Start, out var junctionId)
            || float.Abs(1 - trackPosition.Time) < MathConstants.Epsilon && junctionService.TryGetJunctionId(track.End, out junctionId))
        {
            return new TrainJunction(junctionId);
        }

        return TrainJunction.None;
    }

    public Result<TrackEndpoint> GetTrainTrackPoint(Id<Train> trainId)
    {
        if (!trainPositionService.TryGetTrackPosition(trainId, out var trackPosition))
        {
            return new ResultProblem("Train does not have a track position - likely not on a track");
        }

        if (!trackService.TryGetTrack(trackPosition.TrackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackPosition.TrackId);
        }

        if (trackPosition.Time < 0.5f)
        {
            return track.Start;
        }

        return track.End;
    }

    public Result<bool> IsAtTrackEnd(Id<Train> trainId, Id<Track> trackId)
    {
        if (!trainPositionService.TryGetTrackPosition(trainId, out var trackPosition))
        {
            return new ResultProblem("Train does not have a track position - likely not on a track");
        }

        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Track not found: '{0}'", trackId);
        }

        if (trackSplineService.GetPosition(trackPosition.TrackId, trackPosition.Time).TryPickProblems(out var problems, out var position))
        {
            return problems.Prepend("Failed to sample point with t '{0}' on track with id '{1}' for train with id '{2}'", trackPosition.Time, trackPosition.TrackId, trainId);
        }

        var endDelta = position.Position - track.End.Point;

        return endDelta.Length < MathConstants.Epsilon;
    }
}
