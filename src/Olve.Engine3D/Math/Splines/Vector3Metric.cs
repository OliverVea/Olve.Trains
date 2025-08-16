namespace Olve.Engine3D.Math.Splines;

public sealed class Vector3Metric : IArcLengthMetric<Vector3D<float>>
{
    public float Distance(in Vector3D<float> a, in Vector3D<float> b)
        => MathF.Sqrt((a.X - b.X)*(a.X - b.X) + (a.Y - b.Y)*(a.Y - b.Y) + (a.Z - b.Z)*(a.Z - b.Z));
}