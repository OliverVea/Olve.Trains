using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class HeightmapEntityManager(OpenGLHeightmapManager openGLHeightmapManager) : RenderingEntityManagerBase<HeightmapData, HeightmapEntityManager.Registration>
{
    public readonly record struct Registration(VAO VAO, VBO VBO, EBO EBO, Texture2D Texture);

    protected override Result<Registration> RegisterInOpenGL(HeightmapData entity)
    {
        if (openGLHeightmapManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering heightmap in OpenGL");
        }

        var (vao, vbo, ebo, texture) = registration;

        return new Registration(vao, vbo, ebo, texture);
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        OpenGLHeightmapManager.Registration openGLRegistration = new(registration.VAO, registration.VBO, registration.EBO, registration.Texture);

        if (openGLHeightmapManager.Unregister(openGLRegistration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering heightmap in OpenGL");
        }

        return Result.Success();
    }
}