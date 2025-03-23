using Olve.Engine3D.Graphics;

namespace Olve.Engine3D.Rendering;

public abstract class RenderingManagerBase<T, TRegistration> : RenderingInstanceManagerBase<T, TRegistration>, IRenderingManager where TRegistration : IHasInstanceCount
{
    protected abstract Result Load(TRegistration registration, RenderingParameters parameters);
    protected abstract Result Render(TRegistration registration, Matrix4X4<float> world, RenderingParameters parameters);

    public Result Render(RenderingParameters parameters)
    {
        RenderingEntityId<T>? currentEntityId = null;
        TRegistration? currentModelRegistration = default(TRegistration);

        foreach (var instance in Instances.Values)
        {
            ResultProblemCollection? problems;

            if (currentModelRegistration == null || currentEntityId != instance.EntityId)
            {
                if (!ModelRegistrations.TryGetValue(instance.EntityId, out currentModelRegistration))
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