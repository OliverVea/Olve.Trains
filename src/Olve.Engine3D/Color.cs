namespace Olve.Engine3D;

public readonly record struct RGB(float R, float G, float B)
{
    public (byte R, byte G, byte B) ToBytes()
    {
        return (Pack(R), Pack(G), Pack(B));
        
    }
    
    private static byte Pack(float v) => (byte)float.Clamp(v * 255f + 0.5f, 0f, 255f);
    public Vector3D<float> ToVector() => new(R, G, B);
}

public readonly record struct RGBA(float R, float G, float B, float A)
{
    public (byte R, byte G, byte B, byte A) ToBytes()
    {
        return (Pack(R), Pack(G), Pack(B), Pack(A));
        
    }
    
    private static byte Pack(float v) => (byte)float.Clamp(v * 255f + 0.5f, 0f, 255f);
    public Vector4D<float> ToVector() => new(R, G, B, A);
}

public readonly record struct LinearRGB(RGB RGB)
{
    public LinearRGB(float r, float g, float b) : this(new RGB(r, g, b)) {}
    
    public static LinearRGB operator *(LinearRGB c, float s) => new(c.RGB.R * s, c.RGB.G * s, c.RGB.B * s);
    public static LinearRGB operator +(LinearRGB a, LinearRGB b) => new(a.RGB.R + b.RGB.R, a.RGB.G + b.RGB.G, a.RGB.B + b.RGB.B);
    
    public static implicit operator RGB(LinearRGB a) => a.RGB;
}


public readonly record struct PremultipliedRGBA
{
    public LinearRGB RGB { get; }
    public float A { get; }

    public PremultipliedRGBA(RGB rgb, float a)
    {
        RGB = new LinearRGB(rgb.R, rgb.G, rgb.B);
        A = a;
    }
    
    public static PremultipliedRGBA FromStraight(float r, float g, float b, float a)
        => new(new RGB(r * a, g * a, b * a), a);
    

    public PremultipliedRGBA BlendOver(PremultipliedRGBA dst)
    {
        var inv = 1f - A;
        return new PremultipliedRGBA(dst.RGB * inv + RGB, A + dst.A * inv);
    }

    public PremultipliedRGBA Scale(float s) => new(RGB * s, A);
    public static PremultipliedRGBA operator *(PremultipliedRGBA c, float s) => c.Scale(s);
    public static PremultipliedRGBA operator *(float s, PremultipliedRGBA c) => c.Scale(s);

    public (byte R, byte G, byte B, byte A) ToBytes()
    {
        RGB rgb = RGB;
        return (Pack(rgb.R), Pack(rgb.G), Pack(rgb.B), Pack(A));
        
        static byte Pack(float v) => (byte)float.Clamp(v * 255f + 0.5f, 0f, 255f);
    }
}