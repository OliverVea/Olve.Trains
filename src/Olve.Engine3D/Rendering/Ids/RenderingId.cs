using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering;

public readonly record struct RenderingId<TEntity>(Id Id)
{
    public static RenderingId<TEntity> New() => new(Id.New());
}