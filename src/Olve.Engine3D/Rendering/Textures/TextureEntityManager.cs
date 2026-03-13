using Olve.Engine3D.Rendering.OpenGL;
using Olve.Engine3D.Rendering.OpenGL.Handles;

namespace Olve.Engine3D.Rendering.Textures;

public class TextureEntityManager(TextureManager textureManager, OpenGLTextureManager openGLTextureManager)
{
    private readonly Dictionary<UntypedTextureId, Texture2D> _registrations = new();

    public Result Register<T, TPixelFormat>(TextureId<T> textureId, TextureUploadOptions options)
        where T : unmanaged
        where TPixelFormat : IPixelFormat<T>
    {
        if (_registrations.ContainsKey(textureId))
        {
            return Result.Success();
        }

        if (!textureManager.TryGetTextureData(textureId, out var textureData))
        {
            return new ResultProblem("Could not find texture with id '{0}'", textureId);
        }

        if (openGLTextureManager.Register<T, TPixelFormat>(textureData, options).TryPickProblems(out var problems, out var openGlTexture))
        {
            return problems.Prepend("Failed registering texture in OpenGL");
        }

        _registrations[textureId] = openGlTexture;
        return Result.Success();
    }

    public DeletionResult Unregister(UntypedTextureId textureId)
    {
        if (!_registrations.TryGetValue(textureId, out var openGlTexture))
        {
            return DeletionResult.NotFound();
        }

        if (openGLTextureManager.Unregister(openGlTexture).TryPickProblems(out var problems))
        {
            return DeletionResult.Error(problems.Prepend("Failed unregistering texture in OpenGL"));
        }

        return DeletionResult.Success();
    }

    /// <summary>
    /// Registers a texture that was created externally (e.g. a framebuffer attachment)
    /// so it can be bound as a sampler uniform in shaders.
    /// </summary>
    public void RegisterHandle(UntypedTextureId textureId, Texture2D texture)
    {
        _registrations[textureId] = texture;
    }

    public bool TryGetRegistration(UntypedTextureId textureId, out Texture2D openGlTexture)
    {
        return _registrations.TryGetValue(textureId, out openGlTexture);
    }
}