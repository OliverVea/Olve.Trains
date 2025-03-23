namespace Olve.Engine3D.Rendering;

public readonly record struct RenderingEntityId<T>(uint Id) : IComparable<RenderingEntityId<T>>
{
    public int CompareTo(RenderingEntityId<T> other) => Id.CompareTo(other.Id);
}