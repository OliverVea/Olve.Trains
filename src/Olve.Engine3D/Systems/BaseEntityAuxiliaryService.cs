using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityAuxiliaryService<TEntity>(
    ILoggingManager loggingManager,
    IEntityService<TEntity> entityService) : SceneService(loggingManager)
    where TEntity : IHasId<Id<TEntity>>
{
    protected override Result OnLoad()
    {
        entityService.OnAdded.Subscribe(OnAdded);
        entityService.OnRemoved.Subscribe(OnRemoved);
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        entityService.OnAdded.Unsubscribe(OnAdded);
        entityService.OnRemoved.Unsubscribe(OnRemoved);
        return Result.Success();
    }

    protected virtual void OnAdded(Id<TEntity> id)
    {
    }

    protected virtual void OnRemoved(Id<TEntity> id)
    {
    }
}
