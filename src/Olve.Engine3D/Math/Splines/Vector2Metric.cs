namespace Olve.Engine3D.Math.Splines;

public sealed class Vector2Metric : IArcLengthMetric<Vector2D<float>>
{
    public float Distance(in Vector2D<float> a, in Vector2D<float> b)
        => MathF.Sqrt((a.X - b.X)*(a.X - b.X) + (a.Y - b.Y)*(a.Y - b.Y));
}