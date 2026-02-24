using Olve.Engine3D.Rendering.Primitives;

namespace Olve.Engine3D.Rendering;

public static class RenderingManager3DExtensions
{
    public static Result<GeometryId> RegisterUnitCube<TVertex>(
        this RenderingManager3D renderingManager3D,
        Func<Vector3D<float>, Vector3D<float>, TVertex> vertexFactory)
        where TVertex : IVertexData
    {
        Span<Vector3D<float>> positions = stackalloc Vector3D<float>[UnitCube.VertexCount];
        Span<Vector3D<float>> normals = stackalloc Vector3D<float>[UnitCube.VertexCount];
        UnitCube.GetVertices(positions, normals);

        var vertices = new TVertex[UnitCube.VertexCount];
        for (var i = 0; i < UnitCube.VertexCount; i++)
            vertices[i] = vertexFactory(positions[i], normals[i]);

        Span<uint> indices = stackalloc uint[UnitCube.IndexCount];
        UnitCube.GetIndices(indices);

        return renderingManager3D.RegisterGeometry<TVertex>(vertices, indices);
    }
}
