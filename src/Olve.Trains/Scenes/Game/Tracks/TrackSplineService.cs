using Olve.Engine3D.Math;
using Olve.Engine3D.Math.Splines;
using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackSplineService(ILoggingManager loggingManager, TrackService trackService) : BaseEntityAuxiliaryService<Track>(loggingManager, trackService)
{
    private static readonly ResultProblem TimeInvalidProblem = new("Time must be between {0} and {1}", StartTime, EndTime);

    private const float StartTime = 0.0f;
    private const float EndTime = 1.0f;

    private readonly Dictionary<Id<Track>, UniformHermite<Vector3D<float>>> _trackSplines = new();

    protected override void OnRemoved(Id<Track> id)
    {
        _trackSplines.Remove(id);
    }

    private Result<UniformHermite<Vector3D<float>>> GetOrAddSpline(Id<Track> trackId)
    {
        if (_trackSplines.TryGetValue(trackId, out var trackSpline))
        {
            return trackSpline;
        }

        if (CreateSpline(trackId).TryPickProblems(out var problems, out trackSpline))
        {
            return problems;
        }

        _trackSplines[trackId] = trackSpline;

        return trackSpline;
    }

    public Result<Vector3D<float>> GetPoint(Id<Track> trackId, float time)
    {
        if (time is > EndTime or < StartTime)
        {
            return TimeInvalidProblem;
        }

        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            return problems.Prepend("Failed to get spline");
        }

        return spline.Sample(time);
    }

    public Result<(Vector3D<float> Start, Vector3D<float> End)> GetEnds(Id<Track> trackId)
    {

        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            return problems.Prepend("Failed to get spline");
        }

        return (spline.Sample(StartTime), spline.Sample(EndTime));
    }

    public Result<float> GetLength(Id<Track> trackId)
    {
        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
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

        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
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

        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
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

        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
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

    public Result<bool> GetClosestTrackPoint(Vector3D<float> position, float maxDistance, out TrackPoint closestPoint)
    {
        List<TrackPoint> points = [];

        foreach (var trackId in _trackSplines.Keys)
        {
            if (GetClosestTrackPoint(trackId, position, maxDistance, out var point)
                .TryPickProblems(out var problems, out var foundPoint))
            {
                closestPoint = default;
                return problems;
            }

            if (foundPoint)
            {
                points.Add(point);
            }
        }

        closestPoint = points.OrderBy(x => float.Abs((x.Point - position).LengthSquared)).FirstOrDefault();
        return points.Count > 0;
    }

    public Result<bool> GetClosestTrackPoint(Id<Track> trackId, Vector3D<float> target, float maxDistance, out TrackPoint closestTrackPoint)
    {
        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            closestTrackPoint = default;
            return problems.Prepend("Failed to get spline");
        }

        var deltaStart = target - spline.Sample(0);
        var deltaEnd = target - spline.Sample(1);

        var maxDistanceSquared = maxDistance * maxDistance;
        var splineLengthSquared = spline.Length * spline.Length;

        if (deltaStart.LengthSquared > maxDistanceSquared + splineLengthSquared &&
            deltaEnd.LengthSquared > maxDistanceSquared + splineLengthSquared)
        {
            closestTrackPoint = default;
            return false;
        }

        const int sampleCount = 100;
        const float sampleDist = 1f / sampleCount;

        var closestLengthSquared = float.MaxValue;
        var closestT = -1f;

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i * sampleDist;
            var point = spline.Sample(t);

            var distanceSquared = (point - target).LengthSquared;

            if (distanceSquared < closestLengthSquared)
            {
                closestT = t;
                closestLengthSquared = distanceSquared;
            }
        }

        if (closestT < 0)
        {
            closestTrackPoint = default;
            return false;
        }

        var closestPoint =  spline.Sample(closestT);
        var closestTangent =  spline.Tangent(closestT);

        closestTrackPoint = new TrackPoint(closestPoint, closestTangent);
        return closestTangent.LengthSquared > closestLengthSquared;
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