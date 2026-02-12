using Olve.Engine3D.Math;
using Olve.Engine3D.Math.Splines;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public static class HermiteSplineExtensions
{
    private static readonly ResultProblem TimeInvalidProblem = new("Time must be between {0} and {1}", StartTime, EndTime);

    public const float StartTime = 0f;
    public const float EndTime = 1f;

    extension(UniformHermite<Vector3D<float>> spline)
    {
        public Result<Vector3D<float>> GetPoint(float time)
        {
            if (time is > EndTime or < StartTime)
            {
                return TimeInvalidProblem;
            }

            return spline.Sample(time);
        }

        public (Vector3D<float> Start, Vector3D<float> End) GetEnds()
        {
            return (spline.Sample(StartTime), spline.Sample(EndTime));
        }

        public Result<Vector3D<float>> GetTangent(float time)
        {
            if (time is > EndTime or < StartTime)
            {
                return TimeInvalidProblem;
            }

            return spline.Tangent(time);
        }

        public Result<Position3D> GetPosition(float time)
        {
            if (time is > EndTime or < StartTime)
            {
                return TimeInvalidProblem;
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

        public Result<IEnumerable<Vector3D<float>>> GetPoints(int count)
        {
            return GetTimes(count)
                .MapValue(x => x
                    .Select(spline.Sample));
        }

        public bool TryGetClosestTime(Vector3D<float> target,
            float maxDistance,
            out float closestT)
        {
            closestT = -1f;
            var deltaStart = target - spline.Sample(0);
            var deltaEnd = target - spline.Sample(1);

            var maxDistanceSquared = maxDistance * maxDistance;
            var splineLengthSquared = spline.Length * spline.Length;

            if (deltaStart.LengthSquared > maxDistanceSquared + splineLengthSquared &&
                deltaEnd.LengthSquared > maxDistanceSquared + splineLengthSquared)
            {
                return false;
            }

            const int sampleCount = 100;
            const float sampleDist = 1f / sampleCount;

            var closestLengthSquared = float.MaxValue;
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
                return false;
            }

            return closestLengthSquared < maxDistanceSquared;
        }

        public Result<IEnumerable<float>> GetCurvatures(int count)
        {
            return GetTimes(count)
                .MapValue(x => x
                    .Select(spline.GetCurvatureSafe));
        }

        public Result<float> GetCurvature(float time)
        {
            if (time is > EndTime or < StartTime)
            {
                return TimeInvalidProblem;
            }

            return spline.GetCurvatureSafe(time);
        }

        private float GetCurvatureSafe(float time)
        {
            var d1 = spline.Tangent(time);
            var d2 = spline.GetSecondDerivative(time);

            var speed = d1.Length;
            if (speed <= 1e-6f)
            {
                return float.PositiveInfinity;
            }

            var curvature =
                Vector3D.Cross(d1, d2).Length /
                (speed * speed * speed);

            return curvature;
        }

        public Vector3D<float> GetSecondDerivative(float time)
        {
            const float eps = 0.001f;

            var t1 = Math.Clamp(time - eps, StartTime, EndTime);
            var t2 = Math.Clamp(time + eps, StartTime, EndTime);

            var d1 = spline.Tangent(t1);
            var d2 = spline.Tangent(t2);

            return (d2 - d1) / (t2 - t1);
        }
    }

    public static Result<IEnumerable<float>> GetTimes(int count)
    {
        if (count < 1)
        {
            return new ResultProblem("Count must be greater than 0");
        }

        return Result.Success(Enumerable
            .Range(0, count)
            .Select(i => StartTime + (EndTime - StartTime) * i / (count - 1)));
    }
}