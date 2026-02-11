using Olve.Engine3D.Scenes;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityAuxiliaryService<TEntity>(
    IEntityService<TEntity> entityService) : ISceneService
    where TEntity : IHasId<Id<TEntity>>
{
    public Result Load()
    {
        entityService.OnAdded.Subscribe(OnAdded);
        entityService.OnRemoved.Subscribe(OnRemoved);
        return Result.Success();
    }

    public Result Unload()
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
