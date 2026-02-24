using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Utilities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.Geometry;

public class GeometryManager(Provider<GL> glProvider)
{
    private readonly Dictionary<UntypedGeometryId, GeometryData> _geometries = new();

    public Result<GeometryId<TVertex>> Register<TVertex>(
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<uint> indices,
        PrimitiveType primitiveType = PrimitiveType.Triangles,
        BufferUsageARB usage = BufferUsageARB.StaticDraw)
        where TVertex : IVertexData
    {
        var gl = glProvider.Value;
        var floats = MarshalVertices(vertices);

        var meshVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, meshVboHandle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, floats, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        EBO? ebo = null;
        if (indices.Length > 0)
        {
            var eboHandle = gl.CreateBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, eboHandle);
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
            ebo = new EBO(eboHandle, (uint)indices.Length);
        }

        var id = GeometryId<TVertex>.New();
        _geometries[id] = new GeometryData(
            MeshVBO: new VBO(meshVboHandle, (uint)vertices.Length),
            MeshEBO: ebo,
            VertexCount: (uint)vertices.Length,
            PrimitiveType: primitiveType,
            ConfigureMeshAttributes: TVertex.ConfigureAttributes);

        return id;
    }

    public Result<UntypedGeometryId> RegisterDrawArrays(
        uint vertexCount,
        PrimitiveType primitiveType = PrimitiveType.Triangles)
    {
        var id = UntypedGeometryId.New();
        _geometries[id] = new GeometryData(
            MeshVBO: new VBO(0, vertexCount),
            MeshEBO: null,
            VertexCount: vertexCount,
            PrimitiveType: primitiveType,
            ConfigureMeshAttributes: null);

        return id;
    }

    public Result UpdateVertices<TVertex>(
        GeometryId<TVertex> geometryId,
        ReadOnlySpan<TVertex> vertices,
        BufferUsageARB usage = BufferUsageARB.DynamicDraw)
        where TVertex : IVertexData
    {
        if (!_geometries.TryGetValue(geometryId, out var geoData))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        var gl = glProvider.Value;
        var floats = MarshalVertices(vertices);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, geoData.MeshVBO.Handle);
        gl.BufferData(BufferTargetARB.ArrayBuffer, floats, usage);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        _geometries[geometryId] = geoData with
        {
            MeshVBO = geoData.MeshVBO with { VertexCount = (uint)vertices.Length },
            VertexCount = (uint)vertices.Length,
        };

        return Result.Success();
    }

    public Result Deregister(UntypedGeometryId geometryId)
    {
        if (!_geometries.Remove(geometryId, out var geoData))
        {
            return new ResultProblem("Geometry with id '{0}' is not registered", geometryId);
        }

        var gl = glProvider.Value;
        if (geoData.MeshVBO.Handle != 0)
            gl.DeleteBuffer(geoData.MeshVBO.Handle);
        if (geoData.MeshEBO is { } ebo)
            gl.DeleteBuffer(ebo.Handle);

        return Result.Success();
    }

    internal bool TryGet(UntypedGeometryId geometryId, out GeometryData geoData)
    {
        return _geometries.TryGetValue(geometryId, out geoData!);
    }

    internal static float[] MarshalVertices<T>(ReadOnlySpan<T> vertices) where T : IVertexData
    {
        var floats = new float[vertices.Length * T.FloatCount];
        var span = floats.AsSpan();
        var offset = 0;
        foreach (var vertex in vertices)
        {
            vertex.WriteTo(span.Slice(offset, T.FloatCount));
            offset += T.FloatCount;
        }

        return floats;
    }
}
