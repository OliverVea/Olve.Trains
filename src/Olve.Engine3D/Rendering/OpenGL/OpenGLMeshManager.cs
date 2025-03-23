using System.Buffers;
using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Entities;
using Olve.Engine3D.Rendering.OpenGL.Types;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLMeshManager : IOpenGLEntityManager<MeshData, OpenGLMeshManager.Registration>
{
    private const int MaxStackAllocationSize = 2048;

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

        var vertexCount = meshData.Positions.Length;

        // Bind VAO
        var vao = GameManager.GL.CreateVertexArray();
        GameManager.GL.BindVertexArray(vao);

        // Bind VBO
        var meshVertices = meshData.Positions;
        var meshNormals = meshData.Normals;
        var meshTexture = meshData.TextureCoordinates;

        var vbo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        var verticesLength = vertexCount * VertexFields;
        var array = verticesLength > MaxStackAllocationSize ? ArrayPool<float>.Shared.Rent(verticesLength) : null;
        var vertices = verticesLength > MaxStackAllocationSize ? array![..verticesLength] : stackalloc float[vertexCount * VertexFields];
        BufferHelper.CopyTo(meshVertices, vertices, VertexFields, offset: 0);
        BufferHelper.CopyTo(meshNormals, vertices, VertexFields, offset: PositionFields);
        BufferHelper.CopyTo(meshTexture, vertices, VertexFields, offset: PositionFields + NormalFields);
        GameManager.GL.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);
        if (array is not null)
        {
            ArrayPool<float>.Shared.Return(array);
        }

        // Bind EBO
        var meshIndices = meshData.Indices;
        var indexCount = meshIndices.Length * 3;

        var ebo = GameManager.GL.CreateBuffer();
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        var array2 = indexCount > MaxStackAllocationSize ? ArrayPool<uint>.Shared.Rent(indexCount) : null;
        var indices = indexCount > MaxStackAllocationSize ? array2![..indexCount] : stackalloc uint[indexCount];
        BufferHelper.CopyTo(meshIndices, indices);
        GameManager.GL.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);
        if (array2 is not null)
        {
            ArrayPool<uint>.Shared.Return(array2);
        }

        // Set vertex attributes
        GameManager.GL.VertexAttribPointer(0, PositionFields, VertexAttribPointerType.Float, false, VertexSize, IntPtr.Zero);
        GameManager.GL.EnableVertexAttribArray(0);

        GameManager.GL.VertexAttribPointer(1, NormalFields, VertexAttribPointerType.Float, false, VertexSize, IntPtr.Zero + PositionSize);
        GameManager.GL.EnableVertexAttribArray(1);

        GameManager.GL.VertexAttribPointer(2, TextureFields, VertexAttribPointerType.Float, false, VertexSize, IntPtr.Zero + PositionSize + NormalSize);
        GameManager.GL.EnableVertexAttribArray(2);

        // Unbind VAO
        GameManager.GL.BindVertexArray(0);

        // Unbind VBO
        GameManager.GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        // Unbind EBO
        GameManager.GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        return new Registration(
            new VAO(vao),
            new VBO(vbo, (uint)vertexCount),
            new EBO(ebo, (uint)indexCount));
    }

    public Result Unregister(Registration registration)
    {
        GameManager.GL.DeleteVertexArray(registration.VAO.Handle);
        GameManager.GL.DeleteBuffer(registration.VBO.Handle);
        GameManager.GL.DeleteBuffer(registration.EBO.Handle);

        return Result.Success();
    }
}