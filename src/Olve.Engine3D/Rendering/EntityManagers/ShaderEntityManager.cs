using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class ShaderEntityManager(OpenGLShaderManager openGLShaderManager) : RenderingEntityManagerBase<ShaderData, OpenGLShaderManager.Registration>
{
    protected override Result<OpenGLShaderManager.Registration> RegisterInOpenGL(ShaderData entity)
    {
        return openGLShaderManager.Register(entity);
    }

    protected override Result UpdateInOpenGL(OpenGLShaderManager.Registration registration, ShaderData entity)
    {
        throw new NotSupportedException("Updating shader data is not yet supported");
    }

    protected override Result DeregisterFromOpenGL(OpenGLShaderManager.Registration registration)
    {
        return openGLShaderManager.Unregister(registration);
    }
}