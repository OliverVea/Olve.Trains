using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Textures;

public sealed record TextureId<T>(Id Value) : UntypedTextureId(Value, typeof(T))
{
    public static TextureId<T> New() => new(Id.New());
}