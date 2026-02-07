using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class LineStripEntityManager(OpenGLLineStripManager openGLLineStripManager)
    : RenderingEntityManagerBase<LineStripData, LineStripEntityManager.Registration>
{
    public readonly record struct Registration(VAO VAO, VBO VBO);

    protected override Result<Registration> RegisterInOpenGL(LineStripData entity)
    {
        if (openGLLineStripManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering line strip in OpenGL");
        }

        var (vao, vbo) = registration;

        return new Registration(vao, vbo);
    }

    protected override Result UpdateInOpenGL(Registration registration, LineStripData entity)
    {
        OpenGLLineStripManager.Registration openGLRegistration = new(registration.VAO, registration.VBO);

        if (openGLLineStripManager.Update(openGLRegistration, entity).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed updating line strip in OpenGL");
        }

        return Result.Success();
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        OpenGLLineStripManager.Registration openGLRegistration = new(registration.VAO, registration.VBO);

        if (openGLLineStripManager.Unregister(openGLRegistration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering line strip in OpenGL");
        }

        return Result.Success();
    }
}