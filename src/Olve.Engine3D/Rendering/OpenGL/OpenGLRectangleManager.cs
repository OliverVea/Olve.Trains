using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLRectangleManager(
    Provider<GL> glProvider,
    OpenGLQuadRenderingManager quadRenderingManager)
    : IOpenGLEntityManager<RectangleData, OpenGLRectangleManager.Registration>
{
    // pos2, size2, color4, depth1, radius1, borderColor4
    private const int InstFields = 2 + 2 + 4 + 1 + 1 + 4;

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
            return problems.Prepend("Failed to attach unit quad to rectangle VAO");
        }

        var instVboHandle = gl.CreateBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instVboHandle);

        Span<float> instance = stackalloc float[InstFields];
        int i = 0;
        instance[i++] = entityData.PositionPx.X; instance[i++] = entityData.PositionPx.Y; // pos2
        instance[i++] = entityData.SizePx.X;     instance[i++] = entityData.SizePx.Y;     // size2
        instance[i++] = entityData.ColorRgba.X;  instance[i++] = entityData.ColorRgba.Y;
        instance[i++] = entityData.ColorRgba.Z;  instance[i++] = entityData.ColorRgba.W;  // color4
        instance[i++] = entityData.Depth;                                          // depth1
        instance[i++] = entityData.CornerRadiusPx;                                 // radius1
        var bc = entityData.BorderColorRgba ?? new Vector4D<float>(0, 0, 0, 0);    // borderColor4
        instance[i++] = bc.X; instance[i++] = bc.Y; instance[i++] = bc.Z; instance[i++] = bc.W;

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
            Attr(3, 4); // iColor
            Attr(4, 1); // iDepth
            Attr(5, 1); // iRadiusPx
            Attr(6, 4); // iBorderColor
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
