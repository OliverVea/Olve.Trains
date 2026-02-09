using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Textures;

public class RGBPixelFormat : IPixelFormat<RGB>
{
    public static InternalFormat InternalFormat => InternalFormat.Rgb;
    public static PixelFormat PixelFormat => PixelFormat.Rgb;
    public static PixelType PixelType => PixelType.UnsignedByte;
    public static int BytesPerPixel => 3;

    public static void WriteBytes(ReadOnlySpan<RGB> pixels, Span<byte> destination)
    {
        for (var i = 0; i < pixels.Length; i++)
        {
            var (r, g, b) = pixels[i].ToBytes();
            destination[i * BytesPerPixel + 0] = r;
            destination[i * BytesPerPixel + 1] = g;
            destination[i * BytesPerPixel + 2] = b;
        }
    }
}