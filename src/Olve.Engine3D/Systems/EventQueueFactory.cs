using Microsoft.Extensions.Logging;

namespace Olve.Engine3D.Systems;

public class EventQueueFactory(ILoggerFactory loggerFactory)
{
    public EventQueue<T> Create<T>(Event<T> e)
        => new(e, loggerFactory.CreateLogger<EventQueue<T>>());

    public EventQueue<T> Create<T>(Event<T> e, Func<T, Result> handler)
        => new(e, handler, loggerFactory.CreateLogger<EventQueue<T>>());
}
