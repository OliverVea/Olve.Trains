using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public interface IVertexData
{
    void WriteTo(Span<float> buffer);
    static abstract int FloatCount { get; }
    static abstract void ConfigureAttributes(GL gl);
}
