using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering;

public readonly record struct RenderingInstanceId(Id Id) : IComparable<RenderingInstanceId>
{
    public int CompareTo(RenderingInstanceId other) => Id.CompareTo(other.Id);
}