namespace Olve.Engine3D.Math;

public readonly record struct Position3D(Vector3D<float> Position, Quaternion<float> Rotation)
{
    public Matrix4X4<float> ToMatrix4X4()
    {
        var q = Quaternion<float>.Normalize(Rotation);
        var m = Matrix4X4.CreateFromQuaternion(q);

        m.M41 = Position.X;
        m.M42 = Position.Y;
        m.M43 = Position.Z;

        return m;
    }
}