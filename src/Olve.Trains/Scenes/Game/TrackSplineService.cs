using Olve.Engine3D.Math.Splines;
using Olve.Results;
using Olve.Utilities.Ids;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game;

public class TrackSplineService(TrackService trackService)
{
    private const float StartTime = 0.0f;
    private const float EndTime = 1.0f;
    
    public Result<Hermite3> GetSpline(Id<Track> trackId)
    {
        if (!trackService.TryGetTrack(trackId, out var track))
        {
            return new ResultProblem("Failed to get track");
        }

        Hermite3.Knot startKnot = new(track.Start.Point, track.Start.Tangent, track.Start.Tangent);
        Hermite3.Knot endKnot = new(track.End.Point, track.End.Tangent, track.End.Tangent);
        
        KeyFrame<Hermite3.Knot> startKeyFrame = new(StartTime, startKnot);
        KeyFrame<Hermite3.Knot> endKeyFrame = new(EndTime, endKnot);
        
        return new Hermite3([startKeyFrame, endKeyFrame]);
    }

    public Result<Vector3D<float>> GetPoint(Id<Track> trackId, float time)
    {
        if (time is < StartTime or > EndTime)
        {
            return new ResultProblem("Time must be between {0} and {1}", StartTime, EndTime);
        }
        
        if (GetSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            return problems.Prepend("Failed to get spline");
        }
        
        return spline.Sample(time);
    }

    public Result<Vector3D<float>[]> GetPoints(Id<Track> trackId, int count)
    {
        if (count < 1)
        {
            return new ResultProblem("Count must be greater than 0");
        }
        
        if (GetSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            return problems.Prepend("Failed to get spline");
        }
        
        var points = new Vector3D<float>[count];
        
        for (var i = 0; i < count; i++)
        {
            var time = StartTime + (EndTime - StartTime) * i / (count - 1);
            points[i] = spline.Sample(time);
        }

        return points;
    }
}