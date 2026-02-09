using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
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