using System.Collections.Concurrent;

namespace Olve.Engine3D.Systems;

public sealed class EventQueue<T>(Event<T> e)
{
    private readonly ConcurrentQueue<T> _queue = new();

    private Func<T, Result>? _handlers;
    
    public EventQueue<T> Init()
    {
        e.Subscribe(_queue.Enqueue);
        return this;
    }

    public EventQueue<T> Cleanup()
    {
        e.Unsubscribe(_queue.Enqueue);
        return this;
    }

    public EventQueue<T> SetHandler(Func<T, Result>? handler)
    {
        _handlers = handler;
        return this;
    }

    public Result Update()
    {
        if (_queue.IsEmpty || _handlers is not {} handlers)
        {
            return Result.Success();
        }
        
        while (_queue.TryDequeue(out var entityId))
        {
            if (handlers.Invoke(entityId).TryPickProblems(out var problems))
            {
                return problems;
            }
        }
        
        return Result.Success();
    }
}