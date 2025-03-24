namespace Olve.Engine3D.Rendering;

public readonly record struct RenderingInstanceId(uint Id) : IComparable<RenderingInstanceId>
{
    public int CompareTo(RenderingInstanceId other) => Id.CompareTo(other.Id);
}