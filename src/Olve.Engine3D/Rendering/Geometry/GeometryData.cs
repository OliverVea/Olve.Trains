using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Geometry;

internal sealed record GeometryData(
    VBO MeshVBO,
    EBO? MeshEBO,
    uint VertexCount,
    PrimitiveType PrimitiveType,
    Action<GL>? ConfigureMeshAttributes);
