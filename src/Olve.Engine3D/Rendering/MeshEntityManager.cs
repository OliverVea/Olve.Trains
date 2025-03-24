using Olve.Engine3D.Graphics.Entities;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering;

public class MeshEntityManager : RenderingEntityManagerBase<MeshRenderingId, MeshData, MeshEntityManager.Registration>
{
    private readonly OpenGLMeshManager _openGLMeshManager = new();
    
    public readonly record struct Registration(OpenGL.Handles.VAO VAO, OpenGL.Handles.VBO VBO, OpenGL.Handles.EBO EBO);

    protected override MeshRenderingId CreateId(uint id, Registration registration)
    {
        return new MeshRenderingId(id, registration.VAO, registration.VBO, registration.EBO);
    }

    protected override Result<Registration> RegisterInOpenGL(MeshData entity)
    {
        if (_openGLMeshManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering mesh in OpenGL");
        }

        var (vao, vbo, ebo) = registration;

        return new Registration(vao, vbo, ebo);
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        OpenGLMeshManager.Registration openGLRegistration = new(registration.VAO, registration.VBO, registration.EBO);

        if (_openGLMeshManager.Unregister(openGLRegistration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering mesh in OpenGL");
        }

        return Result.Success();
    }
}