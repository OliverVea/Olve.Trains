using Olve.Engine3D.Graphics;

namespace Olve.Engine3D.Rendering;

public abstract class RenderingEntityManagerBase<TId, TEntity, TRegistration>
    where TId : notnull
{
    private readonly ThreadSafeUintGenerator _uintGenerator = new();

    protected readonly Dictionary<TId, TRegistration> ModelRegistrations = [];

    protected abstract TId CreateId(uint id, TRegistration registration);
    protected abstract Result<TRegistration> RegisterInOpenGL(TEntity entity);
    protected abstract Result DeregisterFromOpenGL(TRegistration registration);

    public Result<TId> Register(TEntity entity)
    {
        if (RegisterInOpenGL(entity).TryPickProblems(out var problems, out var registration))
        {
            return problems.Prepend(new ResultProblem ("Failed to register entity in OpenGL")
            {
                Tags = [ ProblemTags.OpenGL ]
            });
        }

        var id = _uintGenerator.Next();
        var entityId = CreateId(id, registration);

        ModelRegistrations.Add(entityId, registration);

        return entityId;
    }

    public Result Unregister(TId entityId)
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
}