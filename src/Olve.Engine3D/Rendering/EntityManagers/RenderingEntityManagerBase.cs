using System.Diagnostics.CodeAnalysis;

namespace Olve.Engine3D.Rendering.EntityManagers;

public abstract class RenderingEntityManagerBase<TEntity, TRegistration>
{
    protected readonly Dictionary<RenderingId<TEntity>, TRegistration> ModelRegistrations = [];

    protected abstract Result<TRegistration> RegisterInOpenGL(TEntity entity);
    protected abstract Result UpdateInOpenGL(TRegistration registration, TEntity entity);
    protected abstract Result DeregisterFromOpenGL(TRegistration registration);

    public Result<RenderingId<TEntity>> Register(TEntity entity)
    {
        if (RegisterInOpenGL(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend(new ResultProblem ("Failed to register entity in OpenGL")
            {
                Tags = [ ProblemTags.OpenGL ]
            });
        }

        var entityId = RenderingId<TEntity>.New();

        ModelRegistrations.Add(entityId, registration);

        return entityId;
    }

    public Result Update(RenderingId<TEntity> entityId, TEntity entity)
    {
        if (!ModelRegistrations.TryGetValue(entityId, out var registration))
        {
            return new ResultProblem("Entity with id '{0}' is not registered", entityId);
        }

        if (UpdateInOpenGL(registration, entity).TryPickProblems(out var problems))
        {
            return problems.Prepend(new ResultProblem("Failed to update entity in OpenGL")
            {
                Tags = [ ProblemTags.OpenGL ]
            });
        }

        return Result.Success();
    }

    public Result Unregister(RenderingId<TEntity> entityId)
    {
        if (!ModelRegistrations.TryGetValue(entityId, out var modelRegistration))
        {
            return new ResultProblem("Entity with id '{0}' is not registered", entityId);
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

    public bool TryGetRegistration(RenderingId<TEntity> entityId, [MaybeNullWhen(false)] out TRegistration registration)
    {
        return ModelRegistrations.TryGetValue(entityId, out registration);
    }
    
    public Result<TRegistration> GetRegistration(RenderingId<TEntity> entityId)
    {
        if (!ModelRegistrations.TryGetValue(entityId, out var registration))
        {
            return new ResultProblem("Entity with id '{0}' is not registered", entityId);
        }

        return registration;
    }
}