using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering.Instancing;

public class RenderingInstanceManager(RenderingGroupManager groupManager)
{
    public Result<Id<TInstance>> Add<TInstance>(GroupId<TInstance> groupId, TInstance data)
        where TInstance : IInstanceData
    {
        if (!groupManager.TryGet(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        var store = (InstanceStore<TInstance>)groupData.Instances;
        var instanceId = Id.New<TInstance>();
        store.Add(instanceId, data);
        groupData.MarkDirty();

        return instanceId;
    }

    public Result Update<TInstance>(GroupId<TInstance> groupId, Id<TInstance> instanceId, TInstance data)
        where TInstance : IInstanceData
    {
        if (!groupManager.TryGet(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        var store = (InstanceStore<TInstance>)groupData.Instances;
        if (!store.Update(instanceId, data))
        {
            return new ResultProblem("Instance with id '{0}' not found in group '{1}'", instanceId, groupId);
        }

        groupData.MarkDirty();
        return Result.Success();
    }

    public Result Remove<TInstance>(GroupId<TInstance> groupId, Id<TInstance> instanceId)
        where TInstance : IInstanceData
    {
        if (!groupManager.TryGet(groupId, out var groupData))
        {
            return new ResultProblem("Group with id '{0}' is not registered", groupId);
        }

        var store = (InstanceStore<TInstance>)groupData.Instances;
        if (!store.Remove(instanceId))
        {
            return new ResultProblem("Instance with id '{0}' not found in group '{1}'", instanceId, groupId);
        }

        groupData.MarkDirty();
        return Result.Success();
    }
}
