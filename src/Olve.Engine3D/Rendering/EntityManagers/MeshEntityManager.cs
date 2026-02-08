using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class MeshEntityManager(OpenGLMeshManager openGLMeshManager)
    : RenderingEntityManagerBase<MeshData, OpenGLBufferManager.Registration>
{
    protected override Result<OpenGLBufferManager.Registration> RegisterInOpenGL(MeshData entity)
    {
        if (openGLMeshManager.Register(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend("Failed registering mesh in OpenGL");
        }

        return registration;
    }

    protected override Result UpdateInOpenGL(OpenGLBufferManager.Registration registration, MeshData entity)
    {
        throw new NotSupportedException("Updating mesh data is not yet supported");
    }

    protected override Result DeregisterFromOpenGL(OpenGLBufferManager.Registration registration)
    {
        if (openGLMeshManager.Unregister(registration).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering mesh in OpenGL");
        }

        return Result.Success();
    }
}
