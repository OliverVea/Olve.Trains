using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.Textures;

namespace Olve.Engine3D.Assets;

public class TextureLoadingManager(ILogger<TextureLoadingManager> logger, AssetLoader assetLoader, TextureManager textureManager)
{
    private readonly record struct PathKey
    {
        public string Value { get; }
        public Type Type { get; }

        public PathKey(IPath path, Type type)
        {
            Value = path.Absolute.Path;
            Type = type;
        }
    }

    private readonly Dictionary<PathKey, UntypedTextureId> _registrations = new();

    // Guards the cache so textures can be pre-warmed from a background thread
    // (loading scene) while the main thread loads on demand. Held across the
    // disk read so concurrent callers don't double-load the same texture.
    private readonly Lock _lock = new();

    public Result<TextureId<T>> LoadTexture<T>(AssetPath<TextureData<T>> texturePath)
        where T : unmanaged
    {
        PathKey pathKey = new(texturePath.Path, typeof(T));

        lock (_lock)
        {
            if (_registrations.TryGetValue(pathKey, out var cachedUntypedTextureId))
            {
                if (cachedUntypedTextureId.TryGetAsTypedId<T>(out var cachedTextureId))
                {
                    return cachedTextureId;
                }

                logger.LogError("Cached texture at path '{PathValue}' was incorrect type '{TypeName}' expected '{ExpectedType}'", pathKey.Value, typeof(T).Name, cachedUntypedTextureId.Type.Name);
            }

            if (assetLoader
                .LoadAsset(texturePath)
                .TryPickProblems(out var problems, out var textureData))
            {
                return problems.Prepend("Failed to load texture: {0}", texturePath);
            }

            var textureId = textureManager.RegisterTexture(textureData);
            _registrations[pathKey] = textureId;

            return textureId;
        }
    }
}
