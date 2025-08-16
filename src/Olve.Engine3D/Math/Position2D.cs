namespace Olve.Engine3D.Math;

public readonly record struct Position2D(Vector2D<float> Origin, float Rotation)
{
    public Matrix3X3<float> ToMatrix3X3()
    {
        var c = MathF.Cos(Rotation);
        var s = MathF.Sin(Rotation);
        
        return new Matrix3X3<float>(
            c,  -s, Origin.X,
            s,   c, Origin.Y,
            0f,  0f, 1f
        );
        
    }
}