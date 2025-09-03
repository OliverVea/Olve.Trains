namespace Olve.Engine3D;

public static class Vector3DExtensions
{
    public static int CountZeroDimensions(this Vector3D<float> vec)
    {
        var count = 0;
        if (IsZero(vec.X)) count++;
        if (IsZero(vec.Y)) count++;
        if (IsZero(vec.Z)) count++;
        return count;
    }

    private static bool IsZero(float value)
    {
        return float.Abs(value) < Epsilon;
    }
}