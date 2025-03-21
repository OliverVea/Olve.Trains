namespace Olve.Engine3D.Graphics;

public readonly record struct RenderingInstanceId<T>(uint Id) : IComparable<RenderingInstanceId<T>> where T : RenderingTarget
{
    public int CompareTo(RenderingInstanceId<T> other) => Id.CompareTo(other.Id);
}