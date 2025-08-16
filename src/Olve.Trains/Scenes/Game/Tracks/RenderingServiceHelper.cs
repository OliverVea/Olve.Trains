using Olve.Engine3D.Assets;
using Olve.Engine3D.Rendering.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Engine3D.Rendering.OpenGL.Handles;
using Olve.Engine3D.Rendering.Shaders;
using Olve.Results;

namespace Olve.Trains.Scenes.Game.Tracks;

public static class RenderingServiceHelper
{
    public static Result<Texture2D> LoadTexture(AssetPath<TextureData> texturePath, TextureEntityManager textureEntityManager)
    {
        var textureResult = AssetLoader.LoadAsset(texturePath);
        if (textureResult.TryPickProblems(out var problems, out var textureData))
        {
            return problems.Prepend("Failed to load texture");
        }

        var registrationResult = textureEntityManager.Register(textureData);
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

    public static Result LoadShader(IShader shader, ShaderEntityManager shaderEntityManager)
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