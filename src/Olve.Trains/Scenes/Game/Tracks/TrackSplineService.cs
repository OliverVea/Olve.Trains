using Olve.Engine3D.Math;
using Olve.Engine3D.Math.Splines;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackSplineService(TrackService trackService) : BaseEntityAuxiliaryService<Track>(trackService)
{
    private readonly Dictionary<Id<Track>, UniformHermite<Vector3D<float>>> _trackSplines = [];

    protected override void OnRemoved(Id<Track> id) => _trackSplines.Remove(id);

    public Result<Vector3D<float>> GetPoint(Id<Track> trackId, float time)
        => WithTrackSpline(trackId, spline => spline.GetPoint(time));

    public Result<(Vector3D<float> Start, Vector3D<float> End)> GetEnds(Id<Track> trackId)
        => WithTrackSpline(trackId, spline => spline.GetEnds());

    public Result<float> GetLength(Id<Track> trackId)
        => WithTrackSpline(trackId, spline => spline.Length);

    public Result<Vector3D<float>> GetTangent(Id<Track> trackId, float time)
        => WithTrackSpline(trackId, spline => spline.GetTangent(time));

    public Result<Position3D> GetPosition(Id<Track> trackId, float time)
        => WithTrackSpline(trackId, spline => spline.GetPosition(time));

    public Result<IEnumerable<Vector3D<float>>> GetPoints(Id<Track> trackId, int count)
        => WithTrackSpline(trackId, spline => spline.GetPoints(count));

    public Result<Vector3D<float>> GetSecondDerivative(Id<Track> trackId, float time)
        => WithTrackSpline(trackId, spline => spline.GetSecondDerivative(time));

    public Result<float> GetCurvature(Id<Track> trackId, float time)
        => WithTrackSpline(trackId, spline => spline.GetCurvature(time));

    public Result<bool> GetClosestTrackPoint(Vector3D<float> position, float maxDistance, out TrackPoint closestTrackPoint)
    {
        List<(TrackPoint TrackPoint, Vector3D<float> WorldPosition)> points = [];

        foreach (var trackId in _trackSplines.Keys)
        {
            if (GetClosestTrackPoint(trackId, position, maxDistance, out var trackPoint)
                .TryPickProblems(out var problems, out var foundPoint))
            {
                closestTrackPoint = default;
                return problems;
            }

            if (foundPoint)
            {
                var worldPosition = _trackSplines[trackId].Sample(trackPoint.Time);
                points.Add((trackPoint, worldPosition));
            }
        }

        closestTrackPoint = points
            .OrderBy(x => (x.WorldPosition - position).LengthSquared)
            .Select(x => x.TrackPoint)
            .FirstOrDefault();
        return points.Count > 0;
    }

    public Result<bool> GetClosestTrackPoint(Id<Track> trackId, Vector3D<float> target, float maxDistance, out TrackPoint closestTrackPoint)
    {
        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            closestTrackPoint = default;
            return problems.Prepend("Failed to get spline");
        }

        if (!spline.TryGetClosestTime(target, maxDistance, out var closestT))
        {
            closestTrackPoint = default;
            return false;
        }

        closestTrackPoint = new TrackPoint(trackId, closestT);
        var closestLengthSquared = (spline.Sample(closestT) - target).LengthSquared;
        var maxDistanceSquared = maxDistance * maxDistance;
        return closestLengthSquared < maxDistanceSquared;
    }

    private Result<T> WithTrackSpline<T>(Id<Track> trackId, Func<UniformHermite<Vector3D<float>>, Result<T>> transform)
    {
        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            return problems.Prepend("Failed to get spline");
        }

        return transform(spline);
    }

    private Result<T> WithTrackSpline<T>(Id<Track> trackId, Func<UniformHermite<Vector3D<float>>, T> transform)
    {
        if (GetOrAddSpline(trackId).TryPickProblems(out var problems, out var spline))
        {
            return problems.Prepend("Failed to get spline");
        }

        return transform(spline);
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

    private Result<UniformHermite<Vector3D<float>>> CreateSpline(Id<Track> trackId)
    {
        if (!trackService.TryGet(trackId, out var track))
        {
            return new ResultProblem("Failed to get track");
        }

        return CreateSpline(track.Start, track.End);
    }

    public UniformHermite<Vector3D<float>> CreateSpline(TrackEndpoint start, TrackEndpoint end)
    {
        var tangentScale = (start.Point - end.Point).Length;
        var startTangent = -start.Tangent * tangentScale;
        var endTangent = end.Tangent * tangentScale;

        Hermite3.Knot startKnot = new(start.Point, startTangent, startTangent);
        Hermite3.Knot endKnot = new(end.Point, endTangent, endTangent);

        KeyFrame<Hermite3.Knot> startKeyFrame = new(0, startKnot);
        KeyFrame<Hermite3.Knot> endKeyFrame = new(1, endKnot);

        var hermite = new Hermite3([startKeyFrame, endKeyFrame]);

        return new UniformHermite<Vector3D<float>>(hermite, new Vector3Metric(), 128);
    }
}