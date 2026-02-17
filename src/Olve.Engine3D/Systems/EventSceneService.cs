using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.Systems;

public sealed class EventSceneService<T>(EventQueue<T> queue, Func<IEnumerable<T>>? prefill = null, bool propagateFailedUpdate = false, int priority = 0) : ISceneService
{
    public int Priority => priority;
    public Result Load()
    {
        queue.Init(prefill?.Invoke());
        return Result.Success();
    }

    public Result Unload()
    {
        queue.Cleanup();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        var result = queue.Update();
        return propagateFailedUpdate ? result : Result.Success();
    }
}
