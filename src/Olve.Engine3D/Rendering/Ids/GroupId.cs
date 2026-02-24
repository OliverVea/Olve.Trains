using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering;

public record UntypedGroupId(Id Value)
{
    public static UntypedGroupId New() => new(Id.New());
}

public sealed record GroupId<TInstance>(Id Value) : UntypedGroupId(Value)
    where TInstance : IInstanceData
{
    public new static GroupId<TInstance> New() => new(Id.New());
}
