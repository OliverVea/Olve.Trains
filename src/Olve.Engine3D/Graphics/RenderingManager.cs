using OneOf;

namespace Olve.Engine3D.Graphics;



[GenerateOneOf]
public partial class AnyRenderingParameter : OneOfBase<RenderingParameter.Matrix4X4, RenderingParameter.Vector3D, RenderingParameter.Float>
{
    public AnyRenderingParameter(RenderingParameter.Matrix4X4 value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Vector3D value) : base(value) { }
    public AnyRenderingParameter(RenderingParameter.Float value) : base(value) { }

    public string Name => Match(
        matrix4X4 => matrix4X4.Name,
        vector3D => vector3D.Name,
        f => f.Name);
}


public static class RenderingParameter
{
    public class Matrix4X4(string name, Matrix4X4<float> value) : Base<Matrix4X4<float>>(name, value);
    public class Vector3D(string name, Vector3D<float> value) : Base<Vector3D<float>>(name, value);
    public class Float(string name, float value) : Base<float>(name, value);

    public abstract class Base<T>(string name, T value)
    {
        public string Name { get; } = name;
        public T Value { get; } = value;
    }
}

public class RenderingParameters(IReadOnlyList<AnyRenderingParameter> renderingParameters)
{
    public static string? DefaultWorldMatrixName { get; } = "world";

    public string? WorldMatrixName { get; init; } = DefaultWorldMatrixName;
    public IReadOnlyList<AnyRenderingParameter> Parameters { get; } = renderingParameters;
}

public abstract class RenderingManager<T, TRegistration>
    where T : RenderingTarget
    where TRegistration : IHasInstanceCount
{


    private readonly ThreadSafeIdGenerator _entityIdGenerator = new();
    private readonly ThreadSafeIdGenerator _instanceIdGenerator = new();

    private RenderingEntityId<T> NextEntityId() => new(_entityIdGenerator.Next());
    private RenderingInstanceId<T> NextInstanceId() => new(_instanceIdGenerator.Next());

    private readonly SortedList<RenderingEntityId<T>, TRegistration> _modelRegistrations = new();
    private readonly SortedList<RenderingInstanceId<T>, ModelInstance> _instances = new();

    private readonly record struct ModelInstance(
        RenderingInstanceId<T> InstanceId,
        RenderingEntityId<T> EntityId,
        Matrix4X4<float> Transform);

    protected abstract Result<TRegistration> RegisterInOpenGL(T entity);
    protected abstract Result DeregisterFromOpenGL(TRegistration registration);
    protected abstract Result Load(TRegistration registration, RenderingParameters parameters);
    protected abstract Result Render(TRegistration registration, Matrix4X4<float> world, RenderingParameters parameters);

    public Result<RenderingEntityId<T>> Register(T entity)
    {
        var entityId = NextEntityId();

        if (RegisterInOpenGL(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend(new ResultProblem ("Failed to register entity in OpenGL")
            {
                Tags = [ ProblemTags.OpenGL ]
            });
        }

        _modelRegistrations.Add(entityId, registration);

        return entityId;
    }

    public Result Deregister(RenderingEntityId<T> entityId)
    {
        if (!_modelRegistrations.TryGetValue(entityId, out var modelRegistration))
        {
            return new ResultProblem("Entity with entity ID '{0}' is not registered", entityId);
        }

        if (modelRegistration.InstanceCount > 0)
        {
            return new ResultProblem(
                "Entity with entity ID '{0}' cannot be removed as there are {1} instances depending on it",
                entityId,
                modelRegistration.InstanceCount);
        }

        var instancesToRemove = _instances.Values.Where(instance => instance.EntityId.Equals(entityId)).ToList();

        foreach (var modelInstance in instancesToRemove)
        {
            _instances.Remove(modelInstance.InstanceId);
        }

        if (DeregisterFromOpenGL(modelRegistration).TryPickProblems(out var problems))
        {
            return problems.Prepend(new ResultProblem("Could not unregister entity in OpenGL")
            {
                Severity = ProblemSeverities.Critical,
                Tags = [ ProblemTags.OpenGL ]
            });
        }

        _modelRegistrations.Remove(entityId);
        return Result.Success();
    }
    
    public Result<RenderingInstanceId<T>> RegisterInstance(RenderingEntityId<T> entityId, Matrix4X4<float> worldMatrix)
    {
        if (!_modelRegistrations.TryGetValue(entityId, out var modelRegistration))
        {
            return new ResultProblem("Entity with entity id '{0}' is not registered", entityId);
        }

        var instanceId = NextInstanceId();
        var instance = new ModelInstance(instanceId, entityId, worldMatrix);
        _instances.Add(instanceId, instance);

        modelRegistration.InstanceCount++;

        return instanceId;
    }
    
    public Result DeregisterInstance(RenderingInstanceId<T> instanceId, bool removeEntityIfLastInstance = false)
    {
        if (!_instances.TryGetValue(instanceId, out var modelInstance))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        if (!_modelRegistrations.TryGetValue(modelInstance.EntityId, out var modelRegistration))
        {
            _instances.Remove(instanceId);
            return new ResultProblem("Entity with entity id '{0}' is not registered", modelInstance.EntityId);
        }

        _instances.Remove(instanceId);
        modelRegistration.InstanceCount--;

        if (!removeEntityIfLastInstance || modelRegistration.InstanceCount > 0)
        {
            return Result.Success();
        }

        if (Deregister(modelInstance.EntityId).TryPickProblems(out var problems))
        {
            return problems.Prepend(new ResultProblem("Failed to deregister entity after removing last instance")
            {
                Severity = ProblemSeverities.Critical
            });
        }

        return Result.Success();
    }

    public Result SetInstanceWorld(RenderingInstanceId<T> instanceId, Matrix4X4<float> worldMatrix)
    {
        if (!_instances.ContainsKey(instanceId))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        _instances[instanceId] = _instances[instanceId] with { Transform = worldMatrix };

         return Result.Success();
    }

    public Result<Matrix4X4<float>> GetInstanceWorld(RenderingInstanceId<T> instanceId)
    {
        if (!_instances.TryGetValue(instanceId, out var modelInstance))
        {
            return new ResultProblem("Entity instance with '{0}' is not registered", instanceId);
        }

        return modelInstance.Transform;
    }

    public Result Render(RenderingParameters parameters)
    {
        RenderingEntityId<T>? currentEntityId = null;
        TRegistration? currentModelRegistration = default(TRegistration);

        foreach (var instance in _instances.Values)
        {
            ResultProblemCollection? problems;

            if (currentModelRegistration == null || currentEntityId != instance.EntityId)
            {
                if (!_modelRegistrations.TryGetValue(instance.EntityId, out currentModelRegistration))
                {
                    return new ResultProblem("Entity with entity id '{0}' is not registered", instance.EntityId);
                }

                if (Load(currentModelRegistration, parameters).TryPickProblems(out problems))
                {
                    return problems.Prepend("Failed to load entity");
                }

                currentEntityId = instance.EntityId;
            }

            var transform = instance.Transform;

            if (Render(currentModelRegistration, transform, parameters).TryPickProblems(out problems))
            {
                return problems.Prepend("Failed to render entity");
            }
        }

        return Result.Success();
    }
}