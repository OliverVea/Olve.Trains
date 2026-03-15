using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;

namespace Olve.Engine3D.Systems;

public sealed class ImmediateEventSceneService<T>(
    Event<T> @event,
    Func<T, Result> handler,
    Func<IEnumerable<T>>? prefill = null,
    ILogger? logger = null,
    int priority = 0) : ISceneService
{
    public int Priority => priority;

    public Result Load()
    {
        if (prefill is { } prefillFunc)
        {
            foreach (var item in prefillFunc())
            {
                if (handler(item).TryPickProblems(out var problems))
                {
                    if (logger is not null)
                    {
                        logger.Log(problems.Prepend("Got problems while handling prefill event of type '{0}'", typeof(T).Name));
                    }

                    return problems;
                }
            }
        }

        @event.Subscribe(OnEvent);
        return Result.Success();
    }

    public Result Unload()
    {
        @event.Unsubscribe(OnEvent);
        return Result.Success();
    }

    public Result Update() => Result.Success();

    private void OnEvent(T item)
    {
        if (handler(item).TryPickProblems(out var problems) && logger is not null)
        {
            logger.Log(problems.Prepend("Got problems while handling immediate event of type '{0}'", typeof(T).Name));
        }
    }
}