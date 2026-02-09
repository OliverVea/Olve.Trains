namespace Olve.Engine3D;

public readonly record struct RGB(float R, float G, float B)
{
    public (byte R, byte G, byte B) ToBytes()
    {
        return (Pack(R), Pack(G), Pack(B));
    }

    private static byte Pack(float v) => (byte)float.Clamp(v * 255f + 0.5f, 0f, 255f);
    public Vector3D<float> ToVector() => new(R, G, B);
    public RGBA ToRGBA(float a = 1) => new(R, G, B, a);
    public static RGB FromVector(Vector3D<float> vector) => new(vector.X, vector.Y, vector.Z);

    public static implicit operator RGB((float R, float G, float B) tuple) => new(tuple.R, tuple.G, tuple.B);
}