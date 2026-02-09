using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering;

public readonly record struct GeometryId(Id Id)
{
    public static GeometryId New() => new(Id.New());
}
