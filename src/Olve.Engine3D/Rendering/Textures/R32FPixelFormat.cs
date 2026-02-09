using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Textures;

public readonly struct R32FPixelFormat : IPixelFormat<float>
{
    public static InternalFormat InternalFormat => InternalFormat.R32f;
    public static PixelFormat PixelFormat => PixelFormat.Red;
    public static PixelType PixelType => PixelType.Float;
}