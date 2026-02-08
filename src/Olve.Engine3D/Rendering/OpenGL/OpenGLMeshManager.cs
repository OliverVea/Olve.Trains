using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.Primitives;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLMeshManager(OpenGLBufferManager bufferManager)
    : IOpenGLEntityManager<MeshData, OpenGLBufferManager.Registration>
{
    private const int PositionFields = 3;
    private const int NormalFields = 3;
    private const int TextureFields = 2;
    private const int VertexFields = PositionFields + NormalFields + TextureFields;
    private const int VertexSize = VertexFields * sizeof(float);

    public Result<OpenGLBufferManager.Registration> Register(MeshData meshData)
    {
        if (meshData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        var vertices = new float[meshData.VertexCount * VertexFields];
        meshData.Positions.CopyTo(vertices, VertexFields, offset: 0);
        meshData.Normals.CopyTo(vertices, VertexFields, offset: PositionFields);
        meshData.TextureCoordinates.CopyTo(vertices, VertexFields, offset: PositionFields + NormalFields);

        var indices = new uint[meshData.Indices.Length * 3];
        meshData.Indices.CopyTo(indices);

        return bufferManager.CreateBuffers(
            vertices,
            (uint)meshData.VertexCount,
            indices,
            ConfigureAttributes,
            BufferUsageARB.StaticDraw);
    }

    public Result Unregister(OpenGLBufferManager.Registration registration)
    {
        bufferManager.DeleteBuffers(registration);
        return Result.Success();
    }

    private static void ConfigureAttributes(GL gl)
    {
        gl.VertexAttribPointer(0, PositionFields, VertexAttribPointerType.Float, false, VertexSize, (nint)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, NormalFields, VertexAttribPointerType.Float, false, VertexSize, (nint)(PositionFields * sizeof(float)));
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(2, TextureFields, VertexAttribPointerType.Float, false, VertexSize, (nint)((PositionFields + NormalFields) * sizeof(float)));
        gl.EnableVertexAttribArray(2);
    }
}
