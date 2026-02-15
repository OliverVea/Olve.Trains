using Olve.Engine3D.Scenes;

namespace Olve.Engine3D.Systems;

public sealed class EventSceneService<T>(EventQueue<T> queue, bool propagateFailedUpdate = false, int priority = 0) : ISceneService
{
    public int Priority => priority;
    public Result Load()
    {
        queue.Init();
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
