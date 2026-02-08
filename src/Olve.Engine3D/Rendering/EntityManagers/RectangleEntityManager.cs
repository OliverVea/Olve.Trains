using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class RectangleEntityManager(OpenGLRectangleManager openGLRectangleManager)
    : RenderingEntityManagerBase<RectangleData, OpenGLInstancedBufferManager.Registration>
{
    protected override Result<OpenGLInstancedBufferManager.Registration> RegisterInOpenGL(RectangleData entity)
    {
        if (openGLRectangleManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering rectangle in OpenGL");
        }

        return registration;
    }

    protected override Result UpdateInOpenGL(OpenGLInstancedBufferManager.Registration registration, RectangleData entity)
    {
        throw new NotSupportedException("Updating rectangle data is not yet supported");
    }

    protected override Result DeregisterFromOpenGL(OpenGLInstancedBufferManager.Registration registration)
    {
        if (openGLRectangleManager.Unregister(registration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering rectangle in OpenGL");
        }

        return Result.Success();
    }
}
