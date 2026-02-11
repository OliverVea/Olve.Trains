using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Utilities;

namespace Olve.Engine3D.Systems;

public sealed class EventQueue<T>
{
    private readonly Event<T> _event;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly ILogger? _logger;
    private Func<T, Result>? _handler;

    public EventQueue(Event<T> e, ILogger? logger = null)
    {
        _event = e;
        _logger = logger;
    }

    public EventQueue(Event<T> e, Func<T, Result> handler, ILogger? logger = null)
    {
        _event = e;
        _handler = handler;
        _logger = logger;
    }

    public EventQueue<T> SetHandler(Func<T, Result>? handler)
    {
        _handler = handler;
        return this;
    }

    public EventQueue<T> Init()
    {
        _event.Subscribe(_queue.Enqueue);
        return this;
    }

    public EventQueue<T> Cleanup()
    {
        _event.Unsubscribe(_queue.Enqueue);
        return this;
    }

    public Result Update()
    {
        if (_queue.IsEmpty || _handler is not { } handler)
        {
            return Result.Success();
        }

        while (_queue.TryDequeue(out var item))
        {
            if (handler.Invoke(item).TryPickProblems(out var problems))
            {
                if (_logger is not null)
                {
                    _logger.Log(problems.Prepend("Got problems while handling events of type '{0}'", typeof(T).Name));
                }

                return problems;
            }
        }

        return Result.Success();
    }
}
