using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.Shaders;

namespace Olve.Engine3D.Rendering;

public class RenderingServiceHelper(ShaderEntityManager shaderEntityManager)
{
    public Result LoadShader(IShader shader)
    {
        var registrationResult = shaderEntityManager.Register(shader.ShaderData);
        if (registrationResult.TryPickProblems(out var problems, out var shaderId))
        {
            return problems.Prepend("Failed to register shader");
        }

        shader.RenderingId = shaderId;

        return Result.Success();
    }

    public Result UnloadShader(IShader shader)
    {
        if (shader.RenderingId == default)
        {
            return new ResultProblem("The RenderingId of shader with type '{0}' has not been set", shader.GetType().Name);
        }

        return shaderEntityManager.Unregister(shader.RenderingId);
    }

}