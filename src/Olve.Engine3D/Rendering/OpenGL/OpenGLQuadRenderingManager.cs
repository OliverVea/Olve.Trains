using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLQuadRenderingManager(Provider<GL> glProvider)
{
    private const int UnitFields = 2;
    private const int VerticesPerQuad = 6;

    private uint _unitVboHandle;
    private bool HasUnitVbo => _unitVboHandle != 0;

    public Result AttachUnitQuad(VAO vao)
    {
        EnsureUnitVbo();

        var gl = glProvider.Value;
        gl.BindVertexArray(vao.Handle);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _unitVboHandle);
        uint stride = UnitFields * sizeof(float);
        var offset = IntPtr.Zero;

        gl.VertexAttribPointer(0, UnitFields, VertexAttribPointerType.Float, normalized: false, stride, offset);
        gl.EnableVertexAttribArray(0);

        // leave the VAO bound; caller will continue defining attribs 1..N
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return Result.Success();
    }

    public Result LoadQuadsInOpenGL(VAO vao)
    {
        var gl = glProvider.Value;
        gl.BindVertexArray(vao.Handle);
        return Result.Success();
    }

    public Result RenderQuad(VAO vao)
    {
        var gl = glProvider.Value;
        gl.BindVertexArray(vao.Handle);
        gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, 1);
        gl.BindVertexArray(0);
        return Result.Success();
    }

    public void DisposeUnitVbo()
    {
        if (!HasUnitVbo) return;
        glProvider.Value.DeleteBuffer(_unitVboHandle);
        _unitVboHandle = 0;
    }

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