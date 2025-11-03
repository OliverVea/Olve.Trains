using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLRectangleManager(
    Provider<GL> glProvider,
    OpenGLQuadRenderingManager quadRenderingManager)
    : IOpenGLEntityManager<RectangleData, OpenGLRectangleManager.Registration>
{
    // pos2, size2, tint4, uvMin2, uvMax2
    private const int InstFields = 2 + 2 + 4 + 2 + 2;

    public readonly record struct Registration(VAO VAO, VBO InstanceVBO);

    public Result<Registration> Register(RectangleData entityData)
    {
        if (entityData.Validate().TryPickProblems(out var problems))
            return problems;

        var gl = glProvider.Value;

        var vaoHandle = gl.CreateVertexArray();
        gl.BindVertexArray(vaoHandle);

        if (quadRenderingManager.AttachUnitQuad(new VAO(vaoHandle)).TryPickProblems(out problems))
        {
            gl.BindVertexArray(0);
            return problems.Prepend("Failed to attach unit quad to textured rectangle VAO");
        }

        var instVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instVboHandle);

        Span<float> instance = stackalloc float[InstFields];
        int i = 0;
        instance[i++] = entityData.PositionPx.X; instance[i++] = entityData.PositionPx.Y; // pos2
        instance[i++] = entityData.SizePx.X;     instance[i++] = entityData.SizePx.Y;     // size2
        instance[i++] = entityData.TintRgba.X;   instance[i++] = entityData.TintRgba.Y;
        instance[i++] = entityData.TintRgba.Z;   instance[i++] = entityData.TintRgba.W;   // tint4

        gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)instance, BufferUsageARB.DynamicDraw);

        {
            uint stride = InstFields * sizeof(float);
            nint offsetBytes = 0;

            void Attr(uint index, int comps)
            {
                gl.VertexAttribPointer(index, comps, VertexAttribPointerType.Float, false, stride, offsetBytes);
                gl.EnableVertexAttribArray(index);
                gl.VertexAttribDivisor(index, 1); // mark as per-instance
                offsetBytes += comps * sizeof(float);
            }

            Attr(1, 2); // iPosPx
            Attr(2, 2); // iSizePx
            Attr(3, 4); // iTint
        }

        // Unbind
        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        return new Registration(new VAO(vaoHandle), new VBO(instVboHandle, 6));
    }

    public Result Unregister(Registration r)
    {
        var gl = glProvider.Value;
        gl.DeleteVertexArray(r.VAO.Handle);
        gl.DeleteBuffer(r.InstanceVBO.Handle);
        return Result.Success();
    }
}
