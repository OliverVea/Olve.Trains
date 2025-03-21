namespace Olve.Engine3D.Graphics;

public readonly record struct RenderingEntityId<T>(uint Id) : IComparable<RenderingEntityId<T>> where T : RenderingTarget
{
    public int CompareTo(RenderingEntityId<T> other) => Id.CompareTo(other.Id);
}