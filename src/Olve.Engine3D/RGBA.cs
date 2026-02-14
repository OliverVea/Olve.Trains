namespace Olve.Engine3D;

public readonly record struct RGBA(float R, float G, float B, float A)
{
    public (byte R, byte G, byte B, byte A) ToBytes()
    {
        return (Pack(R), Pack(G), Pack(B), Pack(A));

    }

    private static byte Pack(float v) => (byte)float.Clamp(v * 255f + 0.5f, 0f, 255f);
    public Vector4D<float> ToVector() => new(R, G, B, A);
    public RGB ToRGB() => new(R, G, B);
    public static RGBA FromVector(Vector4D<float> vector) => new(vector.X, vector.Y, vector.Z, vector.W);

    public static readonly RGBA White = new(1f, 1f, 1f, 1f);
    public static readonly RGBA LightGrey = new(0.75f, 0.75f, 0.75f, 1f);
    public static readonly RGBA Black = new(0f, 0f, 0f, 1f);
    public static readonly RGBA Transparent = new(0f, 0f, 0f, 0f);

    public static implicit operator RGBA((float R, float G, float B, float A) tuple) => new(tuple.R, tuple.G, tuple.B, tuple.A);
}