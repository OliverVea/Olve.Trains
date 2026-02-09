using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Textures;

public class TextureRenderingManager(TextureManager textureManager, TextureEntityManager textureEntityManager)
{
    private readonly Dictionary<Id<Texture>, RenderingId<Texture>> _renderingIds = new();

    public Result<RenderingId<Texture>> EnsureTextureLoaded(Id<Texture> textureId)
    {
        if (_renderingIds.TryGetValue(textureId, out var renderingId))
        {
            return renderingId;
        }

        if (!textureManager.TryGetTexture(textureId, out var texture))
        {
            return new ResultProblem("Texture with id '{0}' not found in {1}",  textureId, nameof(TextureManager));
        }

        if (textureEntityManager
            .Register(texture)
            .TryPickProblems(out var problems, out renderingId))
        {
            return problems;
        }

        _renderingIds.Add(textureId, renderingId);

        return renderingId;
    }

    /// <summary>
    /// Registers a float texture (R32F format) and associates it with a texture ID.
    /// </summary>

    // TODO: Boo! Investigate this
    public Result<Id<Texture>> RegisterFloatTexture(string name, FloatTextureData floatTextureData)
    {
        var textureId = textureManager.ReserveExternalTextureId(name);

        if (textureEntityManager.RegisterFloat(floatTextureData).TryPickProblems(out var problems, out var renderingId))
        {
            return problems.Prepend("Failed to register float texture");
        }

        _renderingIds[textureId] = renderingId;
        return textureId;
    }

    /// <summary>
    /// Registers an external texture that already has an OpenGL handle.
    /// This allows textures created by other systems to be used with the unified Id<Texture> system.
    /// </summary>
    public void RegisterExternalTexture(Id<Texture> textureId, RenderingId<Texture> renderingId)
    {
        _renderingIds[textureId] = renderingId;
    }

    /// <summary>
    /// Adopts an externally-created Texture2D into the texture system.
    /// Returns a RenderingId that can be used with RegisterExternalTexture.
    /// </summary>
    public RenderingId<Texture> AdoptExternalTexture(OpenGL.Handles.Texture2D texture)
    {
        return textureEntityManager.AdoptExternalTexture(texture);
    }

    public bool TryGetRenderingId(Id<Texture> id, out RenderingId<Texture> renderingId) => _renderingIds.TryGetValue(id, out renderingId);

    public bool TryGetRenderingId(AssetPath<TextureData> assetPath, out RenderingId<Texture> renderingId)
    {
        var id = textureManager.GetIdForTexturePath(assetPath);
        return TryGetRenderingId(id, out renderingId);
    }
}