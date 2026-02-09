using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Engine3D.Rendering.Textures;

namespace Olve.Engine3D.Rendering;

public class RenderingServiceHelper(TextureEntityManager textureEntityManager, ShaderEntityManager shaderEntityManager, AssetLoader assetLoader)
{
    public Result<Texture2D> LoadTexture(AssetPath<TextureData> texturePath)
    {
        var textureResult = assetLoader.LoadAsset(texturePath);
        if (textureResult.TryPickProblems(out var problems, out var textureData))
        {
            return problems.Prepend("Failed to load texture");
        }

        var texture = new Texture(textureData, texturePath);
        var registrationResult = textureEntityManager.Register(texture);
        if (registrationResult.TryPickProblems(out problems, out var textureId))
        {
            return problems.Prepend("Failed to register texture");
        }

        if (textureEntityManager.GetRegistration(textureId).TryPickProblems(out problems, out var textureRegistration))
        {
            return problems;
        }

        return textureRegistration.Texture;
    }

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