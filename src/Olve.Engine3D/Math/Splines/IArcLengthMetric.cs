namespace Olve.Engine3D.Math.Splines;

public interface IArcLengthMetric<T>
{
    float Distance(in T a, in T b);
}