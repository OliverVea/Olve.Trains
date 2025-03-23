using Olve.Engine3D.Graphics;

namespace Olve.Engine3D.Rendering;

public abstract class RenderingInstanceManagerBase<T, TRegistration> : RenderingEntityManagerBase<T, TRegistration>, IRenderingInstanceManager<T>
    where TRegistration : IHasInstanceCount
{
    private readonly ThreadSafeIdGenerator _instanceIdGenerator = new();
    private RenderingInstanceId<T> NextInstanceId() => new(_instanceIdGenerator.Next());

    protected readonly SortedList<RenderingInstanceId<T>, Instance> Instances = new();
    protected readonly record struct Instance(
        RenderingEntityId<T> EntityId,
        Matrix4X4<float> Transform);
    
    public Result<RenderingInstanceId<T>> RegisterInstance(RenderingEntityId<T> entityId, Matrix4X4<float> worldMatrix)
    {
        if (!ModelRegistrations.TryGetValue(entityId, out var modelRegistration))
        {
            return new ResultProblem("Entity with entity id '{0}' is not registered", entityId);
        }

        var instanceId = NextInstanceId();
        var instance = new Instance(entityId, worldMatrix);
        Instances.Add(instanceId, instance);

        modelRegistration.InstanceCount++;

        return instanceId;
    }

    public Result DeregisterInstance(RenderingInstanceId<T> instanceId)
    {
        if (!Instances.TryGetValue(instanceId, out var modelInstance))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        if (!ModelRegistrations.TryGetValue(modelInstance.EntityId, out var modelRegistration))
        {
            Instances.Remove(instanceId);
            return new ResultProblem("Entity with entity id '{0}' is not registered", modelInstance.EntityId);
        }

        Instances.Remove(instanceId);
        modelRegistration.InstanceCount--;

        return Result.Success();
    }

    public Result SetInstanceWorld(RenderingInstanceId<T> instanceId, Matrix4X4<float> worldMatrix)
    {
        if (!Instances.ContainsKey(instanceId))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        Instances[instanceId] = Instances[instanceId] with { Transform = worldMatrix };

        return Result.Success();
    }

    public Result<Matrix4X4<float>> GetInstanceWorld(RenderingInstanceId<T> instanceId)
    {
        if (!Instances.TryGetValue(instanceId, out var modelInstance))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        return modelInstance.Transform;
    }

}