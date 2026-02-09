using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;

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

}