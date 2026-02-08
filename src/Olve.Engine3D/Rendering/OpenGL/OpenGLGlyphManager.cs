using Olve.Engine3D.Rendering.Entities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

/// <summary>
/// OpenGL manager for glyph rendering with per-instance UV coordinates.
/// Unlike OpenGLRectangleManager, this sends UvMin/UvMax to the shader.
/// </summary>
public class OpenGLGlyphManager(OpenGLInstancedBufferManager instancedBufferManager)
    : IOpenGLEntityManager<RectangleData, OpenGLInstancedBufferManager.Registration>
{
    // pos2, size2, tint4, uvMin2, uvMax2 = 12 floats
    private const int InstFields = 2 + 2 + 4 + 2 + 2;

    public Result<OpenGLInstancedBufferManager.Registration> Register(RectangleData entityData)
    {
        if (entityData.Validate().TryPickProblems(out var problems))
            return problems;

        Span<float> instance = stackalloc float[InstFields];
        int i = 0;
        instance[i++] = entityData.PositionPx.X; instance[i++] = entityData.PositionPx.Y;   // pos2
        instance[i++] = entityData.SizePx.X;     instance[i++] = entityData.SizePx.Y;       // size2
        instance[i++] = entityData.TintRgba.X;   instance[i++] = entityData.TintRgba.Y;
        instance[i++] = entityData.TintRgba.Z;   instance[i++] = entityData.TintRgba.W;     // tint4
        instance[i++] = entityData.UvMin.X;      instance[i++] = entityData.UvMin.Y;        // uvMin2
        instance[i++] = entityData.UvMax.X;      instance[i++] = entityData.UvMax.Y;        // uvMax2

        return instancedBufferManager.CreateInstanceBuffer(
            instance,
            ConfigureInstanceAttributes,
            BufferUsageARB.DynamicDraw);
    }

    public Result Unregister(OpenGLInstancedBufferManager.Registration registration)
    {
        instancedBufferManager.DeleteBuffers(registration);
        return Result.Success();
    }

    private static void ConfigureInstanceAttributes(GL gl)
    {
        const uint stride = InstFields * sizeof(float);
        nint offset = 0;

        void Attr(uint index, int comps)
        {
            gl.VertexAttribPointer(index, comps, VertexAttribPointerType.Float, false, stride, offset);
            gl.EnableVertexAttribArray(index);
            gl.VertexAttribDivisor(index, 1);
            offset += comps * sizeof(float);
        }

        Attr(1, 2); // iPosPx
        Attr(2, 2); // iSizePx
        Attr(3, 4); // iTint
        Attr(4, 2); // iUvMin
        Attr(5, 2); // iUvMax
    }
}
