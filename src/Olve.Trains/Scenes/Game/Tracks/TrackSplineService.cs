using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Math;
using Olve.Engine3D.Math.Splines;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackSplineService(ILoggingManager loggingManager,
    TrackService trackService) : BaseEntityAuxiliaryService<Track>(loggingManager, trackService)
{
    private static readonly ResultProblem TimeInvalidProblem = new("Time must be between {0} and {1}", StartTime, EndTime);
    
    private const float StartTime = 0.0f;
    private const float EndTime = 1.0f;

    private readonly Dictionary<Id<Track>, UniformHermite<Vector3D<float>>> _trackSplines = new();
    
    protected override void OnRemoved(Id<Track> id)
    {
        _trackSplines.Remove(id);
    }

    private bool TryGetSpline(Id<Track> trackId, [MaybeNullWhen(false)] out UniformHermite<Vector3D<float>> trackSpline, [MaybeNullWhen(true)] out ResultProblemCollection problems)
    {
        if (!_trackSplines.TryGetValue(trackId, out trackSpline))
        {
            if (CreateSpline(trackId).TryPickProblems(out problems, out trackSpline))
            {
                return false;
            }

            _trackSplines[trackId] = trackSpline;
        }

        problems = null;
        return true;
    }

    public Result<Vector3D<float>> GetPoint(Id<Track> trackId, float time)
    {
        if (time is > EndTime or < StartTime)
        {
            return TimeInvalidProblem;
        }
        
        if (!TryGetSpline(trackId, out var spline, out var problems))
        {
            return problems.Prepend("Failed to get spline");
        }
        
        return spline.Sample(time);
    }

    public Result<float> GetLength(Id<Track> trackId)
    {
        if (!TryGetSpline(trackId, out var spline, out var problems))
        {
            return problems.Prepend("Failed to get spline");
        }

        return spline.Length;
    }

    public Result<Vector3D<float>> GetTangent(Id<Track> trackId, float time)
    {
        if (time is > EndTime or < StartTime)
        {
            return TimeInvalidProblem;
        }
        
        if (!TryGetSpline(trackId, out var spline, out var problems))
        {
            return problems.Prepend("Failed to get spline");
        }
        
        return spline.Tangent(time);
    }

    public Result<Position3D> GetPosition(Id<Track> trackId, float time)
    {
        if (time is > EndTime or < StartTime)
        {
            return TimeInvalidProblem;
        }
        
        if (!TryGetSpline(trackId, out var spline, out var problems))
        {
            return problems.Prepend("Failed to get spline");
        }

        var position = spline.Sample(time);
        var tangent = spline.Tangent(time);

        if (tangent.Length <= 1e-6f)
        {
            return new ResultProblem("Tangent is zero-length at time {0}", time);
        }

        tangent = Vector3D.Normalize(tangent);
        
        var up = Vector3D<float>.UnitY;
        var dot = Vector3D.Dot(tangent, up);
        if (MathF.Abs(dot) > 0.999f)
        {
            up = Vector3D<float>.UnitZ;
        }
        
        var world = Matrix4X4.CreateWorld(position, tangent, up);
        if (!Matrix4X4.Decompose(world, out _, out var rotation, out _))
        {
            return new ResultProblem("Failed to compute rotation at time {0}", time);
        }
        
        return new Position3D(position, rotation);
    }

    public Result<Vector3D<float>[]> GetPoints(Id<Track> trackId, int count)
    {
        if (count < 1)
        {
            return new ResultProblem("Count must be greater than 0");
        }
        
        if (!TryGetSpline(trackId, out var spline, out var problems))
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
    
    private Result<UniformHermite<Vector3D<float>>> CreateSpline(Id<Track> trackId)
    {
        if (!trackService.TryGet(trackId, out var track))
        {
            return new ResultProblem("Failed to get track");
        }

        var tangentScale = (track.Start.Point - track.End.Point).Length;
        var startTangent = -track.Start.Tangent * tangentScale;
        var endTangent = track.End.Tangent * tangentScale;
        
        Hermite3.Knot startKnot = new(track.Start.Point, startTangent, startTangent);
        Hermite3.Knot endKnot = new(track.End.Point, endTangent, endTangent);
        
        KeyFrame<Hermite3.Knot> startKeyFrame = new(StartTime, startKnot);
        KeyFrame<Hermite3.Knot> endKeyFrame = new(EndTime, endKnot);
        
        var hermite = new Hermite3([startKeyFrame, endKeyFrame]);

        return new UniformHermite<Vector3D<float>>(hermite, new Vector3Metric(), 128);
    }
}