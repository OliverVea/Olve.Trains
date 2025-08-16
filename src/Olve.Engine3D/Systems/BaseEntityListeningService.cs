using System.Collections.Concurrent;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Engine3D.Systems;

public abstract class BaseEntityListeningService<TEntity>(ILoggingManager loggingManager, BaseEntityService<TEntity> entityService) : SceneService(loggingManager) where TEntity : IHasId<Id<TEntity>>
{
    private readonly ConcurrentQueue<Id<TEntity>> _addQueue = new();
    private readonly ConcurrentQueue<Id<TEntity>> _deleteQueue = new();

    protected abstract (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions();
    protected virtual Result OnAdded(Id<TEntity> entityId) => Result.Success();
    protected virtual Result OnRemoved(Id<TEntity> entityId) => Result.Success();

    private bool _addSubscribed, _deleteSubscribed;
    
    protected override Result OnLoad()
    {
        (_addSubscribed, _deleteSubscribed) = GetSubscriptions();

        if (_addSubscribed)
        {
            entityService.OnAdded += _addQueue.Enqueue;
        }

        if (_deleteSubscribed)
        {
            entityService.OnRemoved += _deleteQueue.Enqueue;
        }
        
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        if (_addSubscribed)
        {
            entityService.OnAdded -= _addQueue.Enqueue;
        }

        if (_deleteSubscribed)
        {
            entityService.OnRemoved -= _deleteQueue.Enqueue;
        }
        
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        if (_addSubscribed && !_addQueue.IsEmpty)
        {
            while (_addQueue.TryDequeue(out var entityId))
            {
                if (OnAdded(entityId).TryPickProblems(out var problems))
                {
                    return problems;
                }
            }
        }

        if (_deleteSubscribed && !_deleteQueue.IsEmpty)
        {
            while (_deleteQueue.TryDequeue(out var entityId))
            {
                if (OnRemoved(entityId).TryPickProblems(out var problems))
                {
                    return problems;
                }
            }
        }
        
        return Result.Success();
    }
}