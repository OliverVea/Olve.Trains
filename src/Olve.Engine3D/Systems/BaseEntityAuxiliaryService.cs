using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityAuxiliaryService<TEntityService, TEntity>(
    ILoggingManager loggingManager,
    TEntityService entityService) : SceneService(loggingManager)
    where TEntity : IHasId<Id<TEntity>>
    where TEntityService : BaseEntityService<TEntity>
{
    protected override Result OnLoad()
    {
        entityService.OnAdded += OnAdded;
        entityService.OnRemoved += OnRemoved;
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        entityService.OnAdded -= OnAdded;
        entityService.OnRemoved -= OnRemoved;
        return Result.Success();
    }

    protected virtual void OnAdded(Id<TEntity> id)
    {
    }

    protected virtual void OnRemoved(Id<TEntity> id)
    {
    }
}
