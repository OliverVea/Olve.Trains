using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityListeningService<TEntity>(ILoggingManager loggingManager, IEntityService<TEntity> junctionService) : SceneService(loggingManager) where TEntity : IHasId<Id<TEntity>>
{
    private readonly EventQueue<Id<TEntity>> _addedEventQueue = new(junctionService.OnAdded);
    private readonly EventQueue<Id<TEntity>> _removedEventQueue = new(junctionService.OnRemoved);

    protected abstract (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions();
    protected virtual Result OnAdded(Id<TEntity> entityId) => Result.Success();
    protected virtual Result OnRemoved(Id<TEntity> entityId) => Result.Success();

    private bool _addSubscribed, _deleteSubscribed;
    
    protected override Result OnLoad()
    {
        (_addSubscribed, _deleteSubscribed) = GetSubscriptions();
        
        if (_addSubscribed)
        {
            _addedEventQueue.SetHandler(OnAdded).Init();
        }

        if (_deleteSubscribed)
        {
            _removedEventQueue.SetHandler(OnRemoved).Init();
        }
        
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        if (_addSubscribed)
        {
            _addedEventQueue.Cleanup();
        }

        if (_deleteSubscribed)
        {
            _removedEventQueue.Cleanup();
        }
        
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        if (_addSubscribed)
        {
            if (_addedEventQueue.Update().TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        if (_deleteSubscribed)
        {
            if (_removedEventQueue.Update().TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        return Result.Success();
    }
}