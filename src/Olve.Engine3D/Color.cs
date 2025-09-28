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
}

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
}