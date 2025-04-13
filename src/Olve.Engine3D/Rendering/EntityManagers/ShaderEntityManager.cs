using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;

namespace Olve.Engine3D.Rendering.EntityManagers;

public class ShaderEntityManager : RenderingEntityManagerBase<ShaderData, OpenGLShaderManager.Registration>
{
    private readonly OpenGLShaderManager _openGLShaderManager = new();

    protected override Result<OpenGLShaderManager.Registration> RegisterInOpenGL(ShaderData entity)
    {
        return _openGLShaderManager.Register(entity);
    }

    protected override Result DeregisterFromOpenGL(OpenGLShaderManager.Registration registration)
    {
        return _openGLShaderManager.Unregister(registration);
    }
}