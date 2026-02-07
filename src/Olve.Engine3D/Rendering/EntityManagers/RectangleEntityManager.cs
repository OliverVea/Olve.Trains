using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class RectangleEntityManager(OpenGLRectangleManager openGLRectangleManager)
    : RenderingEntityManagerBase<RectangleData, RectangleEntityManager.Registration>
{
    public readonly record struct Registration(VAO VAO, VBO VBO);

    protected override Result<Registration> RegisterInOpenGL(RectangleData entity)
    {
        if (openGLRectangleManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering line strip in OpenGL");
        }

        var (vao, vbo) = registration;

        return new Registration(vao, vbo);
    }

    protected override Result UpdateInOpenGL(Registration registration, RectangleData entity)
    {
        throw new NotSupportedException("Updating rectangle data is not yet supported");
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        OpenGLRectangleManager.Registration openGLRegistration = new(registration.VAO, registration.VBO);

        if (openGLRectangleManager.Unregister(openGLRegistration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering line strip in OpenGL");
        }

        return Result.Success();
    }
}