using Olve.Engine3D.Rendering.Entities;
using Silk.NET.OpenGL;

namespace Olve.Engine3D.Rendering.OpenGL;

public class OpenGLLineStripManager(
    OpenGLBufferManager bufferManager)
    : IOpenGLEntityManager<LineStripData, OpenGLBufferManager.Registration>
{
    private const int VertexFloats = 6; // position(3) + color(3)

    public Result<OpenGLBufferManager.Registration> Register(LineStripData entityData)
    {
        if (entityData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        OpenGLBufferManager.Registration registration = default;

        BufferHelper.WithSpan<float>(entityData.VertexCount * VertexFloats, vertices =>
        {
            entityData.Positions.CopyTo(vertices, VertexFloats);
            entityData.Colors.CopyTo(vertices, VertexFloats, offset: 3);

            registration = bufferManager.CreateBuffers(
                vertices,
                (uint)entityData.VertexCount,
                ConfigureAttributes,
                BufferUsageARB.DynamicDraw);
        });

        return registration;
    }

    public Result Update(OpenGLBufferManager.Registration registration, LineStripData entityData)
    {
        if (entityData.Validate().TryPickProblems(out var problems))
        {
            return problems;
        }

        BufferHelper.WithSpan<float>(entityData.VertexCount * VertexFloats, vertices =>
        {
            entityData.Positions.CopyTo(vertices, VertexFloats);
            entityData.Colors.CopyTo(vertices, VertexFloats, offset: 3);

            bufferManager.UpdateVBO(registration, vertices, BufferUsageARB.DynamicDraw);
        });

        return Result.Success();
    }

    public Result Unregister(OpenGLBufferManager.Registration registration)
    {
        bufferManager.DeleteBuffers(registration);
        return Result.Success();
    }

    private static void ConfigureAttributes(GL gl)
    {
        const uint stride = VertexFloats * sizeof(float);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (nint)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (nint)12);
        gl.EnableVertexAttribArray(1);
    }
}
