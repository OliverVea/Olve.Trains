namespace Olve.Engine3D.Rendering;

public readonly record struct RenderingInstanceId<T>(uint Id) : IComparable<RenderingInstanceId<T>>
{
    public int CompareTo(RenderingInstanceId<T> other) => Id.CompareTo(other.Id);
}