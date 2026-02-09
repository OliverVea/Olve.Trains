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
}