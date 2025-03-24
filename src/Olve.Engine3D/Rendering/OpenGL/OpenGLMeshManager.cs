using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLMeshManager : IOpenGLEntityManager<MeshData, OpenGLMeshManager.Registration>
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

    private static VAO BindVAO()
    {
        var vao = GameManager.GL.CreateVertexArray();
        GameManager.GL.BindVertexArray(vao);

        return new VAO(vao);
    }

    private static VBO BindVBO(MeshData meshData)
    {
        var vbo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        BufferHelper.UsingSpan<float>(meshData.VertexCount * VertexFields, vertices =>
        {
            BufferHelper.CopyTo(meshData.Positions, vertices, VertexFields, offset: 0);
            BufferHelper.CopyTo(meshData.Normals, vertices, VertexFields, offset: PositionFields);
            BufferHelper.CopyTo(meshData.TextureCoordinates, vertices, VertexFields, offset: PositionFields + NormalFields);
            GameManager.GL.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);
        });

        return new VBO(vbo, (uint)meshData.VertexCount);
    }

    private static EBO BindEBO(MeshData meshData)
    {
        var ebo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

        BufferHelper.UsingSpan<uint>(meshData.Indices.Length * 3, indices =>
        {
            BufferHelper.CopyTo(meshData.Indices, indices);
            GameManager.GL.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);
        });

        return new EBO(ebo, (uint)meshData.Indices.Length * 3);
    }

    private static void SetVertexAttributes()
    {
        GameManager.GL.VertexAttribPointer(0, PositionFields, VertexAttribPointerType.Float, false, VertexSize, IntPtr.Zero);
        GameManager.GL.EnableVertexAttribArray(0);

        GameManager.GL.VertexAttribPointer(1, NormalFields, VertexAttribPointerType.Float, false, VertexSize, new IntPtr(PositionSize));
        GameManager.GL.EnableVertexAttribArray(1);

        GameManager.GL.VertexAttribPointer(2, TextureFields, VertexAttribPointerType.Float, false, VertexSize, new IntPtr(PositionSize + NormalSize));
        GameManager.GL.EnableVertexAttribArray(2);
    }

    private static void Cleanup()
    {
        GameManager.GL.BindVertexArray(0); // Unbind VAO
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0); // Unbind VBO
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0); // Unbind EBO
    }

    public Result Unregister(Registration registration)
    {
        GameManager.GL.DeleteVertexArray(registration.VAO.Handle);
        GameManager.GL.DeleteBuffer(registration.VBO.Handle);
        GameManager.GL.DeleteBuffer(registration.EBO.Handle);

        return Result.Success();
    }
}