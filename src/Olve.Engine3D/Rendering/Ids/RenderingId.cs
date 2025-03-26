namespace Olve.Engine3D.Rendering;

public readonly record struct RenderingId<TEntity>(uint Id)
{
    public static RenderingId<TEntity> New() => new(ThreadSafeUintGenerator.Shared.Next());
}