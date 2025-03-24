using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering;

public class ShaderEntityManager : RenderingEntityManagerBase<ShaderRenderingId, ShaderData, ShaderEntityManager.Registration>
{
    private readonly OpenGLShaderManager _openGLShaderManager = new();
    
    public readonly record struct Registration(ShaderProgram ShaderProgram);

    protected override ShaderRenderingId CreateId(uint id, Registration registration)
    {
        return new ShaderRenderingId(id, registration.ShaderProgram);
    }

    protected override Result<Registration> RegisterInOpenGL(ShaderData entity)
    {
        if (_openGLShaderManager.Register(entity).TryPickProblems(out var problems, out var shaderProgram))
        {
            return problems.Prepend("Failed registering shader in OpenGL");
        }

        return new Registration(shaderProgram);
    }

    protected override Result DeregisterFromOpenGL(Registration registration)
    {
        if (_openGLShaderManager.Unregister(registration.ShaderProgram).TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed unregistering shader in OpenGL");
        }

        return Result.Success();
    }
}