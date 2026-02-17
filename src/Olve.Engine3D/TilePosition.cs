namespace Olve.Engine3D;

public readonly record struct TilePosition(int X, int Y, int Z)
{
    public Matrix4X4<float> ToWorldMatrix()
    {
        return Matrix4X4.CreateTranslation(X + 0.5f, Y, Z + 0.5f);
    }

    public Vector3D<float> AsVector3D() => new(X, Y, Z);
}