using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering;

public interface IInstanceData
{
    void WriteTo(Span<float> buffer);
    static abstract int FloatCount { get; }
    static abstract void ConfigureAttributes(GL gl);
}

/// <summary>
/// Instance data typed to its associated vertex type.
/// Enables compile-time verification that geometry and instance types match when registering groups.
/// </summary>
public interface IInstanceData<TVertex> : IInstanceData where TVertex : IVertexData;
