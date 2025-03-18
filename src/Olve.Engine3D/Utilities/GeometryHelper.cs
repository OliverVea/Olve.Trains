using Olve.Engine3D.Graphics;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Utilities;

public static class GeometryHelper
{
    private static readonly float[] Vertices =
    {
        -0.5f, -0.5f, 0f,
        0.5f, -0.5f, 0f,
        0.5f, 0.5f, 0f,
        -0.5f, 0.5f, 0f
    };

    private static readonly uint[] Indices =
    [
        0, 1, 2,
        0, 2, 3,
    ];

    public static Geometry CreateQuadGeometry(float[]? vertices = null, uint[]? indices = null, GL? gl = null)
    {
        vertices ??= Vertices;
        indices ??= Indices;
        gl ??= GameManager.Gl;

        var vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);

        var vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        ReadOnlySpan<float> vertexSpan = vertices.AsSpan();
        gl.BufferData(BufferTargetARB.ArrayBuffer, vertexSpan, BufferUsageARB.StaticDraw);

        var ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        ReadOnlySpan<uint> indexSpan = indices.AsSpan();
        gl.BufferData(BufferTargetARB.ElementArrayBuffer, indexSpan, BufferUsageARB.StaticDraw);

        const uint positionLoc = 0;
        gl.EnableVertexAttribArray(positionLoc);
        gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), IntPtr.Zero);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

        return new Geometry
        {
            VAO = new(vao),
            VBO = new(vbo),
            EBO = new(ebo),
            IndicesCount = (uint)vertexSpan.Length,
            VerticesCount = (uint)indexSpan.Length,
        };
    }
}