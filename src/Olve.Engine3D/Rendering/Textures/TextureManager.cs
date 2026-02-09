using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.EntityManagers;
using Olve.Utilities.Assertions;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Textures;

public class TextureManager(AssetLoader assetLoader)
{
    private readonly Dictionary<Id<Texture>, Texture> _textures = new();

    /// <summary>
    /// Loads a texture from an asset path. Returns existing Id if already loaded.
    /// </summary>
    public Result<Id<Texture>> EnsureTextureLoaded(AssetPath<TextureData> assetPath)
    {
        var id = GetIdForTexturePath(assetPath);
        if (_textures.TryGetValue(id, out var texture))
        {
            Assert.That(() => texture.AssetPath == assetPath, "Got texture with different asset paths under same id");
            return id;
        }

        if (assetLoader
            .LoadAsset(assetPath)
            .TryPickProblems(out var problems, out var data))
        {
            return problems;
        }

        texture = new Texture(data, assetPath);
        _textures.Add(id, texture);

        return id;
    }

    /// <summary>
    /// Registers a dynamically created texture (heightmap, render target, etc.)
    /// Returns the Id for use in shader parameters.
    /// </summary>
    public Id<Texture> RegisterDynamicTexture(TextureData data, string? name = null)
    {
        var id = name != null ? Id.FromName<Texture>(name) : Id.New<Texture>();
        var texture = new Texture(data);
        _textures[id] = texture;
        return id;
    }

    /// <summary>
    /// Reserves an Id for a texture that will be registered externally (e.g., heightmaps).
    /// The actual OpenGL texture is managed elsewhere; this just provides an Id for the shader system.
    /// </summary>
    public Id<Texture> ReserveExternalTextureId(string name)
    {
        return Id.FromName<Texture>(name);
    }

    public bool TryGetTexture(Id<Texture> id, out Texture texture) => _textures.TryGetValue(id, out texture);
    public Id<Texture> GetIdForTexturePath(AssetPath<TextureData> assetPath) => Id.FromName<Texture>(assetPath.Path.Path);
}

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