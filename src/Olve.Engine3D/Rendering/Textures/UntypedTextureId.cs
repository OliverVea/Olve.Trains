using System.Diagnostics.CodeAnalysis;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Textures;

public record UntypedTextureId(Id Value, Type Type)
{
    public bool TryGetAsTypedId<T>([MaybeNullWhen(false)] out TextureId<T> textureId)
    {
        if (typeof(T) == Type)
        {
            textureId = new TextureId<T>(Value);
            return true;
        }

        textureId = null;
        return false;
    }
}