namespace Olve.Engine3D;

public readonly record struct TileCoordinate(int X, int Z);

public readonly record struct TilePosition(int X, int Y, int Z)
{
    public Matrix4X4<float> ToWorldMatrix()
    {
        return Matrix4X4.CreateTranslation(X + 0.5f, Y + 0.5f, Z);
    }
    
    public Matrix4X4<float> ToWorldMatrix(Quaternion<float> rotation)
    {
        return Matrix4X4.CreateFromQuaternion(rotation) * Matrix4X4.CreateTranslation(X + 0.5f, Y + 0.5f, Z);
    }
}