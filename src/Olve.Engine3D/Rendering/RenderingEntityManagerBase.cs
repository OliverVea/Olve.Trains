using Olve.Engine3D.Graphics;

namespace Olve.Engine3D.Rendering;

public abstract class RenderingEntityManagerBase<T, TRegistration> : IRenderingEntityManager<T>
    where TRegistration : IHasInstanceCount
{
    private readonly ThreadSafeIdGenerator _entityIdGenerator = new();

    protected readonly SortedList<RenderingEntityId<T>, TRegistration> ModelRegistrations = new();

    private RenderingEntityId<T> NextEntityId() => new(_entityIdGenerator.Next());

    protected abstract Result<TRegistration> RegisterInOpenGL(T entity);
    protected abstract Result DeregisterFromOpenGL(TRegistration registration);

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

        ModelRegistrations.Add(entityId, registration);

        return entityId;
    }

    public Result Unregister(RenderingEntityId<T> entityId)
    {
        if (!ModelRegistrations.TryGetValue(entityId, out var modelRegistration))
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

        if (DeregisterFromOpenGL(modelRegistration).TryPickProblems(out var problems))
        {
            return problems.Prepend(new ResultProblem("Could not unregister entity in OpenGL")
            {
                Severity = ProblemSeverities.Critical,
                Tags = [ ProblemTags.OpenGL ]
            });
        }

        ModelRegistrations.Remove(entityId);
        return Result.Success();
    }
}