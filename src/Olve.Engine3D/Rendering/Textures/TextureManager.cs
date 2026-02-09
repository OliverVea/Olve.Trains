using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Systems;

namespace Olve.Engine3D.Rendering.Textures;

public class TextureManager
{
    private readonly Dictionary<UntypedTextureId, (ITextureData, Type)> _textures = new();

    public Event<UntypedTextureId> OnAdded { get; } = new();
    public Event<UntypedTextureId> OnRemoved { get; } = new();

    public TextureId<T> RegisterTexture<T>(TextureData<T> textureData)
        where T : unmanaged
    {
        // TODO: perhaps generate id from texture data hash
        var id = TextureId<T>.New();
        _textures[id] = (textureData, typeof(T));
        OnAdded.Invoke(id);
        return id;
    }

    public DeletionResult UnregisterTexture<T>(TextureId<T> textureId)
    {
        if (!_textures.ContainsKey(textureId))
        {
            return DeletionResult.NotFound();
        }

        OnRemoved.Invoke(textureId);
        _textures.Remove(textureId);
        return DeletionResult.Success();
    }

    public bool TryGetTextureData<T>(TextureId<T> id, [MaybeNullWhen(false)] out TextureData<T> data)
        where T : unmanaged
    {
        data = null;
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
        return data != null;
    }
}