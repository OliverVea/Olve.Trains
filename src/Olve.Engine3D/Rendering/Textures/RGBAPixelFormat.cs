using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Textures;

public readonly struct RGBAPixelFormat : IPixelFormat<RGBA>
{
    public static InternalFormat InternalFormat => InternalFormat.Rgba;
    public static PixelFormat PixelFormat => PixelFormat.Rgba;
    public static PixelType PixelType => PixelType.UnsignedByte;
    public static int BytesPerPixel => 4;

    public static void WriteBytes(ReadOnlySpan<RGBA> pixels, Span<byte> destination)
    {
        for (var i = 0; i < pixels.Length; i++)
        {
            var (r, g, b, a) = pixels[i].ToBytes();
            destination[i * 4 + 0] = r;
            destination[i * 4 + 1] = g;
            destination[i * 4 + 2] = b;
            destination[i * 4 + 3] = a;
        }
    }
}