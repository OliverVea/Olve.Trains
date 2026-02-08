using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class LineStripEntityManager(OpenGLLineStripManager openGLLineStripManager)
    : RenderingEntityManagerBase<LineStripData, OpenGLBufferManager.Registration>
{
    protected override Result<OpenGLBufferManager.Registration> RegisterInOpenGL(LineStripData entity)
    {
        if (openGLLineStripManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering line strip in OpenGL");
        }

        return registration;
    }

    protected override Result UpdateInOpenGL(OpenGLBufferManager.Registration registration, LineStripData entity)
    {
        if (openGLLineStripManager.Update(registration, entity).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed updating line strip in OpenGL");
        }

        return Result.Success();
    }

    protected override Result DeregisterFromOpenGL(OpenGLBufferManager.Registration registration)
    {
        if (openGLLineStripManager.Unregister(registration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering line strip in OpenGL");
        }

        return Result.Success();
    }
}
