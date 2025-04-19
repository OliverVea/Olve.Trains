using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class MeshEntityManager(OpenGLMeshManager openGLMeshManager) : RenderingEntityManagerBase<MeshData, MeshEntityManager.Registration>
{
    public readonly record struct Registration(VAO VAO, VBO VBO, EBO EBO);

    protected override Result<Registration> RegisterInOpenGL(MeshData entity)
    {
        if (openGLMeshManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering mesh in OpenGL");
        }

        var (vao, vbo, ebo) = registration;

        return new Registration(vao, vbo, ebo);
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        OpenGLMeshManager.Registration openGLRegistration = new(registration.VAO, registration.VBO, registration.EBO);

        if (openGLMeshManager.Unregister(openGLRegistration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering mesh in OpenGL");
        }

        return Result.Success();
    }
}