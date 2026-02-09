using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Textures;

public interface IPixelFormat<T> where T : unmanaged
{
    static abstract InternalFormat InternalFormat { get; }
    static abstract PixelFormat PixelFormat { get; }
    static abstract PixelType PixelType { get; }

    static virtual int BytesPerPixel => Unsafe.SizeOf<T>();

    static virtual void WriteBytes(ReadOnlySpan<T> pixels, Span<byte> destination)
    {
        MemoryMarshal.AsBytes(pixels).CopyTo(destination);
    }
}