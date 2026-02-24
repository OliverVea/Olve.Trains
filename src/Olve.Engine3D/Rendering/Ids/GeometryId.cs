using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering;

public record UntypedGeometryId(Id Value)
{
    public static UntypedGeometryId New() => new(Id.New());
}

public sealed record GeometryId<TVertex>(Id Value) : UntypedGeometryId(Value)
    where TVertex : IVertexData
{
    public new static GeometryId<TVertex> New() => new(Id.New());
}
