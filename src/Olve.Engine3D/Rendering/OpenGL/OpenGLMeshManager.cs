using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Primitives;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLMeshManager(Provider<GL> glProvider) : IOpenGLEntityManager<MeshData, OpenGLMeshManager.Registration>
{
    private const int PositionFields = 3;
    private const int NormalFields = 3;
    private const int TextureFields = 2;
    private const int VertexFields = PositionFields + NormalFields + TextureFields;

    private const int PositionSize = PositionFields * sizeof(float);
    private const int NormalSize = NormalFields * sizeof(float);
    private const int TextureSize = TextureFields * sizeof(float);
    private const int VertexSize = PositionSize + NormalSize + TextureSize;

    public readonly record struct Registration(VAO VAO, VBO VBO, EBO EBO);

    public Result<Registration> Register(MeshData meshData)
    {
        if (meshData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }
        
        var vao = BindVAO();
        var vbo = BindVBO(meshData);
        var ebo = BindEBO(meshData);

        SetVertexAttributes();
        Cleanup();

        return new Registration(vao, vbo, ebo);
    }

    private VAO BindVAO()
    {
        var vao = glProvider.Value.CreateVertexArray();
        glProvider.Value.BindVertexArray(vao);

        return new VAO(vao);
    }

    private VBO BindVBO(MeshData meshData)
    {
        var vbo = glProvider.Value.CreateBuffer();
        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        BufferHelper.WithSpan<float>(meshData.VertexCount * VertexFields, vertices =>
        {
            meshData.Positions.CopyTo(vertices, VertexFields, offset: 0);
            meshData.Normals.CopyTo(vertices, VertexFields, offset: PositionFields);
            meshData.TextureCoordinates.CopyTo(vertices, VertexFields, offset: PositionFields + NormalFields);

            glProvider.Value.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);
        });

        return new VBO(vbo, (uint)meshData.VertexCount);
    }

    private EBO BindEBO(MeshData meshData)
    {
        var ebo = glProvider.Value.CreateBuffer();
        glProvider.Value.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

        BufferHelper.WithSpan<uint>(meshData.Indices.Length * 3, indices =>
        {
            meshData.Indices.CopyTo(indices);

            glProvider.Value.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);
        });

        return new EBO(ebo, (uint)meshData.Indices.Length * 3);
    }

    private void SetVertexAttributes()
    {
        glProvider.Value.VertexAttribPointer(0, PositionFields, VertexAttribPointerType.Float, false, VertexSize, IntPtr.Zero);
        glProvider.Value.EnableVertexAttribArray(0);

        glProvider.Value.VertexAttribPointer(1, NormalFields, VertexAttribPointerType.Float, false, VertexSize, new IntPtr(PositionSize));
        glProvider.Value.EnableVertexAttribArray(1);

        glProvider.Value.VertexAttribPointer(2, TextureFields, VertexAttribPointerType.Float, false, VertexSize, new IntPtr(PositionSize + NormalSize));
        glProvider.Value.EnableVertexAttribArray(2);
    }

    private void Cleanup()
    {
        glProvider.Value.BindVertexArray(0); // Unbind VAO
        glProvider.Value.BindBuffer(BufferTargetARB.ArrayBuffer, 0); // Unbind VBO
        glProvider.Value.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0); // Unbind EBO
    }

    public Result Unregister(Registration registration)
    {
        glProvider.Value.DeleteVertexArray(registration.VAO.Handle);
        glProvider.Value.DeleteBuffer(registration.VBO.Handle);
        glProvider.Value.DeleteBuffer(registration.EBO.Handle);

        return Result.Success();
    }
}