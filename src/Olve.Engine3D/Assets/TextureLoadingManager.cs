using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Rendering.Textures;
using Olve.Logging;
using Olve.Paths;

namespace Olve.Engine3D.Assets;

public class TextureLoadingManager(ILoggingManager loggingManager, AssetLoader assetLoader, TextureManager textureManager)
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

    public Result<TextureId<T>> LoadTexture<T>(AssetPath<TextureData<T>> texturePath)
        where T : unmanaged
    {
        PathKey pathKey = new(texturePath.Path, typeof(T));
        if (_registrations.TryGetValue(pathKey, out var cachedUntypedTextureId))
        {
            if (cachedUntypedTextureId.TryGetAsTypedId<T>(out var cachedTextureId))
            {
                return cachedTextureId;
            }

            loggingManager.Log(LogLevel.Error, $"Cached texture at path '{pathKey.Value}' was incorrect type '{typeof(T).Name}' expected '{cachedUntypedTextureId.Type.Name}'");
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