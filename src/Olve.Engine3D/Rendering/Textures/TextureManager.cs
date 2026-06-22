using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Systems;

namespace Olve.Engine3D.Rendering.Textures;

public class TextureManager
{
    private readonly Dictionary<UntypedTextureId, (ITextureData, Type)> _textures = new();

    // The cache is guarded by _lock so it can be populated from a background
    // thread (asset pre-warming in the loading scene) while the main thread
    // reads it. The lock is reentrant, so event subscribers may call back in.
    private readonly Lock _lock = new();

    // TextureManager is a singleton (see OpenGLServiceRegistration). Do NOT
    // subscribe to these events from a scoped service — the singleton would
    // outlive the scope and retain a handler firing into a disposed scope.
    public Event<UntypedTextureId> OnAdded { get; } = new();
    public Event<UntypedTextureId> OnRemoved { get; } = new();

    public TextureId<T> RegisterTexture<T>(TextureData<T> textureData)
        where T : unmanaged
    {
        // TODO: perhaps generate id from texture data hash
        var id = TextureId<T>.New();
        lock (_lock)
        {
            _textures[id] = (textureData, typeof(T));
        }
        OnAdded.Invoke(id);
        return id;
    }

    public DeletionResult UnregisterTexture<T>(TextureId<T> textureId)
    {
        lock (_lock)
        {
            if (!_textures.ContainsKey(textureId))
            {
                return DeletionResult.NotFound();
            }
        }

        OnRemoved.Invoke(textureId);
        lock (_lock)
        {
            _textures.Remove(textureId);
        }
        return DeletionResult.Success();
    }

    public bool TryGetTextureData<T>(TextureId<T> id, [MaybeNullWhen(false)] out TextureData<T> data)
        where T : unmanaged
    {
        data = null;
        lock (_lock)
        {
            if (!_textures.TryGetValue(id, out var textureDataAndType))
            {
                return false;
            }

            var (textureData, textureType) = textureDataAndType;
            if (textureType != typeof(T))
            {
                return false;
            }

            data = textureData as TextureData<T>;
        }

        return data != null;
    }
}