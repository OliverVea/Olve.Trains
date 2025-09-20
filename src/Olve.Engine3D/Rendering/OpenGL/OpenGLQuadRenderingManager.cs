using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLQuadRenderingManager(Provider<GL> glProvider)
{
    private const int UnitFields = 2;              // vec2
    private const int VerticesPerQuad = 6;         // two triangles

    private uint _unitVboHandle;                   // 0 == not created
    private bool HasUnitVbo => _unitVboHandle != 0;
    
    /// <summary>
    /// Ensure a shared unit-quad VBO exists and attach it to 'vao' at location 0 (aUnit: vec2).
    /// After calling this once per VAO, you can set per-instance attributes at locations 1..N as usual.
    /// </summary>
    public Result AttachUnitQuad(VAO vao)
    {
        EnsureUnitVbo();

        var gl = glProvider.Value;
        gl.BindVertexArray(vao.Handle);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _unitVboHandle);
        uint stride = (uint)(UnitFields * sizeof(float));
        var offset = IntPtr.Zero;

        gl.VertexAttribPointer(0, UnitFields, VertexAttribPointerType.Float, normalized: false, stride, offset);
        gl.EnableVertexAttribArray(0);
        // unit quad is per-vertex data (no divisor)

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);

        return Result.Success();
    }

    /// <summary>Bind VAO (+ instance VBO if you want) before drawing.</summary>
    public Result LoadQuadsInOpenGL(VAO vao, VBO? instanceVbo = null)
    {
        var gl = glProvider.Value;
        gl.BindVertexArray(vao.Handle);
        if (instanceVbo is { } vbo) gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo.Handle);
        return Result.Success();
    }

    /// <summary>Draw instanced quads; each instance is one rectangle.</summary>
    public Result RenderQuads(uint instanceCount)
    {
        glProvider.Value.DrawArraysInstanced(PrimitiveType.Triangles, first: 0, count: VerticesPerQuad, instancecount: instanceCount);
        return Result.Success();
    }

    public Result RenderQuad() => RenderQuads(1);

    /// <summary>Free the shared unit-quad VBO (optional on shutdown).</summary>
    public void DisposeUnitVbo()
    {
        if (!HasUnitVbo) return;
        glProvider.Value.DeleteBuffer(_unitVboHandle);
        _unitVboHandle = 0;
    }

    // ---------- Internals ----------

    private void EnsureUnitVbo()
    {
        if (HasUnitVbo) return;

        var gl = glProvider.Value;
        _unitVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _unitVboHandle);

        ReadOnlySpan<float> unit =
        [
            0f,0f,  1f,0f,  1f,1f,
            0f,0f,  1f,1f,  0f,1f
        ];
        gl.BufferData(BufferTargetARB.ArrayBuffer, unit, BufferUsageARB.StaticDraw);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }
}